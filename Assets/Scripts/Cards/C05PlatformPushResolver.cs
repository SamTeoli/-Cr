using System;
using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public static class C05PlatformPushResolver
    {
        private const string EffectId = "C05-MAIN";

        public static bool TryResolve(
            BattleEventRecord playedEvent,
            BattleCardInstance sourceSkill,
            string fixedTargetEnemyId,
            BattleEnemyPositionState positions,
            BattleEnemyMovementLockState movementLocks,
            BattleEnemyStatusRegistry statuses,
            BattleEventLog eventLog,
            BattleEffectResolutionTracker resolutions,
            out int movedSteps,
            out int weakenGained,
            out int vulnerableGained)
        {
            movedSteps = 0;
            weakenGained = 0;
            vulnerableGained = 0;
            if (playedEvent == null || sourceSkill == null || positions == null ||
                statuses == null || eventLog == null || resolutions == null ||
                playedEvent.EventType != BattleEventType.CardPlayed ||
                eventLog.Find(playedEvent.EventId) != playedEvent ||
                !string.Equals(sourceSkill.SourceCard.CatalogCardId, "C05", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(playedEvent.ActorId, sourceSkill.Ids.BattleCardId, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(fixedTargetEnemyId) ||
                positions.FindPosition(fixedTargetEnemyId) == null ||
                statuses.Find(fixedTargetEnemyId) == null ||
                !resolutions.TryBegin(EffectId, playedEvent.EventId))
            {
                return false;
            }

            int requestedSteps = sourceSkill.CurrentLevel >= 5 ? 2 : 1;
            for (int step = 0; step < requestedSteps; step++)
            {
                if (!BattleEnemyMovementResolver.TryMoveOneStep(
                        positions, movementLocks, fixedTargetEnemyId, EnemyMoveDirection.Right,
                        out List<EnemyPositionMoveRecord> moves, out _))
                {
                    break;
                }

                movedSteps++;
                foreach (EnemyPositionMoveRecord move in moves)
                {
                    eventLog.Record(
                        BattleEventType.EnemyMoved, "C05Move", sourceSkill.Ids.BattleCardId,
                        sourceSkill.Ids.BattleCardId, move.EnemyId,
                        parentEventId: playedEvent.EventId, sourceEffectId: EffectId,
                        beforeValue: (int)move.From, afterValue: (int)move.To);
                }
            }

            BattleEnemyStatusState target = statuses.Find(fixedTargetEnemyId);
            int weakenAmount = sourceSkill.CurrentLevel >= 2 ? 2 : 1;
            int beforeWeaken = target.Weaken;
            weakenGained = target.ApplyWeaken(weakenAmount);
            eventLog.Record(
                BattleEventType.StatusApplied, "C05Weaken", sourceSkill.Ids.BattleCardId,
                sourceSkill.Ids.BattleCardId, target.EnemyId,
                parentEventId: playedEvent.EventId, sourceEffectId: EffectId,
                beforeValue: beforeWeaken, afterValue: target.Weaken);

            if (movedSteps > 0 && sourceSkill.CurrentLevel >= 4)
            {
                int beforeVulnerable = target.Vulnerable;
                vulnerableGained = target.ApplyVulnerable(1);
                eventLog.Record(
                    BattleEventType.StatusApplied, "C05Vulnerable", sourceSkill.Ids.BattleCardId,
                    sourceSkill.Ids.BattleCardId, target.EnemyId,
                    parentEventId: playedEvent.EventId, sourceEffectId: EffectId,
                    beforeValue: beforeVulnerable, afterValue: target.Vulnerable);
            }

            return true;
        }
    }
}
