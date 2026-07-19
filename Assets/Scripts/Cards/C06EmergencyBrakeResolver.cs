using System;
using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public static class C06EmergencyBrakeResolver
    {
        private const string EffectId = "C06-MAIN";

        public static bool TryResolve(
            BattleEventRecord playedEvent,
            BattleCardInstance sourceSkill,
            string fixedTargetEnemyId,
            IEnumerable<BattleEnemyAttackSnapshot> livingEnemies,
            BattleEnemyStatusRegistry statuses,
            BattleEventLog eventLog,
            BattleEffectResolutionTracker resolutions,
            out string secondaryEnemyId)
        {
            secondaryEnemyId = null;
            if (playedEvent == null || sourceSkill == null || livingEnemies == null ||
                statuses == null || eventLog == null || resolutions == null ||
                playedEvent.EventType != BattleEventType.CardPlayed ||
                eventLog.Find(playedEvent.EventId) != playedEvent ||
                !string.Equals(sourceSkill.SourceCard.CatalogCardId, "C06", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(playedEvent.ActorId, sourceSkill.Ids.BattleCardId, StringComparison.OrdinalIgnoreCase) ||
                statuses.Find(fixedTargetEnemyId) == null ||
                !resolutions.TryBegin(EffectId, playedEvent.EventId))
            {
                return false;
            }

            BattleEnemyStatusState target = statuses.Find(fixedTargetEnemyId);
            int bindAmount = sourceSkill.CurrentLevel >= 4 ? 2 : 1;
            int beforeBind = target.Bind;
            int bindGained = target.ApplyBind(bindAmount);
            if (bindGained == 0)
            {
                return true;
            }

            eventLog.Record(
                BattleEventType.StatusApplied, "C06Bind", sourceSkill.Ids.BattleCardId,
                sourceSkill.Ids.BattleCardId, target.EnemyId,
                parentEventId: playedEvent.EventId, sourceEffectId: EffectId,
                beforeValue: beforeBind, afterValue: target.Bind);

            if (sourceSkill.CurrentLevel >= 2)
            {
                ApplyWeaken(target, sourceSkill, playedEvent, eventLog);
            }

            if (sourceSkill.CurrentLevel >= 5)
            {
                int highestAttack = int.MinValue;
                foreach (BattleEnemyAttackSnapshot enemy in livingEnemies)
                {
                    if (string.IsNullOrWhiteSpace(enemy.EnemyId) ||
                        string.Equals(enemy.EnemyId, fixedTargetEnemyId, StringComparison.OrdinalIgnoreCase) ||
                        statuses.Find(enemy.EnemyId) == null)
                    {
                        continue;
                    }

                    if (enemy.Attack > highestAttack ||
                        enemy.Attack == highestAttack && string.Compare(
                            enemy.EnemyId, secondaryEnemyId, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        highestAttack = enemy.Attack;
                        secondaryEnemyId = enemy.EnemyId;
                    }
                }

                BattleEnemyStatusState secondary = statuses.Find(secondaryEnemyId);
                if (secondary != null)
                {
                    ApplyWeaken(secondary, sourceSkill, playedEvent, eventLog);
                }
            }

            return true;
        }

        private static void ApplyWeaken(
            BattleEnemyStatusState target, BattleCardInstance source,
            BattleEventRecord parent, BattleEventLog eventLog)
        {
            int before = target.Weaken;
            target.ApplyWeaken(1);
            eventLog.Record(
                BattleEventType.StatusApplied, "C06Weaken", source.Ids.BattleCardId,
                source.Ids.BattleCardId, target.EnemyId,
                parentEventId: parent.EventId, sourceEffectId: EffectId,
                beforeValue: before, afterValue: target.Weaken);
        }
    }
}
