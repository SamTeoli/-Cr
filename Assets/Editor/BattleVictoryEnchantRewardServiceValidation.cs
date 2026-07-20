using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleVictoryEnchantRewardServiceValidation
    {
        [MenuItem("Have a Break/Validate Victory Enchant Rewards")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            if (valid)
            {
                Debug.Log("Victory enchant rewards passed.");
            }
            else
            {
                Debug.LogError("Victory enchant rewards failed.");
            }

            EditorUtility.DisplayDialog(
                "Victory Enchant Reward Validation",
                valid
                    ? "Victory enchant choices and run deck attachment passed."
                    : "Victory enchant rewards failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            RunDeckState runDeck = BuildRunDeck();
            EnchantData e01 = FindEnchant("E01");
            EnchantData e02 = FindEnchant("E02");
            EnchantData e03 = FindEnchant("E03");
            EnchantData e04 = FindEnchant("E04");
            EnchantData e06 = FindEnchant("E06");
            if (runDeck == null || e01 == null || e02 == null ||
                e03 == null || e04 == null || e06 == null)
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
                    "TEST-ENEMY-47",
                    "Test Enchant Reward Enemy",
                    0,
                    1);
                encounter.EditorInitialize(
                    "TEST-ENCOUNTER-47",
                    "Test Enchant Reward Encounter",
                    BattleEncounterGrade.Elite,
                    new[]
                    {
                        new EncounterEnemySlot(
                            "TEST-ENEMY-47-A",
                            enemy,
                            EnemyFieldPosition.Center)
                    });

                if (!TryCreateSettledVictory(
                        runDeck,
                        encounter,
                        out BattleRuntimeEncounterContext context))
                {
                    return false;
                }

                return ValidateRejectedChoiceSets(
                           context,
                           runDeck,
                           e01,
                           e02,
                           e03,
                           e04) &&
                       ValidateClaim(
                           context,
                           runDeck,
                           e01,
                           e02,
                           e03,
                           e06);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemy);
                UnityEngine.Object.DestroyImmediate(encounter);
            }
        }

        private static bool TryCreateSettledVictory(
            RunDeckState runDeck,
            EncounterData encounter,
            out BattleRuntimeEncounterContext context)
        {
            bool created = BattleRuntimeEncounterFlowService.TryCreateAndBegin(
                runDeck,
                "TEST-BATTLE-47",
                new RunBattleState(30, 24, 5),
                encounter,
                470,
                5,
                Array.Empty<string>(),
                1,
                out context,
                out BattleRuntimeEncounterFlowFailure flowFailure,
                out RunDeckFailure runDeckFailure,
                out BattleRuntimeBootstrapFailure bootstrapFailure,
                out BattleRuntimeSessionFailure sessionFailure,
                out StartingHandRedrawFailure redrawFailure,
                out BattleTurnFailure turnFailure,
                out List<string> validationErrors);
            if (!created || context == null ||
                flowFailure != BattleRuntimeEncounterFlowFailure.None ||
                runDeckFailure != RunDeckFailure.None ||
                bootstrapFailure != BattleRuntimeBootstrapFailure.None ||
                sessionFailure != BattleRuntimeSessionFailure.None ||
                redrawFailure != StartingHandRedrawFailure.None ||
                turnFailure != BattleTurnFailure.None ||
                validationErrors.Count != 0)
            {
                return false;
            }

            BattleEnemyRuntimeState enemy =
                context.Runtime.FindEnemy("TEST-ENEMY-47-A");
            if (enemy == null ||
                enemy.Vital.ApplyDamage(enemy.Vital.CurrentHealth) != 1 ||
                !context.Runtime.LivingEnemies.TryRemove(enemy.EnemyId))
            {
                return false;
            }

            bool settled = BattleRuntimeEncounterFlowService.TrySettle(
                context,
                out flowFailure,
                out sessionFailure,
                out BattleSettlementFailure settlementFailure);
            return settled &&
                   flowFailure == BattleRuntimeEncounterFlowFailure.None &&
                   sessionFailure == BattleRuntimeSessionFailure.None &&
                   settlementFailure == BattleSettlementFailure.None &&
                   context.Settlement.RewardEligible;
        }

        private static bool ValidateRejectedChoiceSets(
            BattleRuntimeEncounterContext context,
            RunDeckState runDeck,
            EnchantData e01,
            EnchantData e02,
            EnchantData e03,
            EnchantData e04)
        {
            RunDeckState unrelatedDeck = BuildRunDeck();
            if (unrelatedDeck == null)
            {
                return false;
            }

            bool unrelatedDeckCreated =
                BattleVictoryEnchantRewardService.TryCreate(
                    context,
                    unrelatedDeck,
                    new[] { e01, e03, e04 },
                    out BattleVictoryEnchantRewardService unrelatedDeckService,
                    out BattleVictoryEnchantRewardFailure unrelatedDeckFailure);
            bool duplicateCreated =
                BattleVictoryEnchantRewardService.TryCreate(
                    context,
                    runDeck,
                    new[] { e01, e01, e03 },
                    out BattleVictoryEnchantRewardService duplicateService,
                    out BattleVictoryEnchantRewardFailure duplicateFailure);
            bool commonOnlyCreated =
                BattleVictoryEnchantRewardService.TryCreate(
                    context,
                    runDeck,
                    new[] { e01, e02, e04 },
                    out BattleVictoryEnchantRewardService commonOnlyService,
                    out BattleVictoryEnchantRewardFailure commonOnlyFailure);
            bool shortSetCreated =
                BattleVictoryEnchantRewardService.TryCreate(
                    context,
                    runDeck,
                    new[] { e01, e03 },
                    out BattleVictoryEnchantRewardService shortSetService,
                    out BattleVictoryEnchantRewardFailure shortSetFailure);

            return !unrelatedDeckCreated &&
                   unrelatedDeckService == null &&
                   unrelatedDeckFailure ==
                   BattleVictoryEnchantRewardFailure.InvalidDeck &&
                   !duplicateCreated && duplicateService == null &&
                   duplicateFailure ==
                   BattleVictoryEnchantRewardFailure.InvalidChoiceSet &&
                   !commonOnlyCreated && commonOnlyService == null &&
                   commonOnlyFailure ==
                   BattleVictoryEnchantRewardFailure.InvalidChoiceSet &&
                   !shortSetCreated && shortSetService == null &&
                   shortSetFailure ==
                   BattleVictoryEnchantRewardFailure.InvalidChoiceSet;
        }

        private static bool ValidateClaim(
            BattleRuntimeEncounterContext context,
            RunDeckState runDeck,
            EnchantData e01,
            EnchantData e02,
            EnchantData e03,
            EnchantData e06)
        {
            bool created = BattleVictoryEnchantRewardService.TryCreate(
                context,
                runDeck,
                new[] { e01, e03, e06 },
                out BattleVictoryEnchantRewardService reward,
                out BattleVictoryEnchantRewardFailure createFailure);
            if (!created || reward == null ||
                createFailure != BattleVictoryEnchantRewardFailure.None ||
                reward.OfferedChoices.Count != 3 ||
                context.VictoryEnchantRewards != reward)
            {
                return false;
            }

            bool recreated = BattleVictoryEnchantRewardService.TryCreate(
                context,
                runDeck,
                new[] { e01, e03, e06 },
                out BattleVictoryEnchantRewardService recreatedReward,
                out BattleVictoryEnchantRewardFailure recreateFailure);
            if (recreated || recreatedReward != null ||
                recreateFailure !=
                BattleVictoryEnchantRewardFailure.AlreadyCreated ||
                context.VictoryEnchantRewards != reward)
            {
                return false;
            }

            RunCardInstance validTarget = runDeck.Cards.FirstOrDefault(
                card => card?.Enchants != null &&
                        card.Enchants.HasImmediateAttachmentTarget(e01));
            RunCardInstance incompatibleTarget = runDeck.Cards.FirstOrDefault(
                card => card?.Enchants != null &&
                        !EnchantCompatibilityEvaluator.IsCompatible(
                            e01,
                            card.Card));
            if (validTarget == null || incompatibleTarget == null)
            {
                return false;
            }

            bool unofferedClaimed = reward.TryClaim(
                e02.DefinitionId,
                validTarget.OwnedCardId,
                0,
                out EnchantAttachmentFailure unofferedAttachmentFailure,
                out BattleVictoryEnchantRewardFailure unofferedFailure);
            bool missingCardClaimed = reward.TryClaim(
                e01.DefinitionId,
                "OWNED-47-MISSING",
                0,
                out EnchantAttachmentFailure missingCardAttachmentFailure,
                out BattleVictoryEnchantRewardFailure missingCardFailure);
            bool incompatibleClaimed = reward.TryClaim(
                e01.DefinitionId,
                incompatibleTarget.OwnedCardId,
                0,
                out EnchantAttachmentFailure incompatibleAttachmentFailure,
                out BattleVictoryEnchantRewardFailure incompatibleFailure);
            if (unofferedClaimed ||
                unofferedAttachmentFailure != EnchantAttachmentFailure.None ||
                unofferedFailure !=
                BattleVictoryEnchantRewardFailure.ChoiceNotOffered ||
                missingCardClaimed ||
                missingCardAttachmentFailure != EnchantAttachmentFailure.None ||
                missingCardFailure !=
                BattleVictoryEnchantRewardFailure.CardNotFound ||
                incompatibleClaimed ||
                incompatibleAttachmentFailure !=
                EnchantAttachmentFailure.IncompatibleCardType ||
                incompatibleFailure !=
                BattleVictoryEnchantRewardFailure.AttachmentFailed ||
                reward.Claimed)
            {
                return false;
            }

            bool claimed = reward.TryClaim(
                e01.DefinitionId,
                validTarget.OwnedCardId,
                0,
                out EnchantAttachmentFailure attachmentFailure,
                out BattleVictoryEnchantRewardFailure claimFailure);
            bool duplicateClaimed = reward.TryClaim(
                e03.DefinitionId,
                validTarget.OwnedCardId,
                0,
                out EnchantAttachmentFailure duplicateAttachmentFailure,
                out BattleVictoryEnchantRewardFailure duplicateFailure);
            return claimed &&
                   attachmentFailure == EnchantAttachmentFailure.None &&
                   claimFailure == BattleVictoryEnchantRewardFailure.None &&
                   reward.Claimed && reward.ClaimedEnchant == e01 &&
                   reward.TargetOwnedCardId == validTarget.OwnedCardId &&
                   validTarget.Enchants.Slots[0].Enchant == e01 &&
                   validTarget.Enchants.Slots[0].Active &&
                   !duplicateClaimed &&
                   duplicateAttachmentFailure ==
                   EnchantAttachmentFailure.None &&
                   duplicateFailure ==
                   BattleVictoryEnchantRewardFailure.AlreadyClaimed;
        }

        private static RunDeckState BuildRunDeck()
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
                        $"OWNED-47-{catalogCardId}"),
                    out RunDeckFailure failure);
                if (!added || failure != RunDeckFailure.None)
                {
                    return null;
                }
            }

            return runDeck;
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
