namespace HaveABreak.Cards
{
    public sealed class BattleRuntimeRoundResult
    {
        internal BattleRuntimeRoundResult(
            BattleRuntimeTurnEffectResult playerTurnEndEffects,
            BattleRuntimeEnemyTurnPipelineResult enemyTurnPipeline)
        {
            PlayerTurnEndEffects = playerTurnEndEffects;
            EnemyTurnPipeline = enemyTurnPipeline;
        }

        public BattleRuntimeTurnEffectResult PlayerTurnEndEffects { get; }
        public BattleRuntimeEnemyTurnPipelineResult EnemyTurnPipeline { get; }
        public BattleOutcome Outcome =>
            EnemyTurnPipeline.TurnResult.Outcome;
        public bool PlayerTurnStarted =>
            EnemyTurnPipeline.TurnResult.PlayerTurnStarted;
        public int ProcessedEnemyActionCount =>
            EnemyTurnPipeline.TurnResult.ProcessedActionCount;
    }
}
