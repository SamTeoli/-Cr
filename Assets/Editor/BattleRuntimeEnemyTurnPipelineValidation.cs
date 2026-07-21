using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleRuntimeEnemyTurnPipelineValidation
    {
        [MenuItem("Have a Break/Validate Runtime Enemy Turn Pipeline")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            if (valid)
            {
                Debug.Log("Battle runtime enemy turn pipeline passed.");
            }
            else
            {
                Debug.LogError("Battle runtime enemy turn pipeline failed.");
            }

            EditorUtility.DisplayDialog(
                "Battle Runtime Enemy Turn Pipeline Validation",
                valid
                    ? "Battle runtime enemy turn pipeline passed."
                    : "Battle runtime enemy turn pipeline failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            CardData c01 = FindCard(TestContentIds.C01);
            if (c01 == null ||
                !TryCreateEnemyTurnRuntime(
                    c01, out BattleRuntimeState runtime))
            {
                return false;
            }

            BattleRuntimeEnemyTurnCommand[] shuffledCommands =
            {
                Ability("ENEMY-PIPE-RIGHT", "ABILITY-PIPE-RIGHT"),
                Ability("ENEMY-PIPE-LEFT", "ABILITY-PIPE-LEFT"),
                Ability("ENEMY-PIPE-CENTER", "ABILITY-PIPE-CENTER")
            };

            bool resolved = BattleRuntimeEnemyTurnPipelineService.TryResolve(
                runtime,
                shuffledCommands,
                out BattleRuntimeEnemyTurnPipelineResult result,
                out BattleRuntimeEnemyTurnPipelineFailure pipelineFailure,
                out BattleRuntimeEnemyTurnPlanFailure planFailure,
                out BattleRuntimeEnemyTurnFailure turnFailure,
                out int failedActionIndex);

            return resolved &&
                   pipelineFailure ==
                   BattleRuntimeEnemyTurnPipelineFailure.None &&
                   planFailure == BattleRuntimeEnemyTurnPlanFailure.None &&
                   turnFailure == BattleRuntimeEnemyTurnFailure.None &&
                   failedActionIndex == -1 &&
                   result != null &&
                   ValidateOrder(result) &&
                   result.TurnResult.Outcome == BattleOutcome.Ongoing &&
                   result.TurnResult.PlayerTurnStarted &&
                   runtime.Turn.Phase == BattleTurnPhase.PlayerAction &&
                   runtime.Turn.PlayerTurnNumber == 2 &&
                   ValidatePlanFailure(runtime) &&
                   ValidateTurnFailure(runtime);
        }

        private static bool ValidateOrder(
            BattleRuntimeEnemyTurnPipelineResult result)
        {
            string[] expected =
            {
                "ENEMY-PIPE-LEFT",
                "ENEMY-PIPE-CENTER",
                "ENEMY-PIPE-RIGHT"
            };
            if (result.Plan == null || result.TurnResult == null ||
                result.Plan.ActionCount != expected.Length ||
                result.TurnResult.ProcessedActionCount != expected.Length)
            {
                return false;
            }

            for (int i = 0; i < expected.Length; i++)
            {
                BattleRuntimeEnemyTurnCommand planned =
                    result.Plan.Commands[i];
                BattleRuntimeEnemyTurnIntent intent =
                    result.Plan.Intents[i];
                BattleRuntimeEnemyTurnActionResult executed =
                    result.TurnResult.ActionResults[i];
                if (planned.EnemyId != expected[i] ||
                    intent.EnemyId != expected[i] ||
                    executed.Command.EnemyId != expected[i] ||
                    executed.AbilityResult == null ||
                    executed.AbilityResult.Cancelled)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ValidatePlanFailure(BattleRuntimeState runtime)
        {
            bool resolved = BattleRuntimeEnemyTurnPipelineService.TryResolve(
                runtime,
                new[]
                {
                    Ability("ENEMY-PIPE-UNKNOWN", "ABILITY-UNKNOWN")
                },
                out _,
                out BattleRuntimeEnemyTurnPipelineFailure pipelineFailure,
                out BattleRuntimeEnemyTurnPlanFailure planFailure,
                out BattleRuntimeEnemyTurnFailure turnFailure,
                out int failedActionIndex);

            return !resolved &&
                   pipelineFailure == BattleRuntimeEnemyTurnPipelineFailure
                       .PlanCreationFailed &&
                   planFailure ==
                   BattleRuntimeEnemyTurnPlanFailure.EnemyNotFound &&
                   turnFailure == BattleRuntimeEnemyTurnFailure.None &&
                   failedActionIndex == 0;
        }

        private static bool ValidateTurnFailure(BattleRuntimeState runtime)
        {
            bool resolved = BattleRuntimeEnemyTurnPipelineService.TryResolve(
                runtime,
                new[]
                {
                    Ability("ENEMY-PIPE-LEFT", "ABILITY-WRONG-PHASE")
                },
                out _,
                out BattleRuntimeEnemyTurnPipelineFailure pipelineFailure,
                out BattleRuntimeEnemyTurnPlanFailure planFailure,
                out BattleRuntimeEnemyTurnFailure turnFailure,
                out int failedActionIndex);

            return !resolved &&
                   pipelineFailure == BattleRuntimeEnemyTurnPipelineFailure
                       .TurnResolutionFailed &&
                   planFailure == BattleRuntimeEnemyTurnPlanFailure.None &&
                   turnFailure ==
                   BattleRuntimeEnemyTurnFailure.InvalidTurnPhase &&
                   failedActionIndex == -1;
        }

        private static bool TryCreateEnemyTurnRuntime(
            CardData c01,
            out BattleRuntimeState runtime)
        {
            BattleCardInstance ally = new(
                c01,
                new CardInstanceIds(
                    c01.CatalogCardId,
                    "OWNED-RUNTIME-37EF-ALLY",
                    "BATTLE-RUNTIME-37EF-ALLY"),
                1,
                CardZone.DrawPile);
            runtime = new BattleRuntimeState(new[] { ally }, 375, 10);
            return runtime.TryAddEnemy(
                       "ENEMY-PIPE-LEFT", 1, 10,
                       EnemyFieldPosition.Left, out _) &&
                   runtime.TryAddEnemy(
                       "ENEMY-PIPE-CENTER", 1, 10,
                       EnemyFieldPosition.Center, out _) &&
                   runtime.TryAddEnemy(
                       "ENEMY-PIPE-RIGHT", 1, 10,
                       EnemyFieldPosition.Right, out _) &&
                   runtime.Turn.TryBeginBattle(out _) &&
                   runtime.Turn.TryConfirmStartingHand(
                       Array.Empty<string>(), out _, out _, out _) &&
                   runtime.Deck.Zones.TryMove(
                       ally.Ids.BattleCardId,
                       CardZone.MonsterField,
                       out _) &&
                   runtime.TryRegisterFieldMonster(
                       ally.Ids.BattleCardId,
                       out _) &&
                   EndPlayerTurn(runtime);
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
