using System;
using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public static class BattleRuntimeEncounterFlowService
    {
        public static bool TryCreateAndBegin(
            IEnumerable<BattleCardInstance> deckCards,
            RunBattleState runState,
            EncounterData encounter,
            int shuffleSeed,
            int maximumMana,
            IEnumerable<string> selectedStartingHandRedrawIds,
            uint rewardSeed,
            out BattleRuntimeEncounterContext context,
            out BattleRuntimeEncounterFlowFailure failure,
            out BattleRuntimeBootstrapFailure bootstrapFailure,
            out BattleRuntimeSessionFailure sessionFailure,
            out StartingHandRedrawFailure redrawFailure,
            out BattleTurnFailure turnFailure,
            out List<string> validationErrors)
        {
            return TryCreateAndBeginCore(
                deckCards,
                null,
                runState,
                encounter,
                shuffleSeed,
                maximumMana,
                selectedStartingHandRedrawIds,
                rewardSeed,
                out context,
                out failure,
                out _,
                out bootstrapFailure,
                out sessionFailure,
                out redrawFailure,
                out turnFailure,
                out validationErrors);
        }

        public static bool TryCreateAndBegin(
            RunDeckState runDeck,
            string battleInstanceId,
            RunBattleState runState,
            EncounterData encounter,
            int shuffleSeed,
            int maximumMana,
            IEnumerable<string> selectedStartingHandRedrawIds,
            uint rewardSeed,
            out BattleRuntimeEncounterContext context,
            out BattleRuntimeEncounterFlowFailure failure,
            out RunDeckFailure runDeckFailure,
            out BattleRuntimeBootstrapFailure bootstrapFailure,
            out BattleRuntimeSessionFailure sessionFailure,
            out StartingHandRedrawFailure redrawFailure,
            out BattleTurnFailure turnFailure,
            out List<string> validationErrors)
        {
            context = null;
            bootstrapFailure = BattleRuntimeBootstrapFailure.None;
            sessionFailure = BattleRuntimeSessionFailure.None;
            redrawFailure = StartingHandRedrawFailure.NotAvailable;
            turnFailure = BattleTurnFailure.None;
            validationErrors = new List<string>();

            if (!RunDeckBattleSnapshotService.TryCreate(
                    runDeck,
                    battleInstanceId,
                    out RunDeckBattleSnapshot deckSnapshot,
                    out runDeckFailure))
            {
                failure =
                    BattleRuntimeEncounterFlowFailure.RunDeckSnapshotFailed;
                return false;
            }

            return TryCreateAndBeginCore(
                deckSnapshot.Cards,
                deckSnapshot,
                runState,
                encounter,
                shuffleSeed,
                maximumMana,
                selectedStartingHandRedrawIds,
                rewardSeed,
                out context,
                out failure,
                out runDeckFailure,
                out bootstrapFailure,
                out sessionFailure,
                out redrawFailure,
                out turnFailure,
                out validationErrors);
        }

        private static bool TryCreateAndBeginCore(
            IEnumerable<BattleCardInstance> deckCards,
            RunDeckBattleSnapshot deckSnapshot,
            RunBattleState runState,
            EncounterData encounter,
            int shuffleSeed,
            int maximumMana,
            IEnumerable<string> selectedStartingHandRedrawIds,
            uint rewardSeed,
            out BattleRuntimeEncounterContext context,
            out BattleRuntimeEncounterFlowFailure failure,
            out RunDeckFailure runDeckFailure,
            out BattleRuntimeBootstrapFailure bootstrapFailure,
            out BattleRuntimeSessionFailure sessionFailure,
            out StartingHandRedrawFailure redrawFailure,
            out BattleTurnFailure turnFailure,
            out List<string> validationErrors)
        {
            context = null;
            runDeckFailure = RunDeckFailure.None;
            sessionFailure = BattleRuntimeSessionFailure.None;
            redrawFailure = StartingHandRedrawFailure.NotAvailable;
            turnFailure = BattleTurnFailure.None;
            if (!BattleRuntimeBootstrapService.TryCreate(
                    deckCards,
                    runState,
                    encounter,
                    shuffleSeed,
                    maximumMana,
                    out BattleRuntimeBootstrapResult bootstrap,
                    out bootstrapFailure,
                    out validationErrors))
            {
                failure = BattleRuntimeEncounterFlowFailure.BootstrapFailed;
                return false;
            }

            if (deckSnapshot != null &&
                !deckSnapshot.TryRegisterEnchants(
                    bootstrap.Runtime,
                    out runDeckFailure))
            {
                failure = BattleRuntimeEncounterFlowFailure.EnchantRegistrationFailed;
                return false;
            }

            if (!BattleRuntimeSessionService.TryBegin(
                    bootstrap.Session,
                    selectedStartingHandRedrawIds ?? Array.Empty<string>(),
                    out _,
                    out sessionFailure,
                    out redrawFailure,
                    out turnFailure))
            {
                failure =
                    BattleRuntimeEncounterFlowFailure.SessionBeginFailed;
                return false;
            }

            BattleRunChanges runChanges = new();
            BattleEffectQueue pendingSettlementEffects = new();
            BattleSettlementService settlement = new(
                bootstrap.Runtime.Player,
                new BattleOutcomeEvaluator(
                    bootstrap.Runtime.Player,
                    bootstrap.Runtime.LivingEnemies),
                pendingSettlementEffects,
                runState,
                runChanges);
            BattleVictoryRewardService victoryRewards = new(
                settlement,
                runState,
                encounter.EncounterGrade,
                rewardSeed);
            context = new BattleRuntimeEncounterContext(
                bootstrap,
                runState,
                runChanges,
                pendingSettlementEffects,
                settlement,
                victoryRewards,
                deckSnapshot);
            bootstrapFailure = BattleRuntimeBootstrapFailure.None;
            sessionFailure = BattleRuntimeSessionFailure.None;
            runDeckFailure = RunDeckFailure.None;
            failure = BattleRuntimeEncounterFlowFailure.None;
            return true;
        }

        public static bool TrySettle(
            BattleRuntimeEncounterContext context,
            out BattleRuntimeEncounterFlowFailure failure,
            out BattleRuntimeSessionFailure sessionFailure,
            out BattleSettlementFailure settlementFailure)
        {
            sessionFailure = BattleRuntimeSessionFailure.None;
            settlementFailure = BattleSettlementFailure.None;
            if (context?.Session == null || context.Settlement == null)
            {
                failure = BattleRuntimeEncounterFlowFailure.InvalidContext;
                return false;
            }

            if (!context.Session.IsFinished &&
                !BattleRuntimeSessionService.TryFinalizeTerminalOutcome(
                    context.Session,
                    out _,
                    out sessionFailure))
            {
                if (sessionFailure ==
                    BattleRuntimeSessionFailure.BattleOngoing)
                {
                    settlementFailure =
                        BattleSettlementFailure.BattleOngoing;
                }

                failure =
                    BattleRuntimeEncounterFlowFailure.SessionNotFinished;
                return false;
            }

            if (!context.Settlement.TrySettle(out settlementFailure))
            {
                failure =
                    BattleRuntimeEncounterFlowFailure.SettlementFailed;
                return false;
            }

            sessionFailure = BattleRuntimeSessionFailure.None;
            failure = BattleRuntimeEncounterFlowFailure.None;
            return true;
        }
    }
}
