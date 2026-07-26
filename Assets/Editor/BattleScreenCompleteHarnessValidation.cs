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
            bool screen = BattleScreenViewModelValidation.Validate();
            bool valid = existing && screen;
            if (valid)
            {
                Debug.Log(
                    "Complete test harness with battle screen passed.");
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
