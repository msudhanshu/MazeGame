using System;

namespace Game.Core.Domain
{
    /// <summary>Cute labels for the level ladder. The route still changes every session.</summary>
    public static class LevelTitles
    {
        static readonly string[] Names =
        {
            "Sunny Steps",
            "Lost Forest",
            "Coral Walk",
            "Moonlit Grid",
            "Candy Lane",
            "Quiet Harbor",
            "Ember Trail",
            "Star Meadow",
            "Misty Court",
            "Glow Garden"
        };

        public static string Name(int levelNumber)
        {
            if (levelNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(levelNumber));
            return Names[(levelNumber - 1) % Names.Length];
        }

        public static string Headline(int levelNumber) => "Level " + levelNumber + ": " + Name(levelNumber);
    }
}
