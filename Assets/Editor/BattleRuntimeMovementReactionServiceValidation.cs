using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleRuntimeMovementReactionServiceValidation
    {
        [MenuItem("Have a Break/Validate Runtime C04 C12 Movement Reactions")]
        private static void ValidateFromMenu()
        {
            CardData c04 = FindCard("C04");
            CardData c05 = FindCard("C05");
            CardData c12 = FindCard("C12");
            bool valid = c04 != null && c05 != null && c12 != null &&
                         Validate(c04, c05, c12);

            if (valid)
            {
                Debug.Log("Battle runtime C04 and C12 movement reactions passed.");
            }
            else
            {
                Debug.LogError("Battle runtime C04 and C12 movement reactions failed.");
            }

            EditorUtility.DisplayDialog(
                "Battle Runtime Movement Reactions Validation",
                valid
                    ? "Battle runtime C04 and C12 movement reactions passed."
                    : "Battle runtime C04 and C12 movement reactions failed. Check the Console.",
                "OK");
        }

        private static bool Validate(CardData c04, CardData c05, CardData c12)
        {
            BattleCardInstance cat = Instance(c04, 1, "C04");
            BattleCardInstance push = Instance(c05, 1, "C05");
            BattleCardInstance barrier = Instance(c12, 5, "C12");
            BattleRuntimeState runtime = new(
                new[] { cat, push, barrier }, 35, 10);
            if (!runtime.TryAddEnemy(
                    "ENEMY-A", 5, 10, EnemyFieldPosition.Left,
                    out BattleEnemyRuntimeState enemy) ||
                !runtime.Turn.TryBeginBattle(out _) ||
                !runtime.Turn.TryConfirmStartingHand(
                    Array.Empty<string>(), out _, out _, out _) ||
                !runtime.Deck.Zones.TryMove(
                    cat.Ids.BattleCardId, CardZone.MonsterField, out _) ||
                !runtime.TryRegisterFieldMonster(
                    cat.Ids.BattleCardId, out BattleMonsterState catMonster))
            {
                return false;
            }

            if (!BattleRuntimeCardPlayService.TryPlay(
                    runtime,
                    barrier.Ids.BattleCardId,
                    out BattleRuntimeCardPlayResult barrierPlay,
                    out _,
                    out _) ||
                barrierPlay.Card.Zone != CardZone.SkillField ||
                !BattleRuntimeCardPlayService.TryPlay(
                    runtime,
                    push.Ids.BattleCardId,
                    out BattleRuntimeCardPlayResult pushPlay,
                    out _,
                    out _) ||
                !BattleRuntimeSkillEffectService.TryResolve(
                    runtime,
                    pushPlay,
                    "ENEMY-A",
                    out _,
                    out _))
            {
                return false;
            }

            BattleEventRecord movedEvent = runtime.EventLog.Events
                .FirstOrDefault(item =>
                    item.EventType == BattleEventType.EnemyMoved &&
                    item.ParentEventId == pushPlay.PlayedEvent.EventId &&
                    item.TargetId == "ENEMY-A");
            if (!BattleRuntimeMovementReactionService.TryResolve(
                    runtime,
                    movedEvent,
                    out BattleRuntimeMovementReactionResult result))
            {
                return false;
            }

            BattleEnemyStatusState enemyStatus =
                runtime.EnemyStatuses.Find("ENEMY-A");
            bool valid = runtime.Turn.Phase == BattleTurnPhase.PlayerAction &&
                         result.ResolvedC04Count == 1 &&
                         result.ResolvedC12Count == 1 &&
                         result.AttackEnhancementGained == 1 &&
                         result.VulnerableGained == 2 &&
                         result.DamageApplied == 1 &&
                         catMonster.AttackEnhancement == 1 &&
                         enemyStatus != null &&
                         enemyStatus.Vulnerable == 2 &&
                         enemy.Vital.CurrentHealth == 9;

            valid &= BattleRuntimeTurnEffectService.TryResolveEnemyMoved(
                         runtime,
                         movedEvent,
                         out BattleRuntimeTurnEffectResult legacyResult) &&
                     legacyResult.ResolvedC04Count == 0 &&
                     legacyResult.TotalAttackEnhancement == 0 &&
                     catMonster.AttackEnhancement == 1 &&
                     enemyStatus.Vulnerable == 2 &&
                     enemy.Vital.CurrentHealth == 9;
            return valid;
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
                    $"OWNED-RUNTIME-35H-{suffix}",
                    $"BATTLE-RUNTIME-35H-{suffix}"),
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
