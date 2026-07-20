using System;
using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public static class BattleRuntimeEnemyTurnPlanService
    {
        public static bool TryCreate(
            IEnumerable<BattleRuntimeEnemyTurnCommand> commands,
            out BattleRuntimeEnemyTurnPlan plan,
            out BattleRuntimeEnemyTurnPlanFailure failure,
            out int failedActionIndex)
        {
            plan = null;
            failedActionIndex = -1;
            if (commands == null)
            {
                failure = BattleRuntimeEnemyTurnPlanFailure.InvalidCommands;
                return false;
            }

            List<BattleRuntimeEnemyTurnCommand> commandSnapshot =
                new(commands);
            List<BattleRuntimeEnemyTurnIntent> intents = new();
            for (int i = 0; i < commandSnapshot.Count; i++)
            {
                BattleRuntimeEnemyTurnCommand command = commandSnapshot[i];
                if (!TryValidate(command, out failure))
                {
                    failedActionIndex = i;
                    return false;
                }

                intents.Add(new BattleRuntimeEnemyTurnIntent(command));
            }

            plan = new BattleRuntimeEnemyTurnPlan(commandSnapshot, intents);
            failure = BattleRuntimeEnemyTurnPlanFailure.None;
            return true;
        }

        public static bool TryResolve(
            BattleRuntimeState runtime,
            BattleRuntimeEnemyTurnPlan plan,
            out BattleRuntimeEnemyTurnResult result,
            out BattleRuntimeEnemyTurnFailure failure,
            out int failedActionIndex)
        {
            if (plan == null)
            {
                result = null;
                failure = BattleRuntimeEnemyTurnFailure.InvalidCommands;
                failedActionIndex = -1;
                return false;
            }

            return BattleRuntimeEnemyTurnService.TryResolve(
                runtime,
                plan.Commands,
                out result,
                out failure,
                out failedActionIndex);
        }

        private static bool TryValidate(
            BattleRuntimeEnemyTurnCommand command,
            out BattleRuntimeEnemyTurnPlanFailure failure)
        {
            if (command == null)
            {
                failure = BattleRuntimeEnemyTurnPlanFailure.InvalidAction;
                return false;
            }

            if (string.IsNullOrWhiteSpace(command.EnemyId))
            {
                failure = BattleRuntimeEnemyTurnPlanFailure.InvalidEnemyId;
                return false;
            }

            switch (command.ActionType)
            {
                case BattleRuntimeEnemyTurnActionType.Move:
                    if (command.MoveSteps <= 0 ||
                        (command.MoveDirection != EnemyMoveDirection.Left &&
                         command.MoveDirection != EnemyMoveDirection.Right))
                    {
                        failure =
                            BattleRuntimeEnemyTurnPlanFailure.InvalidMovement;
                        return false;
                    }
                    break;

                case BattleRuntimeEnemyTurnActionType.Attack:
                    if (string.IsNullOrWhiteSpace(
                            command.TargetBattleCardId))
                    {
                        failure = BattleRuntimeEnemyTurnPlanFailure
                            .InvalidAttackTarget;
                        return false;
                    }
                    break;

                case BattleRuntimeEnemyTurnActionType.Ability:
                    if (string.IsNullOrWhiteSpace(command.Ability.AbilityId) ||
                        string.IsNullOrWhiteSpace(
                            command.Ability.SourceEnemyId) ||
                        !string.Equals(
                            command.EnemyId,
                            command.Ability.SourceEnemyId,
                            StringComparison.Ordinal) ||
                        command.Ability.IsNormalAttack)
                    {
                        failure =
                            BattleRuntimeEnemyTurnPlanFailure.InvalidAbility;
                        return false;
                    }
                    break;

                default:
                    failure =
                        BattleRuntimeEnemyTurnPlanFailure.InvalidAction;
                    return false;
            }

            failure = BattleRuntimeEnemyTurnPlanFailure.None;
            return true;
        }
    }
}
