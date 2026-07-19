using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleNextSkillModifierState
    {
        [Serializable]
        private sealed class PendingModifier
        {
            public string SourceBattleCardId;
            public int CostReduction;
            public int FirstNumericEffectBonus;
        }

        [Serializable]
        private sealed class ConfirmedBonus
        {
            public string SkillBattleCardId;
            public int Value;
        }

        [SerializeField] private List<PendingModifier> pending = new();
        [SerializeField] private List<ConfirmedBonus> confirmedBonuses = new();

        public int PendingCount => pending.Count;

        public void Add(string sourceBattleCardId, int costReduction, int firstNumericEffectBonus)
        {
            pending.Add(new PendingModifier
            {
                SourceBattleCardId = sourceBattleCardId,
                CostReduction = Mathf.Max(0, costReduction),
                FirstNumericEffectBonus = Mathf.Max(0, firstNumericEffectBonus)
            });
        }

        public int ResolveManaCost(BattleCardInstance card, int currentCost)
        {
            if (card == null || card.SourceCard.CardType != CardType.Skill)
            {
                return Mathf.Max(0, currentCost);
            }

            int reduction = 0;
            for (int i = 0; i < pending.Count; i++)
            {
                reduction += pending[i].CostReduction;
            }

            return Mathf.Max(0, currentCost - reduction);
        }

        public bool TryConsumeOnConfirmedSkill(BattleCardInstance card, out int firstNumericEffectBonus)
        {
            firstNumericEffectBonus = 0;
            if (card == null || card.SourceCard.CardType != CardType.Skill || pending.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < pending.Count; i++)
            {
                firstNumericEffectBonus += pending[i].FirstNumericEffectBonus;
            }

            pending.Clear();
            if (firstNumericEffectBonus > 0)
            {
                confirmedBonuses.Add(new ConfirmedBonus
                {
                    SkillBattleCardId = card.Ids.BattleCardId,
                    Value = firstNumericEffectBonus
                });
            }

            return true;
        }

        public bool TryTakeFirstNumericEffectBonus(string skillBattleCardId, out int bonus)
        {
            bonus = 0;
            int index = confirmedBonuses.FindIndex(item => item != null && string.Equals(
                item.SkillBattleCardId, skillBattleCardId, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                return false;
            }

            bonus = confirmedBonuses[index].Value;
            confirmedBonuses.RemoveAt(index);
            return true;
        }

        public void EndPlayerTurn()
        {
            pending.Clear();
        }
    }
}
