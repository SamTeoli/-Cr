namespace HaveABreak.Cards
{
    public sealed class BattleRuntimeEnemyTurnPipelineResult
    {
        internal BattleRuntimeEnemyTurnPipelineResult(
            BattleRuntimeEnemyTurnPlan plan,
            BattleRuntimeEnemyTurnResult turnResult)
        {
            Plan = plan;
            TurnResult = turnResult;
        }

        public BattleRuntimeEnemyTurnPlan Plan { get; }
        public BattleRuntimeEnemyTurnResult TurnResult { get; }
    }
}
