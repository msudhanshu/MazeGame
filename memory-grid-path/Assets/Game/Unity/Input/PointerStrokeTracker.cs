using Nixin.Grid.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Game.Unity.Input
{
    /// <summary>
    /// Follows one finger or mouse button from down to up and reports a tap or a cardinal
    /// swipe. Always sample, including idle frames: a missed lift is what made every other
    /// swipe a no-op on phones.
    /// </summary>
    public sealed class PointerStrokeTracker
    {
        readonly StrokeTracker _tracker = new StrokeTracker();

        public PointerStrokeTracker()
        {
            if (!EnhancedTouchSupport.enabled)
                EnhancedTouchSupport.Enable();
        }

        public bool TryComplete(out StrokeResult result, out Vector2 screenPosition)
        {
            return TryPoll(out result, out screenPosition, out _, out _);
        }

        public bool TryPoll(out StrokeResult result, out Vector2 screenPosition, out bool contacting, out bool overUI)
        {
            result = StrokeResult.None;
            var sample = ReadSample();
            contacting = sample.Contact || sample.Pressed;
            overUI = sample.OverUI;
            screenPosition = sample.HasPosition ? sample.Position : default;

            var thresholds = StrokeThresholds.ForDisplay(Screen.dpi, Mathf.Min(Screen.width, Screen.height));
            return _tracker.Feed(ToStroke(sample), thresholds, out result);
        }

        static StrokeSample ToStroke(PointerSample sample)
        {
            return new StrokeSample(
                sample.Contact,
                sample.Pressed,
                sample.Released,
                sample.HasPosition,
                sample.Position.x,
                sample.Position.y,
                sample.OverUI);
        }

        static PointerSample ReadSample()
        {
            if (TryReadTouch(out var touch))
                return touch;
            if (TryReadPointer(Pointer.current, out var pointer))
                return pointer;
            if (TryReadPointer(Mouse.current, out var mouse))
                return mouse;
            return default;
        }

        static bool TryReadTouch(out PointerSample sample)
        {
            sample = default;
            if (Touchscreen.current == null)
                return false;
            if (!EnhancedTouchSupport.enabled)
                EnhancedTouchSupport.Enable();

            var touches = Touch.activeTouches;
            for (var i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                var phase = touch.phase;
                if (phase == UnityEngine.InputSystem.TouchPhase.None)
                    continue;

                var began = phase == UnityEngine.InputSystem.TouchPhase.Began;
                var ended = phase == UnityEngine.InputSystem.TouchPhase.Ended
                    || phase == UnityEngine.InputSystem.TouchPhase.Canceled;
                sample = new PointerSample(
                    touch.screenPosition,
                    contact: !ended,
                    began,
                    ended,
                    hasPosition: true,
                    OverUI(touch.screenPosition));
                return true;
            }

            return false;
        }

        static bool TryReadPointer(Pointer pointer, out PointerSample sample)
        {
            sample = default;
            if (pointer == null)
                return false;

            var pressed = pointer.press.wasPressedThisFrame;
            var released = pointer.press.wasReleasedThisFrame;
            var held = pointer.press.isPressed;
            if (!pressed && !released && !held)
                return false;

            var screen = pointer.position.ReadValue();
            sample = new PointerSample(
                screen,
                contact: held || pressed,
                pressed,
                released,
                hasPosition: true,
                OverUI(screen));
            return true;
        }

        static bool OverUI(Vector2 screenPosition)
        {
            return PlayfieldInputGate.BlocksPlayfield(screenPosition);
        }

        readonly struct PointerSample
        {
            public PointerSample(Vector2 position, bool contact, bool pressed, bool released, bool hasPosition, bool overUI)
            {
                Position = position;
                Contact = contact;
                Pressed = pressed;
                Released = released;
                HasPosition = hasPosition;
                OverUI = overUI;
            }

            public Vector2 Position { get; }
            public bool Contact { get; }
            public bool Pressed { get; }
            public bool Released { get; }
            public bool HasPosition { get; }
            public bool OverUI { get; }
        }
    }
}
