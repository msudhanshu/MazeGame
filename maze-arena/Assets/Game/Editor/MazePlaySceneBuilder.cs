using System.Collections.Generic;
using Game.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Builds Assets/Scenes/MazePlay.unity with MazeGame, MazeArena, HUD, run, and walker
    /// already in the Hierarchy so play settings can be edited before Play.
    /// </summary>
    public static class MazePlaySceneBuilder
    {
        public const string PlayScenePath = "Assets/Scenes/MazePlay.unity";
        const string SandboxPath = "Assets/Scenes/MazeSandbox.unity";

        [MenuItem("Nixin Studio/Maze Arena/Build Play Scene")]
        public static void BuildPlayScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            BuildPlaySceneInternal();
        }

        /// <summary>
        /// Headless: <c>unity --execute-method Game.Editor.MazePlaySceneBuilder.BuildPlaySceneFromCli</c>
        /// </summary>
        public static void BuildPlaySceneFromCli()
        {
            BuildPlaySceneInternal();
        }

        [MenuItem("Nixin Studio/Maze Arena/Install Play Objects In Open Scene")]
        public static void InstallInOpenScene()
        {
            var host = InstallPlayObjects(showDebugTools: false);
            EditorSceneManager.MarkSceneDirty(host.gameObject.scene);
            Selection.activeGameObject = host.gameObject;
        }

        static void BuildPlaySceneInternal()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(PlayScenePath) != null)
                AssetDatabase.DeleteAsset(PlayScenePath);

            if (!AssetDatabase.CopyAsset(SandboxPath, PlayScenePath))
                throw new System.InvalidOperationException("Could not copy " + SandboxPath + " to " + PlayScenePath);

            EditorSceneManager.OpenScene(PlayScenePath);
            var sample = GameObject.Find("RedCylinder");
            if (sample != null)
                Object.DestroyImmediate(sample);

            var host = InstallPlayObjects(showDebugTools: false);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            EnsureBuildSettings();
            Selection.activeGameObject = host.gameObject;
            Debug.Log("Maze play scene saved: " + PlayScenePath + ". Select MazeGame for walk and debug defaults.");
        }

        static MazeGameHost InstallPlayObjects(bool showDebugTools)
        {
            var host = Object.FindAnyObjectByType<MazeGameHost>();
            var created = host == null;
            if (created)
                host = new GameObject("MazeGame").AddComponent<MazeGameHost>();
            if (created)
                host.ShowDebugTools = showDebugTools;
            host.Wire();
            EditorUtility.SetDirty(host);
            if (host.Arena != null)
                EditorUtility.SetDirty(host.Arena);
            return host;
        }

        static void EnsureBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(PlayScenePath, true)
            };
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SandboxPath) != null)
                scenes.Add(new EditorBuildSettingsScene(SandboxPath, false));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
