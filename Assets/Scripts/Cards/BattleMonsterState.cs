using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleMonsterState
    {
        [SerializeField] private BattleCardInstance card;
        [SerializeField] private int attack;
        [SerializeField] private int maximumHealth;
        [SerializeField] private int currentHealth;

        public BattleMonsterState(BattleCardInstance card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            if (card.SourceCard.CardType != CardType.Monster)
            {
                throw new ArgumentException("Combat stats require a monster card.", nameof(card));
            }

            this.card = card;
            ResolvedCardData resolved = card.Resolved;
            attack = Mathf.Max(0, resolved.Attack);
            maximumHealth = Mathf.Max(1, resolved.Health);
            currentHealth = maximumHealth;
        }

        public BattleCardInstance Card => card;
        public string BattleCardId => card.Ids.BattleCardId;
        public int Attack => attack;
        public int MaximumHealth => maximumHealth;
        public int CurrentHealth => currentHealth;
        public bool IsDestructionCandidate => currentHealth <= 0;

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
    }
}
