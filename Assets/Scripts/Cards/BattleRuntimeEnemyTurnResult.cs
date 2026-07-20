using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public sealed class BattleRuntimeEnemyTurnResult
    {
        internal BattleRuntimeEnemyTurnResult(
            List<BattleRuntimeEnemyTurnActionResult> actionResults,
            BattleOutcome outcome,
            bool playerTurnStarted,
            BattleRuntimePlayerTurnStartEffectResult playerTurnStartEffects)
        {
            ActionResults = actionResults.AsReadOnly();
            Outcome = outcome;
            PlayerTurnStarted = playerTurnStarted;
            PlayerTurnStartEffects = playerTurnStartEffects;
        }

        public IReadOnlyList<BattleRuntimeEnemyTurnActionResult> ActionResults { get; }
        public int ProcessedActionCount => ActionResults.Count;
        public BattleOutcome Outcome { get; }
        public bool PlayerTurnStarted { get; }
        public BattleRuntimePlayerTurnStartEffectResult PlayerTurnStartEffects { get; }
    }
}
