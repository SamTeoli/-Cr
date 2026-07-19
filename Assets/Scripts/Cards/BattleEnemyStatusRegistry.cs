using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleEnemyStatusRegistry
    {
        [SerializeField] private List<BattleEnemyStatusState> enemies = new();

        public IReadOnlyList<BattleEnemyStatusState> Enemies => enemies;

        public bool TryAdd(string enemyId, out BattleEnemyStatusState state)
        {
            state = null;
            if (string.IsNullOrWhiteSpace(enemyId) || Find(enemyId) != null)
            {
                return false;
            }

            state = new BattleEnemyStatusState(enemyId);
            enemies.Add(state);
            return true;
        }

        public BattleEnemyStatusState Find(string enemyId)
        {
            if (string.IsNullOrWhiteSpace(enemyId))
            {
                return null;
            }

            return enemies.Find(enemy => enemy != null && string.Equals(
                enemy.EnemyId, enemyId, StringComparison.OrdinalIgnoreCase));
        }
    }
}
