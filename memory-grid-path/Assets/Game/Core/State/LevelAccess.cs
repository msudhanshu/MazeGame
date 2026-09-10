using System;

namespace Game.Core.State
{
    public enum LevelLane
    {
        Locked,
        Current,
        Cleared
    }

    /// <summary>
    /// How a level should read on the select grid: locked, the one in play, or already cleared.
    /// Stars follow the Figma board — three on a clear, none until then.
    /// </summary>
    public static class LevelAccess
    {
        public const int StarsPerClear = 3;

        public static LevelLane Lane(PlayerProgress progress, int levelNumber)
        {
            if (progress == null)
                throw new ArgumentNullException(nameof(progress));
            if (levelNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(levelNumber));

            if (levelNumber > progress.HighestUnlockedLevel)
                return LevelLane.Locked;
            if (levelNumber < progress.HighestUnlockedLevel)
                return LevelLane.Cleared;
            return LevelLane.Current;
        }

        public static int StarsOn(LevelLane lane) => lane == LevelLane.Cleared ? StarsPerClear : 0;

        public static int StarsEarned(PlayerProgress progress)
        {
            if (progress == null)
                throw new ArgumentNullException(nameof(progress));
            return Math.Max(0, progress.HighestUnlockedLevel - 1) * StarsPerClear;
        }

        public static int StarsPossible(int levelCount)
        {
            if (levelCount < 1)
                throw new ArgumentOutOfRangeException(nameof(levelCount));
            return levelCount * StarsPerClear;
        }

        /// <summary>
        /// How many levels are finished. Completing the last catalog row still counts even
        /// though <see cref="PlayerProgress.HighestUnlockedLevel"/> cannot move past it.
        /// </summary>
        public static int ClearedCount(PlayerProgress progress, int catalogCount)
        {
            if (progress == null)
                throw new ArgumentNullException(nameof(progress));
            if (catalogCount < 1)
                throw new ArgumentOutOfRangeException(nameof(catalogCount));

            return Math.Min(progress.HighestClearedLevel, catalogCount);
        }
    }
}
