using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleRuntimeEnemyMoveServiceValidation
    {
        [MenuItem("Have a Break/Validate Runtime Enemy Move And C08")]
        private static void ValidateFromMenu()
        {
            CardData c04 = FindCard("C04");
            CardData c08 = FindCard("C08");
            CardData c12 = FindCard("C12");
            bool valid = c04 != null && c08 != null && c12 != null &&
                         ValidateC08Replacement(c08) &&
                         ValidateMovementReactions(c04, c12);

            if (valid)
            {
                Debug.Log("Battle runtime enemy move and C08 passed.");
            }
            else
            {
                Debug.LogError("Battle runtime enemy move and C08 failed.");
            }

            EditorUtility.DisplayDialog(
                "Battle Runtime Enemy Move And C08 Validation",
                valid
                    ? "Battle runtime enemy move and C08 passed."
                    : "Battle runtime enemy move and C08 failed. Check the Console.",
                "OK");
        }

        private static bool ValidateC08Replacement(CardData card)
        {
            BattleCardInstance trap = Instance(card, 5, "C08");
            BattleRuntimeState runtime = Start(
                new[] { trap }, "C08-ENEMY");
            if (runtime == null ||
                !BattleRuntimeCardPlayService.TryPlay(
                    runtime, trap.Ids.BattleCardId,
                    out BattleRuntimeCardPlayResult playResult, out _, out _) ||
                !BattleRuntimeTrapEffectService.TryRegisterInstallation(
                    runtime, playResult, out BattleRuntimeTrapInstallation installation))
            {
                return false;
            }

            if (BattleRuntimeEnemyMoveService.TryResolve(
                    runtime,
                    "C08-ENEMY",
                    EnemyMoveDirection.Right,
                    2,
                    out _,
                    out BattleRuntimeEnemyMoveFailure phaseFailure,
                    out _) ||
                phaseFailure != BattleRuntimeEnemyMoveFailure.InvalidTurnPhase ||
                !EndPlayerTurn(runtime))
            {
                return false;
            }

            bool resolved = BattleRuntimeEnemyMoveService.TryResolve(
                runtime,
                "C08-ENEMY",
                EnemyMoveDirection.Right,
                2,
                out BattleRuntimeEnemyMoveResult result,
                out BattleRuntimeEnemyMoveFailure failure,
                out EnemyPositionMoveFailure positionFailure);
            BattleEnemyStatusState status =
                runtime.EnemyStatuses.Find("C08-ENEMY");

            return resolved &&
                   failure == BattleRuntimeEnemyMoveFailure.None &&
                   positionFailure == EnemyPositionMoveFailure.None &&
                   result != null &&
                   result.MoveAttemptEvent.EventType ==
                   BattleEventType.EnemyMoveAttempt &&
                   result.RequestedSteps == 2 &&
                   result.ResolvedSteps == 0 &&
                   result.ReplacedByTrap &&
                   result.TriggeredTrapBattleCardId ==
                   installation.SourceTrap.Ids.BattleCardId &&
                   result.Moves.Count == 0 &&
                   result.MovedEvents.Count == 0 &&
                   runtime.EnemyPositions.FindPosition("C08-ENEMY") ==
                   EnemyFieldPosition.Left &&
                   status != null && status.Bind == 2 && status.Weaken == 1;
        }

        private static bool ValidateMovementReactions(
            CardData c04,
            CardData c12)
        {
            BattleCardInstance cat = Instance(c04, 1, "C04");
            BattleCardInstance barrier = Instance(c12, 5, "C12");
            BattleRuntimeState runtime = Start(
                new[] { cat, barrier }, "MOVE-ENEMY");
            if (runtime == null ||
                !runtime.Deck.Zones.TryMove(
                    cat.Ids.BattleCardId, CardZone.MonsterField, out _) ||
                !runtime.TryRegisterFieldMonster(
                    cat.Ids.BattleCardId, out BattleMonsterState catMonster) ||
                !BattleRuntimeCardPlayService.TryPlay(
                    runtime, barrier.Ids.BattleCardId, out _, out _, out _) ||
                !EndPlayerTurn(runtime))
            {
                return false;
            }

            bool resolved = BattleRuntimeEnemyMoveService.TryResolve(
                runtime,
                "MOVE-ENEMY",
                EnemyMoveDirection.Right,
                1,
                out BattleRuntimeEnemyMoveResult result,
                out BattleRuntimeEnemyMoveFailure failure,
                out EnemyPositionMoveFailure positionFailure);
            BattleEnemyStatusState status =
                runtime.EnemyStatuses.Find("MOVE-ENEMY");
            BattleEnemyRuntimeState enemy = runtime.FindEnemy("MOVE-ENEMY");

            return resolved &&
                   failure == BattleRuntimeEnemyMoveFailure.None &&
                   positionFailure == EnemyPositionMoveFailure.None &&
                   result != null &&
                   !result.ReplacedByTrap &&
                   result.ResolvedSteps == 1 &&
                   result.Moves.Count == 1 &&
                   result.MovedEvents.Count == 1 &&
                   result.ResolvedC04Count == 1 &&
                   result.ResolvedC12Count == 1 &&
                   result.AttackEnhancementGained == 1 &&
                   result.VulnerableGained == 2 &&
                   result.DamageApplied == 1 &&
                   catMonster.AttackEnhancement == 1 &&
                   runtime.EnemyPositions.FindPosition("MOVE-ENEMY") ==
                   EnemyFieldPosition.Center &&
                   status != null && status.Vulnerable == 2 &&
                   enemy != null && enemy.Vital.CurrentHealth == 9;
        }

        private static BattleRuntimeState Start(
            BattleCardInstance[] cards,
            string enemyId)
        {
            BattleRuntimeState runtime = new(cards, 36, 10);
            return runtime.TryAddEnemy(
                       enemyId, 5, 10, EnemyFieldPosition.Left, out _) &&
                   runtime.Turn.TryBeginBattle(out _) &&
                   runtime.Turn.TryConfirmStartingHand(
                       Array.Empty<string>(), out _, out _, out _)
                ? runtime
                : null;
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
                    $"OWNED-RUNTIME-36B-{suffix}",
                    $"BATTLE-RUNTIME-36B-{suffix}"),
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
