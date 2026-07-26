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
            bool boundary = BattleScreenSourceBoundaryValidation.Validate();
            bool screen = BattleScreenViewModelValidation.Validate();
            bool valid = existing && boundary && screen;
            if (valid)
            {
                Debug.Log(
                    "Complete test harness with battle screen passed: " +
                    "existing runtime, source boundary, value-type comparison " +
                    "guard, and display snapshot flow.");
            }
            else
            {
                Debug.LogError(
                    "Complete test harness with battle screen failed.");
            }

            return valid;
        }
    }
}
