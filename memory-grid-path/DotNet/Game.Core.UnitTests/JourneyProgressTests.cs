using System.Collections.Generic;
using Game.Core.State;
using NUnit.Framework;

namespace Game.Core.Tests
{
    [TestFixture]
    public class ClearedCountTests
    {
        [Test]
        public void FreshProgressHasZeroClears()
        {
            Assert.That(LevelAccess.ClearedCount(new PlayerProgress(), 12), Is.EqualTo(0));
        }

        [Test]
        public void CompletingMidCatalogCountsEachClear()
        {
            var progress = new PlayerProgress();
            for (var level = 1; level <= 10; level++)
                progress.RecordResult(level, 100, completed: true, levelCount: 12);

            Assert.That(progress.HighestUnlockedLevel, Is.EqualTo(11));
            Assert.That(LevelAccess.ClearedCount(progress, 12), Is.EqualTo(10));
        }

        [Test]
        public void CompletingTheLastLevelOfAShortCatalogStillCounts()
        {
            var progress = new PlayerProgress();
            progress.RecordResult(1, 80, completed: true, levelCount: 2);
            progress.RecordResult(2, 90, completed: true, levelCount: 2);

            Assert.That(progress.HighestUnlockedLevel, Is.EqualTo(2));
            Assert.That(LevelAccess.ClearedCount(progress, 2), Is.EqualTo(2));
        }

        [Test]
        public void FailingTheLastLevelDoesNotCountAsCleared()
        {
            var progress = new PlayerProgress();
            progress.RecordResult(1, 80, completed: true, levelCount: 2);
            progress.RecordResult(2, 10, completed: false, levelCount: 2);

            Assert.That(LevelAccess.ClearedCount(progress, 2), Is.EqualTo(1));
        }
    }

    [TestFixture]
    public class GameModeUnlockTests
    {
        static JourneyProgress WithTileClears(int clears, int catalogCount = 12)
        {
            var tile = new PlayerProgress();
            for (var level = 1; level <= clears; level++)
                tile.RecordResult(level, 50, completed: true, levelCount: catalogCount);
            return new JourneyProgress(tile);
        }

        [Test]
        public void TileArenaIsAlwaysUnlocked()
        {
            var journey = new JourneyProgress();
            Assert.That(
                GameModeUnlock.IsUnlocked(GameModeId.TileArena, journey, GameModeUnlockConfig.Default, 12, 6),
                Is.True);
        }

        [Test]
        public void GraphStaysLockedUntilTenTileClears()
        {
            var nine = WithTileClears(9);
            var ten = WithTileClears(10);

            Assert.That(
                GameModeUnlock.IsUnlocked(GameModeId.GraphArena, nine, GameModeUnlockConfig.Default, 12, 6),
                Is.False);
            Assert.That(
                GameModeUnlock.IsUnlocked(GameModeId.GraphArena, ten, GameModeUnlockConfig.Default, 12, 6),
                Is.True);
        }

        [Test]
        public void ScoutStaysLockedUntilFiveGraphClears()
        {
            var graph = new PlayerProgress();
            for (var level = 1; level <= 4; level++)
                graph.RecordResult(level, 50, completed: true, levelCount: 6);

            var locked = new JourneyProgress(graph: graph);
            Assert.That(
                GameModeUnlock.IsUnlocked(GameModeId.ScoutArena, locked, GameModeUnlockConfig.Default, 12, 6),
                Is.False);

            graph.RecordResult(5, 50, completed: true, levelCount: 6);
            var open = new JourneyProgress(graph: graph);
            Assert.That(
                GameModeUnlock.IsUnlocked(GameModeId.ScoutArena, open, GameModeUnlockConfig.Default, 12, 6),
                Is.True);
        }

        [Test]
        public void ThresholdsAreConfigurable()
        {
            var config = new GameModeUnlockConfig(graphUnlockAfterTileClears: 2, scoutUnlockAfterGraphClears: 1);
            var journey = WithTileClears(2);

            Assert.That(GameModeUnlock.IsUnlocked(GameModeId.GraphArena, journey, config, 12, 6), Is.True);
            Assert.That(
                GameModeUnlock.LockReason(GameModeId.GraphArena, config),
                Is.EqualTo("Clear 2 Tile Arena levels to unlock."));
        }
    }

