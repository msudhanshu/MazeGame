using System.Collections.Generic;
using Game.Core;
using Nixin.Grid.Core;
using Nixin.Maze;
using Nixin.Rail;
using Nixin.Rail.Core;
using UnityEngine;

namespace Game.Unity
{
    /// <summary>
    /// Floor-tile chips on every cell, or space-blob pins on the next tap targets.
    /// </summary>
    public sealed class FloorWaypoints : MonoBehaviour, IRailWaypointView
    {
        readonly List<FloorWaypointMarker> _pool = new List<FloorWaypointMarker>();
        Transform _root;
        FloorPathGlow _glow;
        CorridorRail _glowRail;
        WaypointStyle _builtStyle = WaypointStyle.FloorTile;
        IntermediateWaypointMode _mode = IntermediateWaypointMode.Skip;
        bool _running;
        bool _ownedRoot;

        public MazeArena Arena;
        public float MarkerLift => FloorWaypointMarker.FloorLift;

        public void SetRunning(bool running)
        {
            _running = running;
            if (_glow != null)
                _glow.SetRunning(running && _builtStyle == WaypointStyle.FloorTile);
            if (!running)
            {
                _glowRail = null;
                Clear();
            }
        }

        public void Clear()
        {
            for (var i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null)
                    _pool[i].gameObject.SetActive(false);
            }
        }

        public void Sync(RailTravel travel, WaypointStyle style, IntermediateWaypointMode intermediates)
        {
            var corridor = travel != null ? travel.Rail as CorridorRail : null;
            Sync(travel, Arena, corridor, style, intermediates);
        }

        public void Sync(
            RailTravel travel,
            MazeArena arena,
            CorridorRail rail,
            WaypointStyle style,
            IntermediateWaypointMode intermediates)
        {
            if (!_running || travel == null || rail == null || arena == null || arena.Dimensions == null)
            {
                Clear();
                return;
            }

            EnsureRoot();
            if (_builtStyle != style)
            {
                RebuildPool();
                _builtStyle = style;
                _glowRail = null;
            }

            _mode = intermediates;
            var cells = CellsFor(travel, rail, style, intermediates);
            var y = arena.Dimensions.Origin.y;
            for (var i = 0; i < cells.Count; i++)
            {
                var marker = Ensure(i, style);
                var cell = cells[i];
                marker.Cell = cell;
                rail.CellCenter(cell, out var x, out var z);
                marker.transform.position = new Vector3(x, y, z);
                marker.gameObject.SetActive(true);
                marker.SetKind(KindFor(travel, rail, cell));
            }

            for (var i = cells.Count; i < _pool.Count; i++)
            {
                if (_pool[i] != null)
                    _pool[i].gameObject.SetActive(false);
            }

            var glowOn = style == WaypointStyle.FloorTile;
            if (_glow != null)
            {
                _glow.SetRunning(glowOn);
                if (glowOn && !ReferenceEquals(_glowRail, rail))
                {
                    _glowRail = rail;
                    _glow.Rebuild(rail, y);
                }
            }
        }

        public bool TryPick(Camera camera, Vector2 screen, RailTravel travel, out int node)
        {
            node = 0;
            var rail = travel != null ? travel.Rail as CorridorRail : null;
            if (rail == null || !TryPick(camera, screen, travel, rail, out var cell))
                return false;
            node = rail.Id(cell);
            return true;
        }

        public bool TryPick(
            Camera camera,
            Vector2 screen,
            RailTravel travel,
            CorridorRail rail,
            out GridCoord cell)
        {
            cell = default;
            if (camera == null || travel == null || rail == null || travel.IsMoving)
                return false;

            FloorWaypointMarker best = null;
            var bestDist = float.MaxValue;
            var shortSide = Mathf.Min(Screen.width, Screen.height);
            var max = shortSide * 0.16f;
            var maxSq = max * max;
            for (var i = 0; i < _pool.Count; i++)
            {
                var marker = _pool[i];
                if (marker == null || !marker.gameObject.activeInHierarchy)
                    continue;
                if (!StraightRuns.IsClickable(travel.Rail, travel.Node, rail.Id(marker.Cell), _mode))
                    continue;

                var sp = camera.WorldToScreenPoint(marker.PickPoint);
                if (sp.z <= 0.1f)
                    continue;

                var dx = sp.x - screen.x;
                var dy = sp.y - screen.y;
                var dist = dx * dx + dy * dy;
                if (dist < bestDist && dist <= maxSq)
                {
                    bestDist = dist;
                    best = marker;
                }
            }

            if (best == null)
                return false;

            cell = best.Cell;
            return true;
        }

