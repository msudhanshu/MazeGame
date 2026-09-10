using System;
using System.Collections.Generic;
using Game.Core.Domain;
using Game.Core.Rules;
using NUnit.Framework;
using Nixin.Grid.Core;

namespace Game.Core.Tests
{
    [TestFixture]
    public class GridWalkRunTests
    {
        static readonly GridSize Size = new GridSize(5, 5);

        // A four-step path with two turns, short enough to reason about by hand.
        static GridPath TestPath()
        {
            var cells = new List<GridCoord>
            {
                new GridCoord(0, 0),
                new GridCoord(1, 0),
                new GridCoord(1, 1),
                new GridCoord(2, 1),
                new GridCoord(2, 2)
            };
            return new GridPath(Size, cells);
        }

        static GridWalkRun NewRun(int lives = 3, int runs = 5) =>
            new GridWalkRun(TestPath(), new GameConfig(livesPerRun: lives, runsPerSession: runs));

        static GridCoord WrongOption(GridWalkRun run)
        {
            foreach (var option in run.Options())
            {
                if (option != run.Path.Cells[run.Step + 1])
                    return option;
            }

            throw new InvalidOperationException("Every option was correct.");
        }

        static GridCoord CorrectOption(GridWalkRun run) => run.Path.Cells[run.Step + 1];

        [Test]
        public void StartsAtTheBeginningWithFullHealth()
        {
            var run = NewRun();

            Assert.That(run.Step, Is.EqualTo(0));
            Assert.That(run.CurrentCell, Is.EqualTo(new GridCoord(0, 0)));
            Assert.That(run.LivesLeft, Is.EqualTo(3));
            Assert.That(run.RunNumber, Is.EqualTo(1));
            Assert.That(run.TotalSteps, Is.EqualTo(4));
            Assert.That(run.WalkedCells, Is.EqualTo(new[] { new GridCoord(0, 0) }));
        }

        [Test]
        public void CorrectTileAdvancesWithoutCost()
        {
            var run = NewRun();

            var outcome = run.Choose(CorrectOption(run));

            Assert.That(outcome, Is.EqualTo(WalkOutcome.Advanced));
            Assert.That(run.Step, Is.EqualTo(1));
            Assert.That(run.LivesLeft, Is.EqualTo(3));
            Assert.That(run.LastRevealed, Is.Null);
            Assert.That(run.WalkedCells, Is.EqualTo(new[] { new GridCoord(0, 0), new GridCoord(1, 0) }));
        }

        [Test]
        public void WrongTileCostsALifeRevealsTheAnswerAndMovesOn()
        {
            var run = NewRun();
            var correct = CorrectOption(run);

            var outcome = run.Choose(WrongOption(run));

            Assert.That(outcome, Is.EqualTo(WalkOutcome.WrongRevealed));
            Assert.That(run.LivesLeft, Is.EqualTo(2));
            Assert.That(run.MistakesMade, Is.EqualTo(1));
            Assert.That(run.LastRevealed, Is.EqualTo(correct));
            Assert.That(run.RevealedCells, Is.EqualTo(new[] { correct }));
            Assert.That(run.Step, Is.EqualTo(1), "the walker jumps onto the revealed tile");
            Assert.That(run.CurrentCell, Is.EqualTo(correct));
        }

        [Test]
        public void LastLifeStillRevealsTheCorrectTileBeforeTheNextWalk()
        {
            var run = NewRun(lives: 3);
            var route = run.Path.Cells;

            run.Choose(WrongOption(run));
            run.Choose(WrongOption(run));
            var correct = CorrectOption(run);
            var outcome = run.Choose(WrongOption(run));

            Assert.That(outcome, Is.EqualTo(WalkOutcome.RunFailed));
            Assert.That(run.IsAwaitingNextWalk, Is.True);
            Assert.That(run.LastRevealed, Is.EqualTo(correct));
            Assert.That(run.RevealedCells, Contains.Item(correct));
            Assert.That(run.CurrentCell, Is.EqualTo(correct));
            Assert.That(run.RunNumber, Is.EqualTo(1), "the next walk has not started yet");
            Assert.That(run.Path.Cells, Is.EqualTo(route));
            Assert.That(run.Options(), Is.Empty);
        }

