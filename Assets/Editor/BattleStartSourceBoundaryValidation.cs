using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleStartSourceBoundaryValidation
    {
        private static readonly string[] LegacyPaths =
        {
            "Assets/Scripts/Prototype/RuntimePrototypeScreen.Part05.cs",
            "Assets/Editor/IntegratedRunPrototypeWindow.Part04.cs"
        };

        private static readonly string[] ForbiddenLegacyDependencies =
        {
            "RunEncounterPoolService.TryResolve",
            "RunEncounterProgressService.TryBegin",
            "private void BeginSelectedBattle()"
        };

        private static readonly string[] ConnectionPaths =
        {
            "Assets/Scripts/Prototype/RuntimePrototypeScreen.BattleStart.cs",
            "Assets/Editor/IntegratedRunPrototypeWindow.BattleStart.cs"
        };

        private static readonly string[] ForbiddenConnectionDependencies =
        {
            "RunEncounterPoolService.TryResolve",
            "RunEncounterProgressService.TryBegin",
            "IntegratedRunSaveService.TrySave"
        };

        [MenuItem("Have a Break/Validate Battle Start Source Boundary")]
        private static void ValidateFromMenu()
        {
            Debug.Log(Validate()
                ? "Battle start source boundary passed."
                : "Battle start source boundary failed.");
        }

        internal static bool Validate()
        {
            foreach (string path in LegacyPaths)
            {
                if (!File.Exists(path))
                {
                    Debug.LogError($"Battle start legacy source is missing: {path}");
                    return false;
                }

                string source = File.ReadAllText(path);
                foreach (string forbidden in ForbiddenLegacyDependencies)
                {
                    if (!source.Contains(forbidden, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    Debug.LogError(
                        $"Direct battle start dependency remains in {path}: " +
                        forbidden);
                    return false;
                }
            }

            foreach (string path in ConnectionPaths)
            {
                if (!File.Exists(path))
                {
                    Debug.LogError($"Battle start connection is missing: {path}");
                    return false;
                }

                string source = File.ReadAllText(path);
                if (!source.Contains(
                        "BattleStartViewModel battleStart",
                        StringComparison.Ordinal) ||
                    !source.Contains(
                        "battleStart.TryStart(",
                        StringComparison.Ordinal) ||
                    !source.Contains(
                        "battleScreen.Reset()",
                        StringComparison.Ordinal))
                {
                    Debug.LogError(
                        $"Battle start ViewModel connection is invalid: {path}");
                    return false;
                }

                foreach (string forbidden in ForbiddenConnectionDependencies)
                {
                    if (!source.Contains(forbidden, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    Debug.LogError(
                        $"Direct battle start service remains in {path}: " +
                        forbidden);
                    return false;
                }
            }

            return true;
        }
    }
}
