using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleRunChanges
    {
        [SerializeField] private List<string> consumedItemIds = new();
        [SerializeField] private int goldDelta;

        public IReadOnlyList<string> ConsumedItemIds => consumedItemIds;
        public int GoldDelta => goldDelta;

        public bool RecordConsumedItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            consumedItemIds.Add(itemId.Trim());
            return true;
        }

        public void AddGoldDelta(int amount)
        {
            goldDelta += amount;
        }
    }
}
