using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public sealed class BattleRuntimeEnemyRepeatedAttackResult
    {
        internal BattleRuntimeEnemyRepeatedAttackResult(
            int requestedAttackCount,
            List<BattleRuntimeEnemyAutoAttackResult> attacks,
            bool stoppedByPlayerDefeat)
        {
            RequestedAttackCount = requestedAttackCount;
            Attacks = attacks;
            StoppedByPlayerDefeat = stoppedByPlayerDefeat;
        }

        public int RequestedAttackCount { get; }
        public IReadOnlyList<BattleRuntimeEnemyAutoAttackResult> Attacks { get; }
        public int ResolvedAttackCount => Attacks.Count;
        public bool StoppedByPlayerDefeat { get; }
    }
}
