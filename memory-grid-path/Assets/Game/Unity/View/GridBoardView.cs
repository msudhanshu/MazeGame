using System.Collections.Generic;
using Nixin.Grid.Core;
using UnityEngine;

namespace Game.Unity.View
{
    /// <summary>
    /// Owns the tiles for one board. It knows nothing about the rules; the play loop tells it
    /// which tile is in which state.
    /// </summary>
    public sealed class GridBoardView : MonoBehaviour
    {
        readonly Dictionary<GridCoord, ITileView> _tiles = new Dictionary<GridCoord, ITileView>();
        GridPathOverlay _overlay;

        public BoardLayout Layout { get; private set; }
        public bool IsBuilt { get; private set; }
        public GridPathOverlay Overlay => _overlay;

        public IReadOnlyDictionary<GridCoord, ITileView> Tiles => _tiles;

        public void Build(GridSize size, ITileViewFactory factory, float tileSize = 1f, float gap = 0f)
        {
            Clear();

            Layout = new BoardLayout(size, tileSize, gap, transform.position);

            for (var y = 0; y < size.Height; y++)
            {
                for (var x = 0; x < size.Width; x++)
                {
                    var coord = new GridCoord(x, y);
                    _tiles[coord] = factory.CreateTile(coord, size, Layout.WorldPosition(coord), tileSize, transform);
                }
            }

            IsBuilt = true;
            _overlay = GridPathOverlay.Ensure(transform);
        }

        public ITileView TileAt(GridCoord coord) => _tiles.TryGetValue(coord, out var tile) ? tile : null;

        public void SetState(GridCoord coord, TileVisualState state) => TileAt(coord)?.SetState(state);

        public void SetAll(TileVisualState state)
        {
            foreach (var tile in _tiles.Values)
                tile.SetState(state);
        }

        public Vector3 WorldPosition(GridCoord coord) => Layout.WorldPosition(coord);

        public bool TryPick(Ray ray, out GridCoord coord) => Layout.TryCoordUnderRay(ray, out coord);

        public void Clear()
        {
            foreach (var tile in _tiles.Values)
                tile.Destroy();

            _tiles.Clear();
            IsBuilt = false;
            if (_overlay)
                _overlay.ResetVisuals();
        }

        void OnDestroy() => Clear();
    }
}
