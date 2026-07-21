using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public static class BattleRuntimeEnemyTurnService
    {
        public static bool TryResolve(
            BattleRuntimeState runtime,
            IEnumerable<BattleRuntimeEnemyTurnCommand> commands,
            out BattleRuntimeEnemyTurnResult result,
            out BattleRuntimeEnemyTurnFailure failure,
            out int failedActionIndex)
        {
            result = null;
            failedActionIndex = -1;
            if (runtime == null)
            {
                failure = BattleRuntimeEnemyTurnFailure.InvalidRuntime;
                return false;
            }

            if (runtime.Turn.Phase != BattleTurnPhase.EnemyTurn)
            {
                failure = BattleRuntimeEnemyTurnFailure.InvalidTurnPhase;
                return false;
            }

            if (commands == null)
            {
                failure = BattleRuntimeEnemyTurnFailure.InvalidCommands;
                return false;
            }

            List<BattleRuntimeEnemyTurnCommand> commandList = new(commands);
            List<BattleRuntimeEnemyTurnActionResult> actionResults = new();
            PruneDefeatedEnemies(runtime);
            BattleOutcome outcome = EvaluateOutcome(runtime);

            for (int i = 0; i < commandList.Count; i++)
            {
                if (outcome != BattleOutcome.Ongoing)
                {
                    break;
                }

                BattleRuntimeEnemyTurnCommand command = commandList[i];
                if (command == null)
                {
                    failure = BattleRuntimeEnemyTurnFailure.InvalidAction;
                    failedActionIndex = i;
                    return false;
                }

                if (TryGetBlockingStatus(
                        runtime,
                        command,
                        out StatusKeyword blockedByStatus,
                        out BattleEnemyStatusState blockingState))
                {
                    BattleEventRecord blockedEvent = runtime.EventLog.Record(
                        BattleEventType.EnemyActionBlocked,
                        blockedByStatus == StatusKeyword.Stun
                            ? "EnemyActionBlockedByStun"
                            : "EnemyAttackBlockedByBind",
                        command.EnemyId,
                        command.EnemyId,
                        command.EnemyId,
                        beforeValue: blockedByStatus == StatusKeyword.Stun
                            ? blockingState.Stun
                            : blockingState.Bind,
                        afterValue: blockedByStatus == StatusKeyword.Stun
                            ? blockingState.Stun
                            : blockingState.Bind);
                    actionResults.Add(new BattleRuntimeEnemyTurnActionResult(
                        command,
                        null,
                        null,
                        null,
                        null,
                        null,
                        blockedByStatus,
                        blockedEvent));
                    continue;
                }

                BattleRuntimeEnemyTurnActionResult actionResult;
                switch (command.ActionType)
                {
                    case BattleRuntimeEnemyTurnActionType.Move:
                        if (!BattleRuntimeEnemyMoveService.TryResolve(
                                runtime,
                                command.EnemyId,
                                command.MoveDirection,
                                command.MoveSteps,
                                out BattleRuntimeEnemyMoveResult moveResult,
                                out _,
                                out _))
                        {
                            failure = BattleRuntimeEnemyTurnFailure.MoveFailed;
                            failedActionIndex = i;
                            return false;
                        }

                        actionResult = new BattleRuntimeEnemyTurnActionResult(
                            command, moveResult, null, null, null, null);
                        break;

                    case BattleRuntimeEnemyTurnActionType.Attack:
                        if (command.UsesAutomaticTargeting)
                        {
                            if (!BattleRuntimeEnemyRepeatedAttackService.TryResolve(
                                    runtime,
                                    command.EnemyId,
                                    command.AutomaticAttackCount,
                                    command.AttackTieBreakerValues,
                                    out BattleRuntimeEnemyRepeatedAttackResult automaticResult,
                                    out _,
                                    out _))
                            {
                                failure = BattleRuntimeEnemyTurnFailure
                                    .AutomaticAttackFailed;
                                failedActionIndex = i;
                                return false;
                            }

                            actionResult =
                                new BattleRuntimeEnemyTurnActionResult(
                                    command,
                                    null,
                                    null,
                                    null,
                                    automaticResult,
                                    null);
                            break;
                        }

                        if (!BattleRuntimeEnemyAttackService.TryDeclare(
                                runtime,
                                command.EnemyId,
                                command.TargetBattleCardId,
                                out BattleRuntimeEnemyAttackDeclarationResult declaration,
                                out _))
                        {
                            failure =
                                BattleRuntimeEnemyTurnFailure.AttackDeclarationFailed;
                            failedActionIndex = i;
                            return false;
                        }

                        if (!BattleRuntimeEnemyAttackService.TryResolveDamage(
                                runtime,
                                declaration,
                                out BattleRuntimeEnemyAttackResolutionResult attackResult,
                                out _))
                        {
                            failure = BattleRuntimeEnemyTurnFailure.AttackDamageFailed;
                            failedActionIndex = i;
                            return false;
                        }

                        actionResult = new BattleRuntimeEnemyTurnActionResult(
                            command, null, declaration, attackResult, null, null);
                        break;

                    case BattleRuntimeEnemyTurnActionType.Ability:
                        if (!BattleRuntimeEnemyAbilityService.TryResolve(
                                runtime,
                                command.Ability,
                                out BattleRuntimeEnemyAbilityResult abilityResult,
                                out _))
                        {
                            failure = BattleRuntimeEnemyTurnFailure.AbilityFailed;
                            failedActionIndex = i;
                            return false;
                        }

                        actionResult = new BattleRuntimeEnemyTurnActionResult(
                            command, null, null, null, null, abilityResult);
                        break;

                    default:
                        failure = BattleRuntimeEnemyTurnFailure.InvalidAction;
                        failedActionIndex = i;
                        return false;
                }

                actionResults.Add(actionResult);
                PruneDefeatedEnemies(runtime);
                outcome = EvaluateOutcome(runtime);
            }

            BattleRuntimeEnemyStatusTurnResult statusTurnEnd = null;
            if (outcome == BattleOutcome.Ongoing)
            {
                if (!BattleRuntimeEnemyStatusTurnService.TryResolveTurnEnd(
                        runtime,
                        out statusTurnEnd))
                {
                    failure =
                        BattleRuntimeEnemyTurnFailure.StatusTurnEndFailed;
                    return false;
                }

                PruneDefeatedEnemies(runtime);
                outcome = EvaluateOutcome(runtime);
            }

            bool playerTurnStarted = false;
            BattleRuntimePlayerTurnStartEffectResult playerTurnStartEffects = null;
            if (outcome == BattleOutcome.Ongoing)
            {
                if (!BattleRuntimePlayerTurnStartEffectService
                        .TryCompleteEnemyTurnAndResolve(
                            runtime,
                            out playerTurnStartEffects,
                            out _))
                {
                    failure = BattleRuntimeEnemyTurnFailure.PlayerTurnStartFailed;
                    return false;
                }

                playerTurnStarted = true;
            }

            result = new BattleRuntimeEnemyTurnResult(
                actionResults,
                outcome,
                playerTurnStarted,
                playerTurnStartEffects,
                statusTurnEnd);
            failure = BattleRuntimeEnemyTurnFailure.None;
            return true;
        }

        private static bool TryGetBlockingStatus(
            BattleRuntimeState runtime,
            BattleRuntimeEnemyTurnCommand command,
            out StatusKeyword blockedByStatus,
            out BattleEnemyStatusState state)
        {
            blockedByStatus = StatusKeyword.None;
            state = runtime.EnemyStatuses.Find(command.EnemyId);
            if (state == null)
            {
                return false;
            }

            if ((command.ActionType ==
                 BattleRuntimeEnemyTurnActionType.Attack ||
                 command.ActionType ==
                 BattleRuntimeEnemyTurnActionType.Ability) &&
                state.Stun > 0)
            {
                blockedByStatus = StatusKeyword.Stun;
                return true;
            }

            if (command.ActionType ==
                BattleRuntimeEnemyTurnActionType.Attack &&
                state.Bind > 0)
            {
                blockedByStatus = StatusKeyword.Bind;
                return true;
            }

            return false;
        }

        private static BattleOutcome EvaluateOutcome(BattleRuntimeState runtime)
        {
            BattleOutcomeEvaluator evaluator = new(
                runtime.Player, runtime.LivingEnemies);
            return evaluator.Evaluate();
        }

        private static void PruneDefeatedEnemies(BattleRuntimeState runtime)
        {
            foreach (BattleEnemyRuntimeState enemy in runtime.Enemies)
            {
                if (enemy == null || enemy.IsAlive ||
                    !runtime.LivingEnemies.Contains(enemy.EnemyId))
                {
                    continue;
                }

                runtime.LivingEnemies.TryRemove(enemy.EnemyId);
                runtime.EnemyPositions.TryRemove(enemy.EnemyId);
                runtime.EnemyMovementLocks.TryUnlock(enemy.EnemyId);
            }
        }
    }
}
