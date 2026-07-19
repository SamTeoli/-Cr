using System;

namespace HaveABreak.Cards
{
    public static class C08ClosingDoorResolver
    {
        private const string EffectId = "C08-ENEMY-MOVE";

        public static bool TryReplace(
            BattleEventRecord moveAttemptEvent,
            int currentEnemyTurn,
            int eligibleEnemyTurn,
            int requestedSteps,
            string movingEnemyId,
            BattleCardInstance sourceTrap,
            BattleEnemyMovementLockState movementLocks,
            BattleEnemyStatusRegistry statuses,
            BattleCardTurnTriggerState triggers,
            BattleEventLog eventLog,
            out int replacementSteps)
        {
            replacementSteps = requestedSteps;
            if (moveAttemptEvent == null || sourceTrap == null || statuses == null ||
                triggers == null || eventLog == null || currentEnemyTurn < eligibleEnemyTurn ||
                requestedSteps <= 0 || string.IsNullOrWhiteSpace(movingEnemyId) ||
                eventLog.Find(moveAttemptEvent.EventId) != moveAttemptEvent ||
                sourceTrap.Zone != CardZone.SkillField ||
                !string.Equals(sourceTrap.SourceCard.CatalogCardId, "C08", StringComparison.OrdinalIgnoreCase) ||
                movementLocks != null && movementLocks.IsLocked(movingEnemyId) ||
                statuses.Find(movingEnemyId) == null)
            {
                return false;
            }

            int maximumUses = sourceTrap.CurrentLevel >= 4 ? 2 : 1;
            if (!triggers.TryUse(
                    EffectId, sourceTrap.Ids.BattleCardId, currentEnemyTurn,
                    moveAttemptEvent.EventId, maximumUses))
            {
                return false;
            }

            replacementSteps = 0;
            BattleEnemyStatusState target = statuses.Find(movingEnemyId);
            target.ApplyBind(sourceTrap.CurrentLevel >= 5 ? 2 : 1);
            if (sourceTrap.CurrentLevel >= 2)
            {
                target.ApplyWeaken(1);
            }

            eventLog.Record(
                BattleEventType.StatusApplied, "C08ClosingDoor",
                sourceTrap.Ids.BattleCardId, sourceTrap.Ids.BattleCardId, target.EnemyId,
                parentEventId: moveAttemptEvent.EventId, sourceEffectId: EffectId,
                beforeValue: requestedSteps, afterValue: replacementSteps);
            return true;
        }
    }
}
