using System;
using Game.Core;
using NUnit.Framework;
using Nixin.Maze.Core;

namespace Game.Core.Tests
{
    [TestFixture]
    public class MazeRunTests
    {
        [Test]
        public void FailKeepsTheLevelAndRequiresANewSeed()
        {
            var run = MazeRun.Start(CampaignCatalog.Default(), seed: 4);
            var level = run.LevelIndex;
            var spec = run.CurrentSpec;
            var budget = run.TimeRemaining;

            run.Tick(budget + 1f);

            Assert.That(run.Phase, Is.EqualTo(MazeRunPhase.Failed));
            Assert.That(run.LevelIndex, Is.EqualTo(level));
            Assert.That(run.CurrentSpec.Size, Is.EqualTo(spec.Size));
            Assert.That(run.CurrentSpec.Difficulty, Is.EqualTo(spec.Difficulty));
            Assert.That(run.CurrentSpec.BraidFactor, Is.EqualTo(spec.BraidFactor));
            Assert.Throws<ArgumentException>(() => run.BeginAttempt(4));

            run.BeginAttempt(5);
            Assert.That(run.Phase, Is.EqualTo(MazeRunPhase.Playing));
            Assert.That(run.LevelIndex, Is.EqualTo(level));
            Assert.That(run.AttemptSeed, Is.EqualTo(5));
            Assert.That(run.TimeRemaining, Is.EqualTo(budget));
        }

        [Test]
        public void ReachExitAdvancesTheLevelUntilTheCampaignCompletes()
        {
            var catalog = new CampaignCatalog(new[]
            {
                new CampaignLevel("A", 4, 4, MazeDifficulty.Easy, 0.5f),
                new CampaignLevel("B", 6, 6, MazeDifficulty.Medium, 0.2f)
            });
            var run = MazeRun.Start(catalog, seed: 1);
            Assert.That(run.CurrentLevel.Name, Is.EqualTo("A"));

            run.ReachExit();
            Assert.That(run.Phase, Is.EqualTo(MazeRunPhase.Won));
            Assert.That(run.LevelIndex, Is.EqualTo(1));
            Assert.That(run.CurrentLevel.Name, Is.EqualTo("B"));

            run.BeginAttempt(2);
            run.ReachExit();
            Assert.That(run.Phase, Is.EqualTo(MazeRunPhase.Complete));
            Assert.That(run.LevelIndex, Is.EqualTo(1));
        }

        [Test]
        public void ReachExitAfterFailIsIgnored()
        {
            var run = MazeRun.Start(CampaignCatalog.Default(), seed: 8);
            run.Tick(run.TimeRemaining + 0.01f);
            Assert.That(run.Phase, Is.EqualTo(MazeRunPhase.Failed));

            run.ReachExit();
            Assert.That(run.Phase, Is.EqualTo(MazeRunPhase.Failed));
            Assert.That(run.LevelIndex, Is.EqualTo(0));
        }

        [Test]
        public void TickDoesNotRunAfterTheAttemptEnds()
        {
            var run = MazeRun.Start(CampaignCatalog.Default(), seed: 3);
            run.ReachExit();
            var remaining = run.TimeRemaining;
            run.Tick(10f);
            Assert.That(run.TimeRemaining, Is.EqualTo(remaining));
            Assert.That(run.Phase, Is.EqualTo(MazeRunPhase.Won));
        }

        [Test]
        public void RestartCampaignReturnsToTheFirstLevel()
        {
            var catalog = new CampaignCatalog(new[]
            {
                new CampaignLevel("A", 4, 4, MazeDifficulty.Easy, 0.5f),
                new CampaignLevel("B", 6, 6, MazeDifficulty.Medium, 0.2f)
            });
            var run = MazeRun.Start(catalog, seed: 1);
            run.ReachExit();
            run.BeginAttempt(2);
            run.ReachExit();
            Assert.That(run.Phase, Is.EqualTo(MazeRunPhase.Complete));

            run.RestartCampaign(9);
            Assert.That(run.Phase, Is.EqualTo(MazeRunPhase.Playing));
            Assert.That(run.LevelIndex, Is.EqualTo(0));
            Assert.That(run.AttemptSeed, Is.EqualTo(9));
        }

        [Test]
        public void BeginAttemptOnACompleteRunThrowsUntilRestart()
        {
            var catalog = new CampaignCatalog(new[]
            {
                new CampaignLevel("A", 4, 4, MazeDifficulty.Easy, 0.5f)
            });
            var run = MazeRun.Start(catalog, seed: 1);
            run.ReachExit();
            Assert.Throws<InvalidOperationException>(() => run.BeginAttempt(2));
        }
    }
}
