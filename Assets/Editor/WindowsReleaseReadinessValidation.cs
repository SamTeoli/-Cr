using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class WindowsReleaseReadinessValidation
    {
        private const string MenuPath =
            "Have a Break/Tests/Run Windows Release Readiness Validation";

        private static readonly string[] BuildTouchedSettingPaths =
        {
            "Assets/DefaultVolumeProfile.asset",
            "Assets/Settings/UniversalRP.asset",
            "Assets/UniversalRenderPipelineGlobalSettings.asset",
            "ProjectSettings/ProjectSettings.asset",
            "ProjectSettings/UnityConnectSettings.asset"
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
            List<FileTimestampSnapshot> settingSnapshots =
                CaptureBuildTouchedSettings();

            try
            {
                if (!BattleScreenCompleteHarnessValidation.Run())
                {
                    throw new InvalidOperationException(
                        "Windows release readiness failed: complete regression " +
                        "harness did not pass.");
                }

                string executablePath = WindowsDevelopmentBuildValidation.Build();
                WindowsDevelopmentPlayerSmokeValidation.Run();

                TimeSpan elapsed = DateTime.UtcNow - startedAt;
                Debug.Log(
                    "Windows release readiness validation passed: complete " +
                    "regression harness, Development Build, and player startup " +
                    $"smoke test · {elapsed.TotalSeconds:F1} seconds · " +
                    executablePath);
            }
            finally
            {
                RestoreUnchangedSettingTimestamps(settingSnapshots);
            }
        }

        private static List<FileTimestampSnapshot> CaptureBuildTouchedSettings()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            var snapshots = new List<FileTimestampSnapshot>();
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                return snapshots;
            }

            for (int i = 0; i < BuildTouchedSettingPaths.Length; i++)
            {
                string fullPath = Path.Combine(
                    projectRoot,
                    BuildTouchedSettingPaths[i].Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(fullPath))
                {
                    continue;
                }

                snapshots.Add(new FileTimestampSnapshot(
                    fullPath,
                    File.GetLastWriteTimeUtc(fullPath),
                    File.ReadAllBytes(fullPath)));
            }

            return snapshots;
        }

        private static void RestoreUnchangedSettingTimestamps(
            IReadOnlyList<FileTimestampSnapshot> snapshots)
        {
            for (int i = 0; i < snapshots.Count; i++)
            {
                FileTimestampSnapshot snapshot = snapshots[i];
                if (!File.Exists(snapshot.FullPath))
                {
                    continue;
                }

                byte[] currentBytes = File.ReadAllBytes(snapshot.FullPath);
                if (!BytesEqual(snapshot.Contents, currentBytes))
                {
                    continue;
                }

                File.SetLastWriteTimeUtc(snapshot.FullPath, snapshot.LastWriteTimeUtc);
            }
        }

        private static bool BytesEqual(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }

        private sealed class FileTimestampSnapshot
        {
            public FileTimestampSnapshot(
                string fullPath,
                DateTime lastWriteTimeUtc,
                byte[] contents)
            {
                FullPath = fullPath;
                LastWriteTimeUtc = lastWriteTimeUtc;
                Contents = contents;
            }

            public string FullPath { get; }
            public DateTime LastWriteTimeUtc { get; }
            public byte[] Contents { get; }
        }
    }
}
