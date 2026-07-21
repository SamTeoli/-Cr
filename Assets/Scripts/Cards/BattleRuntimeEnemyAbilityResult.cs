using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public sealed class BattleRuntimeEnemyAbilityResult
    {
        internal BattleRuntimeEnemyAbilityResult(
            EnemyAbilityResolutionContext ability,
            BattleEventRecord declaredEvent,
            BattleEventRecord resolutionEvent,
            bool cancelled,
            bool returnedTrapToHand,
            string triggeredTrapBattleCardId,
            IEnumerable<BattleEventRecord> statusApplicationEvents,
            int totalStatusApplied)
        {
            Ability = ability;
            DeclaredEvent = declaredEvent;
            ResolutionEvent = resolutionEvent;
            Cancelled = cancelled;
            ReturnedTrapToHand = returnedTrapToHand;
            TriggeredTrapBattleCardId = triggeredTrapBattleCardId;
            StatusApplicationEvents = statusApplicationEvents == null
                ? new List<BattleEventRecord>()
                : new List<BattleEventRecord>(statusApplicationEvents);
            TotalStatusApplied = totalStatusApplied;
        }

        public EnemyAbilityResolutionContext Ability { get; }
        public BattleEventRecord DeclaredEvent { get; }
        public BattleEventRecord ResolutionEvent { get; }
        public bool Cancelled { get; }
        public bool ReturnedTrapToHand { get; }
        public string TriggeredTrapBattleCardId { get; }
        public IReadOnlyList<BattleEventRecord> StatusApplicationEvents { get; }
        public int TotalStatusApplied { get; }
        public int AffectedTargetCount => StatusApplicationEvents.Count;
    }
}
