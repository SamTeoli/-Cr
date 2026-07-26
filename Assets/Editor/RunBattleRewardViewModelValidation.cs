using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class RunBattleRewardViewModelValidation
    {
        [MenuItem("Have a Break/Validate Run Battle Reward ViewModel")]
        private static void ValidateFromMenu()
        {
            Debug.Log(Validate()
                ? "Run battle reward ViewModel passed."
                : "Run battle reward ViewModel failed.");
        }

        internal static bool Validate()
        {
            CardDatabase cards = AssetDatabase.LoadAssetAtPath<CardDatabase>(
                "Assets/GameData/CardDatabase.asset");
            EnchantDatabase enchants = AssetDatabase.LoadAssetAtPath<EnchantDatabase>(
                "Assets/GameData/EnchantDatabase.asset");
            RunEncounterProgressState progress = CreateProgress(cards);
            RunCampaignState campaign = CampaignAtBattle();
            RunBattleRewardViewModel viewModel = new();
            RunBattleRewardSnapshot unavailable = viewModel.CreateSnapshot(
                campaign,
                progress,
                enchants);
            if (cards == null || enchants == null || progress == null ||
                campaign == null || unavailable.Available ||
                string.IsNullOrWhiteSpace(unavailable.ErrorText))
            {
                return false;
            }

            EnemyDefinitionData enemy =
                ScriptableObject.CreateInstance<EnemyDefinitionData>();
            EncounterData encounter =
                ScriptableObject.CreateInstance<EncounterData>();
            try
            {
                enemy.EditorInitialize(
                    "TEST-ENEMY-REWARD-VM",
                    "Test Reward ViewModel Enemy",
                    0,
                    1);
                encounter.EditorInitialize(
                    "TEST-ENCOUNTER-REWARD-VM",
                    "Test Reward ViewModel Encounter",
                    BattleEncounterGrade.Elite,
                    new[]
                    {
                        new EncounterEnemySlot(
                            "TEST-ENEMY-REWARD-VM-A",
                            enemy,
                            EnemyFieldPosition.Center)
                    });

                if (!TryBegin(progress, encounter, out BattleRuntimeEncounterContext context) ||
                    !MakeVictory(context) ||
                    !TrySettle(progress) ||
                    !context.VictoryRewards.TryClaimGold(
                        out BattleRewardFailure goldFailure) ||
                    goldFailure != BattleRewardFailure.None)
                {
                    return false;
                }

                RunCampaignService.MarkBattleReward(
                    campaign,
                    BattleOutcome.Victory);
                RunBattleRewardSnapshot snapshot = viewModel.CreateSnapshot(
                    campaign,
                    progress,
                    enchants);
                if (!snapshot.Available || !snapshot.GoldClaimed ||
                    snapshot.GoldReward != context.VictoryRewards.GoldReward ||
                    snapshot.GoldLabel !=
                        $"골드 {context.VictoryRewards.GoldReward} 수령 완료" ||
                    !string.IsNullOrWhiteSpace(snapshot.ErrorText) ||
                    snapshot.EnchantOptions.Length !=
                        context.VictoryRewards.EnchantChoiceCount ||
                    snapshot.ConsumableOptions.Length != 3 ||
                    snapshot.EnchantOptions.Any(option =>
                        option == null || option.Enchant == null ||
                        string.IsNullOrWhiteSpace(option.DefinitionId) ||
                        string.IsNullOrWhiteSpace(option.DisplayText) ||
                        !option.HasTarget || !option.CanClaim ||
                        string.IsNullOrWhiteSpace(option.TargetLabel)) ||
                    snapshot.ConsumableOptions.Any(option =>
                        option == null || option.Item == null ||
                        string.IsNullOrWhiteSpace(option.ItemId) ||
                        string.IsNullOrWhiteSpace(option.DisplayText) ||
                        !option.CanClaim) ||
                    snapshot.CanComplete)
                {
                    return false;
                }

                int itemCountBeforeInvalid =
                    progress.RunState.ConsumableItemIds.Count;
                ConsumableData hiddenItem =
                    PrototypeConsumableCatalog.All
                        .Where(item => item != null)
                        .Skip(3)
                        .FirstOrDefault();
                if (viewModel.TryClaimEnchant(
                        campaign,
                        progress,
                        enchants,
                        "MISSING-REWARD-ENCHANT",
                        out _,
                        out _,
                        out _,
                        out _) ||
                    viewModel.TryClaimConsumable(
                        campaign,
                        progress,
                        "MISSING-REWARD-ITEM",
                        out _,
                        out _,
                        out _) ||
                    hiddenItem == null ||
                    viewModel.TryClaimConsumable(
                        campaign,
                        progress,
                        hiddenItem.ItemId,
                        out _,
                        out _,
                        out _) ||
                    progress.RunState.ConsumableItemIds.Count !=
                        itemCountBeforeInvalid)
                {
                    return false;
                }

                RunBattleEnchantRewardOption enchant =
                    snapshot.EnchantOptions.FirstOrDefault(option => option.CanClaim);
                if (enchant == null || enchant.TargetCard == null ||
                    enchant.TargetSlotIndex < 0)
                {
                    return false;
                }

                RunCardInstance target = enchant.TargetCard;
                int targetSlotIndex = enchant.TargetSlotIndex;
                if (!viewModel.TryClaimEnchant(
                        campaign,
                        progress,
                        enchants,
                        enchant.DefinitionId,
                        out RunBattleEnchantRewardOption claimedEnchant,
                        out string enchantResult,
                        out EnchantAttachmentFailure attachmentFailure,
                        out BattleVictoryEnchantRewardFailure enchantFailure) ||
                    attachmentFailure != EnchantAttachmentFailure.None ||
                    enchantFailure != BattleVictoryEnchantRewardFailure.None ||
                    claimedEnchant == null || !claimedEnchant.IsSelected ||
                    !claimedEnchant.RewardClaimed ||
                    target.Enchants.Slots[targetSlotIndex].Enchant == null ||
                    !target.Enchants.Slots[targetSlotIndex].Enchant.MatchesDefinition(
                        enchant.Enchant) ||
                    string.IsNullOrWhiteSpace(enchantResult) ||
                    viewModel.TryClaimEnchant(
                        campaign,
                        progress,
                        enchants,
                        snapshot.EnchantOptions.Last().DefinitionId,
                        out _,
                        out _,
                        out _,
                        out _))
                {
                    return false;
                }

                RunBattleRewardSnapshot afterEnchant = viewModel.CreateSnapshot(
                    campaign,
                    progress,
                    enchants);
                if (!afterEnchant.EnchantRewardComplete ||
                    afterEnchant.EnchantOptions.Count(option => option.IsSelected) != 1 ||
                    afterEnchant.EnchantOptions.Any(option => option.CanClaim) ||
                    afterEnchant.ConsumableRewardComplete ||
                    afterEnchant.CanComplete)
                {
                    return false;
                }

                RunBattleConsumableRewardOption item =
                    afterEnchant.ConsumableOptions.FirstOrDefault();
                int itemCountBefore = progress.RunState.ConsumableItemIds.Count(value =>
                    string.Equals(
                        value,
                        item?.ItemId,
                        StringComparison.OrdinalIgnoreCase));
                if (item == null || !viewModel.TryClaimConsumable(
                        campaign,
                        progress,
                        item.ItemId,
                        out RunBattleConsumableRewardOption claimedItem,
                        out string itemResult,
                        out BattleVictoryConsumableRewardFailure itemFailure) ||
                    itemFailure != BattleVictoryConsumableRewardFailure.None ||
                    claimedItem == null || !claimedItem.IsSelected ||
                    !claimedItem.RewardClaimed ||
                    progress.RunState.ConsumableItemIds.Count(value =>
                        string.Equals(
                            value,
                            item.ItemId,
                            StringComparison.OrdinalIgnoreCase)) != itemCountBefore + 1 ||
                    string.IsNullOrWhiteSpace(itemResult) ||
                    viewModel.TryClaimConsumable(
                        campaign,
                        progress,
                        afterEnchant.ConsumableOptions.Last().ItemId,
                        out _,
                        out _,
                        out _))
                {
                    return false;
                }

                RunBattleRewardSnapshot complete = viewModel.CreateSnapshot(
                    campaign,
                    progress,
                    enchants);
                if (!complete.EnchantRewardComplete ||
                    !complete.ConsumableRewardComplete ||
                    !complete.CanComplete ||
                    complete.ConsumableOptions.Count(option => option.IsSelected) != 1 ||
                    complete.ConsumableOptions.Any(option => option.CanClaim) ||
                    !viewModel.TryComplete(
                        campaign,
                        progress,
                        out string completeResult,
                        out RunEncounterProgressFailure completeFailure) ||
                    completeFailure != RunEncounterProgressFailure.None ||
                    string.IsNullOrWhiteSpace(completeResult) ||
                    progress.HasActiveEncounter ||
                    progress.CompletedEncounterCount != 1 ||
                    campaign.CompletedNodeCount != 1 ||
                    campaign.Phase != RunCampaignPhase.NodeSelection ||
                    campaign.ActiveNode != null)
                {
                    return false;
                }

                RunBattleRewardSnapshot closed = viewModel.CreateSnapshot(
                    campaign,
                    progress,
                    enchants);
                return !closed.Available &&
                       string.IsNullOrWhiteSpace(closed.ErrorText) == false &&
                       !viewModel.TryComplete(
                           campaign,
                           progress,
                           out _,
                           out _);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemy);
                UnityEngine.Object.DestroyImmediate(encounter);
            }
        }

        private static RunEncounterProgressState CreateProgress(
            CardDatabase database)
        {
            if (database == null)
            {
                return null;
            }

            RunDeckState deck = new();
            for (int number = 1; number <= 12; number++)
            {
                string catalogCardId = $"C{number:00}";
                CardData data = database.Cards.FirstOrDefault(card =>
                    card != null && string.Equals(
                        card.CatalogCardId,
                        catalogCardId,
                        StringComparison.OrdinalIgnoreCase));
                if (data == null || !deck.TryAdd(
                        new RunCardInstance(
                            data,
                            $"OWNED-REWARD-VM-{catalogCardId}"),
                        out RunDeckFailure failure) ||
                    failure != RunDeckFailure.None)
                {
                    return null;
                }
            }

            return new RunEncounterProgressState(
                new RunBattleState(30, 25, 3),
                deck);
        }

        private static RunCampaignState CampaignAtBattle()
        {
            for (int seed = 0; seed < 1000; seed++)
            {
                RunCampaignState campaign = new(seed);
                RunNodeChoice choice = RunCampaignService.GetChoices(campaign)
                    .FirstOrDefault(value => value.IsBattle);
                if (choice != null && RunCampaignService.TrySelectNode(
                        campaign,
                        choice.NodeId,
                        out _))
                {
                    return campaign;
                }
            }

            return null;
        }

        private static bool TryBegin(
            RunEncounterProgressState progress,
            EncounterData encounter,
            out BattleRuntimeEncounterContext context)
        {
            bool created = RunEncounterProgressService.TryBegin(
                progress,
                "TEST-BATTLE-REWARD-VM",
                encounter,
                530,
                5,
                Array.Empty<string>(),
                0,
                out context,
                out RunEncounterProgressFailure progressFailure,
                out BattleRuntimeEncounterFlowFailure flowFailure,
                out RunDeckFailure runDeckFailure,
                out BattleRuntimeBootstrapFailure bootstrapFailure,
                out BattleRuntimeSessionFailure sessionFailure,
                out StartingHandRedrawFailure redrawFailure,
                out BattleTurnFailure turnFailure,
                out List<string> validationErrors);
            return created && context != null &&
                   progressFailure == RunEncounterProgressFailure.None &&
                   flowFailure == BattleRuntimeEncounterFlowFailure.None &&
                   runDeckFailure == RunDeckFailure.None &&
                   bootstrapFailure == BattleRuntimeBootstrapFailure.None &&
                   sessionFailure == BattleRuntimeSessionFailure.None &&
                   redrawFailure == StartingHandRedrawFailure.None &&
                   turnFailure == BattleTurnFailure.None &&
                   validationErrors.Count == 0;
        }

        private static bool MakeVictory(
            BattleRuntimeEncounterContext context)
        {
            BattleEnemyRuntimeState enemy =
                context?.Runtime.FindEnemy("TEST-ENEMY-REWARD-VM-A");
            return enemy != null &&
                   enemy.Vital.ApplyDamage(enemy.Vital.CurrentHealth) == 1 &&
                   context.Runtime.LivingEnemies.TryRemove(enemy.EnemyId);
        }

        private static bool TrySettle(
            RunEncounterProgressState progress)
        {
            bool settled = RunEncounterProgressService.TrySettleActive(
                progress,
                out RunEncounterProgressFailure progressFailure,
                out BattleRuntimeEncounterFlowFailure flowFailure,
                out BattleRuntimeSessionFailure sessionFailure,
                out BattleSettlementFailure settlementFailure);
            return settled &&
                   progressFailure == RunEncounterProgressFailure.None &&
                   flowFailure == BattleRuntimeEncounterFlowFailure.None &&
                   sessionFailure == BattleRuntimeSessionFailure.None &&
                   settlementFailure == BattleSettlementFailure.None;
        }
    }
}
