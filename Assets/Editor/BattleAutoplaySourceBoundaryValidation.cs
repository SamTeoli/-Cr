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
            string[] requiredConnections =
            {
                "BattleScreenViewModel battleScreen",
                "battleScreen.TryPlayCard(",
                "battleScreen.TryAttack(",
                "battleScreen.TryEndPlayerTurn(",
                "HashSet<string> attemptedAttackers",
                "attemptedAttackers.Contains(value.BattleCardId)",
                "attemptedAttackers.Add(attacker.BattleCardId)"
            };
            foreach (string required in requiredConnections)
            {
                if (autoplay.Contains(required, StringComparison.Ordinal))
                {
                    continue;
                }

                Debug.LogError(
                    $"Battle autoplay command connection is missing: {required}");
                return false;
            }

            string fullRun = File.ReadAllText(Paths[1]);
            if (!fullRun.Contains(
                    "BattleAutoplayViewModel autoplay",
                    StringComparison.Ordinal) ||
                !fullRun.Contains(
                    "autoplay.TryRun(",
                    StringComparison.Ordinal))
            {
                Debug.LogError(
                    "Player-action full run is not connected to battle autoplay.");
                return false;
            }

            return true;
        }
    }
}
