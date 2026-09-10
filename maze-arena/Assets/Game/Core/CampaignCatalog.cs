using System;
using System.Collections.Generic;
using Nixin.Maze.Core;

namespace Game.Core
{
    public sealed class CampaignCatalog
    {
        readonly IReadOnlyList<CampaignLevel> _levels;

        public CampaignCatalog(IReadOnlyList<CampaignLevel> levels)
        {
            if (levels == null || levels.Count == 0)
                throw new ArgumentException("At least one campaign level is required.", nameof(levels));

            for (var i = 0; i < levels.Count; i++)
            {
                if (levels[i] == null)
                    throw new ArgumentException("Level list cannot contain null entries.", nameof(levels));
            }

            _levels = levels;
        }

        public IReadOnlyList<CampaignLevel> All => _levels;

        public int Count => _levels.Count;

        public CampaignLevel Get(int index)
        {
            if (index < 0 || index >= _levels.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _levels[index];
        }

        public static CampaignCatalog Default()
        {
            return new CampaignCatalog(new[]
            {
                new CampaignLevel("Courtyard", 6, 6, MazeDifficulty.Easy, 0.55f),
                new CampaignLevel("Alley", 7, 7, MazeDifficulty.Easy, 0.50f),
                new CampaignLevel("Garden", 8, 8, MazeDifficulty.Easy, 0.45f),
                new CampaignLevel("Hedge", 8, 8, MazeDifficulty.Medium, 0.25f),
                new CampaignLevel("Grove", 9, 9, MazeDifficulty.Medium, 0.22f),
                new CampaignLevel("Ward", 10, 10, MazeDifficulty.Medium, 0.20f),
                new CampaignLevel("Keep", 10, 10, MazeDifficulty.Hard, 0.08f),
                new CampaignLevel("Bastion", 12, 12, MazeDifficulty.Hard, 0.08f),
                new CampaignLevel("Citadel", 12, 12, MazeDifficulty.Hard, 0.05f),
                new CampaignLevel("Catacombs", 14, 14, MazeDifficulty.Brutal, 0f),
                new CampaignLevel("Labyrinth", 15, 15, MazeDifficulty.Brutal, 0f),
                new CampaignLevel("Abyss", 16, 16, MazeDifficulty.Brutal, 0f)
            });
        }
    }
}
