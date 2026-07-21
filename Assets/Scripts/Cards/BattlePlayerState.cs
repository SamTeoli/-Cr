using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattlePlayerState
    {
        public const string PlayerTargetId = "PLAYER";
        public const int DefaultMaximumHealth = 30;

        [SerializeField] private int maximumHealth;
        [SerializeField] private int currentHealth;
        [SerializeField] private BattleCommonStatusState status;

        private BattlePlayerState()
        {
        }

        public BattlePlayerState(int maximumHealth = DefaultMaximumHealth)
        {
            this.maximumHealth = Mathf.Max(1, maximumHealth);
            currentHealth = this.maximumHealth;
            status = new BattleCommonStatusState();
        }

        public int MaximumHealth => maximumHealth;
        public int CurrentHealth => currentHealth;
        public BattleCommonStatusState Status =>
            status ??= new BattleCommonStatusState();
        public bool IsDefeated => currentHealth <= 0;

        public int ApplyDamage(int amount)
        {
            int applied = Mathf.Min(Mathf.Max(0, amount), currentHealth);
            currentHealth -= applied;
            return applied;
        }

        public int ApplyHealing(int amount)
        {
            int applied = Mathf.Min(Mathf.Max(0, amount), maximumHealth - currentHealth);
            currentHealth += applied;
            return applied;
        }

        public void SetMaximumHealth(int value)
        {
            int nextMaximum = Mathf.Max(1, value);
            if (nextMaximum > maximumHealth)
            {
                currentHealth += nextMaximum - maximumHealth;
            }

            maximumHealth = nextMaximum;
            if (currentHealth > maximumHealth)
            {
                currentHealth = maximumHealth;
            }
        }
    }
}
