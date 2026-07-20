using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleRuntimeEnemyTurnPlanValidation
    {
        [MenuItem("Have a Break/Validate Runtime Enemy Turn Planning")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            if (valid)
            {
                Debug.Log("Battle runtime enemy turn planning passed.");
            }
            else
            {
                Debug.LogError("Battle runtime enemy turn planning failed.");
            }

            EditorUtility.DisplayDialog(
                "Battle Runtime Enemy Turn Planning Validation",
                valid
                    ? "Battle runtime enemy turn planning passed."
                    : "Battle runtime enemy turn planning failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            return ValidateSnapshotAndIntents() &&
                   ValidateRejectedCommands() &&
                   ValidatePlanExecution();
        }

        private static bool ValidateSnapshotAndIntents()
        {
            List<BattleRuntimeEnemyTurnCommand> commands = new()
            {
                BattleRuntimeEnemyTurnCommand.CreateMove(
                    "ENEMY-PLAN-A", EnemyMoveDirection.Right, 2),
                BattleRuntimeEnemyTurnCommand.CreateAttack(
                    "ENEMY-PLAN-A", "BATTLE-TARGET-A"),
                BattleRuntimeEnemyTurnCommand.CreateAbility(
                    new EnemyAbilityResolutionContext(
                        "ABILITY-PLAN-A",
                        "ENEMY-PLAN-A",
                        false,
                        true,
                        true))
            };

            if (!BattleRuntimeEnemyTurnPlanService.TryCreate(
                    commands,
                    out BattleRuntimeEnemyTurnPlan plan,
                    out BattleRuntimeEnemyTurnPlanFailure failure,
                    out int failedActionIndex))
            {
                return false;
            }

            commands.Clear();
            BattleRuntimeEnemyTurnIntent move = plan.Intents[0];
            BattleRuntimeEnemyTurnIntent attack = plan.Intents[1];
            BattleRuntimeEnemyTurnIntent ability = plan.Intents[2];
            return failure == BattleRuntimeEnemyTurnPlanFailure.None &&
                   failedActionIndex == -1 &&
                   plan.ActionCount == 3 &&
                   plan.Commands.Count == 3 &&
                   plan.Intents.Count == 3 &&
                   move.ActionType ==
                   BattleRuntimeEnemyTurnActionType.Move &&
                   move.EnemyId == "ENEMY-PLAN-A" &&
                   move.MoveDirection == EnemyMoveDirection.Right &&
                   move.MoveSteps == 2 &&
                   string.IsNullOrEmpty(move.TargetBattleCardId) &&
                   string.IsNullOrEmpty(move.AbilityId) &&
                   attack.ActionType ==
                   BattleRuntimeEnemyTurnActionType.Attack &&
                   attack.EnemyId == "ENEMY-PLAN-A" &&
                   attack.TargetBattleCardId == "BATTLE-TARGET-A" &&
                   ability.ActionType ==
                   BattleRuntimeEnemyTurnActionType.Ability &&
                   ability.EnemyId == "ENEMY-PLAN-A" &&
                   ability.AbilityId == "ABILITY-PLAN-A" &&
                   ability.AbilityAffectsFriendlySide &&
                   ability.AbilityIsArea;
        }

        private static bool ValidateRejectedCommands()
        {
            IEnumerable<BattleRuntimeEnemyTurnCommand> missing = null;
            bool missingRejected =
                !BattleRuntimeEnemyTurnPlanService.TryCreate(
                    missing,
                    out _,
                    out BattleRuntimeEnemyTurnPlanFailure missingFailure,
                    out int missingIndex) &&
                missingFailure ==
                BattleRuntimeEnemyTurnPlanFailure.InvalidCommands &&
                missingIndex == -1;

            BattleRuntimeEnemyTurnCommand[] withNull =
            {
                BattleRuntimeEnemyTurnCommand.CreateMove(
                    "ENEMY-VALID", EnemyMoveDirection.Left, 1),
                null
            };
            bool nullRejected =
                !BattleRuntimeEnemyTurnPlanService.TryCreate(
                    withNull,
                    out _,
                    out BattleRuntimeEnemyTurnPlanFailure nullFailure,
                    out int nullIndex) &&
                nullFailure ==
                BattleRuntimeEnemyTurnPlanFailure.InvalidAction &&
                nullIndex == 1;

            return missingRejected &&
                   nullRejected &&
                   Reject(
                       BattleRuntimeEnemyTurnCommand.CreateMove(
                           "", EnemyMoveDirection.Right, 1),
                       BattleRuntimeEnemyTurnPlanFailure.InvalidEnemyId) &&
                   Reject(
                       BattleRuntimeEnemyTurnCommand.CreateMove(
                           "ENEMY-PLAN", EnemyMoveDirection.Right, 0),
                       BattleRuntimeEnemyTurnPlanFailure.InvalidMovement) &&
                   Reject(
                       BattleRuntimeEnemyTurnCommand.CreateAttack(
                           "ENEMY-PLAN", ""),
                       BattleRuntimeEnemyTurnPlanFailure.InvalidAttackTarget) &&
                   Reject(
                       BattleRuntimeEnemyTurnCommand.CreateAbility(default),
                       BattleRuntimeEnemyTurnPlanFailure.InvalidEnemyId);
        }

        private static bool ValidatePlanExecution()
        {
            CardData c01 = FindCard("C01");
            if (c01 == null)
            {
                return false;
            }

            BattleCardInstance ally = new(
                c01,
                new CardInstanceIds(
                    c01.CatalogCardId,
                    "OWNED-RUNTIME-37B-ALLY",
                    "BATTLE-RUNTIME-37B-ALLY"),
                1,
                CardZone.DrawPile);
            BattleRuntimeState runtime = new(new[] { ally }, 373, 10);
            if (!runtime.TryAddEnemy(
                    "ENEMY-PLAN-EXEC",
                    5,
                    1,
                    EnemyFieldPosition.Left,
                    out _) ||
                !runtime.Turn.TryBeginBattle(out _) ||
                !runtime.Turn.TryConfirmStartingHand(
                    Array.Empty<string>(), out _, out _, out _) ||
                !runtime.Deck.Zones.TryMove(
                    ally.Ids.BattleCardId,
                    CardZone.MonsterField,
                    out _) ||
                !runtime.TryRegisterFieldMonster(
                    ally.Ids.BattleCardId,
                    out _) ||
                !EndPlayerTurn(runtime) ||
                !BattleRuntimeEnemyTurnPlanService.TryCreate(
                    new[]
                    {
                        BattleRuntimeEnemyTurnCommand.CreateMove(
                            "ENEMY-PLAN-EXEC",
                            EnemyMoveDirection.Right,
                            1)
                    },
                    out BattleRuntimeEnemyTurnPlan plan,
                    out BattleRuntimeEnemyTurnPlanFailure planFailure,
                    out int planFailureIndex))
            {
                return false;
            }

            bool resolved = BattleRuntimeEnemyTurnPlanService.TryResolve(
                runtime,
                plan,
                out BattleRuntimeEnemyTurnResult result,
                out BattleRuntimeEnemyTurnFailure failure,
                out int failedActionIndex);

            return planFailure ==
                   BattleRuntimeEnemyTurnPlanFailure.None &&
                   planFailureIndex == -1 &&
                   resolved &&
                   failure == BattleRuntimeEnemyTurnFailure.None &&
                   failedActionIndex == -1 &&
                   result != null &&
                   result.ProcessedActionCount == 1 &&
                   result.ActionResults[0].MoveResult != null &&
                   result.Outcome == BattleOutcome.Ongoing &&
                   result.PlayerTurnStarted &&
                   runtime.EnemyPositions.FindPosition(
                       "ENEMY-PLAN-EXEC") == EnemyFieldPosition.Center &&
                   runtime.Turn.Phase == BattleTurnPhase.PlayerAction &&
                   runtime.Turn.PlayerTurnNumber == 2;
        }

        private static bool Reject(
            BattleRuntimeEnemyTurnCommand command,
            BattleRuntimeEnemyTurnPlanFailure expectedFailure)
        {
            return !BattleRuntimeEnemyTurnPlanService.TryCreate(
                       new[] { command },
                       out _,
                       out BattleRuntimeEnemyTurnPlanFailure failure,
                       out int failedActionIndex) &&
                   failure == expectedFailure &&
                   failedActionIndex == 0;
        }

        private static bool EndPlayerTurn(BattleRuntimeState runtime)
        {
            int firstPlayerTurnEventIndex = runtime.EventLog.Events.Count;
            return BattleRuntimeTurnEffectService.TryEndPlayerTurn(
                       runtime,
                       firstPlayerTurnEventIndex,
                       out _,
                       out _) &&
                   runtime.Turn.Phase == BattleTurnPhase.EnemyTurn;
        }

        private static CardData FindCard(string catalogCardId)
        {
            return AssetDatabase.FindAssets("t:CardData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<CardData>)
                .FirstOrDefault(card => card != null && string.Equals(
                    card.CatalogCardId,
                    catalogCardId,
                    StringComparison.OrdinalIgnoreCase));
        }
    }
}
