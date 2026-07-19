using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleEnemyVitalState
    {
        [SerializeField] private string enemyId;
        [SerializeField] private int currentHealth;

        public BattleEnemyVitalState(string enemyId, int health)
        {
            if (string.IsNullOrWhiteSpace(enemyId))
            {
                throw new ArgumentException("Enemy ID is required.", nameof(enemyId));
            }

            this.enemyId = enemyId.Trim();
            currentHealth = Mathf.Max(0, health);
        }

        public string EnemyId => enemyId;
        public int CurrentHealth => currentHealth;

        public int ApplyDamage(int amount)
        {
            int applied = Mathf.Min(Mathf.Max(0, amount), currentHealth);
            currentHealth -= applied;
            return applied;
        }
    }
}
