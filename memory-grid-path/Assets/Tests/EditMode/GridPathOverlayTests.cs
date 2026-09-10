using System.Collections.Generic;
using Game.Core.Domain;
using Game.Core.Rules;
using Game.Unity.View;
using NUnit.Framework;
using Nixin.Grid.Core;
using UnityEngine;

namespace Game.Unity.Tests
{
    [TestFixture]
    public class GridPathOverlayTests
    {
        GameObject _host;
        GridBoardView _board;
        FakeTileViewFactory _factory;

        static readonly GridSize Size = new GridSize(5, 5);

        static GridPath TestPath()
        {
            return new GridPath(Size, new List<GridCoord>
            {
                new GridCoord(0, 0),
                new GridCoord(1, 0),
                new GridCoord(1, 1),
                new GridCoord(2, 1),
                new GridCoord(2, 2)
            });
        }

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("OverlayHost");
            _board = _host.AddComponent<GridBoardView>();
            _factory = new FakeTileViewFactory();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void BuildingTheBoardAttachesAnOverlay()
        {
            _board.Build(Size, _factory);

            Assert.That(_board.Overlay, Is.Not.Null);
            Assert.That(_host.transform.Find(GridPathOverlay.ObjectName), Is.Not.Null);
        }

        [Test]
        public void StartOfAWalkShowsADotHomeAndNoTrailYet()
        {
            _board.Build(Size, _factory);
            var run = new GridWalkRun(TestPath());
            GridBoardPresenter.Refresh(_board, run);

            var overlay = _board.Overlay;
            Assert.That(overlay.transform.Find(GridPathOverlay.DotsName).childCount, Is.EqualTo(1));
            Assert.That(overlay.Trail.enabled, Is.False);
            Assert.That(overlay.transform.Find(GridPathOverlay.HomeName).gameObject.activeSelf, Is.True);
            Assert.That(overlay.IsCelebrating, Is.False);

            var home = overlay.transform.Find(GridPathOverlay.HomeName);
            Assert.That(home.position, Is.EqualTo(_board.WorldPosition(run.Path.Goal) + Vector3.up * (GridPathOverlay.Lift + 0.012f)));
        }

        [Test]
        public void WalkingDrawsALineDotsAndForwardArrows()
        {
            _board.Build(Size, _factory);
            var run = new GridWalkRun(TestPath());
            run.Choose(run.Path.Cells[1]);
            run.Choose(run.Path.Cells[2]);
            GridBoardPresenter.Refresh(_board, run);

            var overlay = _board.Overlay;
            Assert.That(overlay.transform.Find(GridPathOverlay.DotsName).childCount, Is.EqualTo(3));
            Assert.That(overlay.Trail.enabled, Is.True);
            Assert.That(overlay.Trail.positionCount, Is.EqualTo(3));
            Assert.That(overlay.Trail.GetPosition(0), Is.EqualTo(_board.WorldPosition(run.Path.Cells[0]) + Vector3.up * GridPathOverlay.Lift));
            Assert.That(overlay.Trail.GetPosition(2), Is.EqualTo(_board.WorldPosition(run.Path.Cells[2]) + Vector3.up * GridPathOverlay.Lift));
            Assert.That(overlay.transform.Find(GridPathOverlay.ArrowsName).childCount, Is.GreaterThan(0));
        }

        [Test]
        public void BlockedTilesGetACross()
        {
            _board.Build(Size, _factory);
            var blocked = new GridCoord(4, 4);
            var run = new GridWalkRun(
                TestPath(),
                aids: new PathAids(new GridCoord[0], new PathPickup[0], new[] { blocked }));
            GridBoardPresenter.Refresh(_board, run);

            var root = _board.Overlay.transform.Find(GridPathOverlay.BlockedName);
            Assert.That(root.childCount, Is.EqualTo(1));
            Assert.That(root.GetChild(0).position, Is.EqualTo(_board.WorldPosition(blocked) + Vector3.up * (GridPathOverlay.Lift + 0.01f)));
        }

        [Test]
        public void CompletingThePathCelebratesAlongTheFullTrail()
        {
            _board.Build(Size, _factory);
            var run = new GridWalkRun(TestPath());
            for (var i = 1; i < run.Path.Cells.Count; i++)
                run.Choose(run.Path.Cells[i]);

            GridBoardPresenter.Refresh(_board, run);

            Assert.That(run.IsLevelCompleted, Is.True);
            Assert.That(_board.Overlay.IsCelebrating, Is.True);
            Assert.That(_board.Overlay.Trail.enabled, Is.True);
            Assert.That(_board.Overlay.Trail.positionCount, Is.EqualTo(run.Path.Cells.Count));
            Assert.That(_board.Overlay.transform.Find(GridPathOverlay.ArrowsName).childCount, Is.GreaterThanOrEqualTo(4));
        }

        [Test]
        public void TrailAndDotsAreTranslucent()
        {
            Assert.That(GridPathOverlay.TrailTint.a, Is.LessThan(0.7f));
            Assert.That(GridPathOverlay.FocusTrailTint.a, Is.LessThan(GridPathOverlay.TrailTint.a));
            Assert.That(GridPathOverlay.DotTint.a, Is.LessThan(0.7f));
            Assert.That(GridPathOverlay.CelebrateTint.a, Is.GreaterThan(GridPathOverlay.TrailTint.a));
        }

