namespace HaveABreak.Cards
{
    public sealed class BattleRuntimeRoundResult
    {
        internal BattleRuntimeRoundResult(
            BattleRuntimeTurnEffectResult playerTurnEndEffects,
            BattleRuntimeEnemyTurnPipelineResult enemyTurnPipeline,
            BattleOutcome outcome = BattleOutcome.Ongoing)
        {
            PlayerTurnEndEffects = playerTurnEndEffects;
            EnemyTurnPipeline = enemyTurnPipeline;
            Outcome = enemyTurnPipeline != null
                ? enemyTurnPipeline.TurnResult.Outcome
                : outcome;
        }

        public BattleRuntimeTurnEffectResult PlayerTurnEndEffects { get; }
        public BattleRuntimeEnemyTurnPipelineResult EnemyTurnPipeline { get; }
        public BattleOutcome Outcome { get; }
        public bool PlayerTurnStarted =>
            EnemyTurnPipeline?.TurnResult?.PlayerTurnStarted ?? false;
        public int ProcessedEnemyActionCount =>
            EnemyTurnPipeline?.TurnResult?.ProcessedActionCount ?? 0;
    }
}
