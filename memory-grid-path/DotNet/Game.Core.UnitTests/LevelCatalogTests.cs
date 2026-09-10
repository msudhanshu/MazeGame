using System;
using System.Collections.Generic;
using Game.Core.Domain;
using Game.Core.Rules;
using NUnit.Framework;
using Nixin.Game.Core;
using Nixin.Grid.Core;

namespace Game.Core.Tests
{
    [TestFixture]
    public class LevelCatalogTests
    {
        [Test]
        public void ShipsFiftyLevelsNumberedInOrder()
        {
            var catalog = new LevelCatalog();

            Assert.That(catalog.Count, Is.EqualTo(50));
            for (var level = 1; level <= catalog.Count; level++)
                Assert.That(catalog.Get(level).Number, Is.EqualTo(level));
        }

        [Test]
        public void StartsOnAThreeByThreeBoard()
        {
            var catalog = new LevelCatalog();

            Assert.That(catalog.Get(1).Size, Is.EqualTo(new GridSize(3, 3)));
            Assert.That(catalog.Get(2).Size, Is.EqualTo(new GridSize(3, 4)));
            Assert.That(catalog.Get(1).LivesPerRun, Is.EqualTo(2));
            Assert.That(catalog.Get(1).RunsPerSession, Is.EqualTo(2));
            Assert.That(catalog.Get(4).LivesPerRun, Is.EqualTo(2));
            Assert.That(catalog.Get(4).RunsPerSession, Is.EqualTo(3));
            Assert.That(catalog.Get(7).LivesPerRun, Is.EqualTo(3));
            Assert.That(catalog.Get(7).RunsPerSession, Is.EqualTo(4));
            Assert.That(catalog.Get(13).RunsPerSession, Is.EqualTo(5));
            Assert.That(catalog.Get(50).LivesPerRun, Is.EqualTo(3));
            Assert.That(catalog.Get(8).LighthouseCount, Is.EqualTo(1));
            Assert.That(catalog.Get(15).GlimpseCount, Is.EqualTo(1));
            Assert.That(catalog.Get(26).BeaconCount, Is.EqualTo(1));
            Assert.That(catalog.Get(1).LivesPerRun * catalog.Get(1).RunsPerSession, Is.EqualTo(4),
                "early luck budget should be small enough that a random walk can fail");
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
                Assert.That(current.LighthouseCount, Is.LessThanOrEqualTo(2));
                Assert.That(current.GlimpseCount, Is.LessThanOrEqualTo(1));
                Assert.That(current.BeaconCount, Is.LessThanOrEqualTo(1));
                Assert.That(current.RunsPerSession, Is.GreaterThanOrEqualTo(previous.RunsPerSession),
                    $"level {level} cut session walks");
            }

            Assert.That(catalog.Get(50).UnmarkedDifficulty, Is.GreaterThan(catalog.Get(1).UnmarkedDifficulty));
            Assert.That(catalog.Get(50).Size.CellCount, Is.GreaterThan(catalog.Get(1).Size.CellCount));
        }

        [Test]
        public void EveryLevelGeneratesAPathMatchingItsShape()
        {
            var catalog = new LevelCatalog();
            var factory = new LevelPathFactory(catalog);

            for (var level = 1; level <= catalog.Count; level++)
            {
                var definition = catalog.Get(level);

                for (var seed = 0; seed < 2; seed++)
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
                distinct.Add(string.Join(">", factory.Create(3, seed).Cells));

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
            Assert.That(catalog.Contains(51), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => catalog.Get(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => catalog.Get(51));
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
    }
}
