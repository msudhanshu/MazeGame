using System.Collections.Generic;
using Game.Unity.View;
using NUnit.Framework;
using Nixin.Grid.Core;
using UnityEngine;

namespace Game.Unity.Tests
{
    [TestFixture]
    public class GridBoardViewTests
    {
        GameObject _host;
        GridBoardView _board;
        FakeTileViewFactory _factory;

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
        public void BuildsOneTilePerCell()
        {
            _board.Build(new GridSize(4, 3), _factory);

            Assert.That(_board.IsBuilt, Is.True);
            Assert.That(_board.Tiles.Count, Is.EqualTo(12));
            Assert.That(_factory.Created.Count, Is.EqualTo(12));
            Assert.That(_board.Overlay, Is.Not.Null);

            for (var y = 0; y < 3; y++)
            {
                for (var x = 0; x < 4; x++)
                    Assert.That(_board.TileAt(new GridCoord(x, y)), Is.Not.Null);
            }
        }

        [Test]
        public void TilesStartIdleAndAreBuiltAtTheirLayoutPosition()
        {
            _board.Build(new GridSize(3, 3), _factory);

            foreach (var tile in _factory.Created)
            {
                Assert.That(tile.State, Is.EqualTo(TileVisualState.Idle));
                Assert.That(tile.BuiltAt, Is.EqualTo(_board.WorldPosition(tile.Coord)));
            }
        }

        [Test]
        public void SettingAStateOnlyTouchesThatTile()
        {
            _board.Build(new GridSize(3, 3), _factory);

            _board.SetState(new GridCoord(1, 2), TileVisualState.Revealed);

            Assert.That(_board.TileAt(new GridCoord(1, 2)).State, Is.EqualTo(TileVisualState.Revealed));
            Assert.That(_board.TileAt(new GridCoord(0, 0)).State, Is.EqualTo(TileVisualState.Idle));
        }

        [Test]
        public void SetAllRepaintsEveryTile()
        {
            _board.Build(new GridSize(3, 3), _factory);
            _board.SetState(new GridCoord(0, 0), TileVisualState.Wrong);

            _board.SetAll(TileVisualState.Idle);

            foreach (var tile in _board.Tiles.Values)
                Assert.That(tile.State, Is.EqualTo(TileVisualState.Idle));
        }

        [Test]
        public void CellsOutsideTheBoardHaveNoTile()
        {
            _board.Build(new GridSize(3, 3), _factory);

            Assert.That(_board.TileAt(new GridCoord(9, 9)), Is.Null);
            Assert.DoesNotThrow(() => _board.SetState(new GridCoord(9, 9), TileVisualState.Walked));
        }

        [Test]
        public void RebuildingReplacesTheOldTiles()
        {
            _board.Build(new GridSize(3, 3), _factory);
            var first = new List<FakeTileView>(_factory.Created);

            _board.Build(new GridSize(5, 5), _factory);

            Assert.That(_board.Tiles.Count, Is.EqualTo(25));
            foreach (var tile in first)
                Assert.That(tile.Destroyed, Is.True);
        }

        [Test]
        public void ClearingDestroysEveryTile()
        {
            _board.Build(new GridSize(3, 3), _factory);

            _board.Clear();

            Assert.That(_board.IsBuilt, Is.False);
            Assert.That(_board.Tiles, Is.Empty);
            foreach (var tile in _factory.Created)
                Assert.That(tile.Destroyed, Is.True);
        }

        [Test]
        public void PickingResolvesARayToATile()
        {
            _board.Build(new GridSize(5, 5), _factory);
            var target = new GridCoord(4, 0);
            var ray = new Ray(_board.WorldPosition(target) + Vector3.up * 8f, Vector3.down);

            Assert.That(_board.TryPick(ray, out var picked), Is.True);
            Assert.That(picked, Is.EqualTo(target));
        }

        sealed class FakeTileView : ITileView
        {
            public FakeTileView(GridCoord coord, Vector3 builtAt)
            {
                Coord = coord;
                BuiltAt = builtAt;
            }

            public GridCoord Coord { get; }
            public Vector3 BuiltAt { get; }
            public TileVisualState State { get; private set; } = TileVisualState.Idle;
            public bool Destroyed { get; private set; }

            public void SetState(TileVisualState state) => State = state;

            public void Flash(TileVisualState state, float seconds)
            {
            }

            public void Destroy() => Destroyed = true;
        }

        sealed class FakeTileViewFactory : ITileViewFactory
        {
            public List<FakeTileView> Created { get; } = new List<FakeTileView>();

            public string ThemeId => "fake";

            public ITileView CreateTile(GridCoord coord, GridSize size, Vector3 worldPosition, float tileSize, Transform parent)
            {
                var tile = new FakeTileView(coord, worldPosition);
                Created.Add(tile);
                return tile;
            }

            public void ApplyEnvironment(Camera camera, BoardLayout layout, Transform parent)
            {
            }
        }
    }
}
