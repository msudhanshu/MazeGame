using System;
using Game.Core.Domain;
using Game.Core.State;
using Game.Unity.Themes.Experimental;
using UnityEngine;

namespace Game.Unity.Data
{
    /// <summary>
    /// Three-mode Journey Hub catalog: Tile (classic/mosaic), Graph, and Scout (patchwork + graph).
    /// </summary>
    [CreateAssetMenu(fileName = "JourneyCatalog", menuName = "Nixin Studio/Memory Grid Path/Journey Catalog")]
    public sealed class JourneyCatalog : ScriptableObject
    {
        [SerializeField] int _graphUnlockAfterTileClears = 10;
        [SerializeField] int _scoutUnlockAfterGraphClears = 5;
        [SerializeField] JourneyModeDefinition _tileArena = new JourneyModeDefinition
        {
            ModeId = GameModeId.TileArena,
            DisplayName = "Tile Arena",
            CameraMode = ArenaCameraMode.StaticTopDown
        };
        [SerializeField] JourneyModeDefinition _graphArena = new JourneyModeDefinition
        {
            ModeId = GameModeId.GraphArena,
            DisplayName = "Graph Arena",
            CameraMode = ArenaCameraMode.StaticTopDown
        };
        [SerializeField] JourneyModeDefinition _scoutArena = new JourneyModeDefinition
        {
            ModeId = GameModeId.ScoutArena,
            DisplayName = "Scout Arena",
            CameraMode = ArenaCameraMode.FollowWalker,
            FollowOrthographicSize = 1.8f,
            WalkerScale = 0.55f
        };

        public GameModeUnlockConfig UnlockConfig =>
            new GameModeUnlockConfig(_graphUnlockAfterTileClears, _scoutUnlockAfterGraphClears);

        public JourneyModeDefinition TileArena => _tileArena;
        public JourneyModeDefinition GraphArena => _graphArena;
        public JourneyModeDefinition ScoutArena => _scoutArena;

        public JourneyModeDefinition Mode(GameModeId id)
        {
            switch (id)
            {
                case GameModeId.GraphArena:
                    return _graphArena;
                case GameModeId.ScoutArena:
                    return _scoutArena;
                default:
                    return _tileArena;
            }
        }

        public bool TryMode(GameModeId id, out JourneyModeDefinition mode)
        {
            mode = Mode(id);
            return mode != null && mode.Count > 0;
        }

        public LevelCatalog CreateGridCatalog(GameModeId id)
        {
            var mode = Mode(id);
            if (mode.Count < 1)
                throw new InvalidOperationException(id + " has no levels.");

            var specs = new LevelSpec[mode.Count];
            for (var i = 0; i < mode.Count; i++)
            {
                var entry = mode.Levels[i];
                if (!entry.IsGrid)
                    throw new InvalidOperationException(id + " is mixed; use a single-level catalog.");
                specs[i] = entry.GridSpec;
            }

            return new LevelCatalog(LevelCatalog.FromSpecs(specs));
        }

        public LevelCatalog CreateSingleGridCatalog(JourneyLevelEntry entry)
        {
            if (entry == null || !entry.IsGrid)
                throw new ArgumentException("Expected a grid level entry.", nameof(entry));

            return new LevelCatalog(LevelCatalog.FromSpecs(new[] { entry.GridSpec }));
        }

        public void ApplyUnlockThresholds(int graphAfterTileClears, int scoutAfterGraphClears)
        {
            _graphUnlockAfterTileClears = Mathf.Max(1, graphAfterTileClears);
            _scoutUnlockAfterGraphClears = Mathf.Max(1, scoutAfterGraphClears);
        }

        public void ApplyModes(JourneyModeDefinition tile, JourneyModeDefinition graph, JourneyModeDefinition scout)
        {
            _tileArena = tile ?? _tileArena;
            _graphArena = graph ?? _graphArena;
            _scoutArena = scout ?? _scoutArena;
        }
    }
}
