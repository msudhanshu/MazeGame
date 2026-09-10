using System;
using System.Collections.Generic;
using Nixin.Grid.Core;
using Nixin.Maze.Core;
using Nixin.Rail.Core;

namespace Game.Core
{
    /// <summary>
    /// Centerline of every open maze corridor as an <see cref="IRail"/>.
    /// Positions sit on cell centers — path width is discarded. Node id is
    /// <c>x + y * width</c>.
    /// </summary>
    public sealed class CorridorRail : IRail
    {
        readonly MazeGrid _grid;

        public CorridorRail(
            MazeGrid grid,
            float cellSize,
            float originX = 0f,
            float originZ = 0f,
            float originY = 0f)
        {
            if (grid == null)
                throw new ArgumentNullException(nameof(grid));
            if (cellSize <= 0f)
                throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell size must be positive.");

            _grid = grid;
            CellSize = cellSize;
            OriginX = originX;
            OriginY = originY;
            OriginZ = originZ;
        }

        public MazeGrid Grid => _grid;
        public float CellSize { get; }
        public float OriginX { get; }
        public float OriginY { get; }
        public float OriginZ { get; }
        public float StopSpacing => CellSize;

        public int Id(GridCoord cell) => cell.X + cell.Y * _grid.Size.Width;

        public GridCoord Coord(int node)
        {
            var width = _grid.Size.Width;
            if (width <= 0)
                return default;
            return new GridCoord(node % width, node / width);
        }

        public bool Contains(GridCoord cell) => _grid.Size.Contains(cell);

        public bool Contains(int node)
        {
            if (node < 0 || node >= _grid.Size.CellCount)
                return false;
            return Contains(Coord(node));
        }

        public IReadOnlyList<GridCoord> Neighbors(GridCoord cell) => _grid.OpenNeighbors(cell);

        IReadOnlyList<int> IRail.Neighbors(int node)
        {
            if (!Contains(node))
                return Array.Empty<int>();
            return ToIds(Neighbors(Coord(node)));
        }

        public bool AreNeighbors(GridCoord a, GridCoord b) => _grid.IsOpen(a, b);

        bool IRail.AreNeighbors(int a, int b)
        {
            return Contains(a) && Contains(b) && AreNeighbors(Coord(a), Coord(b));
        }

        public void CellCenter(GridCoord cell, out float x, out float z)
        {
            if (!Contains(cell))
                throw new ArgumentOutOfRangeException(nameof(cell), cell, "Cell is outside the rail grid.");

            x = OriginX + cell.X * CellSize + CellSize * 0.5f;
            z = OriginZ + cell.Y * CellSize + CellSize * 0.5f;
        }

        void IRail.Position(int node, out float x, out float y, out float z)
        {
            CellCenter(Coord(node), out x, out z);
            y = OriginY;
        }

        public float EdgeLength(GridCoord from, GridCoord to)
        {
            EnsureEdge(from, to);
            return CellSize;
        }

        float IRail.EdgeLength(int from, int to) => EdgeLength(Coord(from), Coord(to));

        public void LerpEdge(GridCoord from, GridCoord to, float t, out float x, out float z)
        {
            EnsureEdge(from, to);
            if (t < 0f)
                t = 0f;
            else if (t > 1f)
                t = 1f;

            CellCenter(from, out var ax, out var az);
            CellCenter(to, out var bx, out var bz);
            x = ax + (bx - ax) * t;
            z = az + (bz - az) * t;
        }

        void IRail.LerpEdge(int from, int to, float t, out float x, out float y, out float z)
        {
            LerpEdge(Coord(from), Coord(to), t, out x, out z);
            y = OriginY;
        }

        public IReadOnlyList<GridCoord> AllCells()
        {
            var size = _grid.Size;
            var cells = new List<GridCoord>(size.CellCount);
            for (var y = 0; y < size.Height; y++)
            {
                for (var x = 0; x < size.Width; x++)
                    cells.Add(new GridCoord(x, y));
            }

            return cells;
        }

        IReadOnlyList<int> IRail.AllNodes()
        {
            return ToIds(AllCells());
        }

        public IReadOnlyList<RailEdge> OpenEdges()
        {
            var size = _grid.Size;
            var edges = new List<RailEdge>();
            for (var y = 0; y < size.Height; y++)
            {
                for (var x = 0; x < size.Width; x++)
                {
                    var cell = new GridCoord(x, y);
                    if (x + 1 < size.Width)
                    {
                        var east = new GridCoord(x + 1, y);
                        if (AreNeighbors(cell, east))
                            edges.Add(new RailEdge(cell, east));
                    }

                    if (y + 1 < size.Height)
                    {
                        var north = new GridCoord(x, y + 1);
                        if (AreNeighbors(cell, north))
                            edges.Add(new RailEdge(cell, north));
                    }
                }
            }

            return edges;
        }

        List<int> ToIds(IReadOnlyList<GridCoord> cells)
        {
            var ids = new List<int>(cells.Count);
            for (var i = 0; i < cells.Count; i++)
                ids.Add(Id(cells[i]));
            return ids;
        }

        void EnsureEdge(GridCoord from, GridCoord to)
        {
            if (!AreNeighbors(from, to))
                throw new ArgumentException($"Cells {from} and {to} are not an open corridor edge.");
        }
    }
}
