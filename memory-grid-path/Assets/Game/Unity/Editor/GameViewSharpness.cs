using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Game.Unity.Editor
{
    /// <summary>
    /// On a Retina Mac, "16:9 Portrait" is only a shape. If Low Resolution Aspect Ratios
    /// is on, Unity renders that preview at half pixel density and upscales it — the phone
    /// build is not affected. This turns that Editor flag off and leaves the aspect alone.
    /// </summary>
    [InitializeOnLoad]
    public static class GameViewSharpness
    {
        const string AppliedKey = "Nixin.GameViewSharpness.LowResOff.v2";

        static GameViewSharpness()
        {
            EditorApplication.delayCall += ApplyOnceThisSession;
        }

        [MenuItem("Nixin Studio/Memory Grid Path/Extra/Fix Pixelated Game View", false, 80)]
        public static void FixFromMenu()
        {
            ApplyToOpenGameViews();
            Debug.Log(
                "Left 16:9 Portrait as-is. Unchecked Low Resolution Aspect Ratios " +
                "(open the 16:9 Portrait dropdown — it is the checkbox at the bottom).");
        }

        static void ApplyOnceThisSession()
        {
            if (SessionState.GetBool(AppliedKey, false))
                return;

            ApplyToOpenGameViews();
            SessionState.SetBool(AppliedKey, true);
        }

        static void ApplyToOpenGameViews()
        {
            var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (gameViewType == null)
                return;

            var windows = Resources.FindObjectsOfTypeAll(gameViewType);
            if (windows == null || windows.Length == 0)
                return;

            var lowRes = gameViewType.GetProperty(
                "lowResolutionForAspectRatios",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var window in windows)
            {
                var editorWindow = window as EditorWindow;
                if (editorWindow == null)
                    continue;

                lowRes?.SetValue(window, false);

                var serialized = new SerializedObject(window);
                var lowResArray = serialized.FindProperty("m_LowResolutionForAspectRatios");
                if (lowResArray != null && lowResArray.isArray)
                {
                    for (var i = 0; i < lowResArray.arraySize; i++)
                        lowResArray.GetArrayElementAtIndex(i).boolValue = false;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                editorWindow.Repaint();
            }
        }
    }
}
