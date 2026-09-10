using System;

namespace Game.Core.State
{
    public readonly struct GameModeUnlockConfig
    {
        public GameModeUnlockConfig(int graphUnlockAfterTileClears = 10, int scoutUnlockAfterGraphClears = 5)
        {
            if (graphUnlockAfterTileClears < 1)
                throw new ArgumentOutOfRangeException(nameof(graphUnlockAfterTileClears));
            if (scoutUnlockAfterGraphClears < 1)
                throw new ArgumentOutOfRangeException(nameof(scoutUnlockAfterGraphClears));

            GraphUnlockAfterTileClears = graphUnlockAfterTileClears;
            ScoutUnlockAfterGraphClears = scoutUnlockAfterGraphClears;
        }

        public int GraphUnlockAfterTileClears { get; }
        public int ScoutUnlockAfterGraphClears { get; }

        public static GameModeUnlockConfig Default { get; } = new GameModeUnlockConfig(10, 5);
    }

    /// <summary>
    /// Cross-mode gates: Tile Arena is always open; Graph and Scout unlock after enough clears.
    /// </summary>
    public static class GameModeUnlock
    {
        public static bool IsUnlocked(
            GameModeId mode,
            JourneyProgress journey,
            GameModeUnlockConfig config,
            int tileCatalogCount,
            int graphCatalogCount)
        {
            if (journey == null)
                throw new ArgumentNullException(nameof(journey));
            if (tileCatalogCount < 1)
                throw new ArgumentOutOfRangeException(nameof(tileCatalogCount));
            if (graphCatalogCount < 1)
                throw new ArgumentOutOfRangeException(nameof(graphCatalogCount));

            switch (mode)
            {
                case GameModeId.TileArena:
                    return true;
                case GameModeId.GraphArena:
                    return LevelAccess.ClearedCount(journey.For(GameModeId.TileArena), tileCatalogCount)
                           >= config.GraphUnlockAfterTileClears;
                case GameModeId.ScoutArena:
                    return LevelAccess.ClearedCount(journey.For(GameModeId.GraphArena), graphCatalogCount)
                           >= config.ScoutUnlockAfterGraphClears;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }

        public static string LockReason(GameModeId mode, GameModeUnlockConfig config)
        {
            switch (mode)
            {
                case GameModeId.GraphArena:
                    return "Clear " + config.GraphUnlockAfterTileClears + " Tile Arena levels to unlock.";
                case GameModeId.ScoutArena:
                    return "Clear " + config.ScoutUnlockAfterGraphClears + " Graph Arena levels to unlock.";
                default:
                    return string.Empty;
            }
        }
    }
}
