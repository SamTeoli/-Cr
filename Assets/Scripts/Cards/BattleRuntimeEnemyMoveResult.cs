using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public sealed class BattleRuntimeEnemyMoveResult
    {
        internal BattleRuntimeEnemyMoveResult(
            BattleEventRecord moveAttemptEvent,
            int requestedSteps,
            int resolvedSteps,
            bool replacedByTrap,
            string triggeredTrapBattleCardId,
            List<EnemyPositionMoveRecord> moves,
            List<BattleEventRecord> movedEvents,
            int resolvedC04Count,
            int resolvedC12Count,
            int attackEnhancementGained,
            int vulnerableGained,
            int damageApplied)
        {
            MoveAttemptEvent = moveAttemptEvent;
            RequestedSteps = requestedSteps;
            ResolvedSteps = resolvedSteps;
            ReplacedByTrap = replacedByTrap;
            TriggeredTrapBattleCardId = triggeredTrapBattleCardId;
            Moves = moves;
            MovedEvents = movedEvents;
            ResolvedC04Count = resolvedC04Count;
            ResolvedC12Count = resolvedC12Count;
            AttackEnhancementGained = attackEnhancementGained;
            VulnerableGained = vulnerableGained;
            DamageApplied = damageApplied;
        }

        public BattleEventRecord MoveAttemptEvent { get; }
        public int RequestedSteps { get; }
        public int ResolvedSteps { get; }
        public bool ReplacedByTrap { get; }
        public string TriggeredTrapBattleCardId { get; }
        public IReadOnlyList<EnemyPositionMoveRecord> Moves { get; }
        public IReadOnlyList<BattleEventRecord> MovedEvents { get; }
        public int ResolvedC04Count { get; }
        public int ResolvedC12Count { get; }
        public int AttackEnhancementGained { get; }
        public int VulnerableGained { get; }
        public int DamageApplied { get; }
    }
}
