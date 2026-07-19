using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleEnemyTracker
    {
        [SerializeField] private List<string> livingEnemyIds = new();

        public IReadOnlyList<string> LivingEnemyIds => livingEnemyIds;
        public int Count => livingEnemyIds.Count;

        public bool TryAdd(string enemyId)
        {
            if (string.IsNullOrWhiteSpace(enemyId) || Contains(enemyId))
            {
                return false;
            }

            livingEnemyIds.Add(enemyId.Trim());
            return true;
        }

        public bool TryRemove(string enemyId)
        {
            int index = livingEnemyIds.FindIndex(id =>
                string.Equals(id, enemyId, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                return false;
            }

            livingEnemyIds.RemoveAt(index);
            return true;
        }

        public bool Contains(string enemyId)
        {
            return !string.IsNullOrWhiteSpace(enemyId) && livingEnemyIds.Exists(id =>
                string.Equals(id, enemyId, StringComparison.OrdinalIgnoreCase));
        }
    }
}
