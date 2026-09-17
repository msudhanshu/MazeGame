using System;
using System.Collections.Generic;
using Game.Core.Domain;
using Game.Core.Rules;
using Game.Core.State;
using NUnit.Framework;
using Nixin.Game.Core;
using Nixin.Grid.Core;

namespace Game.Core.Tests
{
    [TestFixture]
    public class LevelCatalogTests
    {
        [Test]
        public void ShipsTwentyFiveLevelsNumberedInOrder()
        {
            var catalog = new LevelCatalog();

            Assert.That(catalog.Count, Is.EqualTo(25));
            for (var level = 1; level <= catalog.Count; level++)
                Assert.That(catalog.Get(level).Number, Is.EqualTo(level));
        }

        [Test]
        public void StartsOnAFourByFourBoardWithTwoHearts()
        {
            var catalog = new LevelCatalog();

            Assert.That(catalog.Get(1).Size, Is.EqualTo(new GridSize(4, 4)));
            Assert.That(catalog.Get(2).Size, Is.EqualTo(new GridSize(4, 5)));
            Assert.That(catalog.Get(3).Size, Is.EqualTo(new GridSize(4, 5)));
            Assert.That(catalog.Get(1).LivesPerRun, Is.EqualTo(2));
            Assert.That(catalog.Get(1).RunsPerSession, Is.EqualTo(3));
            Assert.That(catalog.Get(4).LivesPerRun, Is.EqualTo(2));
            Assert.That(catalog.Get(4).RunsPerSession, Is.EqualTo(3));
            Assert.That(catalog.Get(5).LivesPerRun, Is.EqualTo(2));
            Assert.That(catalog.Get(5).RunsPerSession, Is.EqualTo(3));
            Assert.That(catalog.Get(10).LivesPerRun, Is.EqualTo(2));
            Assert.That(catalog.Get(10).RunsPerSession, Is.EqualTo(3));
            Assert.That(catalog.Get(25).LivesPerRun, Is.EqualTo(2));
            Assert.That(catalog.Get(25).RunsPerSession, Is.EqualTo(3));
            Assert.That(catalog.Get(1).Shape.MinTurns, Is.EqualTo(1));
            Assert.That(catalog.Get(1).Shape.MaxTurns, Is.EqualTo(1));
            Assert.That(catalog.Get(2).Shape.MinTurns, Is.EqualTo(1));
            Assert.That(catalog.Get(2).Shape.MaxTurns, Is.EqualTo(1));
            Assert.That(catalog.Get(3).Shape.MinTurns, Is.EqualTo(2));
            Assert.That(catalog.Get(3).Shape.MaxTurns, Is.EqualTo(2));
            Assert.That(catalog.Get(5).Shape.MinTurns, Is.EqualTo(2));
            Assert.That(catalog.Get(5).Shape.MaxTurns, Is.EqualTo(2));
            Assert.That(catalog.Get(6).Shape.MinTurns, Is.EqualTo(3));
            Assert.That(catalog.Get(10).Shape.MinTurns, Is.EqualTo(4));
        }

        [Test]
        public void ShipsNoPathAidsOnTheTwentyFiveLevelSlice()
        {
            var catalog = new LevelCatalog();

            for (var level = 1; level <= catalog.Count; level++)
            {
                var current = catalog.Get(level);
                Assert.That(current.LighthouseCount, Is.EqualTo(0), $"level {level} lighthouse");
                Assert.That(current.GlimpseCount, Is.EqualTo(0), $"level {level} glimpse");
                Assert.That(current.BeaconCount, Is.EqualTo(0), $"level {level} beacon");
                Assert.That(current.BlockedHintCount, Is.EqualTo(0), $"level {level} blocked");
            }
        }

        [Test]
        public void ComplexityNeverDropsAsLevelsGoUp()
        {
            var catalog = new LevelCatalog();

            for (var level = 2; level <= catalog.Count; level++)
            {
                var previous = catalog.Get(level - 1);
                var current = catalog.Get(level);

                Assert.That(current.Size.CellCount, Is.GreaterThanOrEqualTo(previous.Size.CellCount),
                    $"level {level} shrank the board");
                Assert.That(current.UnmarkedDifficulty, Is.GreaterThanOrEqualTo(previous.UnmarkedDifficulty),
                    $"level {level} unmarked difficulty dropped from {previous.UnmarkedDifficulty} to {current.UnmarkedDifficulty}");
                Assert.That(current.LivesPerRun, Is.EqualTo(2),
                    $"level {level} should keep two misses per walk");
                Assert.That(current.RunsPerSession, Is.GreaterThanOrEqualTo(2));
                Assert.That(current.RunsPerSession, Is.LessThanOrEqualTo(3));
            }

            Assert.That(catalog.Get(25).UnmarkedDifficulty, Is.GreaterThan(catalog.Get(1).UnmarkedDifficulty));
            Assert.That(catalog.Get(25).Size.CellCount, Is.GreaterThan(catalog.Get(1).Size.CellCount));
        }

