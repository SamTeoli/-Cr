using System;
using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public static class BattleRuntimeEnemyMoveService
    {
        public static bool TryResolve(
            BattleRuntimeState runtime,
            string enemyId,
            EnemyMoveDirection direction,
            int requestedSteps,
            out BattleRuntimeEnemyMoveResult result,
            out BattleRuntimeEnemyMoveFailure failure,
            out EnemyPositionMoveFailure positionFailure)
        {
            result = null;
            positionFailure = EnemyPositionMoveFailure.None;
            if (runtime == null)
            {
                failure = BattleRuntimeEnemyMoveFailure.InvalidRuntime;
                return false;
            }

            if (runtime.Turn.Phase != BattleTurnPhase.EnemyTurn)
            {
                failure = BattleRuntimeEnemyMoveFailure.InvalidTurnPhase;
                return false;
            }

            BattleEnemyRuntimeState enemy = runtime.FindEnemy(enemyId);
            if (enemy == null || !enemy.IsAlive ||
                !runtime.EnemyPositions.FindPosition(enemyId).HasValue)
            {
                failure = BattleRuntimeEnemyMoveFailure.InvalidEnemy;
                return false;
            }

            if (requestedSteps <= 0)
            {
                failure = BattleRuntimeEnemyMoveFailure.InvalidStepCount;
                return false;
            }

            BattleEventRecord attemptEvent = runtime.EventLog.Record(
                BattleEventType.EnemyMoveAttempt,
                "EnemyMoveAttempt",
                enemyId,
                enemyId,
                enemyId,
                beforeValue: 0,
                afterValue: requestedSteps);

            runtime.TrapInstallations.PruneInactive();
            int effectiveSteps = requestedSteps;
            string triggeredTrapBattleCardId = null;
            IReadOnlyList<BattleRuntimeTrapInstallation> installations =
                runtime.TrapInstallations.Installations;
            for (int i = installations.Count - 1; i >= 0; i--)
            {
                BattleRuntimeTrapInstallation installation = installations[i];
                if (installation?.SourceTrap?.SourceCard == null ||
                    !string.Equals(
                        installation.SourceTrap.SourceCard.CatalogCardId,
                        TestContentIds.C08,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (BattleRuntimeTrapEffectService.TryReplaceEnemyMove(
                        runtime,
                        installation,
                        attemptEvent,
                        effectiveSteps,
                        enemyId,
                        out int replacementSteps))
                {
                    effectiveSteps = replacementSteps;
                    triggeredTrapBattleCardId =
                        installation.SourceTrap.Ids.BattleCardId;
                    break;
                }
            }

            List<EnemyPositionMoveRecord> moves = new();
            List<BattleEventRecord> movedEvents = new();
            int resolvedSteps = 0;
            int resolvedC04Count = 0;
            int resolvedC12Count = 0;
            int attackEnhancementGained = 0;
            int vulnerableGained = 0;
            int damageApplied = 0;

            for (int step = 0; step < effectiveSteps; step++)
            {
                if (!BattleEnemyMovementResolver.TryMoveOneStep(
                        runtime.EnemyPositions,
                        runtime.EnemyMovementLocks,
                        enemyId,
                        direction,
                        out List<EnemyPositionMoveRecord> stepMoves,
                        out positionFailure))
                {
                    failure = BattleRuntimeEnemyMoveFailure.PositionMoveFailed;
                    return false;
                }

                resolvedSteps++;
                foreach (EnemyPositionMoveRecord move in stepMoves)
                {
                    moves.Add(move);
                    BattleEventRecord movedEvent = runtime.EventLog.Record(
                        BattleEventType.EnemyMoved,
                        move.Pushed ? "EnemyPushed" : "EnemyMoved",
                        enemyId,
                        move.EnemyId,
                        move.EnemyId,
                        parentEventId: attemptEvent.EventId,
                        beforeValue: (int)move.From,
                        afterValue: (int)move.To);
                    movedEvents.Add(movedEvent);

                    if (!BattleRuntimeMovementReactionService.TryResolve(
                            runtime,
                            movedEvent,
                            out BattleRuntimeMovementReactionResult reaction))
                    {
                        failure =
                            BattleRuntimeEnemyMoveFailure.MovementReactionFailed;
                        return false;
                    }

                    resolvedC04Count += reaction.ResolvedC04Count;
                    resolvedC12Count += reaction.ResolvedC12Count;
                    attackEnhancementGained +=
                        reaction.AttackEnhancementGained;
                    vulnerableGained += reaction.VulnerableGained;
                    damageApplied += reaction.DamageApplied;
                }
            }

            result = new BattleRuntimeEnemyMoveResult(
                attemptEvent,
                requestedSteps,
                resolvedSteps,
                !string.IsNullOrWhiteSpace(triggeredTrapBattleCardId),
                triggeredTrapBattleCardId,
                moves,
                movedEvents,
                resolvedC04Count,
                resolvedC12Count,
                attackEnhancementGained,
                vulnerableGained,
                damageApplied);
            failure = BattleRuntimeEnemyMoveFailure.None;
            return true;
        }
    }
}
