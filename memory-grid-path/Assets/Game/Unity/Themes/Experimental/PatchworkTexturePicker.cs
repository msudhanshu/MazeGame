using Nixin.Grid.Core;

namespace Game.Unity.Themes.Experimental
{
    /// <summary>
    /// Picks a stable texture index per cell so the patchwork floor does not reshuffle mid-level.
    /// </summary>
    public static class PatchworkTexturePicker
    {
        public static int PickIndex(GridCoord coord, int poolCount, int seed)
        {
            if (poolCount <= 0)
                return 0;

            unchecked
            {
                var hash = seed;
                hash = hash * 31 + coord.X;
                hash = hash * 31 + coord.Y;
                hash ^= hash >> 16;
                return PositiveMod(hash, poolCount);
            }
        }

        static int PositiveMod(int value, int divisor) => (value % divisor + divisor) % divisor;
    }
}
