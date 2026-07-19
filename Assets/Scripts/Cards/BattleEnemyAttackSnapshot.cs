using System;

namespace HaveABreak.Cards
{
    [Serializable]
    public readonly struct BattleEnemyAttackSnapshot
    {
        public BattleEnemyAttackSnapshot(string enemyId, int attack)
        {
            EnemyId = enemyId;
            Attack = attack;
        }

        public string EnemyId { get; }
        public int Attack { get; }
    }
}
