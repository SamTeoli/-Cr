using System;

namespace HaveABreak.Cards
{
    public static class BattleAttackEventService
    {
        public static bool TryRecordCompleted(
            BattleEventLog eventLog,
            BattleEventRecord declaredAttack,
            out BattleEventRecord completedAttack)
        {
            completedAttack = null;
            if (eventLog == null || declaredAttack == null ||
                declaredAttack.EventType != BattleEventType.AttackDeclared ||
                eventLog.Find(declaredAttack.EventId) != declaredAttack ||
                string.IsNullOrWhiteSpace(declaredAttack.ActorId) ||
                string.IsNullOrWhiteSpace(declaredAttack.TargetId))
            {
                return false;
            }

            foreach (BattleEventRecord existing in eventLog.Events)
            {
                if (existing != null && existing.EventType == BattleEventType.AttackCompleted &&
                    string.Equals(
                        existing.ParentEventId,
                        declaredAttack.EventId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            completedAttack = eventLog.Record(
                BattleEventType.AttackCompleted,
                "AttackCompleted",
                declaredAttack.SourceId,
                declaredAttack.ActorId,
                declaredAttack.TargetId,
                parentEventId: declaredAttack.EventId);
            return true;
        }
    }
}
