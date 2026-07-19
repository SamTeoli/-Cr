using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class RunBattleState
    {
        [SerializeField] private int maximumHealth;
        [SerializeField] private int currentHealth;
        [SerializeField] private int gold;
        [SerializeField] private List<string> consumableItemIds = new();
        [SerializeField] private bool runEnded;

        public RunBattleState(
            int maximumHealth,
            int currentHealth,
            int gold,
            IEnumerable<string> consumableItemIds = null)
        {
            this.maximumHealth = Mathf.Max(1, maximumHealth);
            this.currentHealth = Mathf.Clamp(currentHealth, 0, this.maximumHealth);
            this.gold = Mathf.Max(0, gold);
            if (consumableItemIds == null)
            {
                return;
            }

            foreach (string itemId in consumableItemIds)
            {
                if (!string.IsNullOrWhiteSpace(itemId))
                {
                    this.consumableItemIds.Add(itemId.Trim());
                }
            }
        }

        public int MaximumHealth => maximumHealth;
        public int CurrentHealth => currentHealth;
        public int Gold => gold;
        public IReadOnlyList<string> ConsumableItemIds => consumableItemIds;
        public bool RunEnded => runEnded;

        internal bool CanApplySettlement(
            int finalHealth,
            int goldDelta,
            IReadOnlyList<string> consumedItemIds)
        {
            if (finalHealth < 0 || finalHealth > maximumHealth || gold + goldDelta < 0)
            {
                return false;
            }

            List<string> remaining = new(consumableItemIds);
            foreach (string itemId in consumedItemIds)
            {
                int index = remaining.FindIndex(value =>
                    string.Equals(value, itemId, StringComparison.OrdinalIgnoreCase));
                if (index < 0)
                {
                    return false;
                }

                remaining.RemoveAt(index);
            }

            return true;
        }

        internal void ApplySettlement(
            int finalHealth,
            int goldDelta,
            IReadOnlyList<string> consumedItemIds,
            bool endRun)
        {
            currentHealth = finalHealth;
            gold += goldDelta;
            foreach (string itemId in consumedItemIds)
            {
                int index = consumableItemIds.FindIndex(value =>
                    string.Equals(value, itemId, StringComparison.OrdinalIgnoreCase));
                consumableItemIds.RemoveAt(index);
            }

            runEnded = endRun;
        }
    }
}
