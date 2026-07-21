using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public sealed class BattleRuntimeEnemyTurnResult
    {
        internal BattleRuntimeEnemyTurnResult(
            List<BattleRuntimeEnemyTurnActionResult> actionResults,
            BattleOutcome outcome,
            bool playerTurnStarted,
            BattleRuntimePlayerTurnStartEffectResult playerTurnStartEffects,
            BattleRuntimeEnemyStatusTurnResult statusTurnEnd)
        {
            ActionResults = actionResults.AsReadOnly();
            Outcome = outcome;
            PlayerTurnStarted = playerTurnStarted;
            PlayerTurnStartEffects = playerTurnStartEffects;
            StatusTurnEnd = statusTurnEnd;
        }

        public IReadOnlyList<BattleRuntimeEnemyTurnActionResult> ActionResults { get; }
        public int ProcessedActionCount => ActionResults.Count;
        public BattleOutcome Outcome { get; }
        public bool PlayerTurnStarted { get; }
        public BattleRuntimePlayerTurnStartEffectResult PlayerTurnStartEffects { get; }
        public BattleRuntimeEnemyStatusTurnResult StatusTurnEnd { get; }
    }
}
