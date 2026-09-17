using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Unity.Editor
{
    public static class GraphPathPlaySceneSetup
    {
        const string ScenePath = "Assets/Scenes/GraphPathPlay.unity";

        [MenuItem("Nixin Studio/Memory Grid Path/Extra/Open Graph Path Play", false, 80)]
        public static void OpenGraphPathPlay()
        {
            EnsureSceneExists();
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        static void EnsureSceneExists()
        {
            System.IO.Directory.CreateDirectory("Assets/Scenes");

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var go = new GameObject("GraphPathPlay");
            go.AddComponent<GraphPathPlay>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
        }
    }
}
