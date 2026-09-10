using System;
using Nixin.Maze.Core;

namespace Game.Core
{
    public static class MazeTimeBudget
    {
        public const float MinimumSeconds = 20f;

        public static float SecondsPerCell(MazeDifficulty difficulty)
        {
            switch (difficulty)
            {
                case MazeDifficulty.Easy: return 4f;
                case MazeDifficulty.Medium: return 3f;
                case MazeDifficulty.Hard: return 2.25f;
                case MazeDifficulty.Brutal: return 1.75f;
                default:
                    throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, "Unknown difficulty.");
            }
        }

        public static float SecondsFor(MazeSpec spec)
        {
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));

            var seconds = spec.Band.MinSolutionLength * SecondsPerCell(spec.Difficulty);
            return Math.Max(MinimumSeconds, seconds);
        }
    }
}
