using System;
using Game.Core.Domain;
using Game.Core.State;
using Game.Unity.Themes.Experimental;
using Game.Unity.View;
using UnityEngine;

namespace Game.Unity.Data
{
    /// <summary>
    /// Three-mode Journey Hub catalog: Tile (classic/mosaic), Graph, and Scout (city patchwork).
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
            ScoutMoveMode = ScoutMoveMode.RotationMoveMode,
            FollowOrthographicSize = 0.48f,
            WalkerScale = 0.42f
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
                specs[i] = PlayableGridSpec(id, i, entry.GridSpec);
            }

            return new LevelCatalog(LevelCatalog.FromSpecs(specs));
        }

        public LevelCatalog CreateSingleGridCatalog(JourneyLevelEntry entry)
        {
            if (entry == null || !entry.IsGrid)
                throw new ArgumentException("Expected a grid level entry.", nameof(entry));

            return new LevelCatalog(LevelCatalog.FromSpecs(new[] { entry.GridSpec }));
        }

        public LevelCatalog CreateSingleGridCatalog(GameModeId id, int levelNumber, JourneyLevelEntry entry)
        {
            if (id == GameModeId.ScoutArena && LevelCatalog.TryScoutSpec(Math.Max(0, levelNumber - 1), out var scout))
                return new LevelCatalog(LevelCatalog.FromSpecs(new[] { scout }));

            return CreateSingleGridCatalog(entry);
        }

        /// <summary>
        /// Graph Arena play always uses the Core teaching ladder count, even if this asset
        /// still has the old 4 extracted-map slots. Scout play always uses the Core city ladder,
        /// even if this asset still has leftover graph / classic rows.
        /// </summary>
        public static int PlayableLevelCount(JourneyCatalog catalog, GameModeId id)
        {
            var authored = catalog != null ? catalog.Mode(id).Count : 0;
            if (id == GameModeId.GraphArena)
                return Math.Max(authored, GraphLevelLadder.Count);
            if (id == GameModeId.ScoutArena)
                return LevelCatalog.ScoutSpecs.Count;
            return authored;
        }

        public JourneyLevelEntry PlayableEntry(GameModeId id, int levelNumber)
        {
            if (id == GameModeId.ScoutArena)
                return PlayableScoutEntry(levelNumber);

            if (id != GameModeId.GraphArena)
                return Mode(id).Get(levelNumber);

            var authored = _graphArena.Count;
            if (levelNumber >= 1 && levelNumber <= authored)
                return _graphArena.Get(levelNumber);

            if (levelNumber < 1 || levelNumber > PlayableLevelCount(this, id))
                throw new ArgumentOutOfRangeException(nameof(levelNumber));

            return new JourneyLevelEntry { Kind = JourneyBoardKind.Graph };
        }

        JourneyLevelEntry PlayableScoutEntry(int levelNumber)
        {
            if (levelNumber < 1 || levelNumber > PlayableLevelCount(this, GameModeId.ScoutArena))
                throw new ArgumentOutOfRangeException(nameof(levelNumber));

            if (!LevelCatalog.TryScoutSpec(levelNumber - 1, out var spec))
                spec = LevelCatalog.ScoutSpecs[0];

            var city = FindScoutCityPack();
            JourneyLevelEntry authored = null;
            if (_scoutArena != null && levelNumber >= 1 && levelNumber <= _scoutArena.Count)
                authored = _scoutArena.Get(levelNumber);

            Texture2D thumbnail = null;
            if (city != null && city.HasTextures)
                thumbnail = city.Textures[0];
            else if (authored != null)
                thumbnail = authored.ThumbnailTexture;

            return new JourneyLevelEntry
            {
                Kind = JourneyBoardKind.Grid,
                VisualType = ArenaVisualType.PatchworkTiles,
                Grid = LevelRow.FromSpec(spec),
                PatchworkSet = city,
                Thumbnail = authored != null ? authored.Thumbnail : null,
                ThumbnailTexture = thumbnail,
                FollowOrthographicSize = ScoutRotationMove.FollowOrthographicSizeFor(spec.Width, spec.Height),
                WalkerScale = ScoutRotationMove.WalkerScale
            };
        }

        PatchworkTextureSet FindScoutCityPack()
        {
            PatchworkTextureSet fallback = null;
            var levels = _scoutArena != null ? _scoutArena.Levels : null;
            if (levels == null)
                return null;

            for (var i = 0; i < levels.Length; i++)
            {
                var set = levels[i] != null ? levels[i].PatchworkSet : null;
                if (set == null)
                    continue;
                if (IsCityPack(set))
                    return set;
                if (fallback == null)
                    fallback = set;
            }

            return fallback;
        }

        static bool IsCityPack(PatchworkTextureSet set)
        {
            if (set == null)
                return false;
            if (string.Equals(set.ThemeId, "buildings", StringComparison.OrdinalIgnoreCase))
                return true;
            return set.name.IndexOf("Building", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public GraphLevelDefinition PlayableGraphSource(int levelNumber)
        {
            var authored = _graphArena != null ? _graphArena.Count : 0;
            if (levelNumber >= 1 && levelNumber <= authored)
            {
                var entry = _graphArena.Get(levelNumber);
                if (entry != null && entry.GraphLevel != null)
                    return entry.GraphLevel;
            }

            for (var i = 1; i <= authored; i++)
            {
                var graph = _graphArena.Get(i).GraphLevel;
                if (graph != null)
                    return graph;
            }

            return null;
        }

        /// <summary>
        /// Tile Arena play always uses Core economy rows, even if this asset still has an older
        /// 3x3 Grid table. Scout play uses the tiny Scout ladder. Visuals stay on Journey entries.
        /// </summary>
        public static LevelSpec PlayableGridSpec(GameModeId id, int zeroBasedIndex, LevelSpec fallback)
        {
            if (id == GameModeId.ScoutArena && LevelCatalog.TryScoutSpec(zeroBasedIndex, out var scout))
                return scout;

            if (id != GameModeId.TileArena)
                return fallback;

            var core = LevelCatalog.NixinDefaultSpecs;
            if (zeroBasedIndex < 0 || zeroBasedIndex >= core.Count)
                return fallback;

            return core[zeroBasedIndex];
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
