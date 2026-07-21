using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleRuntimeEncounterFlowValidation
    {
        [MenuItem("Have a Break/Validate Battle Runtime Encounter Flow")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            if (valid)
            {
                Debug.Log("Battle runtime encounter flow passed.");
            }
            else
            {
                Debug.LogError("Battle runtime encounter flow failed.");
            }

            EditorUtility.DisplayDialog(
                "Battle Runtime Encounter Flow Validation",
                valid
                    ? "Battle runtime encounter lifecycle, settlement, and rewards passed."
                    : "Battle runtime encounter flow failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            CardData c01 = FindCard(TestContentIds.C01);
            if (c01 == null)
            {
                return false;
            }

            EnemyDefinitionData victoryEnemy =
                ScriptableObject.CreateInstance<EnemyDefinitionData>();
            EnemyDefinitionData defeatEnemy =
                ScriptableObject.CreateInstance<EnemyDefinitionData>();
            EncounterData victoryEncounter =
                ScriptableObject.CreateInstance<EncounterData>();
            EncounterData defeatEncounter =
                ScriptableObject.CreateInstance<EncounterData>();
            EncounterData invalidGradeEncounter =
                ScriptableObject.CreateInstance<EncounterData>();
            try
            {
                victoryEnemy.EditorInitialize(
                    "TEST-ENEMY-44-V",
                    "Test Victory Enemy",
                    0,
                    1);
                defeatEnemy.EditorInitialize(
                    "TEST-ENEMY-44-D",
                    "Test Defeat Enemy",
                    3,
                    5);
                victoryEncounter.EditorInitialize(
                    "TEST-ENCOUNTER-44-V",
                    "Test Victory Encounter",
                    BattleEncounterGrade.Normal,
                    new[]
                    {
                        new EncounterEnemySlot(
                            "TEST-ENEMY-44-V-A",
                            victoryEnemy,
                            EnemyFieldPosition.Center)
                    });
                defeatEncounter.EditorInitialize(
                    "TEST-ENCOUNTER-44-D",
                    "Test Defeat Encounter",
                    BattleEncounterGrade.Normal,
                    new[]
                    {
                        new EncounterEnemySlot(
                            "TEST-ENEMY-44-D-A",
                            defeatEnemy,
                            EnemyFieldPosition.Center)
                    });
                invalidGradeEncounter.EditorInitialize(
                    "TEST-ENCOUNTER-44-INVALID",
                    "Test Invalid Grade Encounter",
                    (BattleEncounterGrade)999,
                    new[]
                    {
                        new EncounterEnemySlot(
                            "TEST-ENEMY-44-INVALID-A",
                            victoryEnemy,
                            EnemyFieldPosition.Center)
                    });

                return ValidateInvalidGrade(
                           c01, invalidGradeEncounter) &&
                       ValidateOngoingRejection(
                           c01, victoryEncounter) &&
                       ValidatePlayerTurnVictory(
                           c01, victoryEncounter) &&
                       ValidateEnemyTurnDefeat(
                           c01, defeatEncounter);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(victoryEnemy);
                UnityEngine.Object.DestroyImmediate(defeatEnemy);
                UnityEngine.Object.DestroyImmediate(victoryEncounter);
                UnityEngine.Object.DestroyImmediate(defeatEncounter);
                UnityEngine.Object.DestroyImmediate(invalidGradeEncounter);
            }
        }

        private static bool ValidateInvalidGrade(
            CardData card,
            EncounterData encounter)
        {
            List<string> directErrors =
                EncounterDataValidationService.ValidateEncounter(encounter);
            bool created = BattleRuntimeEncounterFlowService.TryCreateAndBegin(
                new[] { Instance(card, "INVALID-GRADE") },
                new RunBattleState(30, 30, 0),
                encounter,
                440,
                5,
                Array.Empty<string>(),
                0,
                out _,
                out BattleRuntimeEncounterFlowFailure flowFailure,
                out BattleRuntimeBootstrapFailure bootstrapFailure,
                out _, out _, out _,
                out List<string> bootstrapErrors);

            return directErrors.Exists(error => error.IndexOf(
                       "invalid encounter grade",
                       StringComparison.OrdinalIgnoreCase) >= 0) &&
                   !created &&
                   flowFailure ==
                   BattleRuntimeEncounterFlowFailure.BootstrapFailed &&
                   bootstrapFailure ==
                   BattleRuntimeBootstrapFailure.InvalidEncounter &&
                   bootstrapErrors.Count > 0;
        }

        private static bool ValidateOngoingRejection(
            CardData card,
            EncounterData encounter)
        {
            RunBattleState run = new(30, 26, 7);
            if (!TryCreateFlow(
                    card,
                    "ONGOING",
                    run,
                    encounter,
                    441,
                    1,
                    out BattleRuntimeEncounterContext context))
            {
                return false;
            }

            bool settled = BattleRuntimeEncounterFlowService.TrySettle(
                context,
                out BattleRuntimeEncounterFlowFailure flowFailure,
                out BattleRuntimeSessionFailure sessionFailure,
                out BattleSettlementFailure settlementFailure);
            return !settled &&
                   flowFailure ==
                   BattleRuntimeEncounterFlowFailure.SessionNotFinished &&
                   sessionFailure ==
                   BattleRuntimeSessionFailure.BattleOngoing &&
                   settlementFailure ==
                   BattleSettlementFailure.BattleOngoing &&
                   !context.Session.IsFinished &&
                   !context.Settlement.IsSettled &&
                   run.CurrentHealth == 26 &&
                   run.Gold == 7;
        }

        private static bool ValidatePlayerTurnVictory(
            CardData card,
            EncounterData encounter)
        {
            RunBattleState run = new(30, 23, 5);
            if (!TryCreateFlow(
                    card,
                    "VICTORY",
                    run,
                    encounter,
                    442,
                    0,
                    out BattleRuntimeEncounterContext context))
            {
                return false;
            }

            BattleEnemyRuntimeState enemy =
                context.Runtime.FindEnemy("TEST-ENEMY-44-V-A");
            if (enemy == null ||
                enemy.Vital.ApplyDamage(enemy.Vital.CurrentHealth) != 1 ||
                !context.Runtime.LivingEnemies.TryRemove(enemy.EnemyId))
            {
                return false;
            }

            if (!BattleRuntimeEncounterFlowService.TrySettle(
                    context,
                    out BattleRuntimeEncounterFlowFailure flowFailure,
                    out BattleRuntimeSessionFailure sessionFailure,
                    out BattleSettlementFailure settlementFailure) ||
                flowFailure != BattleRuntimeEncounterFlowFailure.None ||
                sessionFailure != BattleRuntimeSessionFailure.None ||
                settlementFailure != BattleSettlementFailure.None)
            {
                return false;
            }

            bool firstClaim = context.VictoryRewards.TryClaimGold(
                out BattleRewardFailure firstRewardFailure);
            bool duplicateRejected =
                !context.VictoryRewards.TryClaimGold(
                    out BattleRewardFailure duplicateFailure);
            return context.Session.IsFinished &&
                   context.Session.Outcome == BattleOutcome.Victory &&
                   context.Session.CompletedRoundCount == 0 &&
                   context.Settlement.IsSettled &&
                   context.Settlement.RewardEligible &&
                   context.Settlement.BattleStateDiscarded &&
                   context.VictoryRewards.GoldReward == 20 &&
                   firstClaim &&
                   firstRewardFailure == BattleRewardFailure.None &&
                   duplicateRejected &&
                   duplicateFailure == BattleRewardFailure.AlreadyClaimed &&
                   run.CurrentHealth == 23 &&
                   run.Gold == 25 &&
                   !run.RunEnded;
        }

        private static bool ValidateEnemyTurnDefeat(
            CardData card,
            EncounterData encounter)
        {
            RunBattleState run = new(3, 3, 4);
            if (!TryCreateFlow(
                    card,
                    "DEFEAT",
                    run,
                    encounter,
                    443,
                    2,
                    out BattleRuntimeEncounterContext context) ||
                !BattleRuntimeSessionService.TryResolveRound(
                    context.Session,
                    new[]
                    {
                        BattleRuntimeEnemyTurnCommand.CreateAutomaticAttack(
                            "TEST-ENEMY-44-D-A",
                            1,
                            new[] { 0 })
                    },
                    out BattleRuntimeSessionRoundResult round,
                    out BattleRuntimeSessionFailure sessionFailure,
                    out BattleRuntimeRoundFailure roundFailure,
                    out BattleTurnFailure turnFailure,
                    out BattleRuntimeEnemyTurnPipelineFailure pipelineFailure,
                    out BattleRuntimeEnemyTurnPlanFailure planFailure,
                    out BattleRuntimeEnemyTurnFailure enemyTurnFailure,
                    out int failedActionIndex) ||
                sessionFailure != BattleRuntimeSessionFailure.None ||
                roundFailure != BattleRuntimeRoundFailure.None ||
                turnFailure != BattleTurnFailure.None ||
                pipelineFailure !=
                BattleRuntimeEnemyTurnPipelineFailure.None ||
                planFailure != BattleRuntimeEnemyTurnPlanFailure.None ||
                enemyTurnFailure != BattleRuntimeEnemyTurnFailure.None ||
                failedActionIndex != -1 ||
                round == null ||
                round.Outcome != BattleOutcome.Defeat)
            {
                return false;
            }

            bool settled = BattleRuntimeEncounterFlowService.TrySettle(
                context,
                out BattleRuntimeEncounterFlowFailure flowFailure,
                out sessionFailure,
                out BattleSettlementFailure settlementFailure);
            bool rewardRejected =
                !context.VictoryRewards.TryClaimGold(
                    out BattleRewardFailure rewardFailure);
            return settled &&
                   flowFailure == BattleRuntimeEncounterFlowFailure.None &&
                   sessionFailure == BattleRuntimeSessionFailure.None &&
                   settlementFailure == BattleSettlementFailure.None &&
                   context.Session.IsFinished &&
                   context.Session.Outcome == BattleOutcome.Defeat &&
                   context.Settlement.SettledOutcome ==
                   BattleOutcome.Defeat &&
                   !context.Settlement.RewardEligible &&
                   rewardRejected &&
                   rewardFailure == BattleRewardFailure.NotVictory &&
                   run.CurrentHealth == 0 &&
                   run.Gold == 4 &&
                   run.RunEnded;
        }

        private static bool TryCreateFlow(
            CardData card,
            string suffix,
            RunBattleState run,
            EncounterData encounter,
            int shuffleSeed,
            uint rewardSeed,
            out BattleRuntimeEncounterContext context)
        {
            bool created =
                BattleRuntimeEncounterFlowService.TryCreateAndBegin(
                    new[] { Instance(card, suffix) },
                    run,
                    encounter,
                    shuffleSeed,
                    5,
                    Array.Empty<string>(),
                    rewardSeed,
                    out context,
                    out BattleRuntimeEncounterFlowFailure flowFailure,
                    out BattleRuntimeBootstrapFailure bootstrapFailure,
                    out BattleRuntimeSessionFailure sessionFailure,
                    out StartingHandRedrawFailure redrawFailure,
                    out BattleTurnFailure turnFailure,
                    out List<string> errors);
            return created &&
                   flowFailure == BattleRuntimeEncounterFlowFailure.None &&
                   bootstrapFailure == BattleRuntimeBootstrapFailure.None &&
                   sessionFailure == BattleRuntimeSessionFailure.None &&
                   redrawFailure == StartingHandRedrawFailure.None &&
                   turnFailure == BattleTurnFailure.None &&
                   errors.Count == 0 &&
                   context != null &&
                   context.Session.Started &&
                   !context.Session.IsFinished &&
                   context.PendingSettlementEffects.Count == 0 &&
                   context.RunState == run &&
                   context.Encounter == encounter;
        }

        private static BattleCardInstance Instance(
            CardData card,
            string suffix)
        {
            return new BattleCardInstance(
                card,
                new CardInstanceIds(
                    card.CatalogCardId,
                    $"OWNED-RUNTIME-44-{suffix}",
                    $"BATTLE-RUNTIME-44-{suffix}"),
                1,
                CardZone.DrawPile);
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
    }
}
