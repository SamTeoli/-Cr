using System;

namespace HaveABreak.Cards
{
    public static class BattleMainEffectEventService
    {
        public static bool TryRecordCompleted(
            BattleEventLog eventLog,
            BattleEventRecord parentEvent,
            string sourceBattleCardId,
            out BattleEventRecord completedEvent)
        {
            completedEvent = null;
            if (eventLog == null || parentEvent == null ||
                eventLog.Find(parentEvent.EventId) != parentEvent ||
                string.IsNullOrWhiteSpace(sourceBattleCardId))
            {
                return false;
            }

            foreach (BattleEventRecord existing in eventLog.Events)
            {
                if (existing != null && existing.EventType == BattleEventType.MainEffectCompleted &&
                    string.Equals(existing.ParentEventId, parentEvent.EventId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(existing.ActorId, sourceBattleCardId, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            completedEvent = eventLog.Record(
                BattleEventType.MainEffectCompleted,
                "MainEffectCompleted",
                sourceBattleCardId,
                sourceBattleCardId,
                parentEvent.TargetId,
                parentEventId: parentEvent.EventId);
            return true;
        }
    }
}
