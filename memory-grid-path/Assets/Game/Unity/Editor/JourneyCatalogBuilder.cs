using Game.Core.Domain;
using Game.Core.State;
using Game.Unity.Data;
using Game.Unity.Themes.Experimental;
using UnityEditor;
using UnityEngine;

namespace Game.Unity.Editor
{
    public static class JourneyCatalogBuilder
    {
        public const int TileArenaLevelCount = 20;

        public const string CatalogPath = "Assets/Game/Unity/Data/JourneyCatalog.asset";
        const string EasyGraphPath = "Assets/Game/Unity/Data/GraphLevels/EasyFork.asset";
        const string SampleGraphPath = "Assets/Game/Unity/Data/GraphLevels/SampleVillage.asset";
        const string GraphLevel2Path = "Assets/Game/Unity/Data/GraphLevels/GraphLevel2.asset";
        const string MixedPatchworkPath = "Assets/Game/Unity/Data/Patchwork/MixedArenaTiles.asset";

        public static JourneyCatalog CreateOrUpdate()
        {
            System.IO.Directory.CreateDirectory("Assets/Game/Unity/Data");
            System.IO.Directory.CreateDirectory("Assets/Game/Unity/Data/GraphLevels");

            var catalog = AssetDatabase.LoadAssetAtPath<JourneyCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<JourneyCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var sample = AssetDatabase.LoadAssetAtPath<GraphLevelDefinition>(SampleGraphPath);
            var graph2 = AssetDatabase.LoadAssetAtPath<GraphLevelDefinition>(GraphLevel2Path);
            var easy = EnsureEasyGraph();
            var patchwork = AssetDatabase.LoadAssetAtPath<PatchworkTextureSet>(MixedPatchworkPath);

            var graphs = new[]
            {
                sample != null ? sample : easy,
                graph2 != null ? graph2 : easy,
                easy
            };

            catalog.ApplyUnlockThresholds(10, 5);
            catalog.ApplyModes(
                BuildTileArena(),
                BuildGraphArena(graphs),
                BuildScoutArena(patchwork, graphs));
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
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

        static JourneyModeDefinition BuildGraphArena(GraphLevelDefinition[] graphs)
        {
            var levels = new JourneyLevelEntry[graphs.Length];
            for (var i = 0; i < graphs.Length; i++)
            {
                levels[i] = new JourneyLevelEntry
                {
                    Kind = JourneyBoardKind.Graph,
                    GraphLevel = graphs[i],
                    ThumbnailTexture = graphs[i] != null ? graphs[i].Background : null
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

        static JourneyModeDefinition BuildScoutArena(PatchworkTextureSet patchwork, GraphLevelDefinition[] graphs)
        {
            var specs = LevelCatalog.NixinDefaultSpecs;
            var levels = new JourneyLevelEntry[6];
            for (var i = 0; i < 3; i++)
            {
                levels[i] = new JourneyLevelEntry
                {
                    Kind = JourneyBoardKind.Grid,
                    VisualType = ArenaVisualType.PatchworkTiles,
                    Grid = LevelRow.FromSpec(specs[i]),
                    PatchworkSet = patchwork
                };
            }

            for (var i = 0; i < 3; i++)
            {
                levels[3 + i] = new JourneyLevelEntry
                {
                    Kind = JourneyBoardKind.Graph,
                    GraphLevel = graphs[i],
                    ThumbnailTexture = graphs[i] != null ? graphs[i].Background : null
                };
            }

            return new JourneyModeDefinition
            {
                ModeId = GameModeId.ScoutArena,
                DisplayName = "Scout Arena",
                CameraMode = ArenaCameraMode.FollowWalker,
                FollowOrthographicSize = 1.8f,
                WalkerScale = 0.55f,
                Levels = levels
            };
        }
    }
}
