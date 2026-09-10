using System;
using System.Collections.Generic;
using Nixin.Grid.Core;

namespace Game.Core.Rules
{
    /// <summary>
    /// Turns a finger sliding across cells into successive neighbour picks. Tap/swipe stay
    /// intact: nothing is emitted until the pointer leaves the cell it pressed on. A cell
    /// that is not a current option ends the gesture until the pointer lifts.
    /// </summary>
    public sealed class PathDragTracker
    {
        readonly List<GridCoord> _trail = new List<GridCoord>();
        bool _active;
        bool _broken;
        bool _hasOrigin;
        bool _leftOrigin;
        GridCoord _origin;
        GridCoord _lastAppended;
        int _next;

        public void Stop() => _broken = true;

        public bool IgnoreStroke => _active && (_leftOrigin || _broken);

        public void Feed(bool contacting, bool overUI, bool hasCell, GridCoord cell)
        {
            if (!contacting)
            {
                Clear();
                return;
            }

            if (!_active)
            {
                if (overUI)
                    return;

                _active = true;
                _broken = false;
                _leftOrigin = false;
                _next = 0;
                _trail.Clear();
                _hasOrigin = hasCell;
                if (hasCell)
                {
                    _origin = cell;
                    Append(cell);
                }

                return;
            }

            if (_broken || overUI || !hasCell)
                return;

            if (!_hasOrigin)
            {
                _hasOrigin = true;
                _origin = cell;
                Append(cell);
                return;
            }

            AppendLine(_lastAppended, cell);
        }

        public bool TryTake(GridCoord current, Func<GridCoord, bool> isOption, out GridCoord target)
        {
            target = default;
            if (!_active || _broken || !_leftOrigin || isOption == null)
                return false;

            while (_next < _trail.Count)
            {
                var cell = _trail[_next];
                _next++;

                if (cell.Equals(current))
                    continue;

                if (isOption(cell))
                {
                    target = cell;
                    return true;
                }

                _broken = true;
                return false;
            }

            return false;
        }

        void AppendLine(GridCoord from, GridCoord to)
        {
            if (from.Equals(to))
                return;

            if (from.X == to.X || from.Y == to.Y)
            {
                var dx = Math.Sign(to.X - from.X);
                var dy = Math.Sign(to.Y - from.Y);
                var x = from.X;
                var y = from.Y;
                while (x != to.X || y != to.Y)
                {
                    x += dx;
                    y += dy;
                    Append(new GridCoord(x, y));
                }

                return;
            }

            Append(to);
        }

        void Append(GridCoord cell)
        {
            if (_trail.Count > 0 && _trail[_trail.Count - 1].Equals(cell))
                return;

            _trail.Add(cell);
            _lastAppended = cell;
            if (_hasOrigin && !cell.Equals(_origin))
                _leftOrigin = true;
        }

        void Clear()
        {
            _active = false;
            _broken = false;
            _hasOrigin = false;
            _leftOrigin = false;
            _next = 0;
            _trail.Clear();
        }
    }
}
