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
                         ValidatePersistedNodePath() &&
                         ValidateRunMutations() &&
                         ValidateConsumableDefinitions() &&
                         ValidateEventAndShopSlotData() &&
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

        private static bool ValidatePersistedNodePath()
        {
            RunCampaignState campaign = new(20260722);
            IReadOnlyList<RunNodeChoice> first =
                RunCampaignService.GetChoices(campaign);
            string json = JsonUtility.ToJson(campaign);
            RunCampaignState restored =
                JsonUtility.FromJson<RunCampaignState>(json);
            IReadOnlyList<RunNodeChoice> restoredChoices =
                RunCampaignService.GetChoices(restored);
            RunNodeChoice selected =
                restoredChoices.FirstOrDefault(value => value.IsBattle);
            if (first.Count != restoredChoices.Count || first.Count == 0 ||
                first.Select(value => value.NodeId).SequenceEqual(
                    restoredChoices.Select(value => value.NodeId)) == false ||
                selected == null ||
                !RunCampaignService.TrySelectNode(
                    restored, selected.NodeId, out _))
            {
                return false;
            }

            RunCampaignService.MarkBattleReward(restored, BattleOutcome.Victory);
            RunCampaignService.CompleteBattleReward(restored);

            IReadOnlyList<RunNodeChoice> next =
                RunCampaignService.GetChoices(restored);
            return restored.SelectedNodePath.Count == 1 && next.Count > 0 &&
                   next.All(value => value.PreviousNodeId ==
                       restored.SelectedNodePath[0]);
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
                {
                    ShopEconomyConfig economy =
                        AssetDatabase.LoadAssetAtPath<ShopEconomyConfig>(
                            "Assets/GameData/ShopEconomyConfig.asset");
                    if (!RunCampaignService.TryBuyConsumable(
                            campaign, run,
                            PrototypeConsumableCatalog.HealingPotion, out _) ||
                        !RunCampaignService.TryRerollShop(
                            campaign, run, economy, out _) ||
                        !RunCampaignService.TryLeaveShop(
                            campaign, run, out _))
                    {
                        return false;
                    }
                    break;
                }
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
            return run.CanSpendGold(25) && !run.CanSpendGold(101) &&
                   run.TrySpendGold(25) && run.Gold == 75 &&
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
            ConsumableDatabase database =
                AssetDatabase.LoadAssetAtPath<ConsumableDatabase>(
                    "Assets/GameData/ConsumableDatabase.asset");
            IReadOnlyList<ConsumableData> items = database?.Consumables;
            return database != null && items != null && items.Count == 4 &&
                   database.GetValidationErrors().Count == 0 &&
                   items.All(item => item != null &&
                       !string.IsNullOrWhiteSpace(item.ItemId) &&
                       !string.IsNullOrWhiteSpace(item.DisplayName) &&
                       item.ShopPrice > 0) &&
                   items.Select(item => item.ItemId)
                       .Distinct(StringComparer.OrdinalIgnoreCase).Count() ==
                   items.Count &&
                   ReferenceEquals(PrototypeConsumableCatalog.Find(
                       PrototypeConsumableCatalog.HealingPotion), items[0]) &&
                   ValidateInvalidConsumableDefinitions();
        }

        private static bool ValidateEventAndShopSlotData()
        {
            EnchantDatabase enchants = AssetDatabase.LoadAssetAtPath<EnchantDatabase>(
                "Assets/GameData/EnchantDatabase.asset");
            SituationEventDatabase events =
                AssetDatabase.LoadAssetAtPath<SituationEventDatabase>(
                    "Assets/GameData/SituationEventDatabase.asset");
            ShopEconomyConfig economy =
                AssetDatabase.LoadAssetAtPath<ShopEconomyConfig>(
                    "Assets/GameData/ShopEconomyConfig.asset");
            if (enchants == null || events == null || economy == null ||
                events.GetValidationErrors().Count > 0 ||
                events.Events.Count == 0) return false;

            RunCampaignState shop = CampaignAtNode(RunNodeType.Shop);
            RunBattleState run = new(30, 30, 500);
            IReadOnlyList<RunShopProductSlot> first = RunCampaignService.GetShopSlots(
                shop, PrototypeConsumableCatalog.All, enchants.Enchants, economy);
            RunShopProductSlot consumable = first.FirstOrDefault(slot =>
                slot.ProductType == RunShopProductType.Consumable);
            string firstSlotId = first.Count > 0 ? first[0].SlotId : null;
            RunBattleState poorRun = new(30, 30, 0);
            int goldBeforePurchase = run.Gold;
            int expectedRerollCost = economy.GetRerollCost(0);
            if (first.Count != economy.ConsumableOfferCount +
                    economy.EnchantOfferCount ||
                first.Count(slot => slot.ProductType ==
                    RunShopProductType.Consumable) != economy.ConsumableOfferCount ||
                first.Count(slot => slot.ProductType ==
                    RunShopProductType.Enchant) != economy.EnchantOfferCount ||
                first.Where(slot => slot.ProductType ==
                        RunShopProductType.Enchant).Any(slot =>
                        enchants.Find(slot.ContentId) == null ||
                        slot.Price != economy.GetEnchantPrice(
                            enchants.Find(slot.ContentId).Rarity)) ||
                consumable == null || consumable.Purchased ||
                RunCampaignService.TryBuyConsumableSlot(
                    shop, poorRun, consumable.SlotId, out _) ||
                poorRun.Gold != 0 || poorRun.ConsumableItemIds.Count != 0 ||
                consumable.Purchased ||
                RunCampaignService.TryBuyConsumableSlot(
                    shop, run, "MISSING-SLOT", out _) ||
                !RunCampaignService.TryBuyConsumableSlot(
                    shop, run, consumable.SlotId, out _) ||
                !consumable.Purchased ||
                run.Gold != goldBeforePurchase - consumable.Price ||
                RunCampaignService.TryBuyConsumableSlot(
                    shop, run, consumable.SlotId, out _) ||
                !RunCampaignService.TryRerollShop(shop, run, economy, out _) ||
                run.Gold != goldBeforePurchase - consumable.Price -
                    expectedRerollCost ||
                RunCampaignService.GetShopRerollCost(shop, economy) !=
                    economy.GetRerollCost(1))
            {
                return false;
            }

            IReadOnlyList<RunShopProductSlot> rerolled =
                RunCampaignService.GetShopSlots(shop,
                    PrototypeConsumableCatalog.All, enchants.Enchants, economy);
            if (rerolled.Count != economy.ConsumableOfferCount +
                    economy.EnchantOfferCount ||
                rerolled.Any(slot => slot.Purchased) ||
                rerolled[0].SlotId == firstSlotId)
            {
                return false;
            }

            RunCampaignState situation = CampaignAtNode(RunNodeType.SituationEvent);
            IReadOnlyList<RunSituationEventChoice> choices =
                RunCampaignService.GetSituationEventChoices(situation);
            string json = JsonUtility.ToJson(situation);
            RunCampaignState restored = JsonUtility.FromJson<RunCampaignState>(json);
            bool savedSelectionIsValid = choices.Count == 3 &&
                   situation.ActiveSituationEventId ==
                       events.Events[0].EventId &&
                   events.Events[0].MinimumCompletedNodeCount == 0 &&
                   events.Events[0].MaximumCompletedNodeCount == -1 &&
                   events.Events[0].AllowRepeatInRun &&
                   choices.Select(choice => choice.ChoiceId)
                       .Distinct(StringComparer.OrdinalIgnoreCase).Count() == 3 &&
                   restored.EventChoices.Count == 3 &&
                   restored.ActiveSituationEventId ==
                       situation.ActiveSituationEventId &&
                   restored.ResolvedSituationEventIds.Count == 0 &&
                   restored.EventChoices[0].ChoiceId == choices[0].ChoiceId;
            if (!savedSelectionIsValid) return false;

            string resolvedEventId = situation.ActiveSituationEventId;
            RunBattleState eventRun = new(30, 30, 0);
            if (!RunCampaignService.TryResolveSituationEvent(
                    situation, eventRun, choices[0].ChoiceId, out _, out _) ||
                situation.ResolvedSituationEventIds.Count != 1 ||
                situation.ResolvedSituationEventIds[0] != resolvedEventId)
            {
                return false;
            }

            RunCampaignState resolvedRestored =
                JsonUtility.FromJson<RunCampaignState>(
                    JsonUtility.ToJson(situation));
            return resolvedRestored.ResolvedSituationEventIds.Count == 1 &&
                   resolvedRestored.ResolvedSituationEventIds[0] ==
                       resolvedEventId;
        }

        private static RunCampaignState CampaignAtNode(RunNodeType type)
        {
            for (int seed = 0; seed < 100; seed++)
            {
                RunCampaignState campaign = new(seed);
                RunNodeChoice choice = RunCampaignService.GetChoices(campaign)
                    .FirstOrDefault(value => value.NodeType == type);
                if (choice != null && RunCampaignService.TrySelectNode(
                        campaign, choice.NodeId, out _)) return campaign;
            }
            return null;
        }

        private static bool ValidateInvalidConsumableDefinitions()
        {
            ConsumableData blank = ScriptableObject.CreateInstance<ConsumableData>();
            ConsumableData first = ScriptableObject.CreateInstance<ConsumableData>();
            ConsumableData duplicate = ScriptableObject.CreateInstance<ConsumableData>();
            ConsumableDatabase database =
                ScriptableObject.CreateInstance<ConsumableDatabase>();
            try
            {
                blank.EditorInitialize(" ", "Blank", "Test",
                    ConsumableEffect.HealPlayer, 1, 1);
                first.EditorInitialize("DUPLICATE", "First", "Test",
                    ConsumableEffect.HealPlayer, 1, 1);
                duplicate.EditorInitialize("duplicate", "Second", "Test",
                    ConsumableEffect.RestoreMana, 1, 1);
                database.EditorSetConsumables(new[] { blank, first, duplicate });

                IReadOnlyList<string> errors = database.GetValidationErrors();
                return errors.Count == 2 &&
                       errors.Any(error => error.Contains("empty ID")) &&
                       errors.Any(error => error.Contains("Duplicate consumable ID"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(database);
                UnityEngine.Object.DestroyImmediate(duplicate);
                UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(blank);
            }
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
                encounter.EncounterGrade == pair.Value) &&
                   ValidateSeparatedEnemyGradeLists(database);
        }

        private static bool ValidateSeparatedEnemyGradeLists(
            EncounterDatabase database)
        {
            Dictionary<EnemyDefinitionData, BattleEncounterGrade> grades = new();
            HashSet<string> enemyIds = new(StringComparer.OrdinalIgnoreCase);
            foreach (EncounterData encounter in database.Encounters)
            {
                foreach (EncounterEnemySlot slot in encounter.EnemySlots)
                {
                    if (slot?.Enemy == null ||
                        (grades.TryGetValue(
                             slot.Enemy, out BattleEncounterGrade grade) &&
                         grade != encounter.EncounterGrade))
                    {
                        return false;
                    }

                    enemyIds.Add(slot.Enemy.EnemyId);
                    grades[slot.Enemy] = encounter.EncounterGrade;
                }
            }

            return grades.Count == 12 && enemyIds.Count == 12 &&
                   Enum.GetValues(typeof(BattleEncounterGrade))
                       .Cast<BattleEncounterGrade>()
                       .All(grade => grades.Values.Count(value => value == grade) == 3);
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

            if (config.GetEncounterPoolValidationErrors().Count != 0)
            {
                return false;
            }

            RunNodeGenerationConfig generation = config.RunNodeGenerationConfig;
            ShopEconomyConfig economy = config.ShopEconomyConfig;
            BattleRewardConfig rewards = config.BattleRewardConfig;
            if (generation == null ||
                generation.GetValidationErrors().Count != 0 ||
                generation.GeneralNodePool.Count != 5 ||
                generation.OpeningChoiceCount != 3 ||
                generation.MinimumChoiceCount != 2 ||
                generation.MaximumChoiceCount != 4 ||
                generation.EliteUnlockIndex != 2 ||
                generation.MidBossIndex != 4 ||
                generation.FinalBossIndex != 11 ||
                economy == null || economy.GetValidationErrors().Count != 0 ||
                economy.BaseRerollCost != 10 ||
                economy.RerollCostIncrease != 5 ||
                economy.ConsumableOfferCount != 3 ||
                economy.EnchantOfferCount != 4 ||
                economy.CommonEnchantPrice != 45 ||
                economy.RareEnchantPrice != 80 ||
                economy.LegendaryEnchantPrice != 120 ||
                economy.GetEnchantPrice(CardRarity.Common) != 45 ||
                economy.GetEnchantPrice(CardRarity.Rare) != 80 ||
                economy.GetEnchantPrice(CardRarity.Legendary) != 120 ||
                economy.GetRerollCost(0) != 10 ||
                economy.GetRerollCost(2) != 20 ||
                rewards == null || rewards.GetValidationErrors().Count != 0 ||
                config.EncounterProgressionConfig == null ||
                config.EncounterProgressionConfig.GetValidationErrors(
                    config.EncounterDatabase).Count != 0 ||
                !ValidateBattleRewardRules(rewards))
            {
                return false;
            }

            return Enum.GetValues(typeof(BattleEncounterGrade))
                .Cast<BattleEncounterGrade>()
                .All(grade => RunEncounterPoolService.TryResolve(
                    config.EncounterDatabase, config.GetEncounterPool(grade, 0),
                    grade, 20260722, out _, out _)) &&
                   Enum.GetValues(typeof(BattleEncounterGrade))
                       .Cast<BattleEncounterGrade>()
                       .All(grade => config.GetEncounterPool(grade, 11).Count > 0 &&
                                     config.GetEncounterPool(grade, 12).Count == 0) &&
                   ValidateDeterministicEncounterSelection(config) &&
                   ValidateEncounterPoolSelectionRules();
        }

        private static bool ValidateBattleRewardRules(BattleRewardConfig config)
        {
            return config.TryGetRule(BattleEncounterGrade.Normal, out var normal) &&
                   normal.GoldOptions.SequenceEqual(new[] { 20, 25, 30 }) &&
                   normal.EnchantChoiceCount == 3 &&
                   normal.MinimumEnchantRarity == CardRarity.Common &&
                   config.TryGetRule(BattleEncounterGrade.Elite, out var elite) &&
                   elite.GoldOptions.SequenceEqual(new[] { 40, 45, 50, 55 }) &&
                   elite.MinimumEnchantRarity == CardRarity.Rare &&
                   elite.ConsumableItemRewardCount == 1 &&
                   config.TryGetRule(BattleEncounterGrade.MidBoss, out var midBoss) &&
                   midBoss.GoldOptions.SequenceEqual(new[] { 60 }) &&
                   midBoss.MinimumEnchantRarity == CardRarity.Rare &&
                   config.TryGetRule(BattleEncounterGrade.FinalBoss, out var finalBoss) &&
                   finalBoss.GoldOptions.SequenceEqual(new[] { 0 }) &&
                   finalBoss.EnchantChoiceCount == 0 &&
                   finalBoss.GrantsPermanentReward;
        }

        private static bool ValidateDeterministicEncounterSelection(
            RuntimePrototypeConfig config)
        {
            IReadOnlyList<string> pool = config.GetEncounterPool(
                BattleEncounterGrade.Normal, 0);
            return RunEncounterPoolService.TryResolve(
                       config.EncounterDatabase, pool,
                       BattleEncounterGrade.Normal, 41,
                       out EncounterData first, out _) &&
                   RunEncounterPoolService.TryResolve(
                       config.EncounterDatabase, pool,
                       BattleEncounterGrade.Normal, 41,
                       out EncounterData repeated, out _) &&
                   ReferenceEquals(first, repeated);
        }

        private static bool ValidateEncounterPoolSelectionRules()
        {
            EncounterData first = ScriptableObject.CreateInstance<EncounterData>();
            EncounterData second = ScriptableObject.CreateInstance<EncounterData>();
            EncounterDatabase database =
                ScriptableObject.CreateInstance<EncounterDatabase>();
            try
            {
                first.EditorInitialize(
                    "POOL-NORMAL-A", "Pool A", BattleEncounterGrade.Normal,
                    Array.Empty<EncounterEnemySlot>());
                second.EditorInitialize(
                    "POOL-NORMAL-B", "Pool B", BattleEncounterGrade.Normal,
                    Array.Empty<EncounterEnemySlot>());
                database.EditorSetEncounters(new[] { first, second });
                string[] pool = { first.EncounterId, second.EncounterId };

                return RunEncounterPoolService.TryResolve(
                           database, pool, BattleEncounterGrade.Normal, 0,
                           out EncounterData seedZero, out _) &&
                       RunEncounterPoolService.TryResolve(
                           database, pool, BattleEncounterGrade.Normal, 1,
                           out EncounterData seedOne, out _) &&
                       ReferenceEquals(seedZero, first) &&
                       ReferenceEquals(seedOne, second) &&
                       !RunEncounterPoolService.TryResolve(
                           database, pool, BattleEncounterGrade.Elite, 0,
                           out _, out _);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(database);
                UnityEngine.Object.DestroyImmediate(second);
                UnityEngine.Object.DestroyImmediate(first);
            }
        }
    }
}
