using Nixin.Locomotion;
using Nixin.Rail.Core;
using UnityEngine;

namespace Game.Unity
{
    /// <summary>
    /// Applies <see cref="LookYawLimit"/> after walk look, and draws edge fades at the stops.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class LookYawGate : MonoBehaviour
    {
        MazeWalker _walker;
        ArenaControls _controls;
        RailLocomotion _rail;
        FirstPersonController _fps;
        float _center;
        bool _hasCenter;

        public float LeftEdge { get; private set; }
        public float RightEdge { get; private set; }

        public void SetCenter(float yawDegrees)
        {
            _center = yawDegrees;
            _hasCenter = true;
        }

        void Awake()
        {
            Bind();
        }

        void Bind()
        {
            if (_walker == null)
                _walker = GetComponent<MazeWalker>();
            if (_controls == null)
                _controls = GetComponent<ArenaControls>();
            if (_rail == null)
                _rail = GetComponent<RailLocomotion>();
            if (_fps == null)
                _fps = GetComponent<FirstPersonController>();
        }

        void LateUpdate()
        {
            Bind();
            LeftEdge = 0f;
            RightEdge = 0f;
            if (_walker == null || !_walker.Walking)
                return;

            var enabled = _controls == null || _controls.LimitLookYaw;
            if (!enabled)
                return;

            if (!TryCurrentYaw(out var yaw))
                return;
            if (!_hasCenter)
                SetCenter(yaw);

            LookYawLimit.Tick(ref yaw, _center, Time.deltaTime, true, out var left, out var right);
            ApplyYaw(yaw);
            LeftEdge = left;
            RightEdge = right;
        }

        bool TryCurrentYaw(out float yaw)
        {
            yaw = 0f;
            if (_rail != null && _rail.Active)
            {
                yaw = _rail.Yaw;
                return true;
            }

            if (_fps != null && _fps.Active)
            {
                yaw = _fps.Yaw;
                return true;
            }

            return false;
        }

        void ApplyYaw(float yaw)
        {
            if (_rail != null && _rail.Active)
                _rail.SetYaw(yaw);
            else if (_fps != null && _fps.Active)
                _fps.SetYaw(yaw);
        }

        void OnGUI()
        {
            if (LeftEdge <= 0.01f && RightEdge <= 0.01f)
                return;
            if (LeftEdge > 0.01f)
                DrawStrip(true, LeftEdge);
            if (RightEdge > 0.01f)
                DrawStrip(false, RightEdge);
            GUI.color = Color.white;
        }

        static void DrawStrip(bool left, float amount)
        {
            var width = Mathf.Max(10f, Screen.width * 0.035f);
            const int steps = 10;
            var slice = width / steps;
            for (var i = 0; i < steps; i++)
            {
                var t = i / (float)(steps - 1);
                var a = amount * (1f - t) * 0.42f;
                GUI.color = new Color(0f, 0f, 0f, a);
                var x = left ? i * slice : Screen.width - width + i * slice;
                GUI.DrawTexture(new Rect(x, 0f, slice + 1f, Screen.height), Texture2D.whiteTexture);
            }
        }
    }
}