        [Test]
        public void FocusedStyleMakesTheTrailThinnerAndMoreTransparent()
        {
            _board.Build(Size, _factory);
            var run = new GridWalkRun(TestPath());
            run.Choose(run.Path.Cells[1]);
            run.Choose(run.Path.Cells[2]);
            GridBoardPresenter.Refresh(_board, run);

            var overlay = _board.Overlay;
            overlay.SetFocusedStyle(false);
            var overviewWidth = overlay.Trail.startWidth;
            var overviewAlpha = overlay.Trail.sharedMaterial.color.a;

            overlay.SetFocusedStyle(true);

            Assert.That(overlay.Trail.startWidth, Is.LessThan(overviewWidth));
            Assert.That(overlay.Trail.sharedMaterial.color.a, Is.LessThan(overviewAlpha));
        }

        [Test]
        public void WrongTurnPreviewUsesARedSingleArrowSegment()
        {
            _board.Build(Size, _factory);
            var start = _board.WorldPosition(new GridCoord(0, 0)) + Vector3.up * GridPathOverlay.Lift;
            var wrong = _board.WorldPosition(new GridCoord(1, 0)) + Vector3.up * GridPathOverlay.Lift;

            _board.Overlay.RefreshWorld(new[] { start, wrong }, 0.11f, celebrating: false);
            _board.Overlay.ShowWrongTurn(start, wrong, 0.5f);

            Assert.That(_board.Overlay.Trail.enabled, Is.True);
            Assert.That(_board.Overlay.transform.Find(GridPathOverlay.ArrowsName).childCount, Is.EqualTo(1));
            Assert.That(_board.Overlay.Trail.sharedMaterial.color.r, Is.GreaterThan(0.9f));
            Assert.That(_board.Overlay.Trail.sharedMaterial.color.g, Is.LessThan(0.5f));
        }

        [Test]
        public void CelebrateArrowsMoveSlowerThanAWalk()
        {
            Assert.That(GridPathOverlay.CelebrateArrowSpeedFactor, Is.LessThan(1.1f));
            Assert.That(GridPathOverlay.CelebrateArrowSpeedFactor, Is.GreaterThan(GridPathOverlay.WalkArrowSpeedFactor));
            Assert.That(GridPathOverlay.CompletionHoldSeconds, Is.GreaterThan(2f));
        }

        [Test]
        public void AWorldPolylineDrawsTheSameTrailAndDots()
        {
            _board.Build(Size, _factory);
            var points = new[]
            {
                _board.WorldPosition(new GridCoord(0, 0)) + Vector3.up * GridPathOverlay.Lift,
                _board.WorldPosition(new GridCoord(1, 0)) + Vector3.up * GridPathOverlay.Lift,
                _board.WorldPosition(new GridCoord(1, 1)) + Vector3.up * GridPathOverlay.Lift
            };

            _board.Overlay.RefreshWorld(points, 0.11f, celebrating: false);

            Assert.That(_board.Overlay.Trail.enabled, Is.True);
            Assert.That(_board.Overlay.Trail.positionCount, Is.EqualTo(3));
            Assert.That(_board.Overlay.transform.Find(GridPathOverlay.DotsName).childCount, Is.EqualTo(3));
            Assert.That(_board.Overlay.transform.Find(GridPathOverlay.HomeName).gameObject.activeSelf, Is.False);
        }

        [Test]
        public void PointAlongFollowsThePolyline()
        {
            var points = new[] { Vector3.zero, new Vector3(2f, 0f, 0f), new Vector3(2f, 0f, 2f) };

            var start = GridPathOverlay.PointAlong(points, 0f, out var dirStart);
            Assert.That(start, Is.EqualTo(Vector3.zero));
            Assert.That(dirStart.x, Is.GreaterThan(0.9f));

            var mid = GridPathOverlay.PointAlong(points, 1f, out _);
            Assert.That(mid.x, Is.EqualTo(1f).Within(0.0001f));

            var corner = GridPathOverlay.PointAlong(points, 2f, out var dirCorner);
            Assert.That(corner, Is.EqualTo(new Vector3(2f, 0f, 0f)));
            Assert.That(dirCorner.z, Is.GreaterThan(0.9f));

            var end = GridPathOverlay.PointAlong(points, 99f, out _);
            Assert.That(end, Is.EqualTo(new Vector3(2f, 0f, 2f)));
        }

        [Test]
        public void ClearingTheBoardHidesTheTrail()
        {
            _board.Build(Size, _factory);
            var run = new GridWalkRun(TestPath());
            run.Choose(run.Path.Cells[1]);
            GridBoardPresenter.Refresh(_board, run);

            _board.Clear();

            Assert.That(_board.Overlay.Trail.enabled, Is.False);
            Assert.That(_board.Overlay.transform.Find(GridPathOverlay.DotsName).childCount, Is.EqualTo(0));
            Assert.That(_board.Overlay.transform.Find(GridPathOverlay.HomeName).gameObject.activeSelf, Is.False);
        }

        sealed class FakeTileView : ITileView
        {
            public FakeTileView(GridCoord coord) => Coord = coord;

            public GridCoord Coord { get; }
            public TileVisualState State { get; private set; } = TileVisualState.Idle;

            public void SetState(TileVisualState state) => State = state;

            public void Flash(TileVisualState state, float seconds)
            {
            }

            public void Destroy()
            {
            }
        }

        sealed class FakeTileViewFactory : ITileViewFactory
        {
            public string ThemeId => "fake";

            public ITileView CreateTile(GridCoord coord, GridSize size, Vector3 worldPosition, float tileSize, Transform parent)
            {
                return new FakeTileView(coord);
            }

            public void ApplyEnvironment(Camera camera, BoardLayout layout, Transform parent)
            {
            }
        }
    }
}