        [Test]
        public void BeginNextWalkRestartsFromTheStartOnTheSamePath()
        {
            var run = NewRun(lives: 1);
            var route = run.Path.Cells;

            run.Choose(WrongOption(run));
            run.BeginNextWalk();

            Assert.That(run.IsAwaitingNextWalk, Is.False);
            Assert.That(run.RunNumber, Is.EqualTo(2));
            Assert.That(run.LivesLeft, Is.EqualTo(1));
            Assert.That(run.Step, Is.EqualTo(0));
            Assert.That(run.RevealedCells, Is.Empty);
            Assert.That(run.LastRevealed, Is.Null);
            Assert.That(run.WalkedCells, Is.EqualTo(new[] { new GridCoord(0, 0) }));
            Assert.That(run.Path.Cells, Is.EqualTo(route));
        }

        [Test]
        public void ProgressFromEarlierWalksIsRemembered()
        {
            var run = NewRun(lives: 1);

            run.Choose(CorrectOption(run));
            run.Choose(CorrectOption(run));
            Assert.That(run.FurthestStep, Is.EqualTo(2));

            run.Choose(WrongOption(run));

            Assert.That(run.IsAwaitingNextWalk, Is.True);
            Assert.That(run.FurthestStep, Is.EqualTo(3), "the revealed step still counts as ground covered");

            run.BeginNextWalk();

            Assert.That(run.RunNumber, Is.EqualTo(2));
            Assert.That(run.Step, Is.EqualTo(0));
            Assert.That(run.FurthestStep, Is.EqualTo(3));
        }

        [Test]
        public void SpendingEveryRunEndsTheSession()
        {
            var run = NewRun(lives: 1, runs: 3);

            Assert.That(run.Choose(WrongOption(run)), Is.EqualTo(WalkOutcome.RunFailed));
            run.BeginNextWalk();
            Assert.That(run.Choose(WrongOption(run)), Is.EqualTo(WalkOutcome.RunFailed));
            run.BeginNextWalk();
            Assert.That(run.Choose(WrongOption(run)), Is.EqualTo(WalkOutcome.SessionOver));

            Assert.That(run.IsSessionOver, Is.True);
            Assert.That(run.IsOver, Is.True);
            Assert.That(run.IsLevelCompleted, Is.False);
            Assert.That(run.Options(), Is.Empty);
        }

        [Test]
        public void ReachingTheGoalCompletesTheLevel()
        {
            var run = NewRun();

            Assert.That(run.Choose(CorrectOption(run)), Is.EqualTo(WalkOutcome.Advanced));
            Assert.That(run.Choose(CorrectOption(run)), Is.EqualTo(WalkOutcome.Advanced));
            Assert.That(run.Choose(CorrectOption(run)), Is.EqualTo(WalkOutcome.Advanced));
            Assert.That(run.Choose(CorrectOption(run)), Is.EqualTo(WalkOutcome.LevelCompleted));

            Assert.That(run.IsLevelCompleted, Is.True);
            Assert.That(run.CurrentCell, Is.EqualTo(run.Path.Goal));
            Assert.That(run.IsSessionOver, Is.False);
        }

        [Test]
        public void RevealingTheFinalTileStillCompletesTheLevel()
        {
            var run = NewRun(lives: 1, runs: 5);

            run.Choose(CorrectOption(run));
            run.Choose(CorrectOption(run));
            run.Choose(CorrectOption(run));

            var outcome = run.Choose(WrongOption(run));

            Assert.That(outcome, Is.EqualTo(WalkOutcome.LevelCompleted));
            Assert.That(run.IsLevelCompleted, Is.True);
            Assert.That(run.IsSessionOver, Is.False);
        }

        [Test]
        public void OptionsAreAdjacentTilesOnly()
        {
            var run = NewRun();

            foreach (var option in run.Options())
            {
                Assert.That(option.IsOrthogonalNeighbourOf(run.CurrentCell), Is.True);
                Assert.That(run.IsOption(option), Is.True);
            }

            Assert.That(run.IsOption(new GridCoord(4, 4)), Is.False);
            Assert.Throws<ArgumentException>(() => run.Choose(new GridCoord(4, 4)));
        }

