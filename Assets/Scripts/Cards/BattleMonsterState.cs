using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleMonsterState
    {
        [SerializeField] private BattleCardInstance card;
        [SerializeField] private int attack;
        [SerializeField] private int baseMaximumHealth;
        [SerializeField] private int maximumHealth;
        [SerializeField] private int currentHealth;
        [SerializeField] private int counter;
        [SerializeField] private int defense;
        [SerializeField] private int attackEnhancement;
        [SerializeField] private BattleCommonStatusState status;

        public BattleMonsterState(BattleCardInstance card, RunCardEnchantState enchants = null)
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
            baseMaximumHealth = Mathf.Max(1, resolved.Health);
            maximumHealth = baseMaximumHealth;
            currentHealth = maximumHealth;
            status = new BattleCommonStatusState();
            ApplyEnchantState(enchants);
        }

        public BattleCardInstance Card => card;
        public string BattleCardId => card.Ids.BattleCardId;
        public int Attack => attack + attackEnhancement;
        public int BaseAttack => attack;
        public int AttackEnhancement => attackEnhancement;
        public int BaseMaximumHealth => baseMaximumHealth;
        public int MaximumHealth => maximumHealth;
        public int CurrentHealth => currentHealth;
        public int Counter => counter;
        public int Defense => defense;
        public BattleCommonStatusState Status =>
            status ??= new BattleCommonStatusState();
        public bool IsDestructionCandidate => currentHealth <= 0;

        public bool ApplyEnchantState(RunCardEnchantState enchants)
        {
            if (enchants != null && enchants.Card != card.SourceCard)
            {
                return false;
            }

            int resolvedMaximumHealth = baseMaximumHealth;
            if (enchants != null)
            {
                foreach (RunEnchantSlot slot in enchants.SlotsInAttachmentOrder)
                {
                    if (EnchantEffectRegistrationCatalog.TryGetActiveHandler(
                            slot, out IMaximumHealthEnchantEffectHandler handler))
                    {
                        resolvedMaximumHealth = handler.ModifyMaximumHealth(resolvedMaximumHealth);
                    }
                }
            }

            int previousMaximum = maximumHealth;
            maximumHealth = Mathf.Max(1, resolvedMaximumHealth);
            int maximumDelta = maximumHealth - previousMaximum;
            currentHealth = maximumDelta > 0
                ? Mathf.Min(maximumHealth, currentHealth + maximumDelta)
                : Mathf.Min(currentHealth, maximumHealth);
            return true;
        }

        public int ApplyAttackEnhancement(int amount)
        {
            int applied = Mathf.Max(0, amount);
            attackEnhancement += applied;
            return applied;
        }

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

        public int ApplyCounter(int amount)
        {
            int applied = Mathf.Max(0, amount);
            counter += applied;
            return applied;
        }

        public int ApplyDefense(int amount)
        {
            int applied = Mathf.Max(0, amount);
            defense += applied;
            return applied;
        }

        public int ConsumeDefense(int incomingDamage)
        {
            int consumed = Mathf.Min(Mathf.Max(0, incomingDamage), defense);
            defense -= consumed;
            return consumed;
        }
    }
}
