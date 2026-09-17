using Game.Core.Domain;
using Game.Core.State;
using Game.Unity.Data;
using Game.Unity.Themes.Experimental;
using Game.Unity.View;
using UnityEditor;
using UnityEngine;

namespace Game.Unity.Editor
{
    public static class JourneyCatalogBuilder
    {
        public const int TileArenaLevelCount = 25;

        public const string CatalogPath = "Assets/Game/Unity/Data/JourneyCatalog.asset";
        const string EasyGraphPath = "Assets/Game/Unity/Data/GraphLevels/EasyFork.asset";
        const string SampleGraphPath = "Assets/Game/Unity/Data/GraphLevels/SampleVillage.asset";
        const string GraphLevel2Path = "Assets/Game/Unity/Data/GraphLevels/GraphLevel2.asset";
        const string ExtractedGraphPath = "Assets/Game/Unity/Data/GraphLevels/GraphLevel-1 0.asset";
        const string BuildingPatchworkPath = "Assets/Game/Unity/Data/Patchwork/BuildingTiles.asset";

        /// <summary>
        /// In-memory default catalog. Does not write or overwrite any asset.
        /// </summary>
        public static JourneyCatalog BuildDefault()
        {
            var catalog = ScriptableObject.CreateInstance<JourneyCatalog>();
            ApplyDefaultContent(catalog);
            return catalog;
        }

