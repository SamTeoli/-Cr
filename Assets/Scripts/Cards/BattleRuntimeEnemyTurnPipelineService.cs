using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public static class BattleRuntimeEnemyTurnPipelineService
    {
        public static bool TryResolve(
            BattleRuntimeState runtime,
            IEnumerable<BattleRuntimeEnemyTurnCommand> commands,
            out BattleRuntimeEnemyTurnPipelineResult result,
            out BattleRuntimeEnemyTurnPipelineFailure pipelineFailure,
            out BattleRuntimeEnemyTurnPlanFailure planFailure,
            out BattleRuntimeEnemyTurnFailure turnFailure,
            out int failedActionIndex)
        {
            result = null;
            pipelineFailure = BattleRuntimeEnemyTurnPipelineFailure.None;
            planFailure = BattleRuntimeEnemyTurnPlanFailure.None;
            turnFailure = BattleRuntimeEnemyTurnFailure.None;
            failedActionIndex = -1;

            if (!BattleRuntimeEnemyTurnOrderService.TryCreateOrderedPlan(
                    runtime,
                    commands,
                    out BattleRuntimeEnemyTurnPlan plan,
                    out planFailure,
                    out failedActionIndex))
            {
                pipelineFailure =
                    BattleRuntimeEnemyTurnPipelineFailure.PlanCreationFailed;
                return false;
            }

            if (!BattleRuntimeEnemyTurnPlanService.TryResolve(
                    runtime,
                    plan,
                    out BattleRuntimeEnemyTurnResult turnResult,
                    out turnFailure,
                    out failedActionIndex))
            {
                pipelineFailure = BattleRuntimeEnemyTurnPipelineFailure
                    .TurnResolutionFailed;
                return false;
            }

            result = new BattleRuntimeEnemyTurnPipelineResult(
                plan, turnResult);
            return true;
        }
    }
}
