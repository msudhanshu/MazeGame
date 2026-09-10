using Game.Unity.Data;
using Game.Unity.Save;
using Game.Unity.Themes.Experimental;
using UnityEditor;
using UnityEngine;

namespace Game.Unity.Editor
{
    public static class MemoryGridPathMenus
    {
        const string TuningPath = "Assets/Game/Unity/Data/GridPathTuning.asset";
        const string ArenaVisualSettingsPath = "Assets/Game/Unity/Data/ArenaVisualSettings.asset";
        const string BuildingPatchworkSetPath = "Assets/Game/Unity/Data/Patchwork/BuildingTiles.asset";
        const string BoxPatchworkSetPath = "Assets/Game/Unity/Data/Patchwork/BoxTiles.asset";
        const string MixedPatchworkSetPath = "Assets/Game/Unity/Data/Patchwork/MixedArenaTiles.asset";
        const string TileBuildingTexturePath = "Assets/tilebuilding.png";
        const string TileBoxTexturePath = "Assets/tilebox.png";

        [MenuItem("Nixin Studio/Memory Grid Path/Create Building Patchwork Set")]
        static void CreateBuildingPatchworkSet() => CreateDefaultPatchworkSets();

        [MenuItem("Nixin Studio/Memory Grid Path/Create Default Patchwork Tile Sets")]
        static void CreateDefaultPatchworkSets()
        {
            System.IO.Directory.CreateDirectory("Assets/Game/Unity/Data/Patchwork");

            var building = CreateOrUpdatePatchworkSet(
                BuildingPatchworkSetPath,
                "buildings",
                AssetDatabase.LoadAssetAtPath<Texture2D>(TileBuildingTexturePath));

            var box = CreateOrUpdatePatchworkSet(
                BoxPatchworkSetPath,
                "boxes",
                AssetDatabase.LoadAssetAtPath<Texture2D>(TileBoxTexturePath));

            var mixed = CreateOrUpdatePatchworkSet(
                MixedPatchworkSetPath,
                "mixed",
                AssetDatabase.LoadAssetAtPath<Texture2D>(TileBuildingTexturePath),
                AssetDatabase.LoadAssetAtPath<Texture2D>(TileBoxTexturePath));

            WireArenaVisualSettingsForPatchwork(mixed, building, box);
            AssetDatabase.SaveAssets();
            Selection.activeObject = mixed;
            Debug.Log("Patchwork sets ready: BuildingTiles, BoxTiles, MixedArenaTiles. ArenaVisualSettings uses MixedArenaTiles.");
        }

        static PatchworkTextureSet CreateOrUpdatePatchworkSet(string path, string themeId, params Texture2D[] textures)
        {
            var set = AssetDatabase.LoadAssetAtPath<PatchworkTextureSet>(path);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<PatchworkTextureSet>();
                AssetDatabase.CreateAsset(set, path);
            }

