using System;
using System.Collections.Generic;
using Game.Core.Memory;
using NUnit.Framework;
using Nixin.Game.Core;
using Nixin.Grid.Core;
using Nixin.Maze.Core;

namespace Game.Core.Tests
{
    [TestFixture]
    public class MemoryWallPlannerTests
    {
        static MemoryCatalogSnapshot DemoCatalog()
        {
            var genres = new[]
            {
                new MemoryGenreSnapshot("nature", "Nature"),
                new MemoryGenreSnapshot("landmarks", "Landmarks"),
                new MemoryGenreSnapshot("objects", "Objects")
            };
            var entries = new List<MemoryEntrySnapshot>
            {
                new MemoryEntrySnapshot("oak", "nature", MemoryDisplayKind.Photo, "plain"),
                new MemoryEntrySnapshot("river", "nature", MemoryDisplayKind.Photo, "frame"),
                new MemoryEntrySnapshot("tower", "landmarks", MemoryDisplayKind.Photo, "glow"),
                new MemoryEntrySnapshot("bridge", "landmarks", MemoryDisplayKind.Relief, "plain"),
                new MemoryEntrySnapshot("pot", "objects", MemoryDisplayKind.Object3d, string.Empty),
                new MemoryEntrySnapshot("lamp", "objects", MemoryDisplayKind.Object3d, string.Empty)
            };
            return new MemoryCatalogSnapshot(genres, entries);
        }

        static MemoryLevelSpec DemoLevel()
        {
            return new MemoryLevelSpec(
                "level-1",
                new[] { "nature", "landmarks", "objects" },
                maxSameEntryPerWalk: 1,
                maxSameGenrePerWalk: 0,
                avoidPreviousGenres: true,
                decorateOppositeFaces: false);
        }

        static MazeLayout SmallLayout()
        {
            return DifficultyTunedMazeGenerator.Generate(
                MazeSpec.For(new GridSize(3, 3), MazeDifficulty.Easy),
                new XorShiftRandom(3));
        }

        static MemoryCatalogSnapshot LargeCatalog(int count)
        {
            var genres = new[] { new MemoryGenreSnapshot("nature", "Nature") };
            var entries = new MemoryEntrySnapshot[count];
            for (var i = 0; i < count; i++)
                entries[i] = new MemoryEntrySnapshot("entry-" + i, "nature", MemoryDisplayKind.Photo, "plain");
            return new MemoryCatalogSnapshot(genres, entries);
        }

        static MemoryCatalogSnapshot MultiGenreCatalog(int perGenre)
        {
            var genres = new[]
            {
                new MemoryGenreSnapshot("nature", "Nature"),
                new MemoryGenreSnapshot("landmarks", "Landmarks")
            };
            var entries = new List<MemoryEntrySnapshot>(perGenre * 2);
            for (var i = 0; i < perGenre; i++)
                entries.Add(new MemoryEntrySnapshot("n-" + i, "nature", MemoryDisplayKind.Photo, "plain"));
            for (var i = 0; i < perGenre; i++)
                entries.Add(new MemoryEntrySnapshot("l-" + i, "landmarks", MemoryDisplayKind.Photo, "plain"));
            return new MemoryCatalogSnapshot(genres, entries);
        }

        [Test]
        public void PlanCoversEveryWallOnce()
        {
            var layout = DifficultyTunedMazeGenerator.Generate(
                MazeSpec.For(new GridSize(4, 4), MazeDifficulty.Easy),
                new XorShiftRandom(3));
            var wallCount = layout.Grid.OccupiedWalls().Count;
            var plan = MemoryWallPlanner.Plan(
                layout,
                LargeCatalog(wallCount),
                DemoLevel(),
                new XorShiftRandom(9));
            Assert.That(plan.Placements.Count, Is.EqualTo(wallCount));
        }

        [Test]
        public void PlanDoesNotRepeatTheSameEntryOnOneWalk()
        {
            var layout = SmallLayout();
            var wallCount = layout.Grid.OccupiedWalls().Count;
            var plan = MemoryWallPlanner.Plan(
                layout,
                LargeCatalog(wallCount),
                DemoLevel(),
                new XorShiftRandom(11));
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < plan.Placements.Count; i++)
                Assert.That(seen.Add(plan.Placements[i].EntryId), Is.True, "Duplicate entry " + plan.Placements[i].EntryId);
        }

        [Test]
        public void PlanThrowsWhenCatalogIsTooSmallForUniqueWalk()
        {
            Assert.Throws<InvalidOperationException>(() =>
                MemoryWallPlanner.Plan(SmallLayout(), DemoCatalog(), DemoLevel(), new XorShiftRandom(11)));
        }

        [Test]
        public void PlanIsDeterministicForTheSameSeed()
        {
            var layout = SmallLayout();
            var wallCount = layout.Grid.OccupiedWalls().Count;
            var level = DemoLevel();
            var catalog = LargeCatalog(wallCount);
            var first = MemoryWallPlanner.Plan(layout, catalog, level, new XorShiftRandom(5));
            var second = MemoryWallPlanner.Plan(layout, catalog, level, new XorShiftRandom(5));
            Assert.That(first.Placements.Count, Is.EqualTo(second.Placements.Count));
            for (var i = 0; i < first.Placements.Count; i++)
            {
                Assert.That(first.Placements[i].EntryId, Is.EqualTo(second.Placements[i].EntryId));
                Assert.That(first.Placements[i].Kind, Is.EqualTo(second.Placements[i].Kind));
            }
        }

