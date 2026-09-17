using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Game.Unity.Editor
{
    public static class PathGraphExtractMenu
    {
        const string ToolFolder = "path-graph-extract";
        const string ModuleName = "path_graph_extract";

        public static void OpenGui()
        {
            if (!TryLaunchGui(out var error))
                EditorUtility.DisplayDialog("Path Graph Extractor", error, "OK");
        }

        public static string ToolRoot()
        {
            var dir = new DirectoryInfo(Application.dataPath);
            while (dir != null)
            {
                var sibling = Path.Combine(dir.FullName, "NixinStudioUnityCore", "tools", ToolFolder);
                if (Directory.Exists(sibling))
                    return Path.GetFullPath(sibling);

                var nested = Path.Combine(dir.FullName, "tools", ToolFolder);
                if (Directory.Exists(nested))
                    return Path.GetFullPath(nested);

                dir = dir.Parent;
            }

            return Path.GetFullPath(Path.Combine(Application.dataPath, "../../../NixinStudioUnityCore/tools", ToolFolder));
        }

        public static string PythonExe()
        {
            var root = ToolRoot();
            var unix = Path.Combine(root, ".venv", "bin", "python");
            if (File.Exists(unix))
                return unix;

            var windows = Path.Combine(root, ".venv", "Scripts", "python.exe");
            if (File.Exists(windows))
                return windows;

            return null;
        }

        public static bool TryLaunchGui(out string error)
        {
            error = null;
            var root = ToolRoot();
            if (!Directory.Exists(root))
            {
                error = "Could not find path-graph-extract at:\n" + root;
                return false;
            }

            var python = PythonExe();
            if (string.IsNullOrEmpty(python))
            {
                error = "Extractor venv not found at:\n" + root
                    + "\n\nIn Terminal:\ncd " + root
                    + "\npython3 -m venv .venv\n.venv/bin/pip install -r requirements.txt";
                return false;
            }

            var psi = new ProcessStartInfo
            {
                FileName = python,
                WorkingDirectory = root,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("-m");
            psi.ArgumentList.Add(ModuleName);

            try
            {
                Process.Start(psi);
                Debug.Log("Opened path graph extractor (" + python + " -m " + ModuleName + ")");
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
