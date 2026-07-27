using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class WindowsDevelopmentBuildValidation
    {
        private const string MenuPath =
            "Have a Break/Tests/Build Windows Development Player";
        private const string RelativeOutputDirectory =
            "Builds/WindowsDevelopment";
        private const string ExecutableName = "HaveABreak.exe";

        [MenuItem(MenuPath)]
        private static void RunFromMenu()
        {
            Build();
        }

        public static void RunBatchMode()
        {
            Build();
        }

        internal static string Build()
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene != null && scene.enabled)
                .Select(scene => scene.path)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToArray();
            if (scenes.Length == 0)
            {
                throw new InvalidOperationException(
                    "Windows Development Build failed: no enabled build scenes.");
            }

            string missingScene = scenes.FirstOrDefault(
                path => !File.Exists(Path.GetFullPath(path)));
            if (!string.IsNullOrWhiteSpace(missingScene))
            {
                throw new FileNotFoundException(
                    "Windows Development Build failed: scene file not found.",
                    missingScene);
            }

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                throw new InvalidOperationException(
                    "Windows Development Build failed: project root not found.");
            }

            string outputDirectory = Path.Combine(
                projectRoot,
                RelativeOutputDirectory);
            Directory.CreateDirectory(outputDirectory);
            string executablePath = Path.Combine(
                outputDirectory,
                ExecutableName);

            BuildPlayerOptions options = new()
            {
                scenes = scenes,
                locationPathName = executablePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development |
                          BuildOptions.AllowDebugging
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Windows Development Build failed: " +
                    $"result={summary.result}, errors={summary.totalErrors}, " +
                    $"warnings={summary.totalWarnings}.");
            }

            string dataDirectory = Path.Combine(
                outputDirectory,
                Path.GetFileNameWithoutExtension(ExecutableName) + "_Data");
            if (!File.Exists(executablePath) ||
                !Directory.Exists(dataDirectory))
            {
                throw new InvalidOperationException(
                    "Windows Development Build failed: expected player " +
                    "artifacts were not created.");
            }

            Debug.Log(
                "Windows Development Build passed: " +
                $"{scenes.Length} scene(s), {summary.totalSize} bytes, " +
                $"{summary.totalTime.TotalSeconds:F1} seconds, " +
                executablePath);
            return executablePath;
        }
    }
}
