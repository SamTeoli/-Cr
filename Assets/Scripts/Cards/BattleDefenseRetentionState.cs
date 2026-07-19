using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleDefenseRetentionState
    {
        [SerializeField] private List<string> retainUntilPlayerTurnEnd = new();

        public void Mark(string battleCardId)
        {
            if (!string.IsNullOrWhiteSpace(battleCardId) && !IsMarked(battleCardId))
            {
                retainUntilPlayerTurnEnd.Add(battleCardId.Trim());
            }
        }

        public bool IsMarked(string battleCardId)
        {
            return !string.IsNullOrWhiteSpace(battleCardId) &&
                   retainUntilPlayerTurnEnd.Exists(id => string.Equals(
                       id, battleCardId, StringComparison.OrdinalIgnoreCase));
        }

        public void EndPlayerTurn()
        {
            retainUntilPlayerTurnEnd.Clear();
        }
    }
}
