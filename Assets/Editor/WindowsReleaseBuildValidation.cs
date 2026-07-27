using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class WindowsReleaseBuildValidation
    {
        private const string MenuPath =
            "Have a Break/Tests/Run Windows Release Build Validation";
        private const string RelativeOutputDirectory =
            "Builds/WindowsRelease";
        private const string RelativeArchivePath =
            "Builds/HaveABreak-WindowsRelease.zip";
        private const string RelativeSmokeLogPath =
            "Logs/WindowsReleasePlayerSmoke.log";
        private const string ExecutableName = "HaveABreak.exe";
        private const string ManifestName = "SHA256SUMS.txt";

        private static readonly string[] DebugFileExtensions =
        {
            ".pdb",
            ".mdb",
            ".dbg"
        };

        [MenuItem(MenuPath)]
        private static void RunFromMenu()
        {
            Run();
        }

        public static void RunBatchMode()
        {
            Run();
        }

        internal static void Run()
        {
            DateTime startedAt = DateTime.UtcNow;
            if (!BattleScreenCompleteHarnessValidation.Run())
            {
                throw new InvalidOperationException(
                    "Windows Release validation failed: complete regression " +
                    "harness did not pass.");
            }

            string projectRoot = GetProjectRoot();
            string outputDirectory = Path.Combine(
                projectRoot,
                RelativeOutputDirectory);
            string archivePath = Path.Combine(
                projectRoot,
                RelativeArchivePath);
            PrepareOutput(outputDirectory, archivePath);

            string executablePath = Build(outputDirectory);
            ValidateRequiredArtifacts(outputDirectory, executablePath);
            RemoveDebugArtifacts(outputDirectory);
            ValidateNoDebugArtifacts(outputDirectory);
            WarnForPlaceholderMetadata();

            string manifestPath = WriteManifest(outputDirectory);
            CreateArchive(outputDirectory, archivePath);
            WindowsDevelopmentPlayerSmokeValidation.Run(
                executablePath,
                Path.Combine(projectRoot, RelativeSmokeLogPath),
                "Windows Release Player");

            TimeSpan elapsed = DateTime.UtcNow - startedAt;
            Debug.Log(
                "Windows Release Build validation passed: complete regression " +
                "harness, non-development Windows x64 build, required artifact " +
                "and debug-output checks, SHA-256 manifest, ZIP package, and " +
                $"player startup smoke test · {elapsed.TotalSeconds:F1} " +
                $"seconds · {executablePath} · {manifestPath} · {archivePath}");
        }

        private static string Build(string outputDirectory)
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene != null && scene.enabled)
                .Select(scene => scene.path)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToArray();
            if (scenes.Length == 0)
            {
                throw new InvalidOperationException(
                    "Windows Release Build failed: no enabled build scenes.");
            }

            string missingScene = scenes.FirstOrDefault(
                path => !File.Exists(Path.GetFullPath(path)));
            if (!string.IsNullOrWhiteSpace(missingScene))
            {
                throw new FileNotFoundException(
                    "Windows Release Build failed: scene file not found.",
                    missingScene);
            }

            Directory.CreateDirectory(outputDirectory);
            string executablePath = Path.Combine(
                outputDirectory,
                ExecutableName);
            BuildPlayerOptions options = new()
            {
                scenes = scenes,
                locationPathName = executablePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Windows Release Build failed: " +
                    $"result={summary.result}, errors={summary.totalErrors}, " +
                    $"warnings={summary.totalWarnings}.");
            }

            Debug.Log(
                "Windows Release Build passed: " +
                $"{scenes.Length} scene(s), {summary.totalSize} bytes, " +
                $"{summary.totalTime.TotalSeconds:F1} seconds, " +
                executablePath);
            return executablePath;
        }

        private static void PrepareOutput(
            string outputDirectory,
            string archivePath)
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }
            if (File.Exists(archivePath))
            {
                File.Delete(archivePath);
            }
        }

        private static void ValidateRequiredArtifacts(
            string outputDirectory,
            string executablePath)
        {
            string dataDirectory = Path.Combine(
                outputDirectory,
                Path.GetFileNameWithoutExtension(ExecutableName) + "_Data");
            string[] requiredFiles =
            {
                executablePath,
                Path.Combine(outputDirectory, "UnityPlayer.dll")
            };
            string[] requiredDirectories =
            {
                dataDirectory,
                Path.Combine(outputDirectory, "MonoBleedingEdge"),
                Path.Combine(dataDirectory, "Managed")
            };
            string missing = requiredFiles
                .Where(path => !File.Exists(path))
                .Concat(requiredDirectories.Where(path =>
                    !Directory.Exists(path)))
                .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(missing))
            {
                throw new FileNotFoundException(
                    "Windows Release Build failed: required artifact missing.",
                    missing);
            }
        }

        private static void ValidateNoDebugArtifacts(string outputDirectory)
        {
            string debugDirectory = Directory
                .EnumerateDirectories(
                    outputDirectory,
                    "*",
                    SearchOption.AllDirectories)
                .FirstOrDefault(path =>
                    Path.GetFileName(path).IndexOf(
                        "BurstDebugInformation_DoNotShip",
                        StringComparison.OrdinalIgnoreCase) >= 0);
            string debugFile = Directory
                .EnumerateFiles(
                    outputDirectory,
                    "*",
                    SearchOption.AllDirectories)
                .FirstOrDefault(path => DebugFileExtensions.Contains(
                    Path.GetExtension(path),
                    StringComparer.OrdinalIgnoreCase));
            string found = debugDirectory ?? debugFile;
            if (!string.IsNullOrWhiteSpace(found))
            {
                throw new InvalidOperationException(
                    "Windows Release Build failed: debug-only artifact found: " +
                    found);
            }
        }

        private static void RemoveDebugArtifacts(string outputDirectory)
        {
            string[] debugDirectories = Directory
                .EnumerateDirectories(
                    outputDirectory,
                    "*",
                    SearchOption.AllDirectories)
                .Where(path => Path.GetFileName(path).IndexOf(
                    "BurstDebugInformation_DoNotShip",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderByDescending(path => path.Length)
                .ToArray();
            string[] debugFiles = Directory
                .EnumerateFiles(
                    outputDirectory,
                    "*",
                    SearchOption.AllDirectories)
                .Where(path => DebugFileExtensions.Contains(
                    Path.GetExtension(path),
                    StringComparer.OrdinalIgnoreCase))
                .ToArray();

            foreach (string file in debugFiles)
            {
                File.Delete(file);
            }
            foreach (string directory in debugDirectories)
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }

            if (debugDirectories.Length > 0 || debugFiles.Length > 0)
            {
                Debug.Log(
                    "Windows Release package excluded debug-only artifacts: " +
                    $"{debugDirectories.Length} director(ies), " +
                    $"{debugFiles.Length} file(s).");
            }
        }

        private static string WriteManifest(string outputDirectory)
        {
            string manifestPath = Path.Combine(outputDirectory, ManifestName);
            var lines = new List<string>
            {
                "# SHA-256 hashes for Windows Release package files",
                "# Paths are relative to the package root."
            };
            foreach (string file in Directory
                         .EnumerateFiles(
                             outputDirectory,
                             "*",
                             SearchOption.AllDirectories)
                         .Where(path => !string.Equals(
                             path,
                             manifestPath,
                             StringComparison.OrdinalIgnoreCase))
                         .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                string relative = MakeRelativePath(outputDirectory, file)
                    .Replace('\\', '/');
                lines.Add($"{ComputeSha256(file)}  {relative}");
            }

            File.WriteAllLines(
                manifestPath,
                lines,
                new UTF8Encoding(false));
            return manifestPath;
        }

        private static void CreateArchive(
            string outputDirectory,
            string archivePath)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(archivePath) ??
                GetProjectRoot());
            using FileStream stream = File.Create(archivePath);
            using var archive = new ZipArchive(
                stream,
                ZipArchiveMode.Create);
            foreach (string file in Directory
                         .EnumerateFiles(
                             outputDirectory,
                             "*",
                             SearchOption.AllDirectories)
                         .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                string relative = MakeRelativePath(outputDirectory, file)
                    .Replace('\\', '/');
                ZipArchiveEntry entry = archive.CreateEntry(
                    relative,
                    System.IO.Compression.CompressionLevel.Optimal);
                using Stream input = File.OpenRead(file);
                using Stream output = entry.Open();
                input.CopyTo(output);
            }
        }

        private static void WarnForPlaceholderMetadata()
        {
            string identifier = PlayerSettings.GetApplicationIdentifier(
                NamedBuildTarget.Standalone);
            if (string.Equals(
                    PlayerSettings.companyName,
                    "DefaultCompany",
                    StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning(
                    "Windows Release metadata remains undecided: companyName " +
                    "is DefaultCompany.");
            }
            if (string.Equals(
                    PlayerSettings.productName,
                    "프로젝트Cr",
                    StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning(
                    "Windows Release metadata remains undecided: productName " +
                    "is 프로젝트Cr.");
            }
            if (string.IsNullOrWhiteSpace(identifier) ||
                identifier.IndexOf(
                    "DefaultCompany",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Debug.LogWarning(
                    "Windows Release metadata remains undecided: Standalone " +
                    $"application identifier is empty or derived: {identifier}.");
            }
        }

        private static string ComputeSha256(string path)
        {
            using SHA256 sha256 = SHA256.Create();
            using FileStream stream = File.OpenRead(path);
            return BitConverter.ToString(sha256.ComputeHash(stream))
                .Replace("-", string.Empty)
                .ToLowerInvariant();
        }

        private static string MakeRelativePath(string root, string path)
        {
            string normalizedRoot = Path.GetFullPath(root)
                .TrimEnd(Path.DirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
            Uri rootUri = new(normalizedRoot);
            Uri pathUri = new(Path.GetFullPath(path));
            return Uri.UnescapeDataString(
                rootUri.MakeRelativeUri(pathUri).ToString())
                .Replace('/', Path.DirectorySeparatorChar);
        }

        private static string GetProjectRoot()
        {
            string projectRoot =
                Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                throw new InvalidOperationException(
                    "Windows Release Build failed: project root not found.");
            }
            return projectRoot;
        }
    }
}
