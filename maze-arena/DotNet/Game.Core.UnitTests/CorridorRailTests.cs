using System;
using Game.Core;
using NUnit.Framework;
using Nixin.Grid.Core;
using Nixin.Maze.Core;
using Nixin.Rail.Core;

namespace Game.Core.Tests
{
    [TestFixture]
    public class CorridorRailTests
    {
        [Test]
        public void CellCentersSitOnTheCorridorMidline()
        {
            var rail = StraightThree();
            rail.CellCenter(new GridCoord(0, 0), out var x0, out var z0);
            rail.CellCenter(new GridCoord(1, 0), out var x1, out var z1);
            rail.CellCenter(new GridCoord(2, 0), out var x2, out var z2);

            Assert.That(x0, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(x1, Is.EqualTo(3f).Within(0.0001f));
            Assert.That(x2, Is.EqualTo(5f).Within(0.0001f));
            Assert.That(z0, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(z1, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(z2, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void NeighborsFollowOpenPassagesOnly()
        {
            var rail = StraightThree();
            var fromStart = rail.Neighbors(new GridCoord(0, 0));
            Assert.That(fromStart.Count, Is.EqualTo(1));
            Assert.That(fromStart[0], Is.EqualTo(new GridCoord(1, 0)));

            var mid = rail.Neighbors(new GridCoord(1, 0));
            Assert.That(mid.Count, Is.EqualTo(2));
            Assert.That(rail.AreNeighbors(new GridCoord(0, 0), new GridCoord(1, 0)), Is.True);
        }

        [Test]
        public void LerpStaysOnTheCenterline()
        {
            var rail = StraightThree();
            rail.LerpEdge(new GridCoord(0, 0), new GridCoord(1, 0), 0.5f, out var x, out var z);
            Assert.That(x, Is.EqualTo(2f).Within(0.0001f));
            Assert.That(z, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void WalledNeighbourIsRejected()
        {
            var grid = new MazeGrid(new GridSize(2, 2));
            grid.CarvePassage(new GridCoord(0, 0), new GridCoord(1, 0));
            var rail = new CorridorRail(grid, 2f);
            Assert.That(rail.AreNeighbors(new GridCoord(0, 0), new GridCoord(0, 1)), Is.False);
            Assert.Throws<ArgumentException>(() => rail.LerpEdge(new GridCoord(0, 0), new GridCoord(0, 1), 0.5f, out _, out _));
        }

        [Test]
        public void OriginOffsetsTheCenterline()
        {
            var rail = new CorridorRail(StraightGrid(), 2f, 10f, 20f);
            rail.CellCenter(new GridCoord(0, 0), out var x, out var z);
            Assert.That(x, Is.EqualTo(11f).Within(0.0001f));
            Assert.That(z, Is.EqualTo(21f).Within(0.0001f));
        }

        [Test]
        public void AllCellsAndOpenEdgesCoverTheFullRail()
        {
            var rail = StraightThree();
            Assert.That(rail.AllCells().Count, Is.EqualTo(3));
            Assert.That(rail.OpenEdges().Count, Is.EqualTo(2));
        }

        [Test]
        public void NodeIdsRoundTripThroughIRail()
        {
            var rail = StraightThree();
            var cell = new GridCoord(1, 0);
            Assert.That(rail.Coord(rail.Id(cell)), Is.EqualTo(cell));

            IRail asRail = rail;
            Assert.That(asRail.Contains(rail.Id(cell)), Is.True);
            asRail.Position(rail.Id(cell), out var x, out var y, out var z);
            rail.CellCenter(cell, out var cx, out var cz);
            Assert.That(x, Is.EqualTo(cx).Within(0.0001f));
            Assert.That(z, Is.EqualTo(cz).Within(0.0001f));
            Assert.That(y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(asRail.StopSpacing, Is.EqualTo(2f));
        }

        static CorridorRail StraightThree() => new CorridorRail(StraightGrid(), 2f);

        static MazeGrid StraightGrid()
        {
            var grid = new MazeGrid(new GridSize(3, 1));
            grid.CarvePassage(new GridCoord(0, 0), new GridCoord(1, 0));
            grid.CarvePassage(new GridCoord(1, 0), new GridCoord(2, 0));
            return grid;
        }
    }
}
