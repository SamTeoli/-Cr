using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace HaveABreak.Editor
{
    internal static class WindowsDevelopmentPlayerSmokeValidation
    {
        private const string MenuPath =
            "Have a Break/Tests/Run Windows Development Player Smoke Test";
        private const string RelativeExecutablePath =
            "Builds/WindowsDevelopment/HaveABreak.exe";
        private const string RelativeLogPath =
            "Logs/WindowsDevelopmentPlayerSmoke.log";
        private const int StartupProbeMilliseconds = 5000;
        private const int ShutdownWaitMilliseconds = 5000;

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
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                throw new InvalidOperationException(
                    "Windows player smoke test failed: project root not found.");
            }

            string executablePath = Path.Combine(
                projectRoot,
                RelativeExecutablePath);
            if (!File.Exists(executablePath))
            {
                executablePath = WindowsDevelopmentBuildValidation.Build();
            }

            string logPath = Path.Combine(projectRoot, RelativeLogPath);
            Directory.CreateDirectory(Path.GetDirectoryName(logPath) ??
                                      projectRoot);
            if (File.Exists(logPath))
            {
                File.Delete(logPath);
            }

            ProcessStartInfo startInfo = new()
            {
                FileName = executablePath,
                Arguments =
                    "-batchmode -nographics -logFile " +
                    Quote(logPath),
                WorkingDirectory = Path.GetDirectoryName(executablePath) ??
                                   projectRoot,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process player = Process.Start(startInfo);
            if (player == null)
            {
                throw new InvalidOperationException(
                    "Windows player smoke test failed: player did not start.");
            }

            bool exitedDuringProbe =
                player.WaitForExit(StartupProbeMilliseconds);
            int? earlyExitCode = exitedDuringProbe
                ? player.ExitCode
                : null;

            if (!exitedDuringProbe)
            {
                try
                {
                    if (!player.HasExited)
                    {
                        player.Kill();
                    }
                }
                catch (InvalidOperationException)
                {
                    // The player exited between the probe and shutdown request.
                }

                if (!player.WaitForExit(ShutdownWaitMilliseconds))
                {
                    throw new TimeoutException(
                        "Windows player smoke test failed: player process " +
                        "did not stop after the startup probe.");
                }
            }

            WaitForLog(logPath);
            string log = File.ReadAllText(logPath);
            string failureMarker = FindFailureMarker(log);
            if (!string.IsNullOrWhiteSpace(failureMarker))
            {
                throw new InvalidOperationException(
                    "Windows player smoke test failed: Player.log contains " +
                    failureMarker + ".");
            }

            if (earlyExitCode.HasValue && earlyExitCode.Value != 0)
            {
                throw new InvalidOperationException(
                    "Windows player smoke test failed: player exited during " +
                    $"startup with code {earlyExitCode.Value}.");
            }

            Debug.Log(
                "Windows Development Player smoke test passed: " +
                $"startup probe {StartupProbeMilliseconds / 1000.0:F1}s, " +
                $"earlyExit={(earlyExitCode.HasValue ? earlyExitCode.Value.ToString() : "no")}, " +
                logPath);
        }

        private static void WaitForLog(string logPath)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(5);
            while (!File.Exists(logPath) && DateTime.UtcNow < deadline)
            {
                Thread.Sleep(100);
            }

            if (!File.Exists(logPath))
            {
                throw new FileNotFoundException(
                    "Windows player smoke test failed: Player.log was not created.",
                    logPath);
            }
        }

        private static string FindFailureMarker(string log)
        {
            string[] markers =
            {
                "Crash!!!",
                "Unhandled Exception",
                "Aborting batchmode due to failure",
                "Fatal error"
            };

            foreach (string marker in markers)
            {
                if (log.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return marker;
                }
            }

            return null;
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }
    }
}
