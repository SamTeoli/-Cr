using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleManaState
    {
        public const int DefaultMaximumMana = 5;
        public const int MaximumManaLimit = 10;

        [SerializeField] private int maximumMana;
        [SerializeField] private int currentMana;

        private BattleManaState()
        {
        }

        public BattleManaState(int maximumMana = DefaultMaximumMana)
        {
            this.maximumMana = Mathf.Clamp(maximumMana, 0, MaximumManaLimit);
            currentMana = this.maximumMana;
        }

        public int MaximumMana => maximumMana;
        public int CurrentMana => currentMana;

        public void StartPlayerTurn()
        {
            currentMana = maximumMana;
        }

        public void EndPlayerTurn()
        {
            currentMana = 0;
        }

        public bool CanSpend(int amount)
        {
            return amount >= 0 && currentMana >= amount;
        }

        public bool TrySpend(int amount)
        {
            if (!CanSpend(amount))
            {
                return false;
            }

            currentMana -= amount;
            return true;
        }

        internal void Refund(int amount)
        {
            currentMana = Mathf.Clamp(currentMana + Mathf.Max(0, amount), 0, maximumMana);
        }
    }
}
