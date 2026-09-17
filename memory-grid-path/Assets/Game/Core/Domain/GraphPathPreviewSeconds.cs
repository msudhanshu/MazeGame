namespace Game.Core.Domain
{
    /// <summary>
    /// Graph Arena glance window. Flash is a blink; radar is a short sweep.
    /// Later / harder graphs get less time. Tile Arena still uses <see cref="PathPreviewSeconds"/>.
    /// </summary>
    public static class GraphPathPreviewSeconds
    {
        public const float FlashBurst = 0.05f;
        public const float FlashShortest = 0.16f;
        public const float FlashLongest = 0.32f;
        public const float RadarShortest = 0.38f;
        public const float RadarLongest = 0.72f;

        const float PathWeight = 0.012f;
        const float TurnWeight = 0.016f;

        public static float Resolve(float authoredSeconds, int minPath, int minTurns, PathPreviewKind kind)
        {
            if (authoredSeconds > FlashBurst)
                return authoredSeconds;

            return For(minPath, minTurns, kind);
        }

        public static float For(int minPath, int minTurns, PathPreviewKind kind)
        {
            if (minPath < 2)
                minPath = 2;
            if (minTurns < 0)
                minTurns = 0;

            var longest = kind == PathPreviewKind.CameraFlash ? FlashLongest : RadarLongest;
            var shortest = kind == PathPreviewKind.CameraFlash ? FlashShortest : RadarShortest;
            var raw = longest - PathWeight * (minPath - 2) - TurnWeight * minTurns;
            if (raw < shortest)
                return shortest;
            if (raw > longest)
                return longest;
            return raw;
        }
    }
}
