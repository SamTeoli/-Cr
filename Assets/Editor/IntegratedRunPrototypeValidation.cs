using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.EditorTools
{
    internal static class IntegratedRunPrototypeValidation
    {
        [MenuItem("Have a Break/Validate Integrated Run Prototype")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            EditorUtility.DisplayDialog(
                "Integrated Run Prototype Validation",
                valid
                    ? "Integrated run, nodes, shop, consumables, and encounter grades passed."
                    : "Integrated run validation failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            bool valid = ValidateCampaignProgression() &&
                         ValidateRunMutations() &&
                         ValidateConsumableDefinitions() &&
                         ValidateEncounterGrades() &&
                         ValidateRuntimePrototypeConfig();
            if (valid)
            {
                Debug.Log(
                    "Integrated run prototype passed: nodes, shop, events, rest, consumables, and graded encounters.");
            }
            else
            {
                Debug.LogError("Integrated run prototype validation failed.");
            }

            return valid;
        }

        private static bool ValidateCampaignProgression()
        {
            RunCampaignState campaign = new(20260722);
            RunBattleState run = new(30, 20, 200);
            IReadOnlyList<RunNodeChoice> choices =
                RunCampaignService.GetChoices(campaign);
            if (choices.Count < 2 || choices.Count > 4 ||
                !RunCampaignService.TrySelectNode(
                    campaign, choices[0].NodeId, out _))
            {
                return false;
            }

            switch (campaign.ActiveNode.NodeType)
            {
                case RunNodeType.Shop:
                    if (!RunCampaignService.TryBuyConsumable(
                            campaign, run,
                            PrototypeConsumableCatalog.HealingPotion, out _) ||
                        !RunCampaignService.TryRerollShop(
                            campaign, run, out _) ||
                        !RunCampaignService.TryLeaveShop(
                            campaign, run, out _))
                    {
                        return false;
                    }
                    break;
                case RunNodeType.SituationEvent:
                    if (!RunCampaignService.TryResolveSituationEvent(
                            campaign, run, out _, out _))
                    {
                        return false;
                    }
                    break;
                case RunNodeType.RestOrUpgrade:
                    if (!RunCampaignService.TryRest(
                            campaign, run, out _, out _))
                    {
                        return false;
                    }
                    break;
                default:
                    RunCampaignService.MarkBattleReward(
                        campaign, BattleOutcome.Victory);
                    RunCampaignService.CompleteBattleReward(campaign);
                    break;
            }

            return campaign.CompletedNodeCount == 1 &&
                   campaign.Phase == RunCampaignPhase.NodeSelection &&
                   RunCampaignService.GetChoices(campaign).Count >= 2;
        }

        private static bool ValidateRunMutations()
        {
            RunBattleState run = new(
                30, 20, 100,
                new[] { PrototypeConsumableCatalog.EnchantHammer });
            return run.TrySpendGold(25) && run.Gold == 75 &&
                   run.ApplyHealing(5) == 5 && run.CurrentHealth == 25 &&
                   run.ApplyDamage(3) == 3 && run.CurrentHealth == 22 &&
                   run.IncreaseMaximumHealth(2) == 2 &&
                   run.MaximumHealth == 32 && run.CurrentHealth == 24 &&
                   run.RemoveConsumableItem(
                       PrototypeConsumableCatalog.EnchantHammer) &&
                   run.ConsumableItemIds.Count == 0;
        }

        private static bool ValidateConsumableDefinitions()
        {
            IReadOnlyList<PrototypeConsumableDefinition> items =
                PrototypeConsumableCatalog.All;
            return items.Count == 4 &&
                   items.All(item => item != null &&
                       !string.IsNullOrWhiteSpace(item.ItemId) &&
                       !string.IsNullOrWhiteSpace(item.DisplayName) &&
                       item.ShopPrice > 0) &&
                   items.Select(item => item.ItemId)
                       .Distinct(StringComparer.OrdinalIgnoreCase).Count() ==
                   items.Count;
        }

        private static bool ValidateEncounterGrades()
        {
            EncounterDatabase database =
                AssetDatabase.LoadAssetAtPath<EncounterDatabase>(
                    "Assets/GameData/EncounterDatabase.asset");
            if (database == null || database.GetValidationErrors().Count > 0)
            {
                return false;
            }

            Dictionary<string, BattleEncounterGrade> expected = new()
            {
                ["TEST-ENCOUNTER-PROTOTYPE-01"] =
                    BattleEncounterGrade.Normal,
                ["TEST-ENCOUNTER-PROTOTYPE-ELITE"] =
                    BattleEncounterGrade.Elite,
                ["TEST-ENCOUNTER-PROTOTYPE-MIDBOSS"] =
                    BattleEncounterGrade.MidBoss,
                ["TEST-ENCOUNTER-PROTOTYPE-FINALBOSS"] =
                    BattleEncounterGrade.FinalBoss
            };
            return expected.All(pair =>
                database.TryGetEncounter(pair.Key, out EncounterData encounter) &&
                encounter.EncounterGrade == pair.Value);
        }

        private static bool ValidateRuntimePrototypeConfig()
        {
            RuntimePrototypeConfig config =
                Resources.Load<RuntimePrototypeConfig>(
                    "GameData/RuntimePrototypeConfig");
            if (config == null || !config.IsReady)
            {
                return false;
            }

            Dictionary<string, BattleEncounterGrade> expected = new()
            {
                [config.NormalEncounterId] = BattleEncounterGrade.Normal,
                [config.EliteEncounterId] = BattleEncounterGrade.Elite,
                [config.MidBossEncounterId] = BattleEncounterGrade.MidBoss,
                [config.FinalBossEncounterId] = BattleEncounterGrade.FinalBoss
            };
            return expected.Count == 4 && expected.All(pair =>
                config.EncounterDatabase.TryGetEncounter(
                    pair.Key, out EncounterData encounter) &&
                encounter.EncounterGrade == pair.Value);
        }
    }
}