        static List<GridCoord> CellsFor(
            RailTravel travel,
            CorridorRail rail,
            WaypointStyle style,
            IntermediateWaypointMode mode)
        {
            if (style == WaypointStyle.SpaceBlob)
            {
                if (travel.IsMoving)
                    return new List<GridCoord> { rail.Coord(travel.PathTarget) };
                return Coords(rail, StraightRuns.Clickable(travel.Rail, travel.Node, mode));
            }

            if (mode == IntermediateWaypointMode.Remove)
            {
                var cells = Coords(rail, StraightRuns.Clickable(travel.Rail, travel.Node, mode));
                if (!ContainsCell(cells, rail.Coord(travel.Node)))
                    cells.Insert(0, rail.Coord(travel.Node));
                if (travel.IsMoving && !ContainsCell(cells, rail.Coord(travel.PathTarget)))
                    cells.Add(rail.Coord(travel.PathTarget));
                return cells;
            }

            var all = rail.AllCells();
            var copy = new List<GridCoord>(all.Count);
            for (var i = 0; i < all.Count; i++)
                copy.Add(all[i]);
            return copy;
        }

        FloorChipKind KindFor(RailTravel travel, CorridorRail rail, GridCoord cell)
        {
            if (travel.IsMoving && cell.Equals(rail.Coord(travel.PathTarget)))
                return FloorChipKind.Destination;
            if (!travel.IsMoving && cell.Equals(rail.Coord(travel.Node)))
                return FloorChipKind.Occupied;
            if (!travel.IsMoving && StraightRuns.IsClickable(travel.Rail, travel.Node, rail.Id(cell), _mode))
                return FloorChipKind.Reachable;
            return FloorChipKind.Path;
        }

        static List<GridCoord> Coords(CorridorRail rail, List<int> nodes)
        {
            var cells = new List<GridCoord>(nodes.Count);
            for (var i = 0; i < nodes.Count; i++)
                cells.Add(rail.Coord(nodes[i]));
            return cells;
        }

        static bool ContainsCell(IReadOnlyList<GridCoord> cells, GridCoord cell)
        {
            for (var i = 0; i < cells.Count; i++)
            {
                if (cells[i].Equals(cell))
                    return true;
            }

            return false;
        }

        FloorWaypointMarker Ensure(int index, WaypointStyle style)
        {
            while (_pool.Count <= index)
                _pool.Add(CreateMarker(style));
            if (_pool[index] == null)
                _pool[index] = CreateMarker(style);
            return _pool[index];
        }

        FloorWaypointMarker CreateMarker(WaypointStyle style)
        {
            EnsureRoot();
            return FloorWaypointMarker.Create(_root, style);
        }

        void RebuildPool()
        {
            for (var i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] == null)
                    continue;
                if (Application.isPlaying)
                    Object.Destroy(_pool[i].gameObject);
                else
                    Object.DestroyImmediate(_pool[i].gameObject);
            }

            _pool.Clear();
        }

        void EnsureRoot()
        {
            if (_root != null)
                return;
            var go = GameObject.Find("FloorWaypoints");
            if (go == null)
            {
                go = new GameObject("FloorWaypoints");
                _ownedRoot = true;
            }

            _root = go.transform;
            _glow = go.GetComponent<FloorPathGlow>();
            if (_glow == null)
                _glow = go.AddComponent<FloorPathGlow>();
        }

        void OnDestroy()
        {
            if (!_ownedRoot || _root == null)
                return;
            if (Application.isPlaying)
                Destroy(_root.gameObject);
            else
                DestroyImmediate(_root.gameObject);
        }

        void OnDisable()
        {
            Clear();
        }
    }
}
