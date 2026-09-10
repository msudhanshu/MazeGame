using Nixin.Grid.Core;
using UnityEngine;

namespace Game.Unity.View
{
    /// <summary>Eases for the level-start arena intro. Pure math so tests can pin the feel.</summary>
    public static class ArenaIntroMotion
    {
        public const float GridTileSeconds = 0.4f;
        public const float GridStaggerSeconds = 0.05f;
        public const float GridMaxTotalSeconds = 1.15f;
        public const float GraphSeconds = 1.05f;
        public const float GraphPeakBrightness = 1.48f;
        public const float GraphStartScale = 0.68f;
        public const float GraphPeakScale = 1.24f;
        public const float GraphUndershootScale = 0.9f;

        public static float StaggerForCount(int count)
        {
            if (count <= 1)
                return 0f;
            var stagger = GridStaggerSeconds;
            var total = GridTileSeconds + (count - 1) * stagger;
            if (total > GridMaxTotalSeconds)
                stagger = (GridMaxTotalSeconds - GridTileSeconds) / (count - 1);
            return Mathf.Max(0.012f, stagger);
        }

        public static float GridTotalSeconds(int tileCount) =>
            GridTileSeconds + Mathf.Max(0, tileCount - 1) * StaggerForCount(tileCount);

        public static int WaveIndex(GridCoord coord, GridCoord origin) =>
            Mathf.Abs(coord.X - origin.X) + Mathf.Abs(coord.Y - origin.Y);

        public static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        /// <summary>0 → 1.12 → 1 so tiles pop into place.</summary>
        public static float AssembleScale(float t)
        {
            t = Mathf.Clamp01(t);
            if (t <= 0f)
                return 0f;
            if (t < 0.72f)
                return Mathf.Lerp(0f, 1.12f, Smooth(t / 0.72f));
            return Mathf.Lerp(1.12f, 1f, Smooth((t - 0.72f) / 0.28f));
        }

        public static float AssembleDrop(float t) => 1f - Smooth(t);

        public static float EaseInQuart(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * t * t;
        }

        /// <summary>Black → late accelerating punch → rest. Peak lands with the scale bounce.</summary>
        public static float GraphBrightness(float t)
        {
            t = Mathf.Clamp01(t);
            if (t < 0.52f)
                return Mathf.Lerp(0f, GraphPeakBrightness, EaseInQuart(t / 0.52f));
            return Mathf.Lerp(GraphPeakBrightness, 1f, Smooth((t - 0.52f) / 0.48f));
        }

        /// <summary>Long wind-up, then a fast overshoot and a short settle bounce.</summary>
        public static float GraphScale(float t)
        {
            t = Mathf.Clamp01(t);
            if (t < 0.52f)
                return Mathf.Lerp(GraphStartScale, GraphPeakScale, EaseInQuart(t / 0.52f));
            if (t < 0.74f)
                return Mathf.Lerp(GraphPeakScale, GraphUndershootScale, Smooth((t - 0.52f) / 0.22f));
            return Mathf.Lerp(GraphUndershootScale, 1f, Smooth((t - 0.74f) / 0.26f));
        }

        public static Vector3 ScaleAround(Vector3 point, Vector3 origin, float scale) =>
            origin + (point - origin) * scale;
    }
}