        [Test]
        public void PlayingOnAfterTheSessionEndsIsRejected()
        {
            var finished = NewRun();
            while (!finished.IsLevelCompleted)
                finished.Choose(CorrectOption(finished));
            Assert.Throws<InvalidOperationException>(() => finished.Choose(new GridCoord(1, 2)));

            var exhausted = NewRun(lives: 1, runs: 1);
            var correct = CorrectOption(exhausted);
            exhausted.Choose(WrongOption(exhausted));
            Assert.That(exhausted.LastRevealed, Is.EqualTo(correct));
            Assert.Throws<InvalidOperationException>(() => exhausted.Choose(new GridCoord(1, 0)));
            Assert.Throws<InvalidOperationException>(() => exhausted.BeginNextWalk());
        }

        [Test]
        public void CannotChooseWhileAwaitingTheNextWalk()
        {
            var run = NewRun(lives: 1, runs: 3);

            run.Choose(WrongOption(run));

            Assert.Throws<InvalidOperationException>(() => run.Choose(new GridCoord(1, 0)));

            run.BeginNextWalk();
            Assert.That(run.RunNumber, Is.EqualTo(2));
        }

        [Test]
        public void LighthousesStayLitAfterAFailedWalk()
        {
            var lighthouse = new GridCoord(1, 0);
            var run = new GridWalkRun(
                TestPath(),
                new GameConfig(livesPerRun: 1, runsPerSession: 3),
                new PathAids(new[] { lighthouse }, Array.Empty<PathPickup>()));

            Assert.That(run.IsLighthouse(lighthouse), Is.True);
            Assert.That(run.LivesLeft, Is.EqualTo(1));

            run.Choose(WrongOption(run));
            run.BeginNextWalk();

            Assert.That(run.IsLighthouse(lighthouse), Is.True);
            Assert.That(run.RevealedCells, Is.Empty);
            Assert.That(run.Step, Is.EqualTo(0));
        }

        [Test]
        public void GlimpseIsOneShotAndDoesNotSpendALife()
        {
            var pickup = new GridCoord(1, 0);
            var run = new GridWalkRun(
                TestPath(),
                new GameConfig(livesPerRun: 1, runsPerSession: 3),
                new PathAids(Array.Empty<GridCoord>(), new[] { new PathPickup(pickup, PathPickupKind.Glimpse) }));

            Assert.That(run.Choose(pickup), Is.EqualTo(WalkOutcome.Advanced));
            Assert.That(run.LivesLeft, Is.EqualTo(1));
            Assert.That(run.UnusedPickups, Is.Empty);

            var flashed = run.ConsumeGlimpse();
            Assert.That(flashed, Is.EqualTo(run.Path.Cells));
            Assert.That(run.ConsumeGlimpse(), Is.Null);

            Assert.That(run.Choose(WrongOption(run)), Is.EqualTo(WalkOutcome.RunFailed));
            run.BeginNextWalk();

            Assert.That(run.Choose(pickup), Is.EqualTo(WalkOutcome.Advanced));
            Assert.That(run.ConsumeGlimpse(), Is.Null);
        }

        [Test]
        public void BeaconAddsAPersistentWhiteAheadOfTheWalker()
        {
            var pickup = new GridCoord(1, 0);
            var run = new GridWalkRun(
                TestPath(),
                new GameConfig(livesPerRun: 1, runsPerSession: 3),
                new PathAids(Array.Empty<GridCoord>(), new[] { new PathPickup(pickup, PathPickupKind.Beacon) }));

            Assert.That(run.Choose(pickup), Is.EqualTo(WalkOutcome.Advanced));
            Assert.That(run.LivesLeft, Is.EqualTo(1));
            Assert.That(run.LighthouseCells, Is.EqualTo(new[] { new GridCoord(2, 1) }));

            run.Choose(WrongOption(run));
            run.BeginNextWalk();

            Assert.That(run.IsLighthouse(new GridCoord(2, 1)), Is.True);
            Assert.That(run.UnusedPickups, Is.Empty);
        }

