using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleEnemyStatusState
    {
        [SerializeField] private string enemyId;
        [SerializeField] private int weaken;
        [SerializeField] private int vulnerable;

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
        public int Vulnerable => vulnerable;

        public int ApplyWeaken(int amount)
        {
            int applied = Mathf.Max(0, amount);
            weaken += applied;
            return applied;
        }

        public int ApplyVulnerable(int amount)
        {
            int applied = Mathf.Max(0, amount);
            vulnerable += applied;
            return applied;
        }
    }
}

