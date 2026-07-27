using System;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class WindowsReleaseReadinessValidation
    {
        private const string MenuPath =
            "Have a Break/Tests/Run Windows Release Readiness Validation";

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
    }
}
