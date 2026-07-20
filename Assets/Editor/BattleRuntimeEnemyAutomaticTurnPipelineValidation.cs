using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleRuntimeEnemyAutomaticTurnPipelineValidation
    {
        [MenuItem("Have a Break/Validate Runtime Enemy Automatic Turn Pipeline")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            if (valid)
            {
                Debug.Log("Battle runtime automatic enemy turn pipeline passed.");
            }
            else
            {
                Debug.LogError("Battle runtime automatic enemy turn pipeline failed.");
            }

            EditorUtility.DisplayDialog(
                "Battle Runtime Enemy Automatic Turn Pipeline Validation",
                valid
                    ? "Battle runtime automatic enemy turn pipeline passed."
                    : "Battle runtime automatic enemy turn pipeline failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            CardData c01 = FindCard("C01");
            bool valid = c01 != null;
            valid &= Run(
                "automatic command validation",
                ValidateAutomaticCommandRejection);
            valid &= Run(
                "ordered move, automatic attack, and ability",
                () => c01 != null && ValidateOrderedAutomaticAttack(c01));
            valid &= Run(
                "player defeat stops remaining actions",
                ValidateDefeatStopsRemainingActions);
            return valid;
        }

        private static bool Run(string label, Func<bool> validation)
        {
            try
            {
                bool passed = validation();
                if (passed)
                {
                    Debug.Log($"Automatic enemy turn validation passed: {label}.");
                }
                else
                {
                    Debug.LogError($"Automatic enemy turn validation failed: {label}.");
                }

                return passed;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Automatic enemy turn validation threw: {label}.\n{exception}");
                return false;
            }
        }

        private static bool ValidateAutomaticCommandRejection()
        {
            bool created = BattleRuntimeEnemyTurnPlanService.TryCreate(
                new[]
                {
                    BattleRuntimeEnemyTurnCommand.CreateAutomaticAttack(
                        "ENEMY-AUTO-INVALID",
                        2,
                        new[] { 0 })
                },
                out _,
                out BattleRuntimeEnemyTurnPlanFailure failure,
                out int failedActionIndex);

            return !created &&
                   failure == BattleRuntimeEnemyTurnPlanFailure
                       .InvalidAutomaticAttack &&
                   failedActionIndex == 0;
        }

        private static bool ValidateOrderedAutomaticAttack(CardData card)
        {
            BattleCardInstance ally = Instance(card, "ORDERED-ALLY");
            BattleRuntimeState runtime = new(new[] { ally }, 401, 10);
            if (!BeginPlayerTurn(runtime) ||
                !runtime.Deck.Zones.TryMove(
                    ally.Ids.BattleCardId,
                    CardZone.MonsterField,
                    out _) ||
                !runtime.TryRegisterFieldMonster(
                    ally.Ids.BattleCardId,
                    PlayerMonsterFieldPosition.Center,
                    out BattleMonsterState monster) ||
                !runtime.TryAddEnemy(
                    "ENEMY-AUTO-ORDERED",
                    monster.MaximumHealth,
                    10,
                    EnemyFieldPosition.Left,
                    out _) ||
                !EndPlayerTurn(runtime))
            {
                return false;
            }

            BattleRuntimeEnemyTurnCommand[] shuffledCommands =
            {
                Ability("ENEMY-AUTO-ORDERED", "ABILITY-AUTO-ORDERED"),
                BattleRuntimeEnemyTurnCommand.CreateAutomaticAttack(
                    "ENEMY-AUTO-ORDERED", 2, new[] { 0, 0 }),
                BattleRuntimeEnemyTurnCommand.CreateMove(
                    "ENEMY-AUTO-ORDERED",
                    EnemyMoveDirection.Right,
                    1)
            };

            if (!BattleRuntimeEnemyTurnPipelineService.TryResolve(
                    runtime,
                    shuffledCommands,
                    out BattleRuntimeEnemyTurnPipelineResult result,
                    out BattleRuntimeEnemyTurnPipelineFailure pipelineFailure,
                    out BattleRuntimeEnemyTurnPlanFailure planFailure,
                    out BattleRuntimeEnemyTurnFailure turnFailure,
                    out int failedActionIndex))
            {
                return false;
            }

            if (pipelineFailure != BattleRuntimeEnemyTurnPipelineFailure.None ||
                planFailure != BattleRuntimeEnemyTurnPlanFailure.None ||
                turnFailure != BattleRuntimeEnemyTurnFailure.None ||
                failedActionIndex != -1 || result == null ||
                result.Plan.ActionCount != 3 ||
                result.TurnResult.ProcessedActionCount != 3)
            {
                return false;
            }

            BattleRuntimeEnemyTurnActionResult move =
                result.TurnResult.ActionResults[0];
            BattleRuntimeEnemyTurnActionResult attack =
                result.TurnResult.ActionResults[1];
            BattleRuntimeEnemyTurnActionResult ability =
                result.TurnResult.ActionResults[2];
            BattleRuntimeEnemyTurnIntent attackIntent = result.Plan.Intents[1];
            return move.Command.ActionType ==
                   BattleRuntimeEnemyTurnActionType.Move &&
                   move.MoveResult != null &&
                   runtime.EnemyPositions.FindPosition(
                       "ENEMY-AUTO-ORDERED") == EnemyFieldPosition.Center &&
                   attack.Command.ActionType ==
                   BattleRuntimeEnemyTurnActionType.Attack &&
                   attack.AutomaticAttackResult != null &&
                   attack.AttackDeclaration == null &&
                   attack.AttackResolution == null &&
                   attackIntent.UsesAutomaticTargeting &&
                   attackIntent.AutomaticAttackCount == 2 &&
                   attackIntent.AttackTieBreakerValues.Count == 2 &&
                   attack.AutomaticAttackResult.ResolvedAttackCount == 2 &&
                   attack.AutomaticAttackResult.Attacks[0].Target.TargetPosition ==
                   PlayerMonsterFieldPosition.Center &&
                   attack.AutomaticAttackResult.Attacks[1].AttackedPlayer &&
                   ally.Zone == CardZone.Graveyard &&
                   ability.Command.ActionType ==
                   BattleRuntimeEnemyTurnActionType.Ability &&
                   ability.AbilityResult != null &&
                   !ability.AbilityResult.Cancelled &&
                   result.TurnResult.Outcome == BattleOutcome.Ongoing &&
                   result.TurnResult.PlayerTurnStarted &&
                   runtime.Turn.Phase == BattleTurnPhase.PlayerAction;
        }

        private static bool ValidateDefeatStopsRemainingActions()
        {
            BattleRuntimeState runtime = new(
                Array.Empty<BattleCardInstance>(),
                402,
                10,
                5);
            if (!runtime.TryAddEnemy(
                    "ENEMY-AUTO-DEFEAT",
                    5,
                    10,
                    EnemyFieldPosition.Center,
                    out _) ||
                !BeginPlayerTurn(runtime) ||
                !EndPlayerTurn(runtime))
            {
                return false;
            }

            BattleRuntimeEnemyTurnCommand[] commands =
            {
                Ability("ENEMY-AUTO-DEFEAT", "ABILITY-SHOULD-NOT-RUN"),
                BattleRuntimeEnemyTurnCommand.CreateAutomaticAttack(
                    "ENEMY-AUTO-DEFEAT", 3, new[] { 0, 0, 0 })
            };

            if (!BattleRuntimeEnemyTurnPipelineService.TryResolve(
                    runtime,
                    commands,
                    out BattleRuntimeEnemyTurnPipelineResult result,
                    out BattleRuntimeEnemyTurnPipelineFailure pipelineFailure,
                    out BattleRuntimeEnemyTurnPlanFailure planFailure,
                    out BattleRuntimeEnemyTurnFailure turnFailure,
                    out int failedActionIndex))
            {
                return false;
            }

            BattleRuntimeEnemyRepeatedAttackResult attack =
                result?.TurnResult?.ActionResults[0].AutomaticAttackResult;
            return pipelineFailure ==
                   BattleRuntimeEnemyTurnPipelineFailure.None &&
                   planFailure == BattleRuntimeEnemyTurnPlanFailure.None &&
                   turnFailure == BattleRuntimeEnemyTurnFailure.None &&
                   failedActionIndex == -1 &&
                   result.Plan.ActionCount == 2 &&
                   result.TurnResult.ProcessedActionCount == 1 &&
                   attack != null &&
                   attack.RequestedAttackCount == 3 &&
                   attack.ResolvedAttackCount == 1 &&
                   attack.StoppedByPlayerDefeat &&
                   runtime.Player.IsDefeated &&
                   result.TurnResult.Outcome == BattleOutcome.Defeat &&
                   !result.TurnResult.PlayerTurnStarted &&
                   runtime.Turn.Phase == BattleTurnPhase.EnemyTurn;
        }

        private static bool BeginPlayerTurn(BattleRuntimeState runtime)
        {
            return runtime.Turn.TryBeginBattle(out _) &&
                   runtime.Turn.TryConfirmStartingHand(
                       Array.Empty<string>(), out _, out _, out _);
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

        private static BattleCardInstance Instance(
            CardData card,
            string suffix)
        {
            return new BattleCardInstance(
                card,
                new CardInstanceIds(
                    card.CatalogCardId,
                    $"OWNED-RUNTIME-40-{suffix}",
                    $"BATTLE-RUNTIME-40-{suffix}"),
                1,
                CardZone.DrawPile);
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
