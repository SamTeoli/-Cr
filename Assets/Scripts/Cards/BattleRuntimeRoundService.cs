using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public static class BattleRuntimeRoundService
    {
        public static bool TryResolve(
            BattleRuntimeState runtime,
            int firstPlayerTurnEventIndex,
            IEnumerable<BattleRuntimeEnemyTurnCommand> enemyCommands,
            out BattleRuntimeRoundResult result,
            out BattleRuntimeRoundFailure failure,
            out BattleTurnFailure playerTurnEndFailure,
            out BattleRuntimeEnemyTurnPipelineFailure pipelineFailure,
            out BattleRuntimeEnemyTurnPlanFailure planFailure,
            out BattleRuntimeEnemyTurnFailure enemyTurnFailure,
            out int failedActionIndex)
        {
            result = null;
            failure = BattleRuntimeRoundFailure.None;
            playerTurnEndFailure = BattleTurnFailure.None;
            pipelineFailure = BattleRuntimeEnemyTurnPipelineFailure.None;
            planFailure = BattleRuntimeEnemyTurnPlanFailure.None;
            enemyTurnFailure = BattleRuntimeEnemyTurnFailure.None;
            failedActionIndex = -1;

            if (runtime == null || runtime.Player == null ||
                runtime.LivingEnemies == null)
            {
                failure = BattleRuntimeRoundFailure.InvalidRuntime;
                return false;
            }

            if (runtime.Turn.Phase != BattleTurnPhase.PlayerAction)
            {
                failure = BattleRuntimeRoundFailure.InvalidTurnPhase;
                return false;
            }

            BattleOutcome initialOutcome = new BattleOutcomeEvaluator(
                runtime.Player,
                runtime.LivingEnemies).Evaluate();
            if (initialOutcome != BattleOutcome.Ongoing)
            {
                failure = BattleRuntimeRoundFailure.BattleAlreadyFinished;
                return false;
            }

            if (!BattleRuntimeTurnEffectService.TryEndPlayerTurn(
                    runtime,
                    firstPlayerTurnEventIndex,
                    out BattleRuntimeTurnEffectResult playerTurnEndEffects,
                    out playerTurnEndFailure))
            {
                failure = BattleRuntimeRoundFailure.PlayerTurnEndFailed;
                return false;
            }

            if (!BattleRuntimeEnemyTurnPipelineService.TryResolve(
                    runtime,
                    enemyCommands,
                    out BattleRuntimeEnemyTurnPipelineResult enemyTurnPipeline,
                    out pipelineFailure,
                    out planFailure,
                    out enemyTurnFailure,
                    out failedActionIndex))
            {
                failure = BattleRuntimeRoundFailure.EnemyTurnPipelineFailed;
                return false;
            }

            result = new BattleRuntimeRoundResult(
                playerTurnEndEffects,
                enemyTurnPipeline);
            failure = BattleRuntimeRoundFailure.None;
            return true;
        }
    }
}
