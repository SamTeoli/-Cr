using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public sealed class BattleRuntimeFriendlyStatusTurnResult
    {
        internal BattleRuntimeFriendlyStatusTurnResult(
            List<BattleRuntimeFriendlyStatusTurnEntryResult> entries,
            int totalInjuryDamage,
            int defeatedMonsterCount,
            bool playerDefeated)
        {
            Entries = entries.AsReadOnly();
            TotalInjuryDamage = totalInjuryDamage;
            DefeatedMonsterCount = defeatedMonsterCount;
            PlayerDefeated = playerDefeated;
        }

        public IReadOnlyList<BattleRuntimeFriendlyStatusTurnEntryResult>
            Entries { get; }
        public int TotalInjuryDamage { get; }
        public int DefeatedMonsterCount { get; }
        public bool PlayerDefeated { get; }
    }
}
