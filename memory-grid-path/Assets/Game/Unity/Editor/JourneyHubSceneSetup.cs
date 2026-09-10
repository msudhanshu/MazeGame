using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Unity.Editor
{
    public static class JourneyHubSceneSetup
    {
        const string ScenePath = "Assets/Scenes/JourneyHub.unity";

        [MenuItem("Nixin Studio/Memory Grid Path/Open Journey Hub")]
        public static void OpenJourneyHub()
        {
            EnsureSceneExists();
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Nixin Studio/Memory Grid Path/Create Journey Catalog")]
        public static void CreateJourneyCatalog()
        {
            var catalog = JourneyCatalogBuilder.CreateOrUpdate();
            Selection.activeObject = catalog;
            Debug.Log("Journey catalog ready at " + JourneyCatalogBuilder.CatalogPath +
                      ". Tile Arena " + JourneyCatalogBuilder.TileArenaLevelCount +
                      " levels, Graph Arena 3, Scout Arena 6. Unlock: 10 then 5.");
        }

        [MenuItem("Nixin Studio/Memory Grid Path/Create Journey Hub Scene")]
        public static void CreateJourneyHubScene()
        {
            EnsureSceneExists();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Debug.Log("Journey Hub scene ready at " + ScenePath + ". Open it and press Play.");
        }

        static void EnsureSceneExists()
        {
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            var catalog = JourneyCatalogBuilder.CreateOrUpdate();

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                return;

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