    [TestFixture]
    public class JourneyProgressTests
    {
        [Test]
        public void ModesKeepIndependentScoresAndUnlocks()
        {
            var tile = new PlayerProgress();
            tile.RecordResult(1, 400, completed: true, levelCount: 12);
            var graph = new PlayerProgress();
            graph.RecordResult(1, 250, completed: true, levelCount: 6);

            var journey = new JourneyProgress(tile, graph);
            journey.RememberPlayed(GameModeId.GraphArena, 1);

            Assert.That(journey.For(GameModeId.TileArena).CareerScore, Is.EqualTo(400));
            Assert.That(journey.For(GameModeId.GraphArena).CareerScore, Is.EqualTo(250));
            Assert.That(journey.For(GameModeId.ScoutArena).CareerScore, Is.EqualTo(0));
            Assert.That(journey.For(GameModeId.TileArena).HighestUnlockedLevel, Is.EqualTo(2));
            Assert.That(journey.For(GameModeId.GraphArena).HighestUnlockedLevel, Is.EqualTo(2));
            Assert.That(journey.For(GameModeId.ScoutArena).HighestUnlockedLevel, Is.EqualTo(1));
            Assert.That(journey.LastSelectedMode, Is.EqualTo(GameModeId.GraphArena));
        }

        [Test]
        public void LastSelectedModeRoundTripsThroughSave()
        {
            var tile = new PlayerProgress(
                highestUnlockedLevel: 4,
                bestScores: new Dictionary<int, int> { { 1, 100 } },
                careerScore: 100);
            var original = new JourneyProgress(tile, lastSelectedMode: GameModeId.ScoutArena, lastPlayedScout: 3);
            original.SelectMode(GameModeId.ScoutArena);

            var restored = JourneySaveFormat.Parse(JourneySaveFormat.Serialize(original));

            Assert.That(restored.LastSelectedMode, Is.EqualTo(GameModeId.ScoutArena));
            Assert.That(restored.LastPlayedLevel(GameModeId.ScoutArena), Is.EqualTo(3));
            Assert.That(restored.For(GameModeId.TileArena).HighestUnlockedLevel, Is.EqualTo(4));
            Assert.That(restored.For(GameModeId.TileArena).BestScoreFor(1), Is.EqualTo(100));
        }
    }

    [TestFixture]
    public class JourneySaveFormatTests
    {
        [Test]
        public void EmptyBlobYieldsFreshJourney()
        {
            var journey = JourneySaveFormat.Parse(string.Empty);
            Assert.That(journey.LastSelectedMode, Is.EqualTo(GameModeId.TileArena));
            Assert.That(journey.For(GameModeId.TileArena).HighestUnlockedLevel, Is.EqualTo(1));
        }

        [Test]
        public void ThreeModeBlobRoundTrips()
        {
            var tile = new PlayerProgress(highestUnlockedLevel: 11, careerScore: 900);
            tile.RecordResult(10, 50, completed: true, levelCount: 12);
            var graph = new PlayerProgress(highestUnlockedLevel: 3, careerScore: 200);
            var scout = new PlayerProgress(highestUnlockedLevel: 2, careerScore: 70);
            var original = new JourneyProgress(
                tile,
                graph,
                scout,
                GameModeId.GraphArena,
                lastPlayedTile: 10,
                lastPlayedGraph: 2,
                lastPlayedScout: 1);

            var restored = JourneySaveFormat.Parse(JourneySaveFormat.Serialize(original));

            Assert.That(restored.LastSelectedMode, Is.EqualTo(GameModeId.GraphArena));
            Assert.That(restored.LastPlayedLevel(GameModeId.TileArena), Is.EqualTo(10));
            Assert.That(restored.LastPlayedLevel(GameModeId.GraphArena), Is.EqualTo(2));
            Assert.That(restored.For(GameModeId.TileArena).CareerScore, Is.EqualTo(original.For(GameModeId.TileArena).CareerScore));
            Assert.That(restored.For(GameModeId.GraphArena).HighestUnlockedLevel, Is.EqualTo(3));
            Assert.That(restored.For(GameModeId.ScoutArena).CareerScore, Is.EqualTo(70));
        }
    }
}
