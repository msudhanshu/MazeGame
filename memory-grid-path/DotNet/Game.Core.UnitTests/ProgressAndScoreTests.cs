using System;
using System.Collections.Generic;
using Game.Core.Domain;
using Game.Core.Rules;
using Game.Core.State;
using NUnit.Framework;
using Nixin.Grid.Core;

namespace Game.Core.Tests
{
    [TestFixture]
    public class GridScoreTests
    {
        static readonly GameConfig Config = GameConfig.Default;

        static SessionScoreInput Input(
            int scout = 0,
            int memory = 0,
            int mistakes = 0,
            int runsUsed = 1,
            int totalSteps = 8,
            int furthest = 8,
            bool completed = false) =>
            new SessionScoreInput(scout, memory, mistakes, runsUsed, totalSteps, furthest, completed);

        [Test]
        public void MemoryStepsPayMoreThanScoutSteps()
        {
            var scout = GridScore.Calculate(Config, Input(scout: 4));
            var memory = GridScore.Calculate(Config, Input(memory: 4));

            Assert.That(memory, Is.GreaterThan(scout));
            Assert.That(scout, Is.EqualTo(4 * Config.ScoutPoints));
            Assert.That(memory, Is.EqualTo(4 * Config.MemoryPoints));
        }

        [Test]
        public void MistakesCostPoints()
        {
            var clean = GridScore.Calculate(Config, Input(memory: 6, mistakes: 0));
            var messy = GridScore.Calculate(Config, Input(memory: 6, mistakes: 4));

            Assert.That(messy, Is.LessThan(clean));
            Assert.That(clean - messy, Is.EqualTo(4 * Config.MistakePenalty));
        }

        [Test]
        public void FinishingEarlyPaysABonusPerUnusedWalk()
        {
            var lastRun = GridScore.Calculate(Config, Input(memory: 8, runsUsed: 5, completed: true));
            var firstRun = GridScore.Calculate(Config, Input(memory: 8, runsUsed: 1, completed: true));

            Assert.That(lastRun, Is.EqualTo(8 * Config.MemoryPoints + Config.CompletionBonus));
            Assert.That(firstRun - lastRun, Is.EqualTo(4 * Config.UnusedRunBonus));
        }

        [Test]
        public void FailedSessionsDoNotGetCompletionBonuses()
        {
            var failed = GridScore.Calculate(Config, Input(scout: 8, runsUsed: 5, completed: false));
            var done = GridScore.Calculate(Config, Input(scout: 8, runsUsed: 5, completed: true));

            Assert.That(done - failed, Is.EqualTo(Config.CompletionBonus));
        }

        [Test]
        public void ScoreNeverGoesNegative()
        {
            var config = new GameConfig(scoutPoints: 10, memoryPoints: 10, mistakePenalty: 500);

            Assert.That(GridScore.Calculate(config, Input(scout: 1, mistakes: 9, runsUsed: 1, totalSteps: 1, furthest: 1)), Is.EqualTo(0));
        }

        [Test]
        public void FailedGradeIsCapped()
        {
            var grade = GridScore.MemoryGrade(Config, Input(scout: 8, mistakes: 0, furthest: 8, completed: false));

            Assert.That(grade, Is.LessThanOrEqualTo(GridScore.FailedGradeCap));
        }

        [Test]
        public void RejectsIncoherentInput()
        {
            Assert.Throws<ArgumentNullException>(() => GridScore.Calculate(null, Input()));
            Assert.Throws<ArgumentOutOfRangeException>(() => GridScore.Calculate(Config, Input(runsUsed: 0)));
            Assert.Throws<ArgumentOutOfRangeException>(() => GridScore.Calculate(Config, Input(totalSteps: 0)));
            Assert.Throws<ArgumentOutOfRangeException>(() => GridScore.Calculate(Config, Input(mistakes: -1)));
        }
    }

