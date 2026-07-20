using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleRuntimeEnemyTurnServiceValidation
    {
        [MenuItem("Have a Break/Validate Runtime Enemy Turn Orchestration")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            if (valid)
            {
                Debug.Log("Battle runtime enemy turn orchestration passed.");
            }
            else
            {
                Debug.LogError("Battle runtime enemy turn orchestration failed.");
            }

            EditorUtility.DisplayDialog(
                "Battle Runtime Enemy Turn Orchestration Validation",
                valid
                    ? "Battle runtime enemy turn orchestration passed."
                    : "Battle runtime enemy turn orchestration failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            CardData c01 = FindCard("C01");
            CardData c08 = FindCard("C08");
            CardData c09 = FindCard("C09");
            CardData c10 = FindCard("C10");
            return c01 != null && c08 != null && c09 != null && c10 != null &&
                   ValidateCompletedTurn(c01, c08, c09, c10) &&
                   ValidateDefeatStopsTurn(c01);
        }

        private static bool ValidateCompletedTurn(
            CardData c01,
            CardData c08,
            CardData c09,
            CardData c10)
        {
            BattleCardInstance ally = Instance(c01, 1, "ALLY");
            BattleCardInstance c08Trap = Instance(c08, 5, "C08");
            BattleCardInstance c09Trap = Instance(c09, 5, "C09");
            BattleCardInstance c10Trap = Instance(c10, 5, "C10");
            BattleRuntimeState runtime = new(
                new[] { ally, c08Trap, c09Trap, c10Trap },
                371,
                20);
            if (!runtime.TryAddEnemy(
                    "ENEMY-TURN-A",
                    5,
                    10,
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
                    out BattleMonsterState allyState) ||
                !PlayAndRegisterTrap(runtime, c08Trap) ||
                !PlayAndRegisterTrap(runtime, c09Trap) ||
                !PlayAndRegisterTrap(runtime, c10Trap) ||
                !EndPlayerTurn(runtime))
            {
                return false;
            }

            BattleRuntimeEnemyTurnCommand[] commands =
            {
                BattleRuntimeEnemyTurnCommand.CreateMove(
                    "ENEMY-TURN-A", EnemyMoveDirection.Right, 1),
                BattleRuntimeEnemyTurnCommand.CreateAttack(
                    "ENEMY-TURN-A", ally.Ids.BattleCardId),
                BattleRuntimeEnemyTurnCommand.CreateAbility(
                    new EnemyAbilityResolutionContext(
                        "ABILITY-TURN-A",
                        "ENEMY-TURN-A",
                        false,
                        true,
                        false))
            };

            bool resolved = BattleRuntimeEnemyTurnService.TryResolve(
                runtime,
                commands,
                out BattleRuntimeEnemyTurnResult result,
                out BattleRuntimeEnemyTurnFailure failure,
                out int failedActionIndex);
            BattleEnemyStatusState status =
                runtime.EnemyStatuses.Find("ENEMY-TURN-A");

            return resolved &&
                   failure == BattleRuntimeEnemyTurnFailure.None &&
                   failedActionIndex == -1 &&
                   result != null &&
                   result.ProcessedActionCount == 3 &&
                   result.Outcome == BattleOutcome.Ongoing &&
                   result.PlayerTurnStarted &&
                   result.PlayerTurnStartEffects != null &&
                   result.PlayerTurnStartEffects.ResolvedC11Count == 0 &&
                   result.ActionResults[0].MoveResult != null &&
                   result.ActionResults[0].MoveResult.ReplacedByTrap &&
                   result.ActionResults[1].AttackDeclaration != null &&
                   result.ActionResults[1].AttackResolution != null &&
                   result.ActionResults[1].AttackResolution.WeakenReduction == 1 &&
                   result.ActionResults[1].AttackResolution.DefenseConsumed == 4 &&
                   result.ActionResults[1].AttackResolution.MonsterDamage == 0 &&
                   result.ActionResults[2].AbilityResult != null &&
                   result.ActionResults[2].AbilityResult.Cancelled &&
                   result.ActionResults[2].AbilityResult.ReturnedTrapToHand &&
                   runtime.Turn.Phase == BattleTurnPhase.PlayerAction &&
                   runtime.Turn.PlayerTurnNumber == 2 &&
                   runtime.Player.CurrentHealth ==
                   BattlePlayerState.DefaultMaximumHealth &&
                   runtime.EnemyPositions.FindPosition("ENEMY-TURN-A") ==
                   EnemyFieldPosition.Left &&
                   status != null && status.Bind == 2 && status.Weaken == 2 &&
                   allyState.CurrentHealth == allyState.MaximumHealth &&
                   allyState.Defense == 1 &&
                   c10Trap.Zone == CardZone.Hand &&
                   runtime.TrapInstallations.Count == 2;
        }

        private static bool ValidateDefeatStopsTurn(CardData c01)
        {
            int monsterHealth = Mathf.Max(1, c01.ResolveLevel(1).Health);
            BattleCardInstance ally = Instance(c01, 1, "DEFEAT-ALLY");
            BattleRuntimeState runtime = new(new[] { ally }, 372, 10);
            if (!runtime.TryAddEnemy(
                    "ENEMY-TURN-DEFEAT",
                    monsterHealth + BattlePlayerState.DefaultMaximumHealth,
                    10,
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
                !EndPlayerTurn(runtime))
            {
                return false;
            }

            BattleRuntimeEnemyTurnCommand[] commands =
            {
                BattleRuntimeEnemyTurnCommand.CreateAttack(
                    "ENEMY-TURN-DEFEAT", ally.Ids.BattleCardId),
                BattleRuntimeEnemyTurnCommand.CreateAbility(
                    new EnemyAbilityResolutionContext(
                        "ABILITY-SHOULD-NOT-RUN",
                        "ENEMY-TURN-DEFEAT",
                        false,
                        true,
                        false))
            };

            bool resolved = BattleRuntimeEnemyTurnService.TryResolve(
                runtime,
                commands,
                out BattleRuntimeEnemyTurnResult result,
                out BattleRuntimeEnemyTurnFailure failure,
                out int failedActionIndex);

            return resolved &&
                   failure == BattleRuntimeEnemyTurnFailure.None &&
                   failedActionIndex == -1 &&
                   result != null &&
                   result.ProcessedActionCount == 1 &&
                   result.Outcome == BattleOutcome.Defeat &&
                   !result.PlayerTurnStarted &&
                   result.PlayerTurnStartEffects == null &&
                   runtime.Player.IsDefeated &&
                   runtime.Player.CurrentHealth == 0 &&
                   runtime.Turn.Phase == BattleTurnPhase.EnemyTurn &&
                   ally.Zone == CardZone.Graveyard &&
                   runtime.EventLog.Events.All(item =>
                       item.EventType != BattleEventType.EnemyAbilityDeclared);
        }

        private static bool PlayAndRegisterTrap(
            BattleRuntimeState runtime,
            BattleCardInstance trap)
        {
            return BattleRuntimeCardPlayService.TryPlay(
                       runtime,
                       trap.Ids.BattleCardId,
                       out BattleRuntimeCardPlayResult playResult,
                       out _,
                       out _) &&
                   BattleRuntimeTrapEffectService.TryRegisterInstallation(
                       runtime,
                       playResult,
                       out _);
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

        private static BattleCardInstance Instance(
            CardData card,
            int level,
            string suffix)
        {
            return new BattleCardInstance(
                card,
                new CardInstanceIds(
                    card.CatalogCardId,
                    $"OWNED-RUNTIME-37A-{suffix}",
                    $"BATTLE-RUNTIME-37A-{suffix}"),
                level,
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
