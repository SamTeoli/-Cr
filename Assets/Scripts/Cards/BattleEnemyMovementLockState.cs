using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleEnemyMovementLockState
    {
        [SerializeField] private List<string> lockedEnemyIds = new();

        public bool TryLock(string enemyId)
        {
            if (string.IsNullOrWhiteSpace(enemyId) || IsLocked(enemyId))
            {
                return false;
            }

            lockedEnemyIds.Add(enemyId.Trim());
            return true;
        }

        public bool TryUnlock(string enemyId)
        {
            int index = lockedEnemyIds.FindIndex(id => string.Equals(
                id, enemyId, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                return false;
            }

            lockedEnemyIds.RemoveAt(index);
            return true;
        }

        public bool IsLocked(string enemyId)
        {
            return !string.IsNullOrWhiteSpace(enemyId) && lockedEnemyIds.Exists(id =>
                string.Equals(id, enemyId, StringComparison.OrdinalIgnoreCase));
        }
    }
}
