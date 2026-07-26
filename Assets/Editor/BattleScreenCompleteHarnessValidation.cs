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
            bool valid = existing && screenBoundary && screen &&
                         settlementBoundary && settlement &&
                         startBoundary && start;
            if (valid)
            {
                Debug.Log(
                    "Complete test harness with battle screen passed: " +
                    "existing runtime, source boundaries, value-type comparison " +
                    "guard, display snapshot flow, settlement command flow, and " +
                    "battle start checkpoint flow.");
            }
            else
            {
                Debug.LogError(
                    "Complete test harness with battle screen failed. " +
                    $"existing={existing}, screenBoundary={screenBoundary}, " +
                    $"screen={screen}, settlementBoundary={settlementBoundary}, " +
                    $"settlement={settlement}, startBoundary={startBoundary}, " +
                    $"start={start}");
            }

            return valid;
        }
    }
}