        [Test]
        public void PlanAvoidsGenresFromThePreviousLevel()
        {
            var layout = SmallLayout();
            var wallCount = layout.Grid.OccupiedWalls().Count;
            var level = DemoLevel();
            var catalog = MultiGenreCatalog(wallCount);
            var withAvoid = MemoryWallPlanner.Plan(
                layout,
                catalog,
                level,
                new XorShiftRandom(2),
                new[] { "nature" });

            for (var i = 0; i < withAvoid.Placements.Count; i++)
                Assert.That(withAvoid.Placements[i].GenreId, Is.Not.EqualTo("nature"));
        }

        [Test]
        public void ValidatorRejectsMissingGenreReferences()
        {
            var catalog = new MemoryCatalogSnapshot(
                Array.Empty<MemoryGenreSnapshot>(),
                new[] { new MemoryEntrySnapshot("x", "missing", MemoryDisplayKind.Photo, "plain") });
            var result = MemoryCatalogValidator.Validate(catalog, DemoLevel());
            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void PlanPlacesOppositeFaceOnInteriorWallsWhenEnabled()
        {
            var layout = SmallLayout();
            var bothSides = new MemoryLevelSpec(
                "level-1",
                new[] { "nature" },
                maxSameEntryPerWalk: 1,
                decorateOppositeFaces: true);
            var slotCount = MemoryWallPlanner.CountSlots(layout, bothSides);
            Assert.That(slotCount, Is.GreaterThan(layout.Grid.OccupiedWalls().Count));

            var plan = MemoryWallPlanner.Plan(
                layout,
                LargeCatalog(slotCount),
                bothSides,
                new XorShiftRandom(8));
            Assert.That(plan.Placements.Count, Is.EqualTo(slotCount));

            var hasOpposite = false;
            for (var i = 0; i < plan.Placements.Count; i++)
            {
                if (plan.Placements[i].Face == WallFace.Opposite)
                    hasOpposite = true;
            }

            Assert.That(hasOpposite, Is.True);
        }

        [Test]
        public void OppositeFacesOnTheSameWallGetDifferentEntryIds()
        {
            var layout = SmallLayout();
            var bothSides = new MemoryLevelSpec(
                "level-1",
                new[] { "nature" },
                maxSameEntryPerWalk: 1,
                decorateOppositeFaces: true);
            var slotCount = MemoryWallPlanner.CountSlots(layout, bothSides);
            var plan = MemoryWallPlanner.Plan(
                layout,
                LargeCatalog(slotCount),
                bothSides,
                new XorShiftRandom(8));

            var byWall = new Dictionary<MazeEdge, List<string>>();
            for (var i = 0; i < plan.Placements.Count; i++)
            {
                var placement = plan.Placements[i];
                if (!byWall.TryGetValue(placement.Edge, out var ids))
                {
                    ids = new List<string>();
                    byWall[placement.Edge] = ids;
                }

                ids.Add(placement.EntryId);
            }

            var sawBothFaces = false;
            foreach (var pair in byWall)
            {
                if (pair.Value.Count < 2)
                    continue;
                sawBothFaces = true;
                Assert.That(pair.Value[0], Is.Not.EqualTo(pair.Value[1]));
            }

            Assert.That(sawBothFaces, Is.True);
        }

        [Test]
        public void PlanLookupReturnsTheEntryForAWallFace()
        {
            var layout = SmallLayout();
            var wallCount = layout.Grid.OccupiedWalls().Count;
            var plan = MemoryWallPlanner.Plan(
                layout,
                LargeCatalog(wallCount),
                DemoLevel(),
                new XorShiftRandom(9));
            var first = plan.Placements[0];
            Assert.That(plan.TryGetPlacement(first.Edge, first.Face, out var found), Is.True);
            Assert.That(found.EntryId, Is.EqualTo(first.EntryId));
            Assert.That(found.FrameVariantId, Is.EqualTo(first.FrameVariantId));
        }

        [Test]
        public void ValidatorRejectsDuplicateEntryIds()
        {
            var catalog = new MemoryCatalogSnapshot(
                new[] { new MemoryGenreSnapshot("nature", "Nature") },
                new[]
                {
                    new MemoryEntrySnapshot("oak", "nature", MemoryDisplayKind.Photo, "plain"),
                    new MemoryEntrySnapshot("oak", "nature", MemoryDisplayKind.Photo, "frame")
                });
            var result = MemoryCatalogValidator.Validate(catalog, DemoLevel());
            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void CountSlotsDoesNotAddOppositeFacesOnPerimeterOnlyWhenDisabled()
        {
            var layout = SmallLayout();
            var oneSide = DemoLevel();
            Assert.That(
                MemoryWallPlanner.CountSlots(layout, oneSide),
                Is.EqualTo(layout.Grid.OccupiedWalls().Count));
        }
    }
}