        [Test]
        public void ScoutStepsUpgradeToMemoryOnceOnALaterWalk()
        {
            var run = NewRun(lives: 1, runs: 4);

            Assert.That(run.Choose(CorrectOption(run)), Is.EqualTo(WalkOutcome.Advanced));
            Assert.That(run.ScoutCells, Is.EqualTo(1));
            Assert.That(run.MemoryCells, Is.EqualTo(0));

            Assert.That(run.Choose(WrongOption(run)), Is.EqualTo(WalkOutcome.RunFailed));
            run.BeginNextWalk();

            Assert.That(run.Choose(CorrectOption(run)), Is.EqualTo(WalkOutcome.Advanced));
            Assert.That(run.ScoutCells, Is.EqualTo(0));
            Assert.That(run.MemoryCells, Is.EqualTo(1));

            Assert.That(run.Choose(WrongOption(run)), Is.EqualTo(WalkOutcome.RunFailed));
            run.BeginNextWalk();

            Assert.That(run.Choose(CorrectOption(run)), Is.EqualTo(WalkOutcome.Advanced));
            Assert.That(run.MemoryCells, Is.EqualTo(1), "a cell upgrades at most once");
            Assert.That(run.ScoutCells, Is.EqualTo(0));
        }

        [Test]
        public void ALighthousePaysMemoryOnTheFirstVoluntaryStep()
        {
            var lighthouse = new GridCoord(1, 0);
            var run = new GridWalkRun(
                TestPath(),
                new GameConfig(livesPerRun: 3, runsPerSession: 5),
                new PathAids(new[] { lighthouse }, Array.Empty<PathPickup>()));

            run.Choose(lighthouse);

            Assert.That(run.MemoryCells, Is.EqualTo(1));
            Assert.That(run.ScoutCells, Is.EqualTo(0));
        }

        [Test]
        public void AMistakeDoesNotPayStepPoints()
        {
            var run = NewRun();

            run.Choose(WrongOption(run));

            Assert.That(run.ScoutCells, Is.EqualTo(0));
            Assert.That(run.MemoryCells, Is.EqualTo(0));
            Assert.That(run.Score, Is.EqualTo(0));
        }

        [Test]
        public void CompletingOnWalkTwoBeatsGrindingEveryWalk()
        {
            var efficient = NewRun(lives: 1, runs: 5);
            efficient.Choose(WrongOption(efficient));
            efficient.BeginNextWalk();
            while (!efficient.IsLevelCompleted)
                efficient.Choose(CorrectOption(efficient));

            var grind = NewRun(lives: 1, runs: 5);
            for (var i = 0; i < 4; i++)
            {
                grind.Choose(WrongOption(grind));
                grind.BeginNextWalk();
            }

            while (!grind.IsLevelCompleted)
                grind.Choose(CorrectOption(grind));

            Assert.That(efficient.IsLevelCompleted, Is.True);
            Assert.That(grind.IsLevelCompleted, Is.True);
            Assert.That(efficient.Score, Is.GreaterThan(grind.Score));
        }

        [Test]
        public void AFailedSessionGradeStaysCapped()
        {
            var run = NewRun(lives: 1, runs: 1);

            run.Choose(WrongOption(run));

            Assert.That(run.IsSessionOver, Is.True);
            Assert.That(run.MemoryGrade, Is.LessThanOrEqualTo(GridScore.FailedGradeCap));
        }

        [Test]
        public void RejectsAMissingPath()
        {
            Assert.Throws<ArgumentNullException>(() => new GridWalkRun(null));
        }

        [Test]
        public void BlockedCellsAreNeverOptions()
        {
            var blocked = new List<GridCoord> { new GridCoord(2, 0) };
            var aids = new PathAids(Array.Empty<GridCoord>(), Array.Empty<PathPickup>(), blocked);
            var run = new GridWalkRun(TestPath(), new GameConfig(), aids);

            Assert.That(run.BlockedCells, Is.EqualTo(blocked));
            Assert.That(run.IsBlocked(new GridCoord(2, 0)), Is.True);
            Assert.That(run.Options(), Does.Not.Contain(new GridCoord(2, 0)));
            Assert.Throws<ArgumentException>(() => run.Choose(new GridCoord(2, 0)));
        }
    }
}