        /// <summary>
        /// Creates a new unique asset (JourneyCatalog.asset, or JourneyCatalog 1.asset if that
        /// path is taken). Never overwrites an existing catalog.
        /// </summary>
        public static JourneyCatalog CreateNewAsset()
        {
            System.IO.Directory.CreateDirectory("Assets/Game/Unity/Data");
            var catalog = BuildDefault();
            var path = AssetDatabase.GenerateUniqueAssetPath(CatalogPath);
            AssetDatabase.CreateAsset(catalog, path);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        public static JourneyCatalog LoadExisting() =>
            AssetDatabase.LoadAssetAtPath<JourneyCatalog>(CatalogPath);

        /// <summary>
        /// Returns the project catalog if it already exists; otherwise creates a new unique asset.
        /// Existing catalog contents are never rewritten.
        /// </summary>
        public static JourneyCatalog LoadExistingOrCreateNew()
        {
            var existing = LoadExisting();
            return existing != null ? existing : CreateNewAsset();
        }

        /// <summary>
        /// Writes Core Tile Arena economy onto the existing catalog. Keeps graph/scout modes,
        /// icons, and unlock thresholds. Use this when play is still on an old 3x3 Grid table.
        /// </summary>
        public static JourneyCatalog SyncExistingTileArenaEconomy()
        {
            var catalog = LoadExisting();
            if (catalog == null)
                return CreateNewAsset();

            var tile = BuildTileArena();
            tile.Icon = catalog.TileArena.Icon;
            tile.DisplayName = catalog.TileArena.DisplayName;
            tile.CameraMode = catalog.TileArena.CameraMode;
            ReplaceScoutLevels(catalog.ScoutArena);
            catalog.ApplyModes(tile, catalog.GraphArena, catalog.ScoutArena);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        static void ReplaceScoutLevels(JourneyModeDefinition scout)
        {
            if (scout == null)
                return;

            var city = AssetDatabase.LoadAssetAtPath<PatchworkTextureSet>(BuildingPatchworkPath);
            if (city == null && scout.Levels != null)
            {
                for (var i = 0; i < scout.Levels.Length; i++)
                {
                    if (scout.Levels[i] != null && scout.Levels[i].PatchworkSet != null)
                    {
                        city = scout.Levels[i].PatchworkSet;
                        break;
                    }
                }
            }

            var rebuilt = BuildScoutArena(city);
            scout.Levels = rebuilt.Levels;
            scout.ScoutMoveMode = ScoutMoveMode.RotationMoveMode;
            scout.CameraMode = ArenaCameraMode.FollowWalker;
            scout.FollowOrthographicSize = ScoutRotationMove.FollowSizeTwo;
            scout.WalkerScale = ScoutRotationMove.WalkerScale;
        }

        static void ApplyDefaultContent(JourneyCatalog catalog)
        {
            System.IO.Directory.CreateDirectory("Assets/Game/Unity/Data/GraphLevels");

            var extracted = AssetDatabase.LoadAssetAtPath<GraphLevelDefinition>(ExtractedGraphPath);
            var sample = AssetDatabase.LoadAssetAtPath<GraphLevelDefinition>(SampleGraphPath);
            var graph2 = AssetDatabase.LoadAssetAtPath<GraphLevelDefinition>(GraphLevel2Path);
            var easy = EnsureEasyGraph();
            var patchwork = AssetDatabase.LoadAssetAtPath<PatchworkTextureSet>(BuildingPatchworkPath);
            var arena = extracted != null ? extracted : (sample != null ? sample : (graph2 != null ? graph2 : easy));

            catalog.ApplyUnlockThresholds(10, 5);
            catalog.ApplyModes(
                BuildTileArena(),
                BuildGraphArena(arena),
                BuildScoutArena(patchwork));
        }

        static GraphLevelDefinition EnsureEasyGraph()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GraphLevelDefinition>(EasyGraphPath);
            if (existing != null)
                return existing;

            var level = GraphLevelDefinition.CreateSampleRuntime();
            AssetDatabase.CreateAsset(level, EasyGraphPath);
            var serialized = new SerializedObject(level);
            serialized.FindProperty("_displayName").stringValue = "Easy Fork";
            serialized.FindProperty("_minPathLength").intValue = 3;
            serialized.FindProperty("_maxPathLength").intValue = 6;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(level);
            return level;
        }

        static JourneyModeDefinition BuildTileArena()
        {
            var specs = LevelCatalog.NixinDefaultSpecs;
            var count = Mathf.Min(TileArenaLevelCount, specs.Count);
            var levels = new JourneyLevelEntry[count];
            for (var i = 0; i < count; i++)
            {
                var levelNumber = i + 1;
                var mosaicLevel = i % 2 == 1;
                Texture2D mosaicTex = null;
                if (mosaicLevel)
                    mosaicTex = MosaicPhotoLibrary.Pick(levelNumber);

                levels[i] = new JourneyLevelEntry
                {
                    Kind = JourneyBoardKind.Grid,
                    VisualType = mosaicLevel ? ArenaVisualType.MosaicImage : ArenaVisualType.ClassicDanceFloor,
                    Grid = LevelRow.FromSpec(specs[i]),
                    MosaicTexture = mosaicTex,
                    MosaicBackgroundParticles = mosaicLevel
                        && mosaicTex != null
                        && MosaicPhotoLibrary.RollParticles(levelNumber),
                    ThumbnailTexture = mosaicTex
                };
            }

            return new JourneyModeDefinition
            {
                ModeId = GameModeId.TileArena,
                DisplayName = "Tile Arena",
                CameraMode = ArenaCameraMode.StaticTopDown,
                Levels = levels
            };
        }

        static JourneyModeDefinition BuildGraphArena(GraphLevelDefinition arena)
        {
            var levels = new JourneyLevelEntry[GraphLevelLadder.Count];
            for (var i = 0; i < levels.Length; i++)
            {
                levels[i] = new JourneyLevelEntry
                {
                    Kind = JourneyBoardKind.Graph,
                    GraphLevel = arena,
                    ThumbnailTexture = arena != null ? arena.Background : null
                };
            }

            return new JourneyModeDefinition
            {
                ModeId = GameModeId.GraphArena,
                DisplayName = "Graph Arena",
                CameraMode = ArenaCameraMode.StaticTopDown,
                Levels = levels
            };
        }

        static JourneyModeDefinition BuildScoutArena(PatchworkTextureSet patchwork)
        {
            var specs = LevelCatalog.ScoutSpecs;
            var levels = new JourneyLevelEntry[specs.Count];
            for (var i = 0; i < specs.Count; i++)
            {
                var spec = specs[i];
                levels[i] = new JourneyLevelEntry
                {
                    Kind = JourneyBoardKind.Grid,
                    VisualType = ArenaVisualType.PatchworkTiles,
                    Grid = LevelRow.FromSpec(spec),
                    PatchworkSet = patchwork,
                    ThumbnailTexture = patchwork != null && patchwork.HasTextures
                        ? patchwork.Textures[0]
                        : null,
                    FollowOrthographicSize = ScoutRotationMove.FollowOrthographicSizeFor(spec.Width, spec.Height),
                    WalkerScale = ScoutRotationMove.WalkerScale
                };
            }

            return new JourneyModeDefinition
            {
                ModeId = GameModeId.ScoutArena,
                DisplayName = "Scout Arena",
                CameraMode = ArenaCameraMode.FollowWalker,
                ScoutMoveMode = ScoutMoveMode.RotationMoveMode,
                FollowOrthographicSize = ScoutRotationMove.FollowSizeTwo,
                WalkerScale = ScoutRotationMove.WalkerScale,
                Levels = levels
            };
        }
    }
}
