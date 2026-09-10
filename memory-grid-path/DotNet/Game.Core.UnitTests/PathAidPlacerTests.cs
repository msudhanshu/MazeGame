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
    public class PathAidPlacerTests
    {
        static readonly GridSize Size = new GridSize(5, 5);
        static readonly IRandomSource Random = new XorShiftRandom(1);

        static GridPath PathOfLength(int cells)
        {
            var route = new List<GridCoord>(cells) { new GridCoord(0, 0) };
            var x = 0;
            var y = 0;
            var dir = 1;
            while (route.Count < cells)
            {
                var nextX = x + dir;
                if (nextX < 0 || nextX >= Size.Width)
                {
                    y++;
                    dir = -dir;
                    route.Add(new GridCoord(x, y));
                    continue;
                }

                x = nextX;
                route.Add(new GridCoord(x, y));
            }

            return new GridPath(Size, route);
        }

        static LevelDefinition Level(int lighthouses = 0, int glimpse = 0, int beacon = 0)
        {
            return new LevelDefinition(
                1,
                Size,
                new GridCoord(0, 0),
                new GridCoord(4, 4),
                new PathShapeSpec(5, 20, 0, 12),
                "test",
                lighthouseCount: lighthouses,
                glimpseCount: glimpse,
                beaconCount: beacon);
        }

        [Test]
        public void ZeroCountsPlaceNothing()
        {
            var aids = PathAidPlacer.Place(PathOfLength(9), Level(), Random);

            Assert.That(aids.Lighthouses, Is.Empty);
            Assert.That(aids.Pickups, Is.Empty);
        }

        [Test]
        public void OneLighthouseSitsOnAnInteriorCell()
        {
            var path = PathOfLength(9);
            var aids = PathAidPlacer.Place(path, Level(lighthouses: 1), Random);

            Assert.That(aids.Lighthouses.Count, Is.EqualTo(1));
            Assert.That(aids.Lighthouses[0], Is.Not.EqualTo(path.Start));
            Assert.That(aids.Lighthouses[0], Is.Not.EqualTo(path.Goal));
            Assert.That(path.Contains(aids.Lighthouses[0]), Is.True);
        }

        [Test]
        public void TwoLighthousesAreInteriorAndNotAdjacent()
        {
            var path = PathOfLength(9);
            var aids = PathAidPlacer.Place(path, Level(lighthouses: 2), Random);

            Assert.That(aids.Lighthouses.Count, Is.EqualTo(2));
            Assert.That(aids.Lighthouses[0], Is.Not.EqualTo(path.Start));
            Assert.That(aids.Lighthouses[1], Is.Not.EqualTo(path.Goal));

            var a = IndexOf(path, aids.Lighthouses[0]);
            var b = IndexOf(path, aids.Lighthouses[1]);
            Assert.That(Math.Abs(a - b), Is.GreaterThan(1));
        }

        [Test]
        public void TooShortAPathPlacesFewerLighthouses()
        {
            var shortPath = PathOfLength(3);
            var aids = PathAidPlacer.Place(shortPath, Level(lighthouses: 2, glimpse: 1, beacon: 1), Random);

            Assert.That(aids.Lighthouses.Count, Is.EqualTo(1));
            Assert.That(aids.Pickups, Is.Empty);
        }

        [Test]
        public void PickupsSitOnRemainingInteriorCells()
        {
            var path = PathOfLength(9);
            var aids = PathAidPlacer.Place(path, Level(lighthouses: 1, glimpse: 1, beacon: 1), Random);

            Assert.That(aids.Lighthouses.Count, Is.EqualTo(1));
            Assert.That(aids.Pickups.Count, Is.EqualTo(2));
            Assert.That(aids.Pickups[0].Kind, Is.EqualTo(PathPickupKind.Glimpse));
            Assert.That(aids.Pickups[1].Kind, Is.EqualTo(PathPickupKind.Beacon));

            foreach (var pickup in aids.Pickups)
            {
                Assert.That(pickup.Cell, Is.Not.EqualTo(path.Start));
                Assert.That(pickup.Cell, Is.Not.EqualTo(path.Goal));
                Assert.That(pickup.Cell, Is.Not.EqualTo(aids.Lighthouses[0]));
                Assert.That(path.Contains(pickup.Cell), Is.True);
            }

            Assert.That(aids.Pickups[0].Cell, Is.Not.EqualTo(aids.Pickups[1].Cell));
        }

        [Test]
        public void RejectsMissingArguments()
        {
            var path = PathOfLength(5);
            var level = Level();

            Assert.Throws<ArgumentNullException>(() => PathAidPlacer.Place(null, level, Random));
            Assert.Throws<ArgumentNullException>(() => PathAidPlacer.Place(path, null, Random));
            Assert.Throws<ArgumentNullException>(() => PathAidPlacer.Place(path, level, null));
        }

        static int IndexOf(GridPath path, GridCoord cell)
        {
            for (var i = 0; i < path.Cells.Count; i++)
            {
                if (path.Cells[i] == cell)
                    return i;
            }

            throw new InvalidOperationException($"Cell {cell} is not on the path.");
        }
    }
}
