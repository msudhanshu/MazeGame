using System.Collections.Generic;
using Game.Unity.Input;
using Nixin.Graph.Core;
using Nixin.Grid.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Game.Unity.Graph
{
    public sealed class GraphBoardInput
    {
        public const float NodePickRadius = 1.15f;
        public const float EdgePickRadius = 0.85f;

        readonly PointerStrokeTracker _stroke = new PointerStrokeTracker();

        public bool TryReadTarget(
            Camera camera,
            GraphBoardView board,
            GraphNodeId current,
            IReadOnlyList<GraphNodeId> options,
            out GraphNodeId target)
        {
            var strokeReady = _stroke.TryPoll(out _, out var strokeScreen, out _, out var overUI);
            target = default;
            if (board == null || !board.IsBuilt || options == null || options.Count == 0 || camera == null)
                return TryReadKeyboard(board, current, options, out target);

            if (TryPickReleasedPointer(camera, board, current, options, out target))
                return true;

            if (strokeReady && !overUI && TryPickScreen(camera, board, current, options, strokeScreen, out target))
                return true;

            return TryReadKeyboard(board, current, options, out target);
        }

        static bool TryPickReleasedPointer(
            Camera camera,
            GraphBoardView board,
            GraphNodeId current,
            IReadOnlyList<GraphNodeId> options,
            out GraphNodeId target)
        {
            target = default;
            if (!TryReadRelease(out var screen) || PlayfieldInputGate.BlocksPlayfield(screen))
                return false;

            return TryPickScreen(camera, board, current, options, screen, out target);
        }

        static bool TryPickScreen(
            Camera camera,
            GraphBoardView board,
            GraphNodeId current,
            IReadOnlyList<GraphNodeId> options,
            Vector2 screen,
            out GraphNodeId target)
        {
            return board.Layout.TryPickOptionUnderRay(
                camera.ScreenPointToRay(screen),
                current,
                options,
                NodePickRadius,
                EdgePickRadius,
                out target);
        }

        static bool TryReadRelease(out Vector2 screen)
        {
            screen = default;
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasReleasedThisFrame)
            {
                screen = mouse.position.ReadValue();
                return true;
            }

            if (!EnhancedTouchSupport.enabled)
                EnhancedTouchSupport.Enable();

            var touches = Touch.activeTouches;
            for (var i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                if (touch.phase != UnityEngine.InputSystem.TouchPhase.Ended
                    && touch.phase != UnityEngine.InputSystem.TouchPhase.Canceled)
                    continue;

                screen = touch.screenPosition;
                return true;
            }

            return false;
        }

        static bool TryReadKeyboard(
            GraphBoardView board,
            GraphNodeId current,
            IReadOnlyList<GraphNodeId> options,
            out GraphNodeId target)
        {
            target = default;
            var keyboard = Keyboard.current;
            if (keyboard == null || board == null || !board.IsBuilt)
                return false;

            Vector3 direction;
            if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
                direction = Vector3.right;
            else if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
                direction = Vector3.left;
            else if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
                direction = Vector3.forward;
            else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
                direction = Vector3.back;
            else
                return false;

            return TryPickOptionInDirection(board.Layout, current, options, direction, out target);
        }

        public static bool TryPickOptionInDirection(
            GraphBoardLayout layout,
            GraphNodeId current,
            IReadOnlyList<GraphNodeId> options,
            Vector3 worldDirection,
            out GraphNodeId target)
        {
            target = default;
            if (layout == null || options == null || options.Count == 0)
                return false;

            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude < 0.0001f)
                return false;
            worldDirection.Normalize();

            var origin = layout.WorldPosition(current);
            var bestScore = 0f;
            var found = false;
            for (var i = 0; i < options.Count; i++)
            {
                var delta = layout.WorldPosition(options[i]) - origin;
                delta.y = 0f;
                if (delta.sqrMagnitude < 0.0001f)
                    continue;

                var score = Vector3.Dot(delta.normalized, worldDirection);
                if (score <= 0f || (found && score <= bestScore))
                    continue;

                bestScore = score;
                target = options[i];
                found = true;
            }

            return found;
        }
    }
}
