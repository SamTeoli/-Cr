using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleEnemyStatusState
    {
        [SerializeField] private string enemyId;
        [SerializeField] private int weaken;

        public BattleEnemyStatusState(string enemyId)
        {
            if (string.IsNullOrWhiteSpace(enemyId))
            {
                throw new ArgumentException("Enemy ID is required.", nameof(enemyId));
            }

            this.enemyId = enemyId.Trim();
        }

        public string EnemyId => enemyId;
        public int Weaken => weaken;

        public int ApplyWeaken(int amount)
        {
            int applied = Mathf.Max(0, amount);
            weaken += applied;
            return applied;
        }
    }
}
