using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleVictoryPermanentRewardServiceValidation
    {
        private const string TestRewardId = "TEST-PERMANENT-50";

        [MenuItem("Have a Break/Validate Victory Permanent Rewards")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            if (valid)
            {
                Debug.Log("Victory permanent rewards passed.");
            }
            else
            {
                Debug.LogError("Victory permanent rewards failed.");
            }

            EditorUtility.DisplayDialog(
                "Victory Permanent Reward Validation",
                valid
                    ? "Final boss permanent reward and progression passed."
                    : "Victory permanent rewards failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            PlayerPermanentRewardState permanentRewards = new();
            RunEncounterProgressState progress =
                CreateProgress(permanentRewards);
            if (progress == null)
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
                    "TEST-ENEMY-50",
                    "Test Permanent Reward Enemy",
                    0,
                    1);
                encounter.EditorInitialize(
                    "TEST-ENCOUNTER-50",
                    "Test Permanent Reward Encounter",
                    BattleEncounterGrade.FinalBoss,
                    new[]
                    {
                        new EncounterEnemySlot(
                            "TEST-ENEMY-50-A",
                            enemy,
                            EnemyFieldPosition.Center)
                    });

                return ValidateFinalBossReward(
                    progress,
                    permanentRewards,
                    encounter);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemy);
                UnityEngine.Object.DestroyImmediate(encounter);
            }
        }

        private static bool ValidateFinalBossReward(
            RunEncounterProgressState progress,
            PlayerPermanentRewardState permanentRewards,
            EncounterData encounter)
        {
            if (!TryBegin(
                    progress,
                    encounter,
                    out BattleRuntimeEncounterContext context))
            {
                return false;
            }

            bool createdBeforeSettlement =
                BattleVictoryPermanentRewardService.TryCreate(
                    progress,
                    out BattleVictoryPermanentRewardService earlyService,
                    out BattleVictoryPermanentRewardFailure earlyFailure);
            if (createdBeforeSettlement || earlyService != null ||
                earlyFailure != BattleVictoryPermanentRewardFailure
                    .SettlementNotComplete ||
                context.VictoryPermanentRewards != null)
            {
                return false;
            }

            if (!MakeVictory(context) || !TrySettle(progress))
            {
                return false;
            }

            bool goldClaimed = context.VictoryRewards.TryClaimGold(
                out BattleRewardFailure goldFailure);
            if (!goldClaimed || goldFailure != BattleRewardFailure.None ||
                context.VictoryRewards.GoldReward != 0 ||
                !context.VictoryRewards.GrantsFinalBossPermanentReward)
            {
                return false;
            }

            bool completedBeforeReward =
                RunEncounterProgressService.TryCompleteActive(
                    progress,
                    out RunEncounterProgressFailure pendingFailure);
            if (completedBeforeReward ||
                pendingFailure !=
                RunEncounterProgressFailure.PermanentRewardPending ||
                !progress.HasActiveEncounter ||
                permanentRewards.RewardIds.Count != 0)
            {
                return false;
            }

            bool created = BattleVictoryPermanentRewardService.TryCreate(
                progress,
                out BattleVictoryPermanentRewardService reward,
                out BattleVictoryPermanentRewardFailure createFailure);
            if (!created || reward == null ||
                createFailure !=
                BattleVictoryPermanentRewardFailure.None ||
                reward.Claimed || reward.ClaimedRewardId != null ||
                context.VictoryPermanentRewards != reward)
            {
                return false;
            }

            bool recreated = BattleVictoryPermanentRewardService.TryCreate(
                progress,
                out BattleVictoryPermanentRewardService recreatedService,
                out BattleVictoryPermanentRewardFailure recreateFailure);
            bool blankClaimed = reward.TryClaim(
                " ",
                out BattleVictoryPermanentRewardFailure blankFailure);
            if (recreated || recreatedService != null ||
                recreateFailure !=
                BattleVictoryPermanentRewardFailure.AlreadyCreated ||
                blankClaimed ||
                blankFailure !=
                BattleVictoryPermanentRewardFailure.InvalidRewardId ||
                reward.Claimed || permanentRewards.RewardIds.Count != 0)
            {
                return false;
            }

            bool claimed = reward.TryClaim(
                TestRewardId,
                out BattleVictoryPermanentRewardFailure claimFailure);
            bool duplicateClaimed = reward.TryClaim(
                "TEST-PERMANENT-50-DUPLICATE",
                out BattleVictoryPermanentRewardFailure duplicateFailure);
            if (!claimed ||
                claimFailure != BattleVictoryPermanentRewardFailure.None ||
                !reward.Claimed || reward.ClaimedRewardId != TestRewardId ||
                permanentRewards.RewardIds.Count != 1 ||
                permanentRewards.RewardIds[0] != TestRewardId ||
                !permanentRewards.Contains(TestRewardId) ||
                duplicateClaimed ||
                duplicateFailure !=
                BattleVictoryPermanentRewardFailure.AlreadyClaimed)
            {
                return false;
            }

            RunEncounterProgressState nextRun =
                CreateProgress(permanentRewards);
            if (nextRun == null ||
                nextRun.PermanentRewards != permanentRewards ||
                !nextRun.PermanentRewards.Contains(TestRewardId))
            {
                return false;
            }

            bool completed = RunEncounterProgressService.TryCompleteActive(
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
                "TEST-BATTLE-50",
                encounter,
                500,
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
                context?.Runtime.FindEnemy("TEST-ENEMY-50-A");
            return enemy != null &&
                   enemy.Vital.ApplyDamage(enemy.Vital.CurrentHealth) == 1 &&
                   context.Runtime.LivingEnemies.TryRemove(enemy.EnemyId);
        }

        private static RunEncounterProgressState CreateProgress(
            PlayerPermanentRewardState permanentRewards)
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
                        $"OWNED-50-{catalogCardId}"),
                    out RunDeckFailure failure);
                if (!added || failure != RunDeckFailure.None)
                {
                    return null;
                }
            }

            return new RunEncounterProgressState(
                new RunBattleState(30, 25, 3),
                runDeck,
                permanentRewards);
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
