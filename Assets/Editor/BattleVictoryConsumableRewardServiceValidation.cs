using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleVictoryConsumableRewardServiceValidation
    {
        private const string TestItemId = "TEST-CONSUMABLE-49";

        [MenuItem("Have a Break/Validate Victory Consumable Rewards")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            if (valid)
            {
                Debug.Log("Victory consumable rewards passed.");
            }
            else
            {
                Debug.LogError("Victory consumable rewards failed.");
            }

            EditorUtility.DisplayDialog(
                "Victory Consumable Reward Validation",
                valid
                    ? "Elite consumable reward and progression passed."
                    : "Victory consumable rewards failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            RunEncounterProgressState progress = CreateProgress();
            EnchantData e01 = FindEnchant(TestContentIds.E01);
            EnchantData e03 = FindEnchant(TestContentIds.E03);
            EnchantData e06 = FindEnchant(TestContentIds.E06);
            if (progress == null || e01 == null ||
                e03 == null || e06 == null)
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
                    "TEST-ENEMY-49",
                    "Test Consumable Reward Enemy",
                    0,
                    1);
                encounter.EditorInitialize(
                    "TEST-ENCOUNTER-49",
                    "Test Consumable Reward Encounter",
                    BattleEncounterGrade.Elite,
                    new[]
                    {
                        new EncounterEnemySlot(
                            "TEST-ENEMY-49-A",
                            enemy,
                            EnemyFieldPosition.Center)
                    });

                return ValidateEliteReward(
                    progress,
                    encounter,
                    e01,
                    e03,
                    e06);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemy);
                UnityEngine.Object.DestroyImmediate(encounter);
            }
        }

        private static bool ValidateEliteReward(
            RunEncounterProgressState progress,
            EncounterData encounter,
            EnchantData e01,
            EnchantData e03,
            EnchantData e06)
        {
            if (!TryBegin(
                    progress,
                    encounter,
                    out BattleRuntimeEncounterContext context))
            {
                return false;
            }

            bool createdBeforeSettlement =
                BattleVictoryConsumableRewardService.TryCreate(
                    context,
                    out BattleVictoryConsumableRewardService earlyService,
                    out BattleVictoryConsumableRewardFailure earlyFailure);
            if (createdBeforeSettlement || earlyService != null ||
                earlyFailure != BattleVictoryConsumableRewardFailure
                    .SettlementNotComplete ||
                context.VictoryConsumableRewards != null)
            {
                return false;
            }

            if (!MakeVictory(context) || !TrySettle(progress))
            {
                return false;
            }

            bool goldClaimed = context.VictoryRewards.TryClaimGold(
                out BattleRewardFailure goldFailure);
            bool enchantClaimed = ClaimEnchantReward(
                context,
                progress.RunDeck,
                e01,
                e03,
                e06);
            if (!goldClaimed || goldFailure != BattleRewardFailure.None ||
                !enchantClaimed)
            {
                return false;
            }

            bool completedBeforeConsumable =
                RunEncounterProgressService.TryCompleteActive(
                    progress,
                    out RunEncounterProgressFailure pendingFailure);
            if (completedBeforeConsumable ||
                pendingFailure !=
                RunEncounterProgressFailure.ConsumableRewardPending ||
                !progress.HasActiveEncounter ||
                progress.RunState.ConsumableItemIds.Count != 0)
            {
                return false;
            }

            bool created = BattleVictoryConsumableRewardService.TryCreate(
                context,
                out BattleVictoryConsumableRewardService reward,
                out BattleVictoryConsumableRewardFailure createFailure);
            if (!created || reward == null ||
                createFailure !=
                BattleVictoryConsumableRewardFailure.None ||
                reward.RequiredItemCount != 1 || reward.Claimed ||
                reward.ClaimedItemIds.Count != 0 ||
                context.VictoryConsumableRewards != reward)
            {
                return false;
            }

            bool recreated = BattleVictoryConsumableRewardService.TryCreate(
                context,
                out BattleVictoryConsumableRewardService recreatedService,
                out BattleVictoryConsumableRewardFailure recreateFailure);
            bool blankClaimed = reward.TryClaim(
                " ",
                out BattleVictoryConsumableRewardFailure blankFailure);
            if (recreated || recreatedService != null ||
                recreateFailure !=
                BattleVictoryConsumableRewardFailure.AlreadyCreated ||
                blankClaimed ||
                blankFailure !=
                BattleVictoryConsumableRewardFailure.InvalidItemId ||
                reward.Claimed ||
                progress.RunState.ConsumableItemIds.Count != 0)
            {
                return false;
            }

            bool claimed = reward.TryClaim(
                TestItemId,
                out BattleVictoryConsumableRewardFailure claimFailure);
            bool duplicateClaimed = reward.TryClaim(
                "TEST-CONSUMABLE-49-DUPLICATE",
                out BattleVictoryConsumableRewardFailure duplicateFailure);
            if (!claimed ||
                claimFailure != BattleVictoryConsumableRewardFailure.None ||
                !reward.Claimed || reward.ClaimedItemIds.Count != 1 ||
                reward.ClaimedItemIds[0] != TestItemId ||
                progress.RunState.ConsumableItemIds.Count != 1 ||
                progress.RunState.ConsumableItemIds[0] != TestItemId ||
                duplicateClaimed ||
                duplicateFailure !=
                BattleVictoryConsumableRewardFailure.AlreadyClaimed)
            {
                return false;
            }

            bool completed =
                RunEncounterProgressService.TryCompleteActive(
                    progress,
                    out RunEncounterProgressFailure completeFailure);
            return completed &&
                   completeFailure == RunEncounterProgressFailure.None &&
                   !progress.HasActiveEncounter &&
                   progress.CompletedEncounterCount == 1 &&
                   !progress.RunState.RunEnded;
        }

        private static bool TryBegin(
            RunEncounterProgressState progress,
            EncounterData encounter,
            out BattleRuntimeEncounterContext context)
        {
            bool created = RunEncounterProgressService.TryBegin(
                progress,
                "TEST-BATTLE-49",
                encounter,
                490,
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

        private static bool MakeVictory(
            BattleRuntimeEncounterContext context)
        {
            BattleEnemyRuntimeState enemy =
                context?.Runtime.FindEnemy("TEST-ENEMY-49-A");
            return enemy != null &&
                   enemy.Vital.ApplyDamage(enemy.Vital.CurrentHealth) == 1 &&
                   context.Runtime.LivingEnemies.TryRemove(enemy.EnemyId);
        }

        private static bool ClaimEnchantReward(
            BattleRuntimeEncounterContext context,
            RunDeckState runDeck,
            EnchantData e01,
            EnchantData e03,
            EnchantData e06)
        {
            bool created = BattleVictoryEnchantRewardService.TryCreate(
                context,
                runDeck,
                new[] { e01, e03, e06 },
                out BattleVictoryEnchantRewardService reward,
                out BattleVictoryEnchantRewardFailure createFailure);
            RunCardInstance target = runDeck.Cards.FirstOrDefault(
                card => card?.Enchants != null &&
                        card.Enchants.HasImmediateAttachmentTarget(e01));
            if (!created || reward == null || target == null ||
                createFailure != BattleVictoryEnchantRewardFailure.None)
            {
                return false;
            }

            bool claimed = reward.TryClaim(
                e01.DefinitionId,
                target.OwnedCardId,
                0,
                out EnchantAttachmentFailure attachmentFailure,
                out BattleVictoryEnchantRewardFailure claimFailure);
            return claimed &&
                   attachmentFailure == EnchantAttachmentFailure.None &&
                   claimFailure == BattleVictoryEnchantRewardFailure.None;
        }

        private static RunEncounterProgressState CreateProgress()
        {
            RunDeckState runDeck = new();
            for (int number = 1; number <= 12; number++)
            {
                string catalogCardId = $"C{number:00}";
                CardData card = FindCard(catalogCardId);
                if (card == null)
                {
                    return null;
                }

                bool added = runDeck.TryAdd(
                    new RunCardInstance(
                        card,
                        $"OWNED-49-{catalogCardId}"),
                    out RunDeckFailure failure);
                if (!added || failure != RunDeckFailure.None)
                {
                    return null;
                }
            }

            return new RunEncounterProgressState(
                new RunBattleState(30, 25, 3),
                runDeck);
        }

        private static CardData FindCard(string catalogCardId)
        {
            return AssetDatabase.FindAssets("t:CardData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<CardData>)
                .FirstOrDefault(card => card != null && string.Equals(
                    card.CatalogCardId,
                    catalogCardId,
                    StringComparison.OrdinalIgnoreCase));
        }

        private static EnchantData FindEnchant(string definitionId)
        {
            return AssetDatabase.FindAssets("t:EnchantData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<EnchantData>)
                .FirstOrDefault(enchant => enchant != null && string.Equals(
                    enchant.DefinitionId,
                    definitionId,
                    StringComparison.OrdinalIgnoreCase));
        }
    }
}