            var serialized = new SerializedObject(set);
            serialized.FindProperty("_themeId").stringValue = themeId;
            var textureProperty = serialized.FindProperty("_textures");
            textureProperty.ClearArray();
            for (var i = 0; i < textures.Length; i++)
            {
                if (textures[i] == null)
                    continue;

                textureProperty.InsertArrayElementAtIndex(textureProperty.arraySize);
                textureProperty.GetArrayElementAtIndex(textureProperty.arraySize - 1).objectReferenceValue = textures[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(set);
            return set;
        }

        static void WireArenaVisualSettingsForPatchwork(
            PatchworkTextureSet primary,
            params PatchworkTextureSet[] extraSets)
        {
            var settings = AssetDatabase.LoadAssetAtPath<ArenaVisualSettings>(ArenaVisualSettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<ArenaVisualSettings>();
                AssetDatabase.CreateAsset(settings, ArenaVisualSettingsPath);
            }

            var settingsSerialized = new SerializedObject(settings);
            settingsSerialized.FindProperty("_visualType").enumValueIndex = (int)ArenaVisualType.PatchworkTiles;
            settingsSerialized.FindProperty("_cameraMode").enumValueIndex = (int)ArenaCameraMode.FollowWalker;
            settingsSerialized.FindProperty("_followOrthographicSize").floatValue = 1.8f;
            settingsSerialized.FindProperty("_patchworkTextureSet").objectReferenceValue = primary;
            var extraProperty = settingsSerialized.FindProperty("_extraPatchworkSets");
            extraProperty.ClearArray();
            for (var i = 0; i < extraSets.Length; i++)
            {
                if (extraSets[i] == null)
                    continue;

                extraProperty.InsertArrayElementAtIndex(extraProperty.arraySize);
                extraProperty.GetArrayElementAtIndex(extraProperty.arraySize - 1).objectReferenceValue = extraSets[i];
            }

            settingsSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        [MenuItem("Nixin Studio/Memory Grid Path/Create Patchwork Texture Set")]
        static void CreatePatchworkTextureSet()
        {
            System.IO.Directory.CreateDirectory("Assets/Game/Unity/Data/Patchwork");
            var path = AssetDatabase.GenerateUniqueAssetPath("Assets/Game/Unity/Data/Patchwork/PatchworkTextureSet.asset");
            var set = ScriptableObject.CreateInstance<PatchworkTextureSet>();
            AssetDatabase.CreateAsset(set, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = set;
            Debug.Log("Add tile textures to the set, then assign it on ArenaVisualSettings → Patchwork Texture Set.");
        }

        [MenuItem("Nixin Studio/Memory Grid Path/Reset Progress")]
        static void ResetProgress()
        {
            PlayerPrefsProgressRepository.Clear();
            PlayerPrefsJourneyRepository.Clear();
            Debug.Log("Memory Grid Path progress cleared (classic scene + Journey Hub). Press Play to start at level 1.");
        }

        [MenuItem("Nixin Studio/Memory Grid Path/Reset Tuning To Nixin Defaults")]
        static void ResetTuning()
        {
            System.IO.Directory.CreateDirectory("Assets/Game/Unity/Data");

            var tuning = AssetDatabase.LoadAssetAtPath<GridPathTuning>(TuningPath);
            if (tuning == null)
            {
                tuning = ScriptableObject.CreateInstance<GridPathTuning>();
                AssetDatabase.CreateAsset(tuning, TuningPath);
            }

            tuning.ApplyNixinDefaults();
            EditorUtility.SetDirty(tuning);
            AssetDatabase.SaveAssets();
            Debug.Log("Memory Grid Path tuning reset to studio defaults at " + TuningPath);
        }

        [MenuItem("Nixin Studio/Memory Grid Path/Create Arena Visual Settings")]
        static void CreateArenaVisualSettings()
        {
            System.IO.Directory.CreateDirectory("Assets/Game/Unity/Data");

            var settings = AssetDatabase.LoadAssetAtPath<ArenaVisualSettings>(ArenaVisualSettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<ArenaVisualSettings>();
                AssetDatabase.CreateAsset(settings, ArenaVisualSettingsPath);
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Selection.activeObject = settings;
            Debug.Log("Set Visual Type, Camera Mode, and textures on ArenaVisualSettings.");
        }

        const string GraphCatalogPath = "Assets/Game/Unity/Data/GraphLevelCatalog.asset";
        const string SampleGraphLevelPath = "Assets/Game/Unity/Data/GraphLevels/SampleVillage.asset";

        [MenuItem("Nixin Studio/Memory Grid Path/Create Graph Level Catalog")]
        static void CreateGraphLevelCatalog()
        {
            System.IO.Directory.CreateDirectory("Assets/Game/Unity/Data/GraphLevels");

            var sample = AssetDatabase.LoadAssetAtPath<GraphLevelDefinition>(SampleGraphLevelPath);
            if (sample == null)
            {
                sample = GraphLevelDefinition.CreateSampleRuntime();
                AssetDatabase.CreateAsset(sample, SampleGraphLevelPath);
            }

            var catalog = AssetDatabase.LoadAssetAtPath<GraphLevelCatalog>(GraphCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<GraphLevelCatalog>();
                AssetDatabase.CreateAsset(catalog, GraphCatalogPath);
            }

            var serialized = new SerializedObject(catalog);
            var levels = serialized.FindProperty("_levels");
            levels.ClearArray();
            levels.InsertArrayElementAtIndex(0);
            levels.GetArrayElementAtIndex(0).objectReferenceValue = sample;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Selection.activeObject = catalog;
            Debug.Log("Graph level catalog ready at " + GraphCatalogPath);
        }
    }
}
