using System;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleScreenCompleteHarnessValidation
    {
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
            bool autoplayBoundary =
                BattleAutoplaySourceBoundaryValidation.Validate();
            bool autoplay = BattleAutoplayViewModelValidation.Validate();
            bool fullRun = FullRunEndToEndValidation.Validate();
            bool playerActionFullRun =
                FullRunPlayerActionEndToEndValidation.Validate();
            bool valid = existing && screenBoundary && screen &&
                         settlementBoundary && settlement &&
                         startBoundary && start &&
                         lifecycleBoundary && lifecycle &&
                         autoplayBoundary && autoplay && fullRun &&
                         playerActionFullRun;
            if (valid)
            {
                Debug.Log(
                    "Complete test harness with battle screen passed: " +
                    "existing runtime, source boundaries, value-type comparison " +
                    "guard, display snapshot flow, settlement command flow, " +
                    "battle start checkpoint flow, run lifecycle flow, battle " +
                    "player-action autoplay boundary and command flow, full run " +
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
                    $"lifecycle={lifecycle}, autoplayBoundary=" +
                    $"{autoplayBoundary}, autoplay={autoplay}, fullRun=" +
                    $"{fullRun}, playerActionFullRun={playerActionFullRun}");
            }

            return valid;
        }
    }
}
