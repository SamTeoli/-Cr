using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleRuntimeEnemyAttackServiceValidation
    {
        [MenuItem("Have a Break/Validate Runtime Enemy Attack And C09")]
        private static void ValidateFromMenu()
        {
            CardData c01 = FindCard(TestContentIds.C01);
            CardData c09 = FindCard(TestContentIds.C09);
            bool valid = c01 != null && c09 != null &&
                         Validate(c01, c09);

            if (valid)
            {
                Debug.Log("Battle runtime enemy attack and C09 passed.");
            }
            else
            {
                Debug.LogError("Battle runtime enemy attack and C09 failed.");
            }

            EditorUtility.DisplayDialog(
                "Battle Runtime Enemy Attack And C09 Validation",
                valid
                    ? "Battle runtime enemy attack and C09 passed."
                    : "Battle runtime enemy attack and C09 failed. Check the Console.",
                "OK");
        }

        private static bool Validate(CardData c01, CardData c09)
        {
            BattleCardInstance ally = Instance(c01, 1, "ALLY");
            BattleCardInstance trap = Instance(c09, 5, TestContentIds.C09);
            BattleRuntimeState runtime = new(
                new[] { ally, trap }, 36, 10);
            if (!runtime.TryAddEnemy(
                    "ENEMY-A", 7, 10, EnemyFieldPosition.Left, out _) ||
                !runtime.Turn.TryBeginBattle(out _) ||
                !runtime.Turn.TryConfirmStartingHand(
                    Array.Empty<string>(), out _, out _, out _) ||
                !runtime.Deck.Zones.TryMove(
                    ally.Ids.BattleCardId, CardZone.MonsterField, out _) ||
                !runtime.TryRegisterFieldMonster(
                    ally.Ids.BattleCardId, out BattleMonsterState allyState) ||
                !BattleRuntimeCardPlayService.TryPlay(
                    runtime, trap.Ids.BattleCardId,
                    out BattleRuntimeCardPlayResult playResult, out _, out _) ||
                !BattleRuntimeTrapEffectService.TryRegisterInstallation(
                    runtime, playResult, out BattleRuntimeTrapInstallation installation))
            {
                return false;
            }

            if (BattleRuntimeEnemyAttackService.TryDeclare(
                    runtime,
                    "ENEMY-A",
                    ally.Ids.BattleCardId,
                    out _,
                    out BattleRuntimeEnemyAttackFailure phaseFailure) ||
                phaseFailure != BattleRuntimeEnemyAttackFailure.InvalidTurnPhase ||
                !EndPlayerTurn(runtime))
            {
                return false;
            }

            bool firstResolved = BattleRuntimeEnemyAttackService.TryDeclare(
                runtime,
                "ENEMY-A",
                ally.Ids.BattleCardId,
                out BattleRuntimeEnemyAttackDeclarationResult firstResult,
                out BattleRuntimeEnemyAttackFailure firstFailure);
            bool firstValid = firstResolved &&
                              firstFailure == BattleRuntimeEnemyAttackFailure.None &&
                              firstResult != null &&
                              firstResult.DeclaredAttack.EventType ==
                              BattleEventType.AttackDeclared &&
                              firstResult.DeclaredAttack.ActorId == "ENEMY-A" &&
                              firstResult.DeclaredAttack.TargetId ==
                              ally.Ids.BattleCardId &&
                              firstResult.Attacker.Attack == 7 &&
                              firstResult.TargetMonster == allyState &&
                              firstResult.DefenseGained == 5 &&
                              firstResult.TriggeredTrapBattleCardIds.Count == 1 &&
                              firstResult.TriggeredTrapBattleCardIds[0] ==
                              installation.SourceTrap.Ids.BattleCardId &&
                              allyState.Defense == 5 &&
                              runtime.DefenseRetention.IsMarked(
                                  ally.Ids.BattleCardId);

            bool secondResolved = BattleRuntimeEnemyAttackService.TryDeclare(
                runtime,
                "ENEMY-A",
                ally.Ids.BattleCardId,
                out BattleRuntimeEnemyAttackDeclarationResult secondResult,
                out BattleRuntimeEnemyAttackFailure secondFailure);
            bool secondValid = secondResolved &&
                               secondFailure == BattleRuntimeEnemyAttackFailure.None &&
                               secondResult != null &&
                               secondResult.DefenseGained == 0 &&
                               secondResult.TriggeredTrapBattleCardIds.Count == 0 &&
                               allyState.Defense == 5;

            return firstValid && secondValid;
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
                    $"OWNED-RUNTIME-36C-{suffix}",
                    $"BATTLE-RUNTIME-36C-{suffix}"),
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
