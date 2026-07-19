using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleEnemyRuntimeState
    {
        [SerializeField] private string enemyId;
        [SerializeField] private int attack;
        [SerializeField] private BattleEnemyVitalState vital;

        public BattleEnemyRuntimeState(string enemyId, int attack, int health)
        {
            if (string.IsNullOrWhiteSpace(enemyId))
            {
                throw new ArgumentException("Enemy ID is required.", nameof(enemyId));
            }

            this.enemyId = enemyId.Trim();
            this.attack = Mathf.Max(0, attack);
            vital = new BattleEnemyVitalState(this.enemyId, health);
        }

        public string EnemyId => enemyId;
        public int Attack => attack;
        public BattleEnemyVitalState Vital => vital;
        public bool IsAlive => vital.CurrentHealth > 0;

        public BattleEnemyAttackSnapshot SnapshotAttack()
        {
            return new BattleEnemyAttackSnapshot(enemyId, attack);
        }
    }
}
