using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleScreenSourceBoundaryValidation
    {
        private static readonly string[] DisplayPaths =
        {
            "Assets/Scripts/Prototype/RuntimePrototypeScreen.Part02.cs",
            "Assets/Scripts/Prototype/RuntimePrototypeScreen.Part03.cs",
            "Assets/Editor/IntegratedRunPrototypeWindow.Part02.cs",
            "Assets/Editor/IntegratedRunPrototypeWindow.Part03.cs"
        };

        private static readonly string[] ForbiddenDisplayDependencies =
        {
            "BattleRuntimeState",
            "BattleRuntimeSessionState",
            "BattleRuntimeEncounterContext",
            "BattleRuntimeEnemyPatternService",
            "BattleRuntimePlayerCardActionService",
            "BattleRuntimePlayerAttackService",
            "PrototypeConsumableService.TryUseInBattle",
            ".EventLog.Events",
            "BuildEnemyIntentLabels",
            "DescribeEnemyCommand",
            "DescribeEnemyStatus",
            "DescribeCommonStatus",
            "battleActions"
        };

        private static readonly string[] ForbiddenFormattingComparisons =
        {
            "ability == null",
            "ability != null",
            "command.Ability == null",
            "command.Ability != null"
        };

        [MenuItem("Have a Break/Validate Battle Screen Source Boundary")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            if (valid)
            {
                Debug.Log("Battle screen source boundary passed.");
            }
            else
            {
                Debug.LogError("Battle screen source boundary failed.");
            }
        }

        internal static bool Validate()
        {
            foreach (string path in DisplayPaths)
            {
                if (!File.Exists(path))
                {
                    Debug.LogError($"Battle screen source is missing: {path}");
                    return false;
                }

                string source = File.ReadAllText(path);
                if (!source.Contains(
                        "BattleScreenSnapshot",
                        StringComparison.Ordinal))
                {
                    Debug.LogError(
                        $"Battle screen snapshot is not used: {path}");
                    return false;
                }

                foreach (string forbidden in ForbiddenDisplayDependencies)
                {
                    if (!source.Contains(
                            forbidden,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    Debug.LogError(
                        $"Direct battle display dependency remains in " +
                        $"{path}: {forbidden}");
                    return false;
                }
            }

            return ValidateOwner(
                       "Assets/Scripts/Prototype/RuntimePrototypeScreen.cs") &&
                   ValidateOwner(
                       "Assets/Editor/IntegratedRunPrototypeWindow.cs") &&
                   ValidateFormattingComparisons();
        }

        private static bool ValidateOwner(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"Battle screen owner is missing: {path}");
                return false;
            }

            string source = File.ReadAllText(path);
            bool valid = source.Contains(
                             "BattleScreenViewModel battleScreen",
                             StringComparison.Ordinal) &&
                         !source.Contains(
                             "BattlePlayerActionViewModel battleActions",
                             StringComparison.Ordinal);
            if (!valid)
            {
                Debug.LogError(
                    $"Battle screen owner boundary is invalid: {path}");
            }

            return valid;
        }

        private static bool ValidateFormattingComparisons()
        {
            const string path =
                "Assets/Scripts/Prototype/BattleScreenViewModel.Formatting.cs";
            if (!File.Exists(path))
            {
                Debug.LogError($"Battle screen formatting source is missing: {path}");
                return false;
            }

            string source = File.ReadAllText(path);
            foreach (string forbidden in ForbiddenFormattingComparisons)
            {
                if (!source.Contains(forbidden, StringComparison.Ordinal))
                {
                    continue;
                }

                Debug.LogError(
                    $"Value-type null comparison remains in {path}: {forbidden}");
                return false;
            }

            return true;
        }
    }
}
