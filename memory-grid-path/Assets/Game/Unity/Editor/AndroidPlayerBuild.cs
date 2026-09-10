using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.Unity.Editor
{
    /// <summary>
    /// Headless Android APK for <c>unity build --execute-method Game.Unity.Editor.AndroidPlayerBuild.PerformBuild</c>.
    /// Honors <c>-buildOutput</c> from the Unity CLI <c>--output-path</c> flag.
    /// </summary>
    public static class AndroidPlayerBuild
    {
        public static void PerformBuild()
        {
            BrandPlayerSettings.Apply();
            AssetDatabase.SaveAssets();

            var output = ReadArg("-buildOutput");
            if (string.IsNullOrWhiteSpace(output))
                output = "Build/Android/memorizewayhome.apk";

            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("Android build: no enabled scenes in Build Settings.");
                EditorApplication.Exit(1);
                return;
            }

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.CompressWithLz4
            });

            var ok = report.summary.result == BuildResult.Succeeded;
            if (!ok)
                Debug.LogError("Android build failed: " + report.summary.result + " errors=" + report.summary.totalErrors);
            else
                Debug.Log("Android APK: " + output);

            if (Application.isBatchMode)
                EditorApplication.Exit(ok ? 0 : 1);
        }

        static string ReadArg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.Ordinal))
                    return args[i + 1];
            }

            return null;
        }
    }
}
