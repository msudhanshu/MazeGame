using System;
using System.Collections.Generic;
using Game.Core.Domain;
using Game.Core.Rules;
using Game.Unity.View;
using Nixin.Grid.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Unity.Input
{
    /// <summary>
    /// Player intent for the grid: tap/click a neighbouring tile, swipe toward one,
    /// drag across tiles when that option is on, or (Editor / laptop testing only) use
    /// the arrow keys.
    /// </summary>
    public sealed class BoardInput
    {
        public const float CandidateTilePickRadius = 0.42f;
        public const float CandidateEdgePickRadius = 0.26f;

        readonly PointerStrokeTracker _stroke = new PointerStrokeTracker();
        readonly PathDragTracker _pathDrag = new PathDragTracker();

        public void StopPathDrag() => _pathDrag.Stop();

        public bool TryReadTarget(Camera camera, GridBoardView board, GridCoord current, out GridCoord target)
        {
            return TryReadTarget(
                camera,
                board,
                current,
                visibleOptions: null,
                allowPathDrag: false,
                isOption: null,
                out target);
        }

        public bool TryReadTarget(
            Camera camera,
            GridBoardView board,
            GridCoord current,
            IReadOnlyList<GridCoord> visibleOptions,
            out GridCoord target)
        {
            return TryReadTarget(
                camera,
                board,
                current,
                visibleOptions,
                allowPathDrag: false,
                isOption: null,
                out target);
        }

        public bool TryReadTarget(
            Camera camera,
            GridBoardView board,
            GridCoord current,
            bool allowPathDrag,
            Func<GridCoord, bool> isOption,
            out GridCoord target)
        {
            return TryReadTarget(
                camera,
                board,
                current,
                visibleOptions: null,
                allowPathDrag,
                isOption,
                out target);
        }

        public bool TryReadTarget(
            Camera camera,
            GridBoardView board,
            GridCoord current,
            IReadOnlyList<GridCoord> visibleOptions,
            bool allowPathDrag,
            Func<GridCoord, bool> isOption,
            out GridCoord target)
        {
            var strokeReady = _stroke.TryPoll(out var stroke, out var screenPosition, out var contacting, out var overUI);
            var isVisibleOption = visibleOptions != null
                ? new Func<GridCoord, bool>(coord => PathOptionFilter.Contains(visibleOptions, coord))
                : isOption;
            target = default;
            if (board == null || !board.IsBuilt)
                return false;

            if (allowPathDrag)
            {
                var hover = default(GridCoord);
                var hasCell = camera != null
                    && contacting
                    && board.Layout.TryCoordUnderRay(camera.ScreenPointToRay(screenPosition), out hover);
                _pathDrag.Feed(contacting, overUI, hasCell, hover);
                if (_pathDrag.TryTake(current, isVisibleOption, out target))
                    return true;
            }
            else
            {
                _pathDrag.Feed(false, false, false, default);
            }

            if (!(allowPathDrag && _pathDrag.IgnoreStroke) &&
                strokeReady &&
                stroke.Kind == StrokeKind.Tap &&
                camera != null &&
                board.Layout.TryPickOptionUnderRay(
                    camera.ScreenPointToRay(screenPosition),
                    current,
                    visibleOptions,
                    CandidateTilePickRadius,
                    CandidateEdgePickRadius,
                    out target))
            {
                return true;
            }

            if (!(allowPathDrag && _pathDrag.IgnoreStroke) &&
                strokeReady &&
                BoardMove.TryResolve(stroke, screenPosition, camera, board.Layout, current, out target))
            {
                return isVisibleOption == null || isVisibleOption(target);
            }

            if (!TryReadKeyboard(board.Layout.Size, current, out target))
                return false;

            return isVisibleOption == null || isVisibleOption(target);
        }

        static bool TryReadKeyboard(GridSize size, GridCoord current, out GridCoord target)
        {
            target = default;

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return false;

            var dx = 0;
            var dy = 0;

            if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
                dx = 1;
            else if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
                dx = -1;
            else if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
                dy = 1;
            else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
                dy = -1;
            else
                return false;

            var candidate = current.Offset(dx, dy);
            if (!size.Contains(candidate))
                return false;

            target = candidate;
            return true;
        }
    }
}
