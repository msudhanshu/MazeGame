using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Unity.Editor
{
    public static class ArenaTestLabSceneSetup
    {
        const string ScenePath = "Assets/Scenes/ArenaTestLab.unity";

        [MenuItem("Nixin Studio/Memory Grid Path/Open Arena Test Lab")]
        static void OpenArenaTestLab()
        {
            EnsureSceneExists();
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Nixin Studio/Memory Grid Path/Create Arena Test Lab Scene")]
        public static void CreateArenaTestLabScene()
        {
            EnsureSceneExists();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Debug.Log("Arena Test Lab scene ready at " + ScenePath + ". Open it and press Play.");
        }

        static void EnsureSceneExists()
        {
            System.IO.Directory.CreateDirectory("Assets/Scenes");

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var labGo = new GameObject("ArenaTestLab");
            labGo.AddComponent<TestLab.ArenaTestLab>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
        }
    }
}
