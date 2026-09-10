using System;

namespace Game.Core.State
{
    /// <summary>
    /// Per-mode career state for the Journey Hub. Independent of the original GridPathPlay save.
    /// </summary>
    public sealed class JourneyProgress
    {
        readonly PlayerProgress _tile;
        readonly PlayerProgress _graph;
        readonly PlayerProgress _scout;
        readonly int[] _lastPlayed = new int[3];

        public JourneyProgress(
            PlayerProgress tile = null,
            PlayerProgress graph = null,
            PlayerProgress scout = null,
            GameModeId lastSelectedMode = GameModeId.TileArena,
            int lastPlayedTile = 1,
            int lastPlayedGraph = 1,
            int lastPlayedScout = 1)
        {
            _tile = tile ?? new PlayerProgress();
            _graph = graph ?? new PlayerProgress();
            _scout = scout ?? new PlayerProgress();
            LastSelectedMode = lastSelectedMode;
            _lastPlayed[0] = ClampLevel(lastPlayedTile);
            _lastPlayed[1] = ClampLevel(lastPlayedGraph);
            _lastPlayed[2] = ClampLevel(lastPlayedScout);
        }

        public GameModeId LastSelectedMode { get; private set; }

        public PlayerProgress For(GameModeId mode)
        {
            switch (mode)
            {
                case GameModeId.TileArena:
                    return _tile;
                case GameModeId.GraphArena:
                    return _graph;
                case GameModeId.ScoutArena:
                    return _scout;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }

        public int LastPlayedLevel(GameModeId mode) => _lastPlayed[Index(mode)];

        public void SelectMode(GameModeId mode)
        {
            Index(mode);
            LastSelectedMode = mode;
        }

        public void RememberPlayed(GameModeId mode, int levelNumber)
        {
            _lastPlayed[Index(mode)] = ClampLevel(levelNumber);
            LastSelectedMode = mode;
        }

        static int Index(GameModeId mode)
        {
            var index = (int)mode;
            if (index < 0 || index > 2)
                throw new ArgumentOutOfRangeException(nameof(mode));
            return index;
        }

        static int ClampLevel(int levelNumber) => levelNumber < 1 ? 1 : levelNumber;
    }
}
