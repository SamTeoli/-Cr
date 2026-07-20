using System;
using System.Collections.Generic;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleRuntimeEnemyTurnOrderValidation
    {
        [MenuItem("Have a Break/Validate Runtime Enemy Turn Ordering")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            if (valid)
            {
                Debug.Log("Battle runtime enemy turn ordering passed.");
            }
            else
            {
                Debug.LogError("Battle runtime enemy turn ordering failed.");
            }

            EditorUtility.DisplayDialog(
                "Battle Runtime Enemy Turn Ordering Validation",
                valid
                    ? "Battle runtime enemy turn ordering passed."
                    : "Battle runtime enemy turn ordering failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            BattleRuntimeState runtime = CreateRuntime();
            return runtime != null &&
                   ValidateOrderedPlan(runtime) &&
                   ValidateRejectedState(runtime);
        }

        private static BattleRuntimeState CreateRuntime()
        {
            BattleRuntimeState runtime = new(
                Array.Empty<BattleCardInstance>(), 374);
            return runtime.TryAddEnemy(
                       "ENEMY-LEFT", 1, 10,
                       EnemyFieldPosition.Left, out _) &&
                   runtime.TryAddEnemy(
                       "ENEMY-CENTER", 1, 10,
                       EnemyFieldPosition.Center, out _) &&
                   runtime.TryAddEnemy(
                       "ENEMY-RIGHT", 1, 10,
                       EnemyFieldPosition.Right, out _)
                ? runtime
                : null;
        }

        private static bool ValidateOrderedPlan(BattleRuntimeState runtime)
        {
            List<BattleRuntimeEnemyTurnCommand> commands = new()
            {
                Ability("ENEMY-RIGHT", "ABILITY-RIGHT"),
                Attack("ENEMY-CENTER", "TARGET-CENTER"),
                Ability("ENEMY-LEFT", "ABILITY-LEFT-B"),
                Move("ENEMY-RIGHT"),
                Attack("ENEMY-LEFT", "TARGET-LEFT"),
                Move("ENEMY-CENTER"),
                Attack("ENEMY-RIGHT", "TARGET-RIGHT"),
                Ability("ENEMY-CENTER", "ABILITY-CENTER"),
                Move("ENEMY-LEFT"),
                Ability("ENEMY-LEFT", "ABILITY-LEFT-A")
            };

            bool created =
                BattleRuntimeEnemyTurnOrderService.TryCreateOrderedPlan(
                    runtime,
                    commands,
                    out BattleRuntimeEnemyTurnPlan plan,
                    out BattleRuntimeEnemyTurnPlanFailure failure,
                    out int failedActionIndex);

            return created &&
                   failure == BattleRuntimeEnemyTurnPlanFailure.None &&
                   failedActionIndex == -1 &&
                   plan != null &&
                   plan.ActionCount == 10 &&
                   Matches(plan, 0, "ENEMY-LEFT",
                       BattleRuntimeEnemyTurnActionType.Move) &&
                   Matches(plan, 1, "ENEMY-LEFT",
                       BattleRuntimeEnemyTurnActionType.Attack) &&
                   MatchesAbility(plan, 2, "ENEMY-LEFT",
                       "ABILITY-LEFT-B") &&
                   MatchesAbility(plan, 3, "ENEMY-LEFT",
                       "ABILITY-LEFT-A") &&
                   Matches(plan, 4, "ENEMY-CENTER",
                       BattleRuntimeEnemyTurnActionType.Move) &&
                   Matches(plan, 5, "ENEMY-CENTER",
                       BattleRuntimeEnemyTurnActionType.Attack) &&
                   MatchesAbility(plan, 6, "ENEMY-CENTER",
                       "ABILITY-CENTER") &&
                   Matches(plan, 7, "ENEMY-RIGHT",
                       BattleRuntimeEnemyTurnActionType.Move) &&
                   Matches(plan, 8, "ENEMY-RIGHT",
                       BattleRuntimeEnemyTurnActionType.Attack) &&
                   MatchesAbility(plan, 9, "ENEMY-RIGHT",
                       "ABILITY-RIGHT") &&
                   runtime.EnemyPositions.FindPosition("ENEMY-LEFT") ==
                   EnemyFieldPosition.Left &&
                   runtime.EnemyPositions.FindPosition("ENEMY-CENTER") ==
                   EnemyFieldPosition.Center &&
                   runtime.EnemyPositions.FindPosition("ENEMY-RIGHT") ==
                   EnemyFieldPosition.Right;
        }

        private static bool ValidateRejectedState(BattleRuntimeState runtime)
        {
            bool invalidRuntime =
                !BattleRuntimeEnemyTurnOrderService.TryCreateOrderedPlan(
                    null,
                    Array.Empty<BattleRuntimeEnemyTurnCommand>(),
                    out _,
                    out BattleRuntimeEnemyTurnPlanFailure runtimeFailure,
                    out int runtimeIndex) &&
                runtimeFailure ==
                BattleRuntimeEnemyTurnPlanFailure.InvalidRuntime &&
                runtimeIndex == -1;

            bool unknownEnemy = Reject(
                runtime,
                Move("ENEMY-UNKNOWN"),
                BattleRuntimeEnemyTurnPlanFailure.EnemyNotFound);
            bool invalidCommand = Reject(
                runtime,
                BattleRuntimeEnemyTurnCommand.CreateMove(
                    "ENEMY-LEFT", EnemyMoveDirection.Right, 0),
                BattleRuntimeEnemyTurnPlanFailure.InvalidMovement);

            BattleEnemyRuntimeState left =
                runtime.FindEnemy("ENEMY-LEFT");
            left?.Vital.ApplyDamage(left.Vital.CurrentHealth);
            bool inactiveEnemy = Reject(
                runtime,
                Attack("ENEMY-LEFT", "TARGET"),
                BattleRuntimeEnemyTurnPlanFailure.EnemyNotActive);

            bool removed =
                runtime.EnemyPositions.TryRemove("ENEMY-CENTER");
            bool missingPosition = removed && Reject(
                runtime,
                Move("ENEMY-CENTER"),
                BattleRuntimeEnemyTurnPlanFailure.EnemyPositionMissing);

            return invalidRuntime && unknownEnemy && invalidCommand &&
                   inactiveEnemy && missingPosition;
        }

        private static bool Reject(
            BattleRuntimeState runtime,
            BattleRuntimeEnemyTurnCommand command,
            BattleRuntimeEnemyTurnPlanFailure expectedFailure)
        {
            return !BattleRuntimeEnemyTurnOrderService.TryCreateOrderedPlan(
                       runtime,
                       new[] { command },
                       out _,
                       out BattleRuntimeEnemyTurnPlanFailure failure,
                       out int failedActionIndex) &&
                   failure == expectedFailure &&
                   failedActionIndex == 0;
        }

        private static bool Matches(
            BattleRuntimeEnemyTurnPlan plan,
            int index,
            string enemyId,
            BattleRuntimeEnemyTurnActionType actionType)
        {
            BattleRuntimeEnemyTurnCommand command = plan.Commands[index];
            BattleRuntimeEnemyTurnIntent intent = plan.Intents[index];
            return command.EnemyId == enemyId &&
                   command.ActionType == actionType &&
                   intent.EnemyId == enemyId &&
                   intent.ActionType == actionType;
        }

        private static bool MatchesAbility(
            BattleRuntimeEnemyTurnPlan plan,
            int index,
            string enemyId,
            string abilityId)
        {
            return Matches(
                       plan,
                       index,
                       enemyId,
                       BattleRuntimeEnemyTurnActionType.Ability) &&
                   plan.Commands[index].Ability.AbilityId == abilityId &&
                   plan.Intents[index].AbilityId == abilityId;
        }

        private static BattleRuntimeEnemyTurnCommand Move(string enemyId)
        {
            return BattleRuntimeEnemyTurnCommand.CreateMove(
                enemyId, EnemyMoveDirection.Right, 1);
        }

        private static BattleRuntimeEnemyTurnCommand Attack(
            string enemyId,
            string targetBattleCardId)
        {
            return BattleRuntimeEnemyTurnCommand.CreateAttack(
                enemyId, targetBattleCardId);
        }

        private static BattleRuntimeEnemyTurnCommand Ability(
            string enemyId,
            string abilityId)
        {
            return BattleRuntimeEnemyTurnCommand.CreateAbility(
                new EnemyAbilityResolutionContext(
                    abilityId,
                    enemyId,
                    false,
                    true,
                    false));
        }
    }
}
