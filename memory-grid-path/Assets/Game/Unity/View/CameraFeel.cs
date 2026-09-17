using UnityEngine;

namespace Game.Unity.View
{
    /// <summary>
    /// Camera punch and shake applied after framing so LateUpdate cannot wipe it.
    /// </summary>
    public static class CameraFeel
    {
        static Vector3 _punch;
        static float _punchUntil;
        static float _shakeAmp;
        static float _shakeUntil;

        public static void Punch(Vector3 worldDelta, float seconds)
        {
            _punch = worldDelta;
            _punchUntil = Time.unscaledTime + Mathf.Max(0.04f, seconds);
        }

        public static void Shake(float amplitude, float seconds)
        {
            _shakeAmp = amplitude;
            _shakeUntil = Time.unscaledTime + Mathf.Max(0.04f, seconds);
        }

        public static void Apply(Camera camera)
        {
            if (camera == null)
                return;

            var offset = Vector3.zero;
            var now = Time.unscaledTime;
            if (now < _punchUntil)
            {
                var t = (_punchUntil - now) / Mathf.Max(0.04f, _punchUntil - (now - Time.unscaledDeltaTime));
                offset += _punch * Mathf.Clamp01(t);
            }

            if (now < _shakeUntil)
            {
                var falloff = (_shakeUntil - now) / 0.18f;
                offset += new Vector3(
                    (Mathf.PerlinNoise(now * 42f, 0.1f) - 0.5f) * 2f,
                    0f,
                    (Mathf.PerlinNoise(0.2f, now * 38f) - 0.5f) * 2f) * _shakeAmp * Mathf.Clamp01(falloff);
            }

            camera.transform.position += offset;
        }
    }
}
