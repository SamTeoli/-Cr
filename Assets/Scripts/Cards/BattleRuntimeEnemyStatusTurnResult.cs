using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public sealed class BattleRuntimeEnemyStatusTurnResult
    {
        internal BattleRuntimeEnemyStatusTurnResult(
            List<BattleRuntimeEnemyStatusTurnEntryResult> entries,
            int totalInjuryDamage,
            int defeatedEnemyCount)
        {
            Entries = entries.AsReadOnly();
            TotalInjuryDamage = totalInjuryDamage;
            DefeatedEnemyCount = defeatedEnemyCount;
        }

        public IReadOnlyList<BattleRuntimeEnemyStatusTurnEntryResult> Entries
        {
            get;
        }

        public int TotalInjuryDamage { get; }
        public int DefeatedEnemyCount { get; }
    }
}
