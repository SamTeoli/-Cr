using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public sealed class BattleRuntimePlayerTurnStartEffectResult
    {
        internal BattleRuntimePlayerTurnStartEffectResult(
            int resolvedC11Count,
            int drawnCount,
            List<string> defendedMonsterIds)
        {
            ResolvedC11Count = resolvedC11Count;
            DrawnCount = drawnCount;
            DefendedMonsterIds = defendedMonsterIds.AsReadOnly();
        }

        public int ResolvedC11Count { get; }
        public int DrawnCount { get; }
        public IReadOnlyList<string> DefendedMonsterIds { get; }
    }
}
