using System;
using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public static class EnchantRustyAnnouncementResolver
    {
        public static bool TryResolve(
            BattleEventRecord completedMainEffect,
            BattleEffectImpactRecord impact,
            BattleCardEnchantRegistry enchants,
            BattleEnemyStatusRegistry enemyStatuses,
            BattleEventLog eventLog,
            out List<BattleEventRecord> weakenEvents)
        {
            weakenEvents = new List<BattleEventRecord>();
            if (completedMainEffect == null || impact == null || enchants == null ||
                enemyStatuses == null || eventLog == null ||
                completedMainEffect.EventType != BattleEventType.MainEffectCompleted ||
                eventLog.Find(completedMainEffect.EventId) != completedMainEffect ||
                !string.Equals(
                    impact.CompletedEventId,
                    completedMainEffect.EventId,
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    impact.SourceBattleCardId,
                    completedMainEffect.ActorId,
                    StringComparison.OrdinalIgnoreCase) ||
                impact.AffectedEnemyIds.Count == 0 ||
                !HasActiveRustyAnnouncement(enchants.Find(impact.SourceBattleCardId)) ||
                WasAlreadyResolved(completedMainEffect.EventId, eventLog))
            {
                return false;
            }

            foreach (string enemyId in impact.AffectedEnemyIds)
            {
                BattleEnemyStatusState enemy = enemyStatuses.Find(enemyId);
                if (enemy == null)
                {
                    continue;
                }

                int beforeWeaken = enemy.Weaken;
                enemy.ApplyWeaken(1);
                weakenEvents.Add(eventLog.Record(
                    BattleEventType.StatusApplied,
                    "E05RustyAnnouncement",
                    impact.SourceBattleCardId,
                    impact.SourceBattleCardId,
                    enemy.EnemyId,
                    parentEventId: completedMainEffect.EventId,
                    sourceEffectId: "E05",
                    beforeValue: beforeWeaken,
                    afterValue: enemy.Weaken));
            }

            return weakenEvents.Count > 0;
        }

        private static bool WasAlreadyResolved(string completedEventId, BattleEventLog eventLog)
        {
            foreach (BattleEventRecord record in eventLog.Events)
            {
                if (record != null && record.SourceEffectId == "E05" && string.Equals(
                        record.ParentEventId,
                        completedEventId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasActiveRustyAnnouncement(RunCardEnchantState enchants)
        {
            if (enchants == null)
            {
                return false;
            }

            foreach (RunEnchantSlot slot in enchants.Slots)
            {
                if (!slot.IsEmpty && slot.Active && string.Equals(
                        slot.Enchant.DefinitionId,
                        "E05",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
