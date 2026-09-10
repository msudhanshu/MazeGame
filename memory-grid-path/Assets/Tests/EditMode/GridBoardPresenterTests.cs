using System.Collections.Generic;
using Game.Core.Domain;
using Game.Core.Rules;
using Game.Unity.Themes;
using Game.Unity.Themes.Experimental;
using Game.Unity.View;
using NUnit.Framework;
using Nixin.Grid.Core;
using UnityEngine;

namespace Game.Unity.Tests
{
    [TestFixture]
    public class GridBoardPresenterTests
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

        static GridCoord WrongOption(GridWalkRun run)
        {
            foreach (var option in run.Options())
            {
                if (option != run.Path.Cells[run.Step + 1])
                    return option;
            }

            throw new System.InvalidOperationException("Every option was correct.");
        }

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("BoardHost");
            _board = _host.AddComponent<GridBoardView>();
            _factory = new FakeTileViewFactory();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void AfterAMistakeTheCorrectTilePaintsWalkedNotCandidate()
        {
            _board.Build(Size, _factory);
            var run = new GridWalkRun(TestPath());
            var correct = run.Path.Cells[1];

            run.Choose(WrongOption(run));
            GridBoardPresenter.Refresh(_board, run);

            Assert.That(run.LastRevealed, Is.EqualTo(correct));
            Assert.That(_board.TileAt(correct).State, Is.EqualTo(TileVisualState.Walked));
            Assert.That(_board.TileAt(new GridCoord(0, 0)).State, Is.EqualTo(TileVisualState.Walked));
        }

        [Test]
        public void AWrongTileIsNotLeftWhiteAfterPaint()
        {
            _board.Build(Size, _factory);
            var run = new GridWalkRun(TestPath());
            var wrong = WrongOption(run);

            run.Choose(wrong);
            GridBoardPresenter.Refresh(_board, run);

            Assert.That(_board.TileAt(wrong).State, Is.Not.EqualTo(TileVisualState.Walked));
            Assert.That(_board.TileAt(wrong).State, Is.Not.EqualTo(TileVisualState.Revealed));
        }

        [Test]
        public void RefreshPaintsThePathOverlayFromTheWalk()
        {
            _board.Build(Size, _factory);
            var run = new GridWalkRun(TestPath());
            run.Choose(run.Path.Cells[1]);
            GridBoardPresenter.Refresh(_board, run);

            Assert.That(_board.Overlay, Is.Not.Null);
            Assert.That(_board.Overlay.Trail.positionCount, Is.EqualTo(2));
            Assert.That(_board.Overlay.transform.Find(GridPathOverlay.HomeName).gameObject.activeSelf, Is.True);
        }

        [Test]
        public void VisibleOptionsCanHideTheImmediateParentAndDrawChoicePaths()
        {
            _board.Build(Size, _factory);
            var run = new GridWalkRun(TestPath());
            run.Choose(run.Path.Cells[1]);
            var visibleOptions = PathOptionFilter.VisibleGridOptions(run.WalkedCells, run.Options());

            GridBoardPresenter.Refresh(_board, run, visibleOptions: visibleOptions, showChoicePaths: true);

            Assert.That(_board.TileAt(run.Path.Cells[0]).State, Is.EqualTo(TileVisualState.Walked));
            Assert.That(_board.Overlay.transform.Find(GridPathOverlay.ChoicesName).childCount, Is.EqualTo(visibleOptions.Count));
        }

        [Test]
        public void ClassicDanceFloorTilesSitFlush()
        {
            var settings = ArenaVisualSettings.CreateClassicOverride();
            Assert.That(settings.TileGap, Is.EqualTo(0.02f));

            var layout = new BoardLayout(new GridSize(3, 3), settings.TileSize, settings.TileGap, Vector3.zero);
            Assert.That(layout.Pitch, Is.EqualTo(settings.TileSize).Within(0.0001f));
            Assert.That(layout.Gap, Is.EqualTo(0.02f));
        }

        [Test]
        public void ClassicTileHuesStaySaturated()
        {
            var hue = DanceFloorPalette.BaseColorFor(new GridCoord(0, 0));
            Assert.That(Mathf.Max(hue.r, hue.g, hue.b), Is.GreaterThan(0.8f));
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