    [TestFixture]
    public class PlayerProgressTests
    {
        [Test]
        public void OnlyTheFirstLevelIsUnlockedToBeginWith()
        {
            var progress = new PlayerProgress();

            Assert.That(progress.HighestUnlockedLevel, Is.EqualTo(1));
            Assert.That(progress.IsUnlocked(1), Is.True);
            Assert.That(progress.IsUnlocked(2), Is.False);
            Assert.That(progress.BestScoreFor(1), Is.EqualTo(0));
        }

        [Test]
        public void CompletingALevelUnlocksTheNext()
        {
            var progress = new PlayerProgress();

            progress.RecordResult(levelNumber: 1, score: 900, completed: true, levelCount: 20);

            Assert.That(progress.HighestUnlockedLevel, Is.EqualTo(2));
            Assert.That(progress.IsUnlocked(2), Is.True);
            Assert.That(progress.BestScoreFor(1), Is.EqualTo(900));
        }

        [Test]
        public void RunningOutOfHealthDoesNotUnlockAnything()
        {
            var progress = new PlayerProgress();

            progress.RecordResult(levelNumber: 1, score: 400, completed: false, levelCount: 20);

            Assert.That(progress.HighestUnlockedLevel, Is.EqualTo(1));
            Assert.That(progress.BestScoreFor(1), Is.EqualTo(400));
        }

        [Test]
        public void ReplayingAnEarlierLevelDoesNotRollProgressBack()
        {
            var progress = new PlayerProgress(highestUnlockedLevel: 5);

            progress.RecordResult(levelNumber: 2, score: 1000, completed: true, levelCount: 20);

            Assert.That(progress.HighestUnlockedLevel, Is.EqualTo(5));
        }

        [Test]
        public void OnlyBetterScoresAreKept()
        {
            var progress = new PlayerProgress();

            progress.RecordResult(1, 900, true, 20);
            progress.RecordResult(1, 300, true, 20);

            Assert.That(progress.BestScoreFor(1), Is.EqualTo(900));
            Assert.That(progress.CareerScore, Is.EqualTo(1200));
        }

        [Test]
        public void TheLastLevelDoesNotUnlockAnythingBeyondTheCatalog()
        {
            var progress = new PlayerProgress(highestUnlockedLevel: 20);

            progress.RecordResult(20, 5000, true, 20);

            Assert.That(progress.HighestUnlockedLevel, Is.EqualTo(20));
        }

        [Test]
        public void SavedScoresSurviveARoundTrip()
        {
            var restored = new PlayerProgress(3, new Dictionary<int, int> { { 1, 900 }, { 2, 750 } });

            Assert.That(restored.HighestUnlockedLevel, Is.EqualTo(3));
            Assert.That(restored.BestScoreFor(2), Is.EqualTo(750));
            Assert.That(restored.BestScores.Count, Is.EqualTo(2));
        }

        [Test]
        public void ResetReturnsToLevelOneAndClearsScores()
        {
            var progress = new PlayerProgress(highestUnlockedLevel: 10, bestScores: new Dictionary<int, int> { { 1, 900 } });

            progress.Reset();

            Assert.That(progress.HighestUnlockedLevel, Is.EqualTo(1));
            Assert.That(progress.IsUnlocked(2), Is.False);
            Assert.That(progress.BestScoreFor(1), Is.EqualTo(0));
            Assert.That(progress.BestScores, Is.Empty);
            Assert.That(progress.CareerScore, Is.EqualTo(0));
            Assert.That(progress.SkipCharges, Is.EqualTo(0));
            Assert.That(progress.HighGradeStreak, Is.EqualTo(0));
        }

        [Test]
        public void RejectsIncoherentInput()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerProgress(0));

