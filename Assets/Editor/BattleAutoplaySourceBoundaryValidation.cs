using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleAutoplaySourceBoundaryValidation
    {
        private static readonly string[] Paths =
        {
            "Assets/Scripts/Prototype/BattleAutoplayViewModel.cs",
            "Assets/Editor/FullRunPlayerActionEndToEndValidation.cs"
        };

        private static readonly string[] ForbiddenDirectMutations =
        {
            ".ApplyDamage(",
            "LivingEnemies.TryRemove(",
            "BattleRuntimeSessionService.TryFinalizeTerminalOutcome("
        };

        [MenuItem("Have a Break/Validate Battle Autoplay Source Boundary")]
        private static void ValidateFromMenu()
        {
            Debug.Log(Validate()
                ? "Battle autoplay source boundary passed."
                : "Battle autoplay source boundary failed.");
        }

        internal static bool Validate()
        {
            foreach (string path in Paths)
            {
                if (!File.Exists(path))
                {
                    Debug.LogError($"Battle autoplay source is missing: {path}");
                    return false;
                }

                string source = File.ReadAllText(path);
                foreach (string forbidden in ForbiddenDirectMutations)
                {
                    if (!source.Contains(forbidden, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    Debug.LogError(
                        $"Direct battle mutation remains in {path}: {forbidden}");
                    return false;
                }
            }

            string autoplay = File.ReadAllText(Paths[0]);
            string fullRun = File.ReadAllText(Paths[1]);
            if (!autoplay.Contains(
                    "BattleScreenViewModel battleScreen",
                    StringComparison.Ordinal) ||
                !autoplay.Contains(
                    "battleScreen.TryPlayCard(",
                    StringComparison.Ordinal) ||
                !autoplay.Contains(
                    "battleScreen.TryAttack(",
                    StringComparison.Ordinal) ||
                !autoplay.Contains(
                    "battleScreen.TryEndPlayerTurn(",
                    StringComparison.Ordinal) ||
                !fullRun.Contains(
                    "BattleAutoplayViewModel autoplay",
                    StringComparison.Ordinal) ||
                !fullRun.Contains(
                    "autoplay.TryRun(",
                    StringComparison.Ordinal))
            {
                Debug.LogError(
                    "Battle autoplay player-action command connection is invalid.");
                return false;
            }

            return true;
        }
    }
}
