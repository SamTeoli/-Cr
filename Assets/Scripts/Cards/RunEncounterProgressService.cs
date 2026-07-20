using System;
using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public static class RunEncounterProgressService
    {
        public static bool TryBegin(
            RunEncounterProgressState progress,
            string battleInstanceId,
            EncounterData encounter,
            int shuffleSeed,
            int maximumMana,
            IEnumerable<string> selectedStartingHandRedrawIds,
            uint rewardSeed,
            out BattleRuntimeEncounterContext context,
            out RunEncounterProgressFailure failure,
            out BattleRuntimeEncounterFlowFailure flowFailure,
            out RunDeckFailure runDeckFailure,
            out BattleRuntimeBootstrapFailure bootstrapFailure,
            out BattleRuntimeSessionFailure sessionFailure,
            out StartingHandRedrawFailure redrawFailure,
            out BattleTurnFailure turnFailure,
            out List<string> validationErrors)
        {
            context = null;
            flowFailure = BattleRuntimeEncounterFlowFailure.None;
            runDeckFailure = RunDeckFailure.None;
            bootstrapFailure = BattleRuntimeBootstrapFailure.None;
            sessionFailure = BattleRuntimeSessionFailure.None;
            redrawFailure = StartingHandRedrawFailure.NotAvailable;
            turnFailure = BattleTurnFailure.None;
            validationErrors = new List<string>();

            if (progress?.RunState == null || progress.RunDeck == null)
            {
                failure = RunEncounterProgressFailure.InvalidState;
                return false;
            }

            if (progress.RunState.RunEnded)
            {
                failure = RunEncounterProgressFailure.RunEnded;
                return false;
            }

            if (progress.HasActiveEncounter)
            {
                failure =
                    RunEncounterProgressFailure.EncounterAlreadyActive;
                return false;
            }

            if (progress.HasUsedBattleInstanceId(battleInstanceId))
            {
                failure = RunEncounterProgressFailure
                    .BattleInstanceAlreadyUsed;
                return false;
            }

            bool created = BattleRuntimeEncounterFlowService
                .TryCreateAndBegin(
                    progress.RunDeck,
                    battleInstanceId,
                    progress.RunState,
                    encounter,
                    shuffleSeed,
                    maximumMana,
                    selectedStartingHandRedrawIds,
                    rewardSeed,
                    out context,
                    out flowFailure,
                    out runDeckFailure,
                    out bootstrapFailure,
                    out sessionFailure,
                    out redrawFailure,
                    out turnFailure,
                    out validationErrors);
            if (!created)
            {
                failure = RunEncounterProgressFailure.BeginFailed;
                return false;
            }

            progress.SetActiveEncounter(battleInstanceId, context);
            failure = RunEncounterProgressFailure.None;
            return true;
        }

        public static bool TrySettleActive(
            RunEncounterProgressState progress,
            out RunEncounterProgressFailure failure,
            out BattleRuntimeEncounterFlowFailure flowFailure,
            out BattleRuntimeSessionFailure sessionFailure,
            out BattleSettlementFailure settlementFailure)
        {
            flowFailure = BattleRuntimeEncounterFlowFailure.None;
            sessionFailure = BattleRuntimeSessionFailure.None;
            settlementFailure = BattleSettlementFailure.None;
            if (progress?.RunState == null || progress.RunDeck == null)
            {
                failure = RunEncounterProgressFailure.InvalidState;
                return false;
            }

            if (progress.ActiveEncounter == null)
            {
                failure = RunEncounterProgressFailure.NoActiveEncounter;
                return false;
            }

            if (!BattleRuntimeEncounterFlowService.TrySettle(
                    progress.ActiveEncounter,
                    out flowFailure,
                    out sessionFailure,
                    out settlementFailure))
            {
                failure = RunEncounterProgressFailure.SettlementFailed;
                return false;
            }

            failure = RunEncounterProgressFailure.None;
            return true;
        }

        public static bool TryCompleteActive(
            RunEncounterProgressState progress,
            out RunEncounterProgressFailure failure)
        {
            if (progress?.RunState == null || progress.RunDeck == null)
            {
                failure = RunEncounterProgressFailure.InvalidState;
                return false;
            }

            BattleRuntimeEncounterContext context =
                progress.ActiveEncounter;
            if (context == null)
            {
                failure = RunEncounterProgressFailure.NoActiveEncounter;
                return false;
            }

            if (!context.Settlement.IsSettled)
            {
                failure =
                    RunEncounterProgressFailure.SettlementNotComplete;
                return false;
            }

            if (context.Settlement.SettledOutcome == BattleOutcome.Defeat)
            {
                progress.CompleteActiveEncounter();
                failure = RunEncounterProgressFailure.None;
                return true;
            }

            if (context.Settlement.SettledOutcome != BattleOutcome.Victory ||
                context.VictoryRewards == null)
            {
                failure = RunEncounterProgressFailure.InvalidState;
                return false;
            }

            if (!context.VictoryRewards.GoldClaimed)
            {
                failure = RunEncounterProgressFailure.GoldRewardPending;
                return false;
            }

            if (context.VictoryRewards.EnchantChoiceCount > 0 &&
                (context.VictoryEnchantRewards == null ||
                 !context.VictoryEnchantRewards.Claimed))
            {
                failure =
                    RunEncounterProgressFailure.EnchantRewardPending;
                return false;
            }

            if (context.VictoryRewards.ConsumableItemRewardCount > 0 &&
                (context.VictoryConsumableRewards == null ||
                 !context.VictoryConsumableRewards.Claimed))
            {
                failure = RunEncounterProgressFailure
                    .ConsumableRewardPending;
                return false;
            }

            if (context.VictoryRewards.GrantsFinalBossPermanentReward &&
                (context.VictoryPermanentRewards == null ||
                 !context.VictoryPermanentRewards.Claimed))
            {
                failure =
                    RunEncounterProgressFailure.PermanentRewardPending;
                return false;
            }

            progress.CompleteActiveEncounter();
            failure = RunEncounterProgressFailure.None;
            return true;
        }
    }
}
