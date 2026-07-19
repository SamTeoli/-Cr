using System;
using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public static class C01SleeperKeeperResolver
    {
        private const string EffectId = "C01-SUMMON";

        public static bool TryResolve(
            BattleEventRecord summonedEvent,
            BattleMonsterState sourceMonster,
            EnchantFixedTargetDeclaration targetDeclaration,
            BattleEnemyPositionState enemyPositions,
            BattleEnemyMovementLockState movementLocks,
            BattleEventLog eventLog,
            BattleEffectResolutionTracker resolutions,
            out C01SleeperKeeperResult result)
        {
            result = default;
            if (summonedEvent == null || sourceMonster == null || enemyPositions == null ||
                eventLog == null || resolutions == null ||
                summonedEvent.EventType != BattleEventType.MonsterSummoned ||
                eventLog.Find(summonedEvent.EventId) != summonedEvent ||
                !string.Equals(
                    summonedEvent.ActorId,
                    sourceMonster.BattleCardId,
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    sourceMonster.Card.SourceCard.CatalogCardId,
                    "C01",
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    targetDeclaration.SourceBattleCardId,
                    sourceMonster.BattleCardId,
                    StringComparison.OrdinalIgnoreCase) ||
                !resolutions.TryBegin(EffectId, summonedEvent.EventId))
            {
                return false;
            }

            string targetEnemyId = EnchantFixedTargetResolver.Resolve(
                targetDeclaration, enemyPositions);
            if (string.IsNullOrWhiteSpace(targetEnemyId))
            {
                result = new C01SleeperKeeperResult(
                    null,
                    false,
                    EnemyPositionMoveFailure.EnemyNotFound,
                    0,
                    Array.Empty<EnemyPositionMoveRecord>());
                return true;
            }

            bool moved = BattleEnemyMovementResolver.TryMoveOneStep(
                enemyPositions,
                movementLocks,
                targetEnemyId,
                EnemyMoveDirection.Left,
                out List<EnemyPositionMoveRecord> moves,
                out EnemyPositionMoveFailure movementFailure);

            if (moved)
            {
                foreach (EnemyPositionMoveRecord move in moves)
                {
                    eventLog.Record(
                        BattleEventType.EnemyMoved,
                        move.Pushed ? "C01Push" : "C01MoveTarget",
                        sourceMonster.BattleCardId,
                        sourceMonster.BattleCardId,
                        move.EnemyId,
                        parentEventId: summonedEvent.EventId,
                        sourceEffectId: EffectId,
                        beforeValue: (int)move.From,
                        afterValue: (int)move.To);
                }
            }

            int defenseGained = 0;
            if (moved && sourceMonster.Card.CurrentLevel >= 5)
            {
                defenseGained = 1;
            }
            else if (!moved && movementFailure == EnemyPositionMoveFailure.MovementLocked)
            {
                defenseGained = sourceMonster.Card.CurrentLevel >= 4 ? 3 : 2;
            }

            if (defenseGained > 0)
            {
                int beforeDefense = sourceMonster.Defense;
                sourceMonster.ApplyDefense(defenseGained);
                eventLog.Record(
                    BattleEventType.StatusApplied,
                    "C01Defense",
                    sourceMonster.BattleCardId,
                    sourceMonster.BattleCardId,
                    sourceMonster.BattleCardId,
                    parentEventId: summonedEvent.EventId,
                    sourceEffectId: EffectId,
                    beforeValue: beforeDefense,
                    afterValue: sourceMonster.Defense);
            }

            result = new C01SleeperKeeperResult(
                targetEnemyId,
                moved,
                movementFailure,
                defenseGained,
                moves);
            return true;
        }
    }
}
