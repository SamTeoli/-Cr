using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleSettlementCommandFlowValidation
    {
        [MenuItem("Have a Break/Validate Battle Settlement Command Flow")]
        private static void ValidateFromMenu()
        {
            Debug.Log(Validate()
                ? "Battle settlement command flow passed."
                : "Battle settlement command flow failed.");
        }

        internal static bool Validate()
        {
            CardDatabase cards = AssetDatabase.LoadAssetAtPath<CardDatabase>(
                "Assets/GameData/CardDatabase.asset");
            if (cards == null)
            {
                Debug.LogError("Battle settlement validation: card database missing.");
                return false;
            }

            EnemyDefinitionData victoryEnemy =
                ScriptableObject.CreateInstance<EnemyDefinitionData>();
            EnemyDefinitionData defeatEnemy =
                ScriptableObject.CreateInstance<EnemyDefinitionData>();
            EncounterData normalEncounter =
                ScriptableObject.CreateInstance<EncounterData>();
            EncounterData defeatEncounter =
                ScriptableObject.CreateInstance<EncounterData>();
            EncounterData finalBossEncounter =
                ScriptableObject.CreateInstance<EncounterData>();
            try
            {
                victoryEnemy.EditorInitialize(
                    "TEST-ENEMY-SETTLEMENT-COMMAND-V",
                    "Test Settlement Victory Enemy",
                    0,
                    1);
                defeatEnemy.EditorInitialize(
                    "TEST-ENEMY-SETTLEMENT-COMMAND-D",
                    "Test Settlement Defeat Enemy",
                    3,
                    5);
                normalEncounter.EditorInitialize(
                    "TEST-ENCOUNTER-SETTLEMENT-COMMAND-V",
                    "Test Settlement Victory Encounter",
                    BattleEncounterGrade.Normal,
                    new[]
                    {
                        new EncounterEnemySlot(
                            "TEST-ENEMY-SETTLEMENT-COMMAND-V-A",
                            victoryEnemy,
                            EnemyFieldPosition.Center)
                    });
                defeatEncounter.EditorInitialize(
                    "TEST-ENCOUNTER-SETTLEMENT-COMMAND-D",
                    "Test Settlement Defeat Encounter",
                    BattleEncounterGrade.Normal,
                    new[]
                    {
                        new EncounterEnemySlot(
                            "TEST-ENEMY-SETTLEMENT-COMMAND-D-A",
                            defeatEnemy,
                            EnemyFieldPosition.Center)
                    });
                finalBossEncounter.EditorInitialize(
                    "TEST-ENCOUNTER-SETTLEMENT-COMMAND-F",
                    "Test Settlement Final Boss Encounter",
                    BattleEncounterGrade.FinalBoss,
                    new[]
                    {
                        new EncounterEnemySlot(
                            "TEST-ENEMY-SETTLEMENT-COMMAND-F-A",
                            victoryEnemy,
                            EnemyFieldPosition.Center)
                    });

                BattleSettlementViewModel viewModel = new();
                bool valid = true;
                valid &= RunCase(
                    "invalid state rejection",
                    () => ValidateUnavailable(viewModel));
                valid &= RunCase(
                    "ongoing battle rejection",
                    () => ValidateOngoing(cards, normalEncounter, viewModel));
                valid &= RunCase(
                    "victory settlement and single gold claim",
                    () => ValidateVictory(cards, normalEncounter, viewModel));
                valid &= RunCase(
                    "defeat settlement and encounter completion",
                    () => ValidateDefeat(cards, defeatEncounter, viewModel));
                valid &= RunCase(
                    "final boss partial settlement resume",
                    () => ValidateFinalBossResume(
                        cards,
                        finalBossEncounter,
                        viewModel));
                return valid;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(victoryEnemy);
                UnityEngine.Object.DestroyImmediate(defeatEnemy);
                UnityEngine.Object.DestroyImmediate(normalEncounter);
                UnityEngine.Object.DestroyImmediate(defeatEncounter);
                UnityEngine.Object.DestroyImmediate(finalBossEncounter);
            }
        }

        private static bool ValidateUnavailable(
            BattleSettlementViewModel viewModel)
        {
            BattleSettlementCommandResult result =
                viewModel.TrySettle(null, null);
            bool passed = result != null &&
                          !result.Succeeded &&
                          result.Failure ==
                              BattleSettlementCommandFailure.InvalidState &&
                          result.Outcome == BattleOutcome.Ongoing &&
                          !result.GoldClaimed &&
                          !result.ActiveEncounterCompleted &&
                          !string.IsNullOrWhiteSpace(result.Message);
            if (!passed)
            {
                Debug.LogError($"Invalid state details: {Describe(result)}");
            }
            return passed;
        }

        private static bool ValidateOngoing(
            CardDatabase cards,
            EncounterData encounter,
            BattleSettlementViewModel viewModel)
        {
            RunEncounterProgressState progress = CreateProgress(
                cards,
                new PlayerPermanentRewardState(),
                30,
                25,
                3);
            RunCampaignState campaign = CampaignAtBattle();
            if (progress == null || campaign == null ||
                !TryBegin(
                    progress,
                    encounter,
                    "TEST-BATTLE-SETTLEMENT-COMMAND-ONGOING",
                    620,
                    out BattleRuntimeEncounterContext context))
            {
                Debug.LogError("Ongoing setup failed.");
                return false;
            }

            int goldBefore = progress.RunState.Gold;
            BattleSettlementCommandResult result =
                viewModel.TrySettle(campaign, progress);
            bool passed = result != null &&
                          !result.Succeeded &&
                          result.Failure ==
                              BattleSettlementCommandFailure.InvalidState &&
                          !context.Settlement.IsSettled &&
                          progress.HasActiveEncounter &&
                          progress.RunState.Gold == goldBefore &&
                          campaign.Phase == RunCampaignPhase.Battle;
            if (!passed)
            {
                Debug.LogError(
                    $"Ongoing details: {Describe(result)}, " +
                    $"settled={context.Settlement.IsSettled}, " +
                    $"active={progress.HasActiveEncounter}, " +
                    $"gold={progress.RunState.Gold}/{goldBefore}, " +
                    $"phase={campaign.Phase}");
            }
            return passed;
        }

        private static bool ValidateVictory(
            CardDatabase cards,
            EncounterData encounter,
            BattleSettlementViewModel viewModel)
        {
            RunEncounterProgressState progress = CreateProgress(
                cards,
                new PlayerPermanentRewardState(),
                30,
                23,
                5);
            RunCampaignState campaign = CampaignAtBattle();
            if (progress == null || campaign == null ||
                !TryBegin(
                    progress,
                    encounter,
                    "TEST-BATTLE-SETTLEMENT-COMMAND-VICTORY",
                    621,
                    out BattleRuntimeEncounterContext context) ||
                !MakeVictory(
                    context,
                    "TEST-ENEMY-SETTLEMENT-COMMAND-V-A"))
            {
                Debug.LogError("Victory setup failed.");
                return false;
            }

            int goldBefore = progress.RunState.Gold;
            BattleSettlementCommandResult result =
                viewModel.TrySettle(campaign, progress);
            int goldAfterFirst = progress.RunState.Gold;
            BattleSettlementCommandResult duplicate =
                viewModel.TrySettle(campaign, progress);
            bool passed = result != null && result.Succeeded &&
                          result.Failure ==
                              BattleSettlementCommandFailure.None &&
                          result.Outcome == BattleOutcome.Victory &&
                          result.GoldClaimed && result.GoldReward >= 0 &&
                          !result.PermanentRewardRequired &&
                          !result.ActiveEncounterCompleted &&
                          result.CampaignPhase == RunCampaignPhase.Reward &&
                          context.Settlement.IsSettled &&
                          context.VictoryRewards.GoldClaimed &&
                          progress.RunState.Gold ==
                              goldBefore + result.GoldReward &&
                          progress.HasActiveEncounter &&
                          campaign.Phase == RunCampaignPhase.Reward &&
                          !string.IsNullOrWhiteSpace(result.Message) &&
                          duplicate != null && !duplicate.Succeeded &&
                          duplicate.Failure ==
                              BattleSettlementCommandFailure.InvalidState &&
                          progress.RunState.Gold == goldAfterFirst;
            if (!passed)
            {
                Debug.LogError(
                    $"Victory details: first=({Describe(result)}), " +
                    $"duplicate=({Describe(duplicate)}), gold=" +
                    $"{progress.RunState.Gold}, before={goldBefore}, " +
                    $"afterFirst={goldAfterFirst}, settled=" +
                    $"{context.Settlement.IsSettled}, active=" +
                    $"{progress.HasActiveEncounter}, phase={campaign.Phase}");
            }
            return passed;
        }

        private static bool ValidateDefeat(
            CardDatabase cards,
            EncounterData encounter,
            BattleSettlementViewModel viewModel)
        {
            RunEncounterProgressState progress = CreateProgress(
                cards,
                new PlayerPermanentRewardState(),
                3,
                3,
                4);
            RunCampaignState campaign = CampaignAtBattle();
            if (progress == null || campaign == null ||
                !TryBegin(
                    progress,
                    encounter,
                    "TEST-BATTLE-SETTLEMENT-COMMAND-DEFEAT",
                    622,
                    out BattleRuntimeEncounterContext context) ||
                !MakeDefeat(
                    context,
                    "TEST-ENEMY-SETTLEMENT-COMMAND-D-A"))
            {
                Debug.LogError("Defeat setup failed.");
                return false;
            }

            int goldBefore = progress.RunState.Gold;
            BattleSettlementCommandResult result =
                viewModel.TrySettle(campaign, progress);
            bool passed = result != null && result.Succeeded &&
                          result.Failure ==
                              BattleSettlementCommandFailure.None &&
                          result.Outcome == BattleOutcome.Defeat &&
                          !result.GoldClaimed &&
                          !result.PermanentRewardRequired &&
                          result.ActiveEncounterCompleted &&
                          result.CampaignPhase == RunCampaignPhase.Defeated &&
                          !progress.HasActiveEncounter &&
                          progress.CompletedEncounterCount == 1 &&
                          progress.RunState.RunEnded &&
                          progress.RunState.CurrentHealth == 0 &&
                          progress.RunState.Gold == goldBefore &&
                          campaign.Phase == RunCampaignPhase.Defeated &&
                          !string.IsNullOrWhiteSpace(result.Message);
            if (!passed)
            {
                Debug.LogError(
                    $"Defeat details: {Describe(result)}, active=" +
                    $"{progress.HasActiveEncounter}, completed=" +
                    $"{progress.CompletedEncounterCount}, runEnded=" +
                    $"{progress.RunState.RunEnded}, hp=" +
                    $"{progress.RunState.CurrentHealth}, gold=" +
                    $"{progress.RunState.Gold}/{goldBefore}, phase=" +
                    $"{campaign.Phase}");
            }
            return passed;
        }

        private static bool ValidateFinalBossResume(
            CardDatabase cards,
            EncounterData encounter,
            BattleSettlementViewModel viewModel)
        {
            PlayerPermanentRewardState permanentRewards = new();
            RunEncounterProgressState progress = CreateProgress(
                cards,
                permanentRewards,
                30,
                25,
                7);
            RunCampaignState campaign = CampaignAtBattle();
            if (progress == null || campaign == null ||
                !TryBegin(
                    progress,
                    encounter,
                    "TEST-BATTLE-SETTLEMENT-COMMAND-FINAL",
                    623,
                    out BattleRuntimeEncounterContext context) ||
                !MakeVictory(
                    context,
                    "TEST-ENEMY-SETTLEMENT-COMMAND-F-A") ||
                !TrySettleDirect(progress) ||
                !context.VictoryRewards.TryClaimGold(
                    out BattleRewardFailure goldFailure) ||
                goldFailure != BattleRewardFailure.None ||
                !BattleVictoryPermanentRewardService.TryCreate(
                    progress,
                    out BattleVictoryPermanentRewardService permanent,
                    out BattleVictoryPermanentRewardFailure createFailure) ||
                permanent == null || permanent.Claimed ||
                createFailure != BattleVictoryPermanentRewardFailure.None)
            {
                Debug.LogError("Final boss resume setup failed.");
                return false;
            }

            int expectedGoldReward = context.VictoryRewards.GoldReward;
            int goldBeforeRetry = progress.RunState.Gold;
            BattleSettlementCommandResult result =
                viewModel.TrySettle(campaign, progress);
            bool passed = result != null && result.Succeeded &&
                          result.Outcome == BattleOutcome.Victory &&
                          result.GoldClaimed &&
                          result.GoldReward == expectedGoldReward &&
                          result.PermanentRewardRequired &&
                          result.PermanentRewardClaimed &&
                          !result.ActiveEncounterCompleted &&
                          result.CampaignPhase == RunCampaignPhase.Reward &&
                          progress.RunState.Gold == goldBeforeRetry &&
                          permanent.Claimed &&
                          permanent.ClaimedRewardId ==
                              BattleSettlementViewModel
                                  .FinalBossPermanentRewardId &&
                          permanentRewards.Contains(
                              BattleSettlementViewModel
                                  .FinalBossPermanentRewardId) &&
                          permanentRewards.RewardIds.Count == 1 &&
                          campaign.Phase == RunCampaignPhase.Reward &&
                          progress.HasActiveEncounter;
            if (!passed)
            {
                Debug.LogError(
                    $"Final boss details: {Describe(result)}, expectedGold=" +
                    $"{expectedGoldReward}, gold={progress.RunState.Gold}/" +
                    $"{goldBeforeRetry}, permanentClaimed={permanent.Claimed}, " +
                    $"rewardId={permanent.ClaimedRewardId}, count=" +
                    $"{permanentRewards.RewardIds.Count}, active=" +
                    $"{progress.HasActiveEncounter}, phase={campaign.Phase}");
            }
            return passed;
        }

        private static string Describe(BattleSettlementCommandResult result)
        {
            if (result == null)
            {
                return "result=null";
            }

            return $"success={result.Succeeded}, failure={result.Failure}, " +
                   $"outcome={result.Outcome}, gold={result.GoldReward}, " +
                   $"goldClaimed={result.GoldClaimed}, permanentRequired=" +
                   $"{result.PermanentRewardRequired}, permanentClaimed=" +
                   $"{result.PermanentRewardClaimed}, encounterCompleted=" +
                   $"{result.ActiveEncounterCompleted}, phase=" +
                   $"{result.CampaignPhase}, progressFailure=" +
                   $"{result.ProgressFailure}, flowFailure={result.FlowFailure}, " +
                   $"sessionFailure={result.SessionFailure}, settlementFailure=" +
                   $"{result.SettlementFailure}, rewardFailure=" +
                   $"{result.RewardFailure}, permanentFailure=" +
                   $"{result.PermanentRewardFailure}, message={result.Message}";
        }

        private static bool RunCase(string label, Func<bool> validation)
        {
            try
            {
                bool passed = validation();
                if (passed)
                {
                    Debug.Log($"Battle settlement validation passed: {label}.");
                }
                else
                {
                    Debug.LogError($"Battle settlement validation failed: {label}.");
                }

                return passed;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Battle settlement validation threw: {label}.\n{exception}");
                return false;
            }
        }

        private static RunEncounterProgressState CreateProgress(
            CardDatabase database,
            PlayerPermanentRewardState permanentRewards,
            int maximumHealth,
            int currentHealth,
            int gold)
        {
            if (database == null || permanentRewards == null)
            {
                return null;
            }

            RunDeckState deck = new();
            for (int number = 1; number <= 12; number++)
            {
                string catalogCardId = $"C{number:00}";
                CardData card = database.Cards.FirstOrDefault(value =>
                    value != null && string.Equals(
                        value.CatalogCardId,
                        catalogCardId,
                        StringComparison.OrdinalIgnoreCase));
                if (card == null || !deck.TryAdd(
                        new RunCardInstance(
                            card,
                            $"OWNED-SETTLEMENT-COMMAND-{catalogCardId}"),
                        out RunDeckFailure failure) ||
                    failure != RunDeckFailure.None)
                {
                    return null;
                }
            }

            return new RunEncounterProgressState(
                new RunBattleState(maximumHealth, currentHealth, gold),
                deck,
                permanentRewards);
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
                        out RunCampaignFailure failure) &&
                    failure == RunCampaignFailure.None)
                {
                    return campaign;
                }
            }

            return null;
        }

        private static bool TryBegin(
            RunEncounterProgressState progress,
            EncounterData encounter,
            string battleId,
            int seed,
            out BattleRuntimeEncounterContext context)
        {
            bool created = RunEncounterProgressService.TryBegin(
                progress,
                battleId,
                encounter,
                seed,
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
            BattleRuntimeEncounterContext context,
            string enemyId)
        {
            BattleEnemyRuntimeState enemy =
                context?.Runtime.FindEnemy(enemyId);
            if (enemy == null ||
                enemy.Vital.ApplyDamage(enemy.Vital.CurrentHealth) <= 0 ||
                !context.Runtime.LivingEnemies.TryRemove(enemy.EnemyId))
            {
                return false;
            }

            return BattleRuntimeSessionService.TryFinalizeTerminalOutcome(
                       context.Session,
                       out BattleOutcome outcome,
                       out BattleRuntimeSessionFailure failure) &&
                   outcome == BattleOutcome.Victory &&
                   failure == BattleRuntimeSessionFailure.None &&
                   context.Session.IsFinished;
        }

        private static bool MakeDefeat(
            BattleRuntimeEncounterContext context,
            string enemyId)
        {
            bool resolved = BattleRuntimeSessionService.TryResolveRound(
                context.Session,
                new[]
                {
                    BattleRuntimeEnemyTurnCommand.CreateAutomaticAttack(
                        enemyId,
                        1,
                        new[] { 0 })
                },
                out BattleRuntimeSessionRoundResult result,
                out BattleRuntimeSessionFailure sessionFailure,
                out BattleRuntimeRoundFailure roundFailure,
                out BattleTurnFailure turnFailure,
                out BattleRuntimeEnemyTurnPipelineFailure pipelineFailure,
                out BattleRuntimeEnemyTurnPlanFailure planFailure,
                out BattleRuntimeEnemyTurnFailure enemyTurnFailure,
                out int actionIndex);
            return resolved && result != null &&
                   result.Outcome == BattleOutcome.Defeat &&
                   sessionFailure == BattleRuntimeSessionFailure.None &&
                   roundFailure == BattleRuntimeRoundFailure.None &&
                   turnFailure == BattleTurnFailure.None &&
                   pipelineFailure ==
                       BattleRuntimeEnemyTurnPipelineFailure.None &&
                   planFailure == BattleRuntimeEnemyTurnPlanFailure.None &&
                   enemyTurnFailure == BattleRuntimeEnemyTurnFailure.None &&
                   actionIndex == -1 &&
                   context.Session.IsFinished;
        }

        private static bool TrySettleDirect(
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
