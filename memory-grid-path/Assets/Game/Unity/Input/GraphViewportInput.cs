using Game.Unity.Graph;
using Game.Unity.View;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Game.Unity.Input
{
    /// <summary>
    /// Pinch/scroll zoom and drag pan for graph arenas. Pan/zoom are clamped by the caller via
    /// <see cref="BoardViewport.Limits"/>.
    /// </summary>
    public sealed class GraphViewportInput
    {
        const float ScrollZoomScale = 0.12f;
        const float PinchZoomScale = 0.0045f;
        const float DragStartPixels = 14f;

        bool _panning;
        bool _blockedTap;
        bool _pinchActive;
        bool _eatPick;
        Vector2 _lastPanScreen;
        float _lastPinchDistance;
        Vector2 _singlePanStart;
        bool _singlePanArmed;

        public bool BlocksNodePick => _panning || _blockedTap || _eatPick;
        public bool DidZoom { get; private set; }
        public bool DidPan { get; private set; }

        public bool TryUpdate(
            Camera camera,
            GraphBoardLayout layout,
            ref BoardViewport.Framing framing,
            BoardViewport.Limits limits,
            bool allowInput)
        {
            DidZoom = false;
            DidPan = false;
            if (!allowInput || camera == null || layout == null)
            {
                EndPan();
                return false;
            }

            if (!PointerHeld())
            {
                _eatPick = _panning || _blockedTap;
                if (!_pinchActive)
                    EndPanKeepEat();
            }

            var aspect = camera.aspect;
            var changed = false;
            var blockedByUi = IsBlockedByUi();

            if (TryScrollZoom(camera, ref framing, limits, aspect))
            {
                DidZoom = true;
                changed = true;
            }

            if (blockedByUi)
            {
                if (!_pinchActive)
                    EndPanKeepEat();
                if (changed)
                    framing = BoardViewport.Clamp(framing, limits, aspect);
                return changed;
            }

            if (TryPinch(camera, ref framing, limits, aspect, out var pinchZoomed, out var pinchPanned))
            {
                DidZoom |= pinchZoomed;
                DidPan |= pinchPanned;
                changed = true;
            }
            else if (TryMousePan(camera, ref framing, limits, aspect))
            {
                DidPan = true;
                changed = true;
            }
            else if (TrySingleFingerPan(camera, ref framing, limits, aspect))
            {
                DidPan = true;
                changed = true;
            }
            else if (!_pinchActive && !PointerHeld())
                EndPanKeepEat();

            if (changed)
                framing = BoardViewport.Clamp(framing, limits, aspect);

            return changed;
        }

        bool TryScrollZoom(
            Camera camera,
            ref BoardViewport.Framing framing,
            BoardViewport.Limits limits,
            float aspect)
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return false;

            var scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Approximately(scroll, 0f))
                return false;

            var delta = -scroll * ScrollZoomScale * limits.DefaultOrthographicSize;
            framing = BoardViewport.ApplyZoom(
                framing,
                delta,
                mouse.position.ReadValue(),
                camera,
                limits,
                aspect);
            return true;
        }

        bool TryPinch(
            Camera camera,
            ref BoardViewport.Framing framing,
            BoardViewport.Limits limits,
            float aspect,
            out bool zoomed,
            out bool panned)
        {
            zoomed = false;
            panned = false;
            if (!EnhancedTouchSupport.enabled)
                EnhancedTouchSupport.Enable();

            var touches = Touch.activeTouches;
            if (touches.Count < 2)
            {
                _pinchActive = false;
                _lastPinchDistance = 0f;
                return false;
            }

            _pinchActive = true;
            _panning = true;
            _blockedTap = true;

            var a = touches[0].screenPosition;
            var b = touches[1].screenPosition;
            var center = (a + b) * 0.5f;
            var distance = Vector2.Distance(a, b);

            if (_lastPinchDistance <= 0.01f)
            {
                _lastPanScreen = center;
                _lastPinchDistance = distance;
                return false;
            }

            var delta = (_lastPinchDistance - distance) * PinchZoomScale * limits.DefaultOrthographicSize;
            var sizeBefore = framing.OrthographicSize;
            framing = BoardViewport.ApplyZoom(framing, delta, center, camera, limits, aspect);
            zoomed = !Mathf.Approximately(sizeBefore, framing.OrthographicSize);

            var panDelta = center - _lastPanScreen;
            if (panDelta.sqrMagnitude > 0.01f)
            {
                var focusBefore = framing.Focus;
                framing = BoardViewport.ApplyPan(
                    framing,
                    BoardViewport.ScreenDeltaToWorldDelta(panDelta, camera, framing.OrthographicSize),
                    limits,
                    aspect);
                panned = (framing.Focus - focusBefore).sqrMagnitude > 0.0001f;
            }

            _lastPanScreen = center;
            _lastPinchDistance = distance;
            return zoomed || panned;
        }

        bool TryMousePan(
            Camera camera,
            ref BoardViewport.Framing framing,
            BoardViewport.Limits limits,
            float aspect)
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return false;

            var keyboard = Keyboard.current;
            var spacePan = keyboard != null && keyboard.spaceKey.isPressed;
            var leftPan = mouse.leftButton.isPressed
                && (spacePan || BoardViewport.HasPanRoom(framing, limits, aspect));
            var held = mouse.middleButton.isPressed || mouse.rightButton.isPressed || leftPan;
            if (!held)
            {
                if (!_pinchActive)
                    _singlePanArmed = false;
                return false;
            }

            var screen = mouse.position.ReadValue();
            var pressedThisFrame = mouse.middleButton.wasPressedThisFrame
                || mouse.rightButton.wasPressedThisFrame
                || (leftPan && mouse.leftButton.wasPressedThisFrame);
            if (pressedThisFrame)
            {
                _singlePanStart = screen;
                _singlePanArmed = false;
                _lastPanScreen = screen;
                return false;
            }

            if (!_singlePanArmed)
            {
                if ((screen - _singlePanStart).magnitude < DragStartPixels)
                    return false;

                _singlePanArmed = true;
                _panning = true;
                _blockedTap = true;
                _lastPanScreen = screen;
                return false;
            }

            var delta = screen - _lastPanScreen;
            _lastPanScreen = screen;
            if (delta.sqrMagnitude < 0.01f)
                return false;

            framing = BoardViewport.ApplyPan(
                framing,
                BoardViewport.ScreenDeltaToWorldDelta(delta, camera, framing.OrthographicSize),
                limits,
                aspect);
            return true;
        }

        bool TrySingleFingerPan(
            Camera camera,
            ref BoardViewport.Framing framing,
            BoardViewport.Limits limits,
            float aspect)
        {
            if (!BoardViewport.HasPanRoom(framing, limits, aspect))
            {
                _singlePanArmed = false;
                return false;
            }

            if (!EnhancedTouchSupport.enabled)
                EnhancedTouchSupport.Enable();

            if (Touch.activeTouches.Count == 1)
                return TrySingleTouchPan(camera, Touch.activeTouches[0], ref framing, limits, aspect);

            return false;
        }

        bool TrySingleTouchPan(
            Camera camera,
            Touch touch,
            ref BoardViewport.Framing framing,
            BoardViewport.Limits limits,
            float aspect)
        {
            var screen = touch.screenPosition;
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                _singlePanStart = screen;
                _singlePanArmed = false;
                _lastPanScreen = screen;
                return false;
            }

            if (!_singlePanArmed)
            {
                if ((screen - _singlePanStart).magnitude < DragStartPixels)
                    return false;

                _singlePanArmed = true;
                _panning = true;
                _blockedTap = true;
                _lastPanScreen = screen;
                return false;
            }

            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended
                || touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                _singlePanArmed = false;
                return false;
            }

            var delta = screen - _lastPanScreen;
            _lastPanScreen = screen;
            if (delta.sqrMagnitude < 0.01f)
                return false;

            framing = BoardViewport.ApplyPan(
                framing,
                BoardViewport.ScreenDeltaToWorldDelta(delta, camera, framing.OrthographicSize),
                limits,
                aspect);
            return true;
        }

        static bool PointerHeld()
        {
            var mouse = Mouse.current;
            if (mouse != null
                && (mouse.leftButton.isPressed || mouse.middleButton.isPressed || mouse.rightButton.isPressed))
                return true;

            if (!EnhancedTouchSupport.enabled)
                EnhancedTouchSupport.Enable();

            return Touch.activeTouches.Count > 0;
        }

        static bool IsBlockedByUi()
        {
            if (!EnhancedTouchSupport.enabled)
                EnhancedTouchSupport.Enable();

            var touches = Touch.activeTouches;
            if (touches.Count >= 2)
                return false;

            if (touches.Count == 1)
                return PlayfieldInputGate.BlocksPlayfield(touches[0].screenPosition);

            var mouse = Mouse.current;
            if (mouse != null)
                return PlayfieldInputGate.BlocksPlayfield(mouse.position.ReadValue());

            return false;
        }

        void EndPanKeepEat()
        {
            _panning = false;
            _singlePanArmed = false;
            _lastPinchDistance = 0f;
            _pinchActive = false;
            _blockedTap = false;
        }

        void EndPan()
        {
            EndPanKeepEat();
            _eatPick = false;
        }
    }
}
