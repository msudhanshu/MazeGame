using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Game.Unity.Editor
{
    /// <summary>
    /// Imports TMP Essential Resources (if missing) and removes a broken
    /// ColorEmoji.asset that was saved without atlas texture sub-assets.
    /// Color emoji is created at runtime from Apple Color Emoji (see TmpEmojiFallback).
    /// </summary>
    public static class TmpEssentialSetup
    {
        const string ColorEmojiPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/ColorEmoji.asset";

        [MenuItem("Nixin Studio/Memory Grid Path/Import TMP Essentials & Emoji")]
        public static void ImportMenu()
        {
            ImportEssentials();
            RemoveBrokenColorEmojiAsset();
            Game.Unity.Ui.TmpEmojiFallback.Ensure();
            AssetDatabase.SaveAssets();
            Debug.Log("TMP essentials ready. Color emoji uses a runtime Apple Color Emoji fallback.");
        }

        [InitializeOnLoadMethod]
        static void AutoEnsure()
        {
            EditorApplication.delayCall += () =>
            {
                Game.Unity.Ui.TmpEmojiFallback.StripBrokenFallbacks();
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;
                RemoveBrokenColorEmojiAsset();
            };
        }

        static void RemoveBrokenColorEmojiAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ColorEmojiPath);
            if (existing == null)
                return;

            if (Game.Unity.Ui.TmpEmojiFallback.HasLiveAtlas(existing))
                return;

            AssetDatabase.DeleteAsset(ColorEmojiPath);
            Game.Unity.Ui.TmpEmojiFallback.StripBrokenFallbacks();
            if (TMP_Settings.instance != null)
                EditorUtility.SetDirty(TMP_Settings.instance);
        }

        static void ImportEssentials()
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset") != null)
                return;

            var package = FindEssentialPackage();
            if (!string.IsNullOrEmpty(package))
                AssetDatabase.ImportPackage(package, false);
        }

        static string FindEssentialPackage()
        {
            if (!Directory.Exists("Library/PackageCache"))
                return null;
            var dirs = Directory.GetDirectories("Library/PackageCache", "com.unity.ugui@*");
            for (var i = 0; i < dirs.Length; i++)
            {
                var package = Path.Combine(dirs[i], "Package Resources", "TMP Essential Resources.unitypackage");
                if (File.Exists(package))
                    return package;
            }

            return null;
        }
    }
}
