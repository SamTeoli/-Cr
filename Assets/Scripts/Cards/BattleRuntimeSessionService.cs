using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public static class BattleRuntimeSessionService
    {
        public static bool TryBegin(
            BattleRuntimeSessionState session,
            IEnumerable<string> selectedStartingHandRedrawIds,
            out List<BattleCardInstance> replacements,
            out BattleRuntimeSessionFailure failure,
            out StartingHandRedrawFailure redrawFailure,
            out BattleTurnFailure turnFailure)
        {
            replacements = new List<BattleCardInstance>();
            redrawFailure = StartingHandRedrawFailure.NotAvailable;
            turnFailure = BattleTurnFailure.None;
            if (session?.Runtime == null)
            {
                failure = BattleRuntimeSessionFailure.InvalidSession;
                return false;
            }

            if (session.Started)
            {
                failure = BattleRuntimeSessionFailure.AlreadyStarted;
                return false;
            }

            if (!session.Runtime.Turn.TryBeginBattle(out turnFailure))
            {
                failure = BattleRuntimeSessionFailure.BeginBattleFailed;
                return false;
            }

            if (!session.Runtime.Turn.TryConfirmStartingHand(
                    selectedStartingHandRedrawIds,
                    out replacements,
                    out redrawFailure,
                    out turnFailure))
            {
                failure = BattleRuntimeSessionFailure
                    .StartingHandConfirmationFailed;
                return false;
            }

            BattleOutcome initialOutcome = new BattleOutcomeEvaluator(
                session.Runtime.Player,
                session.Runtime.LivingEnemies).Evaluate();
            session.MarkStarted(
                session.Runtime.EventLog.Events.Count,
                initialOutcome);
            failure = BattleRuntimeSessionFailure.None;
            return true;
        }

        public static bool TryResolveRound(
            BattleRuntimeSessionState session,
            IEnumerable<BattleRuntimeEnemyTurnCommand> enemyCommands,
            out BattleRuntimeSessionRoundResult result,
            out BattleRuntimeSessionFailure failure,
            out BattleRuntimeRoundFailure roundFailure,
            out BattleTurnFailure playerTurnEndFailure,
            out BattleRuntimeEnemyTurnPipelineFailure pipelineFailure,
            out BattleRuntimeEnemyTurnPlanFailure planFailure,
            out BattleRuntimeEnemyTurnFailure enemyTurnFailure,
            out int failedActionIndex)
        {
            result = null;
            roundFailure = BattleRuntimeRoundFailure.None;
            playerTurnEndFailure = BattleTurnFailure.None;
            pipelineFailure = BattleRuntimeEnemyTurnPipelineFailure.None;
            planFailure = BattleRuntimeEnemyTurnPlanFailure.None;
            enemyTurnFailure = BattleRuntimeEnemyTurnFailure.None;
            failedActionIndex = -1;

            if (session?.Runtime == null)
            {
                failure = BattleRuntimeSessionFailure.InvalidSession;
                return false;
            }

            if (!session.Started)
            {
                failure = BattleRuntimeSessionFailure.NotStarted;
                return false;
            }

            if (session.IsFinished)
            {
                failure = BattleRuntimeSessionFailure.BattleFinished;
                return false;
            }

            if (!BattleRuntimeRoundService.TryResolve(
                    session.Runtime,
                    session.PlayerTurnEventStartIndex,
                    enemyCommands,
                    out BattleRuntimeRoundResult round,
                    out roundFailure,
                    out playerTurnEndFailure,
                    out pipelineFailure,
                    out planFailure,
                    out enemyTurnFailure,
                    out failedActionIndex))
            {
                failure = BattleRuntimeSessionFailure.RoundResolutionFailed;
                return false;
            }

            session.MarkRoundCompleted(round);
            result = new BattleRuntimeSessionRoundResult(round, session);
            failure = BattleRuntimeSessionFailure.None;
            return true;
        }

        public static bool TryFinalizeTerminalOutcome(
            BattleRuntimeSessionState session,
            out BattleOutcome outcome,
            out BattleRuntimeSessionFailure failure)
        {
            outcome = BattleOutcome.Ongoing;
            if (session?.Runtime == null)
            {
                failure = BattleRuntimeSessionFailure.InvalidSession;
                return false;
            }

            if (!session.Started)
            {
                failure = BattleRuntimeSessionFailure.NotStarted;
                return false;
            }

            if (session.IsFinished)
            {
                outcome = session.Outcome;
                failure = BattleRuntimeSessionFailure.BattleFinished;
                return false;
            }

            outcome = new BattleOutcomeEvaluator(
                session.Runtime.Player,
                session.Runtime.LivingEnemies).Evaluate();
            if (outcome == BattleOutcome.Ongoing)
            {
                failure = BattleRuntimeSessionFailure.BattleOngoing;
                return false;
            }

            if (!session.TryMarkTerminalOutcome(outcome))
            {
                failure = BattleRuntimeSessionFailure.InvalidSession;
                return false;
            }

            failure = BattleRuntimeSessionFailure.None;
            return true;
        }
    }
}
