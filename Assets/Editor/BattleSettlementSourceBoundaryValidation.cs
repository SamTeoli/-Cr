using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleSettlementSourceBoundaryValidation
    {
        private static readonly string[] LegacyPaths =
        {
            "Assets/Scripts/Prototype/RuntimePrototypeScreen.Part05.cs",
            "Assets/Editor/IntegratedRunPrototypeWindow.Part05.cs"
        };

        private static readonly string[] ForbiddenDependencies =
        {
            "RunEncounterProgressService.TrySettleActive",
            "RunEncounterProgressService.TryCompleteActive",
            "VictoryRewards.TryClaimGold",
            "BattleVictoryPermanentRewardService.TryCreate",
            "RunCampaignService.MarkBattleReward",
            "private void SettleBattle()"
        };

        private static readonly string[] ConnectionPaths =
        {
            "Assets/Scripts/Prototype/RuntimePrototypeScreen.Settlement.cs",
            "Assets/Editor/IntegratedRunPrototypeWindow.Settlement.cs"
        };

        [MenuItem("Have a Break/Validate Battle Settlement Source Boundary")]
        private static void ValidateFromMenu()
        {
            Debug.Log(Validate()
                ? "Battle settlement source boundary passed."
                : "Battle settlement source boundary failed.");
        }

        internal static bool Validate()
        {
            foreach (string path in LegacyPaths)
            {
                if (!File.Exists(path))
                {
                    Debug.LogError($"Battle settlement source is missing: {path}");
                    return false;
                }

                string source = File.ReadAllText(path);
                foreach (string forbidden in ForbiddenDependencies)
                {
                    if (!source.Contains(
                            forbidden,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    Debug.LogError(
                        $"Direct battle settlement dependency remains in " +
                        $"{path}: {forbidden}");
                    return false;
                }
            }

            foreach (string path in ConnectionPaths)
            {
                if (!File.Exists(path))
                {
                    Debug.LogError(
                        $"Battle settlement connection is missing: {path}");
                    return false;
                }

                string source = File.ReadAllText(path);
                if (!source.Contains(
                        "BattleSettlementViewModel battleSettlement",
                        StringComparison.Ordinal) ||
                    !source.Contains(
                        "battleSettlement.TrySettle(campaign, progress)",
                        StringComparison.Ordinal) ||
                    !source.Contains(
                        "battleScreen.Reset()",
                        StringComparison.Ordinal))
                {
                    Debug.LogError(
                        $"Battle settlement ViewModel connection is invalid: {path}");
                    return false;
                }
            }

            return true;
        }
    }
}
