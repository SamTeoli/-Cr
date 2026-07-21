using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleRuntimeRoundServiceValidation
    {
        [MenuItem("Have a Break/Validate Complete Battle Runtime Round")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            if (valid)
            {
                Debug.Log("Complete battle runtime round passed.");
            }
            else
            {
                Debug.LogError("Complete battle runtime round failed.");
            }

            EditorUtility.DisplayDialog(
                "Complete Battle Runtime Round Validation",
                valid
                    ? "Complete battle runtime round passed."
                    : "Complete battle runtime round failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            CardData c01 = FindCard(TestContentIds.C01);
            return c01 != null &&
                   ValidateOngoingRound(c01) &&
                   ValidateDefeatRound() &&
                   ValidateRejectedRoundCalls();
        }

        private static bool ValidateOngoingRound(CardData card)
        {
            BattleCardInstance ally = Instance(card, "ONGOING");
            BattleRuntimeState runtime = new(new[] { ally }, 411, 10);
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
                    "ENEMY-ROUND-ONGOING",
                    1,
                    10,
                    EnemyFieldPosition.Center,
                    out _))
            {
                return false;
            }

            int healthBefore = monster.CurrentHealth;
            int firstPlayerTurnEventIndex = runtime.EventLog.Events.Count;
            bool resolved = BattleRuntimeRoundService.TryResolve(
                runtime,
                firstPlayerTurnEventIndex,
                new[]
                {
                    BattleRuntimeEnemyTurnCommand.CreateAutomaticAttack(
                        "ENEMY-ROUND-ONGOING", 1, new[] { 0 })
                },
                out BattleRuntimeRoundResult result,
                out BattleRuntimeRoundFailure failure,
                out BattleTurnFailure playerTurnEndFailure,
                out BattleRuntimeEnemyTurnPipelineFailure pipelineFailure,
                out BattleRuntimeEnemyTurnPlanFailure planFailure,
                out BattleRuntimeEnemyTurnFailure enemyTurnFailure,
                out int failedActionIndex);

            return resolved &&
                   failure == BattleRuntimeRoundFailure.None &&
                   playerTurnEndFailure == BattleTurnFailure.None &&
                   pipelineFailure ==
                   BattleRuntimeEnemyTurnPipelineFailure.None &&
                   planFailure == BattleRuntimeEnemyTurnPlanFailure.None &&
                   enemyTurnFailure == BattleRuntimeEnemyTurnFailure.None &&
                   failedActionIndex == -1 &&
                   result != null &&
                   result.PlayerTurnEndEffects != null &&
                   result.EnemyTurnPipeline != null &&
                   result.ProcessedEnemyActionCount == 1 &&
                   result.Outcome == BattleOutcome.Ongoing &&
                   result.PlayerTurnStarted &&
                   monster.CurrentHealth == healthBefore - 1 &&
                   runtime.Turn.Phase == BattleTurnPhase.PlayerAction &&
                   runtime.Turn.PlayerTurnNumber == 2;
        }

        private static bool ValidateDefeatRound()
        {
            BattleRuntimeState runtime = new(
                Array.Empty<BattleCardInstance>(),
                412,
                10,
                3);
            if (!runtime.TryAddEnemy(
                    "ENEMY-ROUND-DEFEAT",
                    3,
                    10,
                    EnemyFieldPosition.Center,
                    out _) ||
                !BeginPlayerTurn(runtime))
            {
                return false;
            }

            bool resolved = BattleRuntimeRoundService.TryResolve(
                runtime,
                runtime.EventLog.Events.Count,
                new[]
                {
                    Ability(
                        "ENEMY-ROUND-DEFEAT",
                        "ABILITY-ROUND-SHOULD-NOT-RUN"),
                    BattleRuntimeEnemyTurnCommand.CreateAutomaticAttack(
                        "ENEMY-ROUND-DEFEAT", 3, new[] { 0, 0, 0 })
                },
                out BattleRuntimeRoundResult result,
                out BattleRuntimeRoundFailure failure,
                out BattleTurnFailure playerTurnEndFailure,
                out BattleRuntimeEnemyTurnPipelineFailure pipelineFailure,
                out BattleRuntimeEnemyTurnPlanFailure planFailure,
                out BattleRuntimeEnemyTurnFailure enemyTurnFailure,
                out int failedActionIndex);

            BattleRuntimeEnemyRepeatedAttackResult attacks =
                result?.EnemyTurnPipeline?.TurnResult?.ActionResults[0]
                    .AutomaticAttackResult;
            return resolved &&
                   failure == BattleRuntimeRoundFailure.None &&
                   playerTurnEndFailure == BattleTurnFailure.None &&
                   pipelineFailure ==
                   BattleRuntimeEnemyTurnPipelineFailure.None &&
                   planFailure == BattleRuntimeEnemyTurnPlanFailure.None &&
                   enemyTurnFailure == BattleRuntimeEnemyTurnFailure.None &&
                   failedActionIndex == -1 &&
                   result.EnemyTurnPipeline.Plan.ActionCount == 2 &&
                   result.ProcessedEnemyActionCount == 1 &&
                   result.Outcome == BattleOutcome.Defeat &&
                   !result.PlayerTurnStarted &&
                   attacks != null &&
                   attacks.ResolvedAttackCount == 1 &&
                   attacks.StoppedByPlayerDefeat &&
                   runtime.Player.IsDefeated &&
                   runtime.Turn.Phase == BattleTurnPhase.EnemyTurn;
        }

        private static bool ValidateRejectedRoundCalls()
        {
            BattleRuntimeState wrongPhase = new(
                Array.Empty<BattleCardInstance>(), 413);
            bool phaseRejected =
                !BattleRuntimeRoundService.TryResolve(
                    wrongPhase,
                    0,
                    Array.Empty<BattleRuntimeEnemyTurnCommand>(),
                    out _,
                    out BattleRuntimeRoundFailure phaseFailure,
                    out _, out _, out _, out _, out _) &&
                phaseFailure == BattleRuntimeRoundFailure.InvalidTurnPhase;

            BattleRuntimeState finished = new(
                Array.Empty<BattleCardInstance>(), 414);
            bool finishedRejected = BeginPlayerTurn(finished) &&
                !BattleRuntimeRoundService.TryResolve(
                    finished,
                    0,
                    Array.Empty<BattleRuntimeEnemyTurnCommand>(),
                    out _,
                    out BattleRuntimeRoundFailure finishedFailure,
                    out _, out _, out _, out _, out _) &&
                finishedFailure ==
                BattleRuntimeRoundFailure.BattleAlreadyFinished;

            return phaseRejected && finishedRejected;
        }

        private static bool BeginPlayerTurn(BattleRuntimeState runtime)
        {
            return runtime.Turn.TryBeginBattle(out _) &&
                   runtime.Turn.TryConfirmStartingHand(
                       Array.Empty<string>(), out _, out _, out _);
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
                    $"OWNED-RUNTIME-41-{suffix}",
                    $"BATTLE-RUNTIME-41-{suffix}"),
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
