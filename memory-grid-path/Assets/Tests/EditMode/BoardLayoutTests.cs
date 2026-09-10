using Game.Unity.View;
using NUnit.Framework;
using Nixin.Grid.Core;
using UnityEngine;

namespace Game.Unity.Tests
{
    [TestFixture]
    public class BoardLayoutTests
    {
        static BoardLayout Layout(int width = 5, int height = 5) =>
            new BoardLayout(new GridSize(width, height), tileSize: 0.9f, gap: 0.1f, origin: Vector3.zero);

        [Test]
        public void BoardIsCentredOnItsOrigin()
        {
            var layout = Layout();

            var centre = layout.WorldPosition(new GridCoord(2, 2));

            Assert.That(centre.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(centre.z, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(centre.y, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void SurfaceSpanIsTheOuterTileEdges()
        {
            var layout = Layout();

            Assert.That(layout.SurfaceWidth, Is.EqualTo(layout.Width - layout.Gap).Within(0.0001f));
            Assert.That(layout.SurfaceDepth, Is.EqualTo(layout.Depth - layout.Gap).Within(0.0001f));
            Assert.That(layout.SurfaceWidth, Is.EqualTo(5 * 0.9f + 4 * 0.1f).Within(0.0001f));
        }

        [Test]
        public void NeighbouringCellsAreOnePitchApart()
        {
            var layout = Layout();

            var a = layout.WorldPosition(new GridCoord(1, 1));
            var b = layout.WorldPosition(new GridCoord(2, 1));

            Assert.That(b.x - a.x, Is.EqualTo(layout.Pitch).Within(0.0001f));
            Assert.That(layout.Pitch, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void PositiveYMapsAwayFromTheCamera()
        {
            var layout = Layout();

            var near = layout.WorldPosition(new GridCoord(0, 0));
            var far = layout.WorldPosition(new GridCoord(0, 4));

            Assert.That(far.z, Is.GreaterThan(near.z));
        }

        [Test]
        public void WorldPositionsRoundTripBackToTheirCell()
        {
            var layout = Layout(6, 4);

            for (var y = 0; y < 4; y++)
            {
                for (var x = 0; x < 6; x++)
                {
                    var coord = new GridCoord(x, y);
                    Assert.That(layout.TryCoordAt(layout.WorldPosition(coord), out var roundTripped), Is.True);
                    Assert.That(roundTripped, Is.EqualTo(coord));
                }
            }
        }

        [Test]
        public void PointsInsideATileResolveToThatTile()
        {
            var layout = Layout();
            var centre = layout.WorldPosition(new GridCoord(3, 1));

            Assert.That(layout.TryCoordAt(centre + new Vector3(0.3f, 0f, -0.3f), out var coord), Is.True);
            Assert.That(coord, Is.EqualTo(new GridCoord(3, 1)));
        }

        [Test]
        public void PointsOffTheBoardAreRejected()
        {
            var layout = Layout();

            Assert.That(layout.TryCoordAt(new Vector3(50f, 0f, 0f), out _), Is.False);
            Assert.That(layout.TryCoordAt(new Vector3(0f, 0f, -50f), out _), Is.False);
        }

        [Test]
        public void ARayFromAboveHitsTheTileBelowIt()
        {
            var layout = Layout();
            var target = new GridCoord(1, 3);
            var ray = new Ray(layout.WorldPosition(target) + new Vector3(0f, 10f, 0f), Vector3.down);

            Assert.That(layout.TryCoordUnderRay(ray, out var coord), Is.True);
            Assert.That(coord, Is.EqualTo(target));
        }

        [Test]
        public void ARayOnAChoiceSegmentPicksThatOption()
        {
            var layout = Layout();
            var current = new GridCoord(2, 2);
            var option = new GridCoord(3, 2);
            var midpoint = (layout.WorldPosition(current) + layout.WorldPosition(option)) * 0.5f;
            var ray = new Ray(midpoint + new Vector3(0f, 10f, 0f), Vector3.down);

            var picked = layout.TryPickOptionUnderRay(
                ray,
                current,
                new[] { option },
                tilePickRadius: 0.42f,
                edgePickRadius: 0.26f,
                out var target);

            Assert.That(picked, Is.True);
            Assert.That(target, Is.EqualTo(option));
        }

        [Test]
        public void ARayPointingAwayFromTheBoardMisses()
        {
            var layout = Layout();
            var ray = new Ray(new Vector3(0f, 10f, 0f), Vector3.up);

            Assert.That(layout.TryCoordUnderRay(ray, out _), Is.False);
        }

        [Test]
        public void AnOffsetOriginShiftsTheWholeBoard()
        {
            var shifted = new BoardLayout(new GridSize(5, 5), 0.9f, 0.1f, new Vector3(10f, 0f, -4f));

            var centre = shifted.WorldPosition(new GridCoord(2, 2));

            Assert.That(centre, Is.EqualTo(new Vector3(10f, 0f, -4f)));
            Assert.That(shifted.TryCoordAt(centre, out var coord), Is.True);
            Assert.That(coord, Is.EqualTo(new GridCoord(2, 2)));
        }
    }
}