        [Test]
        public void EveryLevelGeneratesAPathMatchingItsShape()
        {
            var catalog = new LevelCatalog();
            var factory = new LevelPathFactory(catalog);

            for (var level = 1; level <= catalog.Count; level++)
            {
                var definition = catalog.Get(level);

                for (var seed = 0; seed < 5; seed++)
                {
                    var path = factory.Create(level, seed);

                    Assert.That(path.Start, Is.EqualTo(definition.Start), $"level {level} seed {seed}");
                    Assert.That(path.Goal, Is.EqualTo(definition.Goal), $"level {level} seed {seed}");
                    Assert.That(definition.Shape.IsSatisfiedBy(path), Is.True,
                        $"level {level} seed {seed}: length {path.Cells.Count}, turns {path.TurnCount}");
                    var aids = PathAidPlacer.Place(path, definition, new XorShiftRandom(seed + 7));
                    var blocked = OffPathHintPlacer.Place(path, definition, new XorShiftRandom(seed + 11));
                    aids = new PathAids(aids.Lighthouses, aids.Pickups, blocked);
                    Assert.DoesNotThrow(
                        () => new GridWalkRun(path, GameConfig.Default.WithBudget(definition.LivesPerRun, definition.RunsPerSession), aids),
                        $"level {level} seed {seed} could not be turned into a playable run");
                }
            }
        }

        [Test]
        public void ReplayingALevelGivesADifferentRoute()
        {
            var factory = new LevelPathFactory(new LevelCatalog());

            var distinct = new HashSet<string>();
            for (var seed = 0; seed < 10; seed++)
                distinct.Add(string.Join(">", factory.Create(12, seed).Cells));

            Assert.That(distinct.Count, Is.GreaterThan(5),
                "a second session on the same level must not reuse the memorised route");
        }

        [Test]
        public void SameSeedReplaysTheSameRoute()
        {
            var factory = new LevelPathFactory(new LevelCatalog());

            Assert.That(factory.Create(7, 8080).Cells, Is.EqualTo(factory.Create(7, 8080).Cells));
        }

        [Test]
        public void UnknownLevelsAreRejected()
        {
            var catalog = new LevelCatalog();

            Assert.That(catalog.Contains(0), Is.False);
            Assert.That(catalog.Contains(26), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => catalog.Get(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => catalog.Get(26));
        }

        [Test]
        public void CustomCatalogsMustBeNumberedFromOne()
        {
            var size = new GridSize(4, 4);
            var shape = new PathShapeSpec(7, 11, 1, 4);
            var level = new LevelDefinition(2, size, new GridCoord(0, 0), new GridCoord(3, 3), shape, "test");

            Assert.Throws<ArgumentException>(() => new LevelCatalog(new List<LevelDefinition> { level }));
            Assert.Throws<ArgumentException>(() => new LevelCatalog(new List<LevelDefinition>()));
        }

        [Test]
        public void PathFactoryAcceptsAnInjectedRandomSource()
        {
            var factory = new LevelPathFactory(new LevelCatalog());

            var first = factory.Create(2, new XorShiftRandom(99));
            var second = factory.Create(2, new XorShiftRandom(99));

            Assert.That(second.Cells, Is.EqualTo(first.Cells));
            Assert.Throws<ArgumentNullException>(() => factory.Create(2, (IRandomSource)null));
        }

        [Test]
        public void ScoutLadderStartsTinyAndGrowsSlowly()
        {
            Assert.That(LevelCatalog.ScoutSpecs.Count, Is.EqualTo(12));
            Assert.That(LevelCatalog.ScoutSpecs[0].Width, Is.EqualTo(2));
            Assert.That(LevelCatalog.ScoutSpecs[0].Height, Is.EqualTo(2));
            Assert.That(LevelCatalog.ScoutSpecs[1].Width, Is.EqualTo(2));
            Assert.That(LevelCatalog.ScoutSpecs[1].Height, Is.EqualTo(2));
            Assert.That(LevelCatalog.ScoutSpecs[2].Width, Is.EqualTo(2));
            Assert.That(LevelCatalog.ScoutSpecs[2].Height, Is.EqualTo(3));
            Assert.That(LevelCatalog.ScoutSpecs[3].Width, Is.EqualTo(2));
            Assert.That(LevelCatalog.ScoutSpecs[3].Height, Is.EqualTo(3));
            Assert.That(LevelCatalog.ScoutSpecs[3].MinTurns, Is.EqualTo(2));

            var catalog = new LevelCatalog(LevelCatalog.FromSpecs(LevelCatalog.ScoutSpecs));
            Assert.That(catalog.Get(1).Size, Is.EqualTo(new GridSize(2, 2)));
            Assert.That(catalog.Get(3).Size, Is.EqualTo(new GridSize(2, 3)));
            Assert.That(catalog.Get(5).Size, Is.EqualTo(new GridSize(3, 3)));
            Assert.That(catalog.Get(8).Size, Is.EqualTo(new GridSize(3, 4)));
            Assert.That(catalog.Get(9).Size, Is.EqualTo(new GridSize(4, 4)));
            Assert.That(catalog.Get(12).Size, Is.EqualTo(new GridSize(4, 5)));
            Assert.That(catalog.Get(6).Shape.MinTurns, Is.GreaterThan(catalog.Get(5).Shape.MinTurns));
            Assert.That(catalog.Get(12).Shape.MinTurns, Is.GreaterThan(catalog.Get(8).Shape.MinTurns));

            var progress = new PlayerProgress(highestUnlockedLevel: catalog.Count);
            var game = new GridPathGame(catalog, GameConfig.Default, progress);
            for (var level = 1; level <= catalog.Count; level++)
                Assert.DoesNotThrow(() => game.StartLevel(level, seed: 11 + level));
        }
    }
}
