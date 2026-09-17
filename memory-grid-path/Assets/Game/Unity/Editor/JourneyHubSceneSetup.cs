using Game.Core.Domain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Unity.Editor
{
    public static class JourneyHubSceneSetup
    {
        const string ScenePath = "Assets/Scenes/JourneyHub.unity";

        [MenuItem("Nixin Studio/Memory Grid Path/Open Journey Hub", false, 0)]
        public static void OpenJourneyHub()
        {
            EnsureSceneExists();
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Nixin Studio/Memory Grid Path/Catalog/Journey Catalog", false, 20)]
        public static void CreateJourneyCatalog()
        {
            var catalog = JourneyCatalogBuilder.CreateNewAsset();
            var path = AssetDatabase.GetAssetPath(catalog);
            Selection.activeObject = catalog;
            Debug.Log("Created a new Journey catalog at " + path +
                      ". Existing catalogs were not modified. Tile Arena " +
                      JourneyCatalogBuilder.TileArenaLevelCount +
                      " levels, Graph Arena " + GraphLevelLadder.Count +
                      ", Scout Arena " + LevelCatalog.ScoutSpecs.Count +
                      ". Unlock: 10 then 5.");
        }

        [MenuItem("Nixin Studio/Memory Grid Path/Catalog/Sync Tile Arena Economy From Core", false, 21)]
        public static void SyncTileArenaEconomy()
        {
            var catalog = JourneyCatalogBuilder.SyncExistingTileArenaEconomy();
            Selection.activeObject = catalog;
            Debug.Log("Tile Arena Grid rows now match LevelCatalog (" +
                      catalog.TileArena.Count + " levels). Scout Arena is " +
                      catalog.ScoutArena.Count + " city-pack grids. Graph Arena was left as-is.");
        }

        [MenuItem("Nixin Studio/Memory Grid Path/Extra/Create Journey Hub Scene", false, 80)]
        public static void CreateJourneyHubScene()
        {
            EnsureSceneExists();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Debug.Log("Journey Hub scene ready at " + ScenePath + ". Open it and press Play.");
        }

        static void EnsureSceneExists()
        {
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                return;

            var catalog = JourneyCatalogBuilder.LoadExistingOrCreateNew();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var go = new GameObject("JourneyHubPlay");
            var hub = go.AddComponent<JourneyHubPlay>();
            var serialized = new SerializedObject(hub);
            serialized.FindProperty("_catalog").objectReferenceValue = catalog;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
        }
    }
}
