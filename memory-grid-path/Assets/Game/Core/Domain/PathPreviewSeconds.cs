using System;

namespace Game.Core.Domain
{
    /// <summary>
    /// How long the radar path preview sweeps, plus a hold after the path is fully shown.
    /// Early Tile Arena levels get extra sweep and hold so a new player can memorize.
    /// </summary>
    public static class PathPreviewSeconds
    {
        public const float Longest = 1.65f;
        public const float Shortest = 0.72f;
        public const float TeachingLongest = 3.4f;
        public const float Hold = 0.12f;

        /// <summary>
        /// Tile and Graph radar get hold-to-pause only after this teaching band.
        /// Scout scan tours keep pause on every level.
        /// </summary>
        public const int ScanPauseAfterLevel = 10;

        const float HeightWeight = 0.12f;
        const float TurnWeight = 0.035f;

        public static float For(LevelDefinition level)
        {
            if (level == null)
                throw new ArgumentNullException(nameof(level));

            var glance = For(level.Size.Height, level.Shape.MinTurns) + TeachingSweepBonus(level.Number);
            if (glance < Shortest)
                return Shortest;
            if (glance > TeachingLongest)
                return TeachingLongest;
            return glance;
        }

        public static float HoldFor(LevelDefinition level)
        {
            if (level == null)
                throw new ArgumentNullException(nameof(level));

            return HoldFor(level.Number);
        }

        public static bool UsesScanPause(LevelDefinition level)
        {
            if (level == null)
                throw new ArgumentNullException(nameof(level));

            return UsesScanPause(level.Number);
        }

        public static bool UsesScanPause(int levelNumber) =>
            levelNumber > ScanPauseAfterLevel;

        public static float HoldFor(int levelNumber)
        {
            if (levelNumber <= 1)
                return 1.5f;
            if (levelNumber == 2)
                return 1.35f;
            if (levelNumber == 3)
                return 1.2f;
            if (levelNumber == 4)
                return 0.5f;
            if (levelNumber == 5)
                return 0.4f;
            if (levelNumber <= 10)
                return 0.22f;
            return Hold;
        }

        static float TeachingSweepBonus(int levelNumber)
        {
            if (levelNumber <= 1)
                return 2.2f;
            if (levelNumber == 2)
                return 2.0f;
            if (levelNumber == 3)
                return 1.8f;
            if (levelNumber == 4)
                return 0.8f;
            if (levelNumber == 5)
                return 0.5f;
            if (levelNumber <= 10)
                return 0.25f;
            return 0f;
        }

        public static float For(int height, int minTurns)
        {
            if (height < 1)
                height = 1;
            if (minTurns < 0)
                minTurns = 0;

            var raw = HeightWeight * height + TurnWeight * minTurns;
            if (raw < Shortest)
                return Shortest;
            if (raw > Longest)
                return Longest;
            return raw;
        }

        public static float ForLevel(int levelNumber)
        {
            var catalog = new LevelCatalog();
            if (levelNumber < 1)
                levelNumber = 1;
            if (levelNumber > catalog.Count)
                levelNumber = catalog.Count;

            return For(catalog.Get(levelNumber));
        }
    }
}