            var progress = new PlayerProgress();
            Assert.Throws<ArgumentOutOfRangeException>(() => progress.RecordResult(0, 10, false, 20));
            Assert.Throws<ArgumentOutOfRangeException>(() => progress.RecordResult(1, -1, false, 20));
            Assert.Throws<ArgumentOutOfRangeException>(() => progress.RecordResult(1, 10, false, 0));
        }

        [Test]
        public void OnboardingLevelsNeverGrantSkipCharges()
        {
            var progress = new PlayerProgress();
            var config = GameConfig.Default;

            for (var level = 1; level <= 8; level++)
                progress.RecordResult(level, 100, true, 50, grade: 100, config: config);

            Assert.That(progress.SkipCharges, Is.EqualTo(0));
        }

        [Test]
        public void ThreeHighGradesAfterOnboardingGrantASkipWindow()
        {
            var progress = new PlayerProgress(highestUnlockedLevel: 12);
            var config = GameConfig.Default;

            progress.RecordResult(9, 100, true, 50, 90, config);
            progress.RecordResult(10, 100, true, 50, 90, config);
            Assert.That(progress.SkipCharges, Is.EqualTo(0));
            Assert.That(progress.HighGradeStreak, Is.EqualTo(2));

            progress.RecordResult(11, 100, true, 50, 90, config);

            Assert.That(progress.SkipCharges, Is.EqualTo(config.SkipWindow));
            Assert.That(progress.HighGradeStreak, Is.EqualTo(0));
            Assert.That(progress.ConsumeSkipGrantNotice(), Is.True);
            Assert.That(progress.ConsumeSkipGrantNotice(), Is.False);
        }

        [Test]
        public void SkippingAddsAModestRewardAndUnlocksWithoutExtendingTheStreak()
        {
            var progress = new PlayerProgress(highestUnlockedLevel: 12, skipCharges: 3);
            var config = GameConfig.Default;

            progress.SkipLevel(12, config.SkipRewardPoints, 50);

            Assert.That(progress.SkipCharges, Is.EqualTo(2));
            Assert.That(progress.CareerScore, Is.EqualTo(config.SkipRewardPoints));
            Assert.That(progress.HighestUnlockedLevel, Is.EqualTo(13));
            Assert.That(progress.HighGradeStreak, Is.EqualTo(0));

            progress.RecordResult(13, 800, true, 50, 95, config);
            Assert.That(progress.SkipCharges, Is.EqualTo(2), "holding charges blocks a new grant");
        }

        [Test]
        public void APoorFinishBreaksTheHighGradeStreak()
        {
            var progress = new PlayerProgress(highestUnlockedLevel: 12);
            var config = GameConfig.Default;

            progress.RecordResult(9, 100, true, 50, 90, config);
            progress.RecordResult(10, 40, false, 50, 20, config);

            Assert.That(progress.HighGradeStreak, Is.EqualTo(0));
            Assert.That(progress.SkipCharges, Is.EqualTo(0));
        }
    }

    [TestFixture]
    public class ProgressSaveFormatTests
    {
        [Test]
        public void RoundTripsCareerAndSkipState()
        {
            var original = new PlayerProgress(
                highestUnlockedLevel: 9,
                bestScores: new Dictionary<int, int> { { 1, 400 }, { 8, 900 } },
                careerScore: 1300,
                skipCharges: 2,
                highGradeStreak: 1);

            var restored = ProgressSaveFormat.Parse(ProgressSaveFormat.Serialize(original));

            Assert.That(restored.HighestUnlockedLevel, Is.EqualTo(9));
            Assert.That(restored.CareerScore, Is.EqualTo(1300));
            Assert.That(restored.SkipCharges, Is.EqualTo(2));
            Assert.That(restored.HighGradeStreak, Is.EqualTo(1));
            Assert.That(restored.BestScoreFor(8), Is.EqualTo(900));
        }

        [Test]
        public void StillLoadsTheLegacySaveBlob()
        {
            var restored = ProgressSaveFormat.Parse("4|1:900,2:750");

            Assert.That(restored.HighestUnlockedLevel, Is.EqualTo(4));
            Assert.That(restored.BestScoreFor(2), Is.EqualTo(750));
            Assert.That(restored.CareerScore, Is.EqualTo(0));
            Assert.That(restored.SkipCharges, Is.EqualTo(0));
        }
    }

    [TestFixture]
    public class GridPathGameTests
    {
        static GridCoord Correct(GridPathGame game) => game.Run.Path.Cells[game.Run.Step + 1];

        static GridCoord Wrong(GridPathGame game)
        {
            foreach (var option in game.Run.Options())
            {
                if (option != Correct(game))
                    return option;
            }

            throw new InvalidOperationException("Every option was correct.");
        }

        [Test]
        public void StartingALevelBuildsARunOnThatLevelsBoard()
        {
            var game = new GridPathGame();

            game.StartLevel(1, seed: 5);

            Assert.That(game.CurrentLevel.Number, Is.EqualTo(1));
            Assert.That(game.Run.Path.Size, Is.EqualTo(game.CurrentLevel.Size));
            Assert.That(game.IsPlaying, Is.True);
            Assert.That(game.NextLevelNumber, Is.EqualTo(1));
        }

        [Test]
        public void LockedLevelsCannotBeStarted()
        {
            var game = new GridPathGame();

            Assert.Throws<InvalidOperationException>(() => game.StartLevel(2, seed: 1));
        }

        [Test]
        public void FinishingALevelRecordsTheScoreAndOffersTheNextLevel()
        {
            var game = new GridPathGame();
            game.StartLevel(1, seed: 12);

            WalkOutcome outcome;
            do
            {
                outcome = game.Choose(Correct(game));
            }
            while (outcome == WalkOutcome.Advanced);

            Assert.That(outcome, Is.EqualTo(WalkOutcome.LevelCompleted));
            Assert.That(game.Progress.HighestUnlockedLevel, Is.EqualTo(2));
            Assert.That(game.Progress.BestScoreFor(1), Is.GreaterThan(0));
            Assert.That(game.Progress.CareerScore, Is.GreaterThan(0));
            Assert.That(game.NextLevelNumber, Is.EqualTo(2));
            Assert.That(game.IsPlaying, Is.False);
        }

        [Test]
        public void StartingALevelUsesThatLevelsWalkBudget()
        {
            var game = new GridPathGame();

            game.StartLevel(1, seed: 5);

            Assert.That(game.Run.LivesLeft, Is.EqualTo(2));
            Assert.That(game.Run.Config.RunsPerSession, Is.EqualTo(2));
            Assert.That(game.Run.LighthouseCells, Is.Empty);
        }

        [Test]
        public void RunningOutOfHealthRecordsTheScoreButKeepsTheLevelLocked()
        {
            var size = new GridSize(4, 4);
            var level = new LevelDefinition(
                1,
                size,
                new GridCoord(0, 0),
                new GridCoord(3, 3),
                new PathShapeSpec(7, 11, 1, 4),
                "test",
                livesPerRun: 1,
                runsPerSession: 2);
            var game = new GridPathGame(catalog: new LevelCatalog(new List<LevelDefinition> { level }));
            game.StartLevel(1, seed: 3);

            WalkOutcome outcome;
            do
            {
                outcome = game.Choose(Wrong(game));
                if (outcome == WalkOutcome.RunFailed)
                    game.BeginNextWalk();
            }
            while (outcome == WalkOutcome.RunFailed || outcome == WalkOutcome.WrongRevealed);

            Assert.That(outcome, Is.EqualTo(WalkOutcome.SessionOver));
            Assert.That(game.Progress.HighestUnlockedLevel, Is.EqualTo(1));
            Assert.That(game.NextLevelNumber, Is.EqualTo(1));
        }

        [Test]
        public void ChoosingBeforeStartingALevelIsRejected()
        {
            var game = new GridPathGame();

            Assert.Throws<InvalidOperationException>(() => game.Choose(new GridCoord(0, 1)));
        }

        [Test]
        public void SkipSpendsAChargeAndUnlocksTheNextLevelWithoutBuildingARun()
        {
            var progress = new PlayerProgress(highestUnlockedLevel: 12, skipCharges: 1);
            var game = new GridPathGame(progress: progress);

            game.SelectLevel(12);

            Assert.That(game.CanSkip, Is.True);
            Assert.That(game.Run, Is.Null);

            game.SkipCurrentLevel();

            Assert.That(game.Progress.SkipCharges, Is.EqualTo(0));
            Assert.That(game.Progress.HighestUnlockedLevel, Is.EqualTo(13));
            Assert.That(game.NextLevelNumber, Is.EqualTo(13));
            Assert.That(game.Run, Is.Null);
            Assert.That(game.CanSkip, Is.False);
        }
    }
}
