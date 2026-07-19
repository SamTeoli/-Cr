using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleMonsterRegistry
    {
        [SerializeField] private List<BattleMonsterState> monsters = new();

        public IReadOnlyList<BattleMonsterState> Monsters => monsters;

        public bool TryAdd(BattleCardInstance card, out BattleMonsterState state)
        {
            state = null;
            if (card == null || card.SourceCard.CardType != CardType.Monster ||
                Find(card.Ids.BattleCardId) != null)
            {
                return false;
            }

            state = new BattleMonsterState(card);
            monsters.Add(state);
            return true;
        }

        public BattleMonsterState Find(string battleCardId)
        {
            if (string.IsNullOrWhiteSpace(battleCardId))
            {
                return null;
            }

            return monsters.Find(monster => monster != null &&
                string.Equals(monster.BattleCardId, battleCardId, StringComparison.OrdinalIgnoreCase));
        }

        public bool TryRemove(string battleCardId, out BattleMonsterState removed)
        {
            removed = Find(battleCardId);
            return removed != null && monsters.Remove(removed);
        }
    }
}
