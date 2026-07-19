using System;

namespace HaveABreak.Cards
{
    public static class C12RouteMapStarlightResolver
    {
        private const string EffectId = "C12-ENEMY-MOVED";

        public static bool TryResolve(
            BattleEventRecord movedEvent,
            int enemyTurnNumber,
            BattleCardInstance sourceBarrier,
            BattleCardTurnTriggerState triggers,
            BattleEnemyStatusRegistry statuses,
            BattleEnemyVitalState targetVital,
            BattleEventLog eventLog,
            out int vulnerableGained,
            out int damageApplied)
        {
            vulnerableGained = 0;
            damageApplied = 0;
            if (movedEvent == null || sourceBarrier == null || triggers == null ||
                statuses == null || eventLog == null ||
                movedEvent.EventType != BattleEventType.EnemyMoved ||
                eventLog.Find(movedEvent.EventId) != movedEvent ||
                sourceBarrier.Zone != CardZone.SkillField ||
                !string.Equals(sourceBarrier.SourceCard.CatalogCardId, "C12",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            BattleEnemyStatusState target = statuses.Find(movedEvent.TargetId);
            if (target == null)
            {
                return false;
            }

            int maximumUses = sourceBarrier.CurrentLevel >= 4 ? 2 : 1;
            if (!triggers.TryUse(
                    EffectId, sourceBarrier.Ids.BattleCardId, enemyTurnNumber,
                    movedEvent.EventId, maximumUses))
            {
                return false;
            }

            int vulnerableAmount = sourceBarrier.CurrentLevel >= 2 ? 2 : 1;
            int before = target.Vulnerable;
            vulnerableGained = target.ApplyVulnerable(vulnerableAmount);
            eventLog.Record(
                BattleEventType.StatusApplied, "C12Vulnerable",
                sourceBarrier.Ids.BattleCardId, sourceBarrier.Ids.BattleCardId, target.EnemyId,
                parentEventId: movedEvent.EventId, sourceEffectId: EffectId,
                beforeValue: before, afterValue: target.Vulnerable);

            if (sourceBarrier.CurrentLevel >= 5 && targetVital != null &&
                string.Equals(targetVital.EnemyId, target.EnemyId, StringComparison.OrdinalIgnoreCase))
            {
                int beforeHealth = targetVital.CurrentHealth;
                damageApplied = targetVital.ApplyDamage(1);
                eventLog.Record(
                    BattleEventType.DamageApplied, "C12DirectDamage",
                    sourceBarrier.Ids.BattleCardId, sourceBarrier.Ids.BattleCardId, target.EnemyId,
                    parentEventId: movedEvent.EventId, sourceEffectId: EffectId,
                    beforeValue: beforeHealth, afterValue: targetVital.CurrentHealth);
            }

            return true;
        }
    }
}
