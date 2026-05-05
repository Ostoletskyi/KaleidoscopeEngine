#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LocalAI.LMStudioBridge
{
    internal static class LMStudioContextBuilder
    {
        private static readonly HashSet<string> AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".cs", ".shader", ".hlsl", ".cginc", ".compute", ".json", ".asmdef", ".uxml", ".uss", ".txt", ".md"
        };

        public static string BuildFromSelection(bool includeEditorLogTail)
        {
            var sb = new StringBuilder(32768);
            sb.AppendLine("PROJECT CONTEXT");
            sb.AppendLine("==============");
            sb.AppendLine("Unity version: " + Application.unityVersion);
            sb.AppendLine("Project path: " + Application.dataPath.Replace("/Assets", string.Empty));
            sb.AppendLine();

            var paths = Selection.objects
                .Select(AssetDatabase.GetAssetPath)
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct()
                .ToList();

            if (paths.Count == 0)
            {
                sb.AppendLine("No project files selected in Unity Project window.");
            }
            else
            {
                foreach (string path in ExpandPaths(paths))
                {
                    AppendFile(sb, path);
                }
            }

            if (includeEditorLogTail)
            {
                sb.AppendLine();
                sb.AppendLine("UNITY EDITOR LOG TAIL");
                sb.AppendLine("=====================");
                sb.AppendLine(ReadEditorLogTail(LMStudioBridgeSettings.EditorLogTailLines));
            }

            return sb.ToString();
        }

        private static IEnumerable<string> ExpandPaths(IEnumerable<string> selectedPaths)
        {
            foreach (string path in selectedPaths)
            {
                if (File.Exists(path))
                {
                    if (AllowedExtensions.Contains(Path.GetExtension(path))) yield return path;
                    continue;
                }

                if (Directory.Exists(path))
                {
                    foreach (string file in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
                    {
                        string normalized = file.Replace('\\', '/');
                        if (normalized.Contains("/Library/") || normalized.Contains("/Temp/") || normalized.Contains("/Obj/")) continue;
                        if (AllowedExtensions.Contains(Path.GetExtension(file))) yield return normalized;
                    }
                }
            }
        }

        private static void AppendFile(StringBuilder sb, string assetPath)
        {
            try
            {
                var info = new FileInfo(assetPath);
                if (info.Length > 256 * 1024)
                {
                    sb.AppendLine($"--- FILE SKIPPED, TOO LARGE: {assetPath} ({info.Length} bytes) ---");
                    return;
                }

                sb.AppendLine($"--- FILE: {assetPath} ---");
                sb.AppendLine(File.ReadAllText(assetPath));
                sb.AppendLine($"--- END FILE: {assetPath} ---");
                sb.AppendLine();
            }
            catch (Exception ex)
            {
                sb.AppendLine($"--- FILE READ ERROR: {assetPath}: {ex.Message} ---");
            }
        }

        private static string ReadEditorLogTail(int lineCount)
        {
            try
            {
                string path = GetEditorLogPath();
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return "Editor.log not found.";

                var queue = new Queue<string>(Math.Max(10, lineCount));
                foreach (string line in File.ReadLines(path))
                {
                    queue.Enqueue(line);
                    while (queue.Count > lineCount) queue.Dequeue();
                }
                return string.Join("\n", queue);
            }
            catch (Exception ex)
            {
                return "Could not read Editor.log: " + ex.Message;
            }
        }

        private static string GetEditorLogPath()
        {
#if UNITY_EDITOR_WIN
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Unity", "Editor", "Editor.log");
#elif UNITY_EDITOR_OSX
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Logs", "Unity", "Editor.log");
#else
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".config", "unity3d", "Editor.log");
#endif
        }
    }
}
#endif
