using Game.Unity.Audio;
using UnityEditor;
using UnityEngine;

namespace Game.Unity.Editor
{
    public static class MemoryPathAudioBuilder
    {
        const string ClipsFolder = "Assets/Game/Unity/Audio/Clips/";

        [MenuItem("Nixin Studio/Memory Grid Path/Create Audio Catalog")]
        public static MemoryPathAudioCatalog CreateOrUpdate()
        {
            System.IO.Directory.CreateDirectory("Assets/Game/Unity/Audio/Resources");
            System.IO.Directory.CreateDirectory("Assets/Game/Unity/Audio/Clips");

            var catalog = AssetDatabase.LoadAssetAtPath<MemoryPathAudioCatalog>(
                MemoryPathAudioCatalog.EditorAssetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<MemoryPathAudioCatalog>();
                AssetDatabase.CreateAsset(catalog, MemoryPathAudioCatalog.EditorAssetPath);
            }

            Bind(catalog, MemoryPathCue.UiClick, "ui-click.wav", 0.55f);
            Bind(catalog, MemoryPathCue.GameStart, "game-start.wav", 0.7f);
            Bind(catalog, MemoryPathCue.Success, "success.wav", 0.8f);
            Bind(catalog, MemoryPathCue.Fail, "fail.wav", 0.7f);
            Bind(catalog, MemoryPathCue.CorrectStep, "correct-step.wav", 0.45f);
            Bind(catalog, MemoryPathCue.Mistake, "mistake.wav", 0.55f);

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Selection.activeObject = catalog;
            Debug.Log("Memory Path audio catalog ready at " + MemoryPathAudioCatalog.EditorAssetPath);
            return catalog;
        }

        static void Bind(MemoryPathAudioCatalog catalog, MemoryPathCue cue, string fileName, float volume)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipsFolder + fileName);
            catalog.Apply(cue, clip, volume);
        }
    }
}
