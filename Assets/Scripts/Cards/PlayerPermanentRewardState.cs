using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class PlayerPermanentRewardState
    {
        [SerializeField] private List<string> rewardIds = new();

        public IReadOnlyList<string> RewardIds =>
            rewardIds ??= new List<string>();

        public bool Contains(string rewardId)
        {
            if (string.IsNullOrWhiteSpace(rewardId))
            {
                return false;
            }

            string normalized = rewardId.Trim();
            rewardIds ??= new List<string>();
            return rewardIds.Exists(id => string.Equals(
                id,
                normalized,
                StringComparison.OrdinalIgnoreCase));
        }

        internal bool TryAdd(string rewardId)
        {
            if (string.IsNullOrWhiteSpace(rewardId) || Contains(rewardId))
            {
                return false;
            }

            rewardIds ??= new List<string>();
            rewardIds.Add(rewardId.Trim());
            return true;
        }
    }
}
