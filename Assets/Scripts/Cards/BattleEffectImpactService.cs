using System;
using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public static class BattleEffectImpactService
    {
        public static bool TryCreate(
            BattleEventRecord completedMainEffect,
            string sourceBattleCardId,
            IEnumerable<string> affectedEnemyIds,
            BattleEnemyTracker livingEnemies,
            out BattleEffectImpactRecord impact)
        {
            impact = null;
            if (completedMainEffect == null ||
                completedMainEffect.EventType != BattleEventType.MainEffectCompleted ||
                string.IsNullOrWhiteSpace(sourceBattleCardId) || affectedEnemyIds == null ||
                livingEnemies == null ||
                !string.Equals(
                    completedMainEffect.ActorId,
                    sourceBattleCardId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            List<string> unique = new();
            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
            foreach (string enemyId in affectedEnemyIds)
            {
                if (string.IsNullOrWhiteSpace(enemyId) || !livingEnemies.Contains(enemyId) ||
                    !seen.Add(enemyId))
                {
                    return false;
                }

                unique.Add(enemyId.Trim());
            }

            impact = new BattleEffectImpactRecord(
                completedMainEffect.EventId,
                sourceBattleCardId.Trim(),
                unique);
            return true;
        }
    }
}
