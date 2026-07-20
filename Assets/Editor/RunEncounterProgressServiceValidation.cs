using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class RunEncounterProgressServiceValidation
    {
        [MenuItem("Have a Break/Validate Run Encounter Progression")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            if (valid)
            {
                Debug.Log("Run encounter progression passed.");
            }
            else
            {
                Debug.LogError("Run encounter progression failed.");
            }

            EditorUtility.DisplayDialog(
                "Run Encounter Progression Validation",
                valid
                    ? "Run encounter gating, rewards, and terminal defeat passed."
                    : "Run encounter progression failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            EnchantData e01 = FindEnchant("E01");
            EnchantData e03 = FindEnchant("E03");
            EnchantData e06 = FindEnchant("E06");
            if (e01 == null || e03 == null || e06 == null)
            {
                return false;
            }

            EnemyDefinitionData enemy =
                ScriptableObject.CreateInstance<EnemyDefinitionData>();
            EncounterData normalEncounter =
                ScriptableObject.CreateInstance<EncounterData>();
            EncounterData eliteEncounter =
                ScriptableObject.CreateInstance<EncounterData>();
            EncounterData finalBossEncounter =
                ScriptableObject.CreateInstance<EncounterData>();
            try
            {
                enemy.EditorInitialize(
                    "TEST-ENEMY-48",
                    "Test Run Progress Enemy",
                    0,
                    1);
                InitializeEncounter(
                    normalEncounter,
                    "TEST-ENCOUNTER-48-NORMAL",
                    BattleEncounterGrade.Normal,
                    enemy);
                InitializeEncounter(
                    eliteEncounter,
                    "TEST-ENCOUNTER-48-ELITE",
                    BattleEncounterGrade.Elite,
                    enemy);
                InitializeEncounter(
                    finalBossEncounter,
                    "TEST-ENCOUNTER-48-FINAL",
                    BattleEncounterGrade.FinalBoss,
                    enemy);

                return ValidateNormalProgressAndDefeat(
                           normalEncounter,
                           e01,
                           e03,
                           e06) &&
                       ValidateUnsupportedRewardGates(
                           eliteEncounter,
                           finalBossEncounter,
                           e01,
                           e03,
                           e06);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemy);
                UnityEngine.Object.DestroyImmediate(normalEncounter);
                UnityEngine.Object.DestroyImmediate(eliteEncounter);
                UnityEngine.Object.DestroyImmediate(finalBossEncounter);
            }
        }

        private static bool ValidateNormalProgressAndDefeat(
            EncounterData encounter,
            EnchantData e01,
            EnchantData e03,
            EnchantData e06)
        {
            RunEncounterProgressState progress = CreateProgress();
            if (progress == null ||
                !TryBegin(
                    progress,
                    "TEST-BATTLE-48-A",
                    encounter,
                    480,
                    out BattleRuntimeEncounterContext first))
            {
                return false;
            }

            if (!TryBeginRejected(
                    progress,
                    "TEST-BATTLE-48-B",
                    encounter,
                    RunEncounterProgressFailure.EncounterAlreadyActive) ||
                progress.ActiveEncounter != first ||
                progress.UsedBattleInstanceIds.Count != 1 ||
                !MakeVictory(first) ||
                !TrySettle(progress))
            {
                return false;
            }

            bool completedBeforeGold =
                RunEncounterProgressService.TryCompleteActive(
                    progress,
                    out RunEncounterProgressFailure beforeGoldFailure);
            bool goldClaimed = first.VictoryRewards.TryClaimGold(
                out BattleRewardFailure goldFailure);
            bool completedBeforeEnchant =
                RunEncounterProgressService.TryCompleteActive(
                    progress,
                    out RunEncounterProgressFailure beforeEnchantFailure);
            if (completedBeforeGold ||
                beforeGoldFailure !=
                RunEncounterProgressFailure.GoldRewardPending ||
                !goldClaimed || goldFailure != BattleRewardFailure.None ||
                completedBeforeEnchant ||
                beforeEnchantFailure !=
                RunEncounterProgressFailure.EnchantRewardPending ||
                !ClaimEnchantReward(
                    first,
                    progress.RunDeck,
                    e01,
                    e03,
                    e06))
            {
                return false;
            }

            bool firstCompleted =
                RunEncounterProgressService.TryCompleteActive(
                    progress,
                    out RunEncounterProgressFailure firstCompleteFailure);
            if (!firstCompleted ||
                firstCompleteFailure != RunEncounterProgressFailure.None ||
                progress.HasActiveEncounter ||
                progress.CompletedEncounterCount != 1 ||
                !TryBeginRejected(
                    progress,
                    "test-battle-48-a",
                    encounter,
                    RunEncounterProgressFailure.BattleInstanceAlreadyUsed) ||
                !TryBegin(
                    progress,
                    "TEST-BATTLE-48-B",
                    encounter,
                    481,
                    out BattleRuntimeEncounterContext second))
            {
                return false;
            }

            int currentHealth = second.Runtime.Player.CurrentHealth;
            if (currentHealth <= 0 ||
                second.Runtime.Player.ApplyDamage(currentHealth) !=
                currentHealth ||
                !TrySettle(progress))
            {
                return false;
            }

            bool defeatCompleted =
                RunEncounterProgressService.TryCompleteActive(
                    progress,
                    out RunEncounterProgressFailure defeatFailure);
            return defeatCompleted &&
                   defeatFailure == RunEncounterProgressFailure.None &&
                   progress.RunState.RunEnded &&
                   !progress.HasActiveEncounter &&
                   progress.CompletedEncounterCount == 2 &&
                   progress.UsedBattleInstanceIds.Count == 2 &&
                   TryBeginRejected(
                       progress,
                       "TEST-BATTLE-48-C",
                       encounter,
                       RunEncounterProgressFailure.RunEnded);
        }

        private static bool ValidateUnsupportedRewardGates(
            EncounterData eliteEncounter,
            EncounterData finalBossEncounter,
            EnchantData e01,
            EnchantData e03,
            EnchantData e06)
        {
            RunEncounterProgressState elite = CreateProgress();
            if (elite == null ||
                !TryBegin(
                    elite,
                    "TEST-BATTLE-48-ELITE",
                    eliteEncounter,
                    482,
                    out BattleRuntimeEncounterContext eliteContext) ||
                !MakeVictory(eliteContext) ||
                !TrySettle(elite) ||
                !eliteContext.VictoryRewards.TryClaimGold(out _) ||
                !ClaimEnchantReward(
                    eliteContext,
                    elite.RunDeck,
                    e01,
                    e03,
                    e06))
            {
                return false;
            }

            bool eliteCompleted =
                RunEncounterProgressService.TryCompleteActive(
                    elite,
                    out RunEncounterProgressFailure eliteFailure);
            if (eliteCompleted ||
                eliteFailure !=
                RunEncounterProgressFailure.ConsumableRewardPending ||
                !elite.HasActiveEncounter ||
                elite.CompletedEncounterCount != 0)
            {
                return false;
            }

            RunEncounterProgressState finalBoss = CreateProgress();
            if (finalBoss == null ||
                !TryBegin(
                    finalBoss,
                    "TEST-BATTLE-48-FINAL",
                    finalBossEncounter,
                    483,
                    out BattleRuntimeEncounterContext finalContext) ||
                !MakeVictory(finalContext) ||
                !TrySettle(finalBoss) ||
                finalContext.VictoryRewards.EnchantChoiceCount != 0 ||
                !finalContext.VictoryRewards.TryClaimGold(out _))
            {
                return false;
            }

            bool finalCompleted =
                RunEncounterProgressService.TryCompleteActive(
                    finalBoss,
                    out RunEncounterProgressFailure finalFailure);
            return !finalCompleted &&
                   finalFailure ==
                   RunEncounterProgressFailure.PermanentRewardPending &&
                   finalBoss.HasActiveEncounter &&
                   finalBoss.CompletedEncounterCount == 0;
        }

        private static bool TryBegin(
            RunEncounterProgressState progress,
            string battleInstanceId,
            EncounterData encounter,
            int shuffleSeed,
            out BattleRuntimeEncounterContext context)
        {
            bool created = RunEncounterProgressService.TryBegin(
                progress,
                battleInstanceId,
                encounter,
                shuffleSeed,
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
                   validationErrors.Count == 0 &&
                   progress.ActiveEncounter == context;
        }

        private static bool TryBeginRejected(
            RunEncounterProgressState progress,
            string battleInstanceId,
            EncounterData encounter,
            RunEncounterProgressFailure expectedFailure)
        {
            bool created = RunEncounterProgressService.TryBegin(
                progress,
                battleInstanceId,
                encounter,
                489,
                5,
                Array.Empty<string>(),
                0,
                out BattleRuntimeEncounterContext context,
                out RunEncounterProgressFailure progressFailure,
                out BattleRuntimeEncounterFlowFailure flowFailure,
                out RunDeckFailure runDeckFailure,
                out BattleRuntimeBootstrapFailure bootstrapFailure,
                out BattleRuntimeSessionFailure sessionFailure,
                out StartingHandRedrawFailure redrawFailure,
                out BattleTurnFailure turnFailure,
                out List<string> validationErrors);
            return !created && context == null &&
                   progressFailure == expectedFailure &&
                   flowFailure == BattleRuntimeEncounterFlowFailure.None &&
                   runDeckFailure == RunDeckFailure.None &&
                   bootstrapFailure == BattleRuntimeBootstrapFailure.None &&
                   sessionFailure == BattleRuntimeSessionFailure.None &&
                   redrawFailure == StartingHandRedrawFailure.NotAvailable &&
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
                context?.Runtime.FindEnemy("TEST-ENEMY-48-A");
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
                        $"OWNED-48-{catalogCardId}"),
                    out RunDeckFailure failure);
                if (!added || failure != RunDeckFailure.None)
                {
                    return null;
                }
            }

            return new RunEncounterProgressState(
                new RunBattleState(30, 24, 5),
                runDeck);
        }

        private static void InitializeEncounter(
            EncounterData encounter,
            string encounterId,
            BattleEncounterGrade grade,
            EnemyDefinitionData enemy)
        {
            encounter.EditorInitialize(
                encounterId,
                encounterId,
                grade,
                new[]
                {
                    new EncounterEnemySlot(
                        "TEST-ENEMY-48-A",
                        enemy,
                        EnemyFieldPosition.Center)
                });
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
