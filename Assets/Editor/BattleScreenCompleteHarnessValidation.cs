using System;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleScreenCompleteHarnessValidation
    {
        internal static bool? LastResult { get; private set; }
        internal static DateTime LastCompletedAtUtc { get; private set; }
        internal static TimeSpan LastDuration { get; private set; }

        internal static bool TryGetRecentPass(
            TimeSpan maximumAge,
            out string summary)
        {
            bool recent = LastResult == true &&
                          LastCompletedAtUtc != default &&
                          DateTime.UtcNow - LastCompletedAtUtc <= maximumAge;
            summary = recent
                ? $"최근 전체 하네스 통과 결과 · " +
                  $"{LastCompletedAtUtc:O} · {LastDuration.TotalSeconds:F1}초"
                : null;
            return recent;
        }

        [MenuItem("Have a Break/Tests/Run Complete Test Harness With Battle Screen")]
        private static void RunFromMenu()
        {
            Run();
        }

        public static void RunBatchMode()
        {
            if (!Run())
            {
                throw new InvalidOperationException(
                    "Complete test harness with battle screen failed. " +
                    "Check the first Console error.");
            }
        }

        internal static bool Run()
        {
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            bool existing =
                BattleRuntimeFullRegressionValidation.RunCompleteTestHarness();
            bool screenBoundary =
                BattleScreenSourceBoundaryValidation.Validate();
            bool screen = BattleScreenViewModelValidation.Validate();
            bool settlementBoundary =
                BattleSettlementSourceBoundaryValidation.Validate();
            bool settlement =
                BattleSettlementCommandFlowValidation.Validate();
            bool startBoundary =
                BattleStartSourceBoundaryValidation.Validate();
            bool start = BattleStartCommandFlowValidation.Validate();
            bool lifecycleBoundary =
                RunLifecycleSourceBoundaryValidation.Validate();
            bool lifecycle = RunLifecycleViewModelValidation.Validate();
            bool finalUiRouter = RuntimeGameScreenRouterValidation.Validate();
            bool finalUiRoot = RuntimeGameUiRootValidation.Validate();
            bool manualE2ETool =
                RunEndToEndManualValidationToolValidation.Validate();
            bool autoplayBoundary =
                BattleAutoplaySourceBoundaryValidation.Validate();
            bool autoplay = BattleAutoplayViewModelValidation.Validate();
            bool fullRun = FullRunEndToEndValidation.Validate();
            bool playerActionFullRun =
                FullRunPlayerActionEndToEndValidation.Validate();
            bool valid = existing && screenBoundary && screen &&
                         settlementBoundary && settlement &&
                         startBoundary && start &&
                         lifecycleBoundary && lifecycle && finalUiRouter &&
                         finalUiRoot &&
                         manualE2ETool &&
                         autoplayBoundary && autoplay && fullRun &&
                         playerActionFullRun;
            if (valid)
            {
                Debug.Log(
                    "Complete test harness with battle screen passed: " +
                    "existing runtime, source boundaries, value-type comparison " +
                    "guard, display snapshot flow, settlement command flow, " +
                    "battle start checkpoint flow, run lifecycle flow, final " +
                    "UI screen routing and UGUI start screen, manual " +
                    "run E2E validation tool, battle player-action autoplay " +
                    "boundary and command flow, full run " +
                    "end-to-end progression, and player-action full run " +
                    "progression.");
            }
            else
            {
                Debug.LogError(
                    "Complete test harness with battle screen failed. " +
                    $"existing={existing}, screenBoundary={screenBoundary}, " +
                    $"screen={screen}, settlementBoundary={settlementBoundary}, " +
                    $"settlement={settlement}, startBoundary={startBoundary}, " +
                    $"start={start}, lifecycleBoundary={lifecycleBoundary}, " +
                    $"lifecycle={lifecycle}, finalUiRouter={finalUiRouter}, " +
                    $"finalUiRoot={finalUiRoot}, " +
                    $"manualE2ETool={manualE2ETool}, " +
                    $"autoplayBoundary=" +
                    $"{autoplayBoundary}, autoplay={autoplay}, fullRun=" +
                    $"{fullRun}, playerActionFullRun={playerActionFullRun}");
            }

            stopwatch.Stop();
            LastResult = valid;
            LastCompletedAtUtc = DateTime.UtcNow;
            LastDuration = stopwatch.Elapsed;
            return valid;
        }
    }
}
