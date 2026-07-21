using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleRuntimeTrapEffectServiceValidation
    {
        [MenuItem("Have a Break/Validate Runtime C08 C09 C10 Trap Effects")]
        private static void ValidateFromMenu()
        {
            CardData c01 = FindCard(TestContentIds.C01);
            CardData c08 = FindCard(TestContentIds.C08);
            CardData c09 = FindCard(TestContentIds.C09);
            CardData c10 = FindCard(TestContentIds.C10);
            bool cardsFound = c01 != null && c08 != null &&
                              c09 != null && c10 != null;
            bool c08Valid = cardsFound && ValidateC08(c08);
            bool c09Valid = cardsFound && ValidateC09(c01, c09);
            bool c10Valid = cardsFound && ValidateC10(c10);
            bool valid = cardsFound && c08Valid && c09Valid && c10Valid;

            if (valid)
            {
                Debug.Log("Battle runtime C08, C09, and C10 trap effects passed.");
            }
            else
            {
                Debug.LogError(
                    $"Battle runtime trap effects failed. cards={cardsFound}, " +
                    $"C08={c08Valid}, C09={c09Valid}, C10={c10Valid}");
            }

            EditorUtility.DisplayDialog(
                "Battle Runtime Trap Effects Validation",
                valid
                    ? "Battle runtime C08, C09, and C10 trap effects passed."
                    : "Battle runtime trap effects failed. Check the Console.",
                "OK");
        }

        private static bool ValidateC08(CardData card)
        {
            if (!TryPrepareEnemyTurn(
                    card, null, TestContentIds.C08, out BattleRuntimeState runtime,
                    out BattleRuntimeTrapInstallation installation,
                    out _))
            {
                return false;
            }

            BattleEventRecord attempt = runtime.EventLog.Record(
                BattleEventType.CardPlayed,
                "EnemyMoveAttempt",
                "ENEMY-A",
                "ENEMY-A",
                "ENEMY-A",
                beforeValue: 2);
            bool valid = BattleRuntimeTrapEffectService.TryReplaceEnemyMove(
                runtime,
                installation,
                attempt,
                2,
                "ENEMY-A",
                out int replacementSteps);
            BattleEnemyStatusState enemy = runtime.EnemyStatuses.Find("ENEMY-A");
            return valid && replacementSteps == 0 && enemy != null &&
                   enemy.Bind == 2 && enemy.Weaken == 1;
        }

        private static bool ValidateC09(CardData monsterCard, CardData trapCard)
        {
            if (!TryPrepareEnemyTurn(
                    trapCard, monsterCard, TestContentIds.C09,
                    out BattleRuntimeState runtime,
                    out BattleRuntimeTrapInstallation installation,
                    out BattleMonsterState ally))
            {
                return false;
            }

            BattleEventRecord attack = runtime.EventLog.Record(
                BattleEventType.AttackDeclared,
                "EnemyAttack",
                "ENEMY-A",
                "ENEMY-A",
                ally.BattleCardId);
            bool valid = BattleRuntimeTrapEffectService.TryResolveIncomingAttack(
                runtime,
                installation,
                attack,
                ally.BattleCardId,
                out int defenseGained);
            return valid && defenseGained == 5 && ally.Defense == 5 &&
                   runtime.DefenseRetention.IsMarked(ally.BattleCardId);
        }

        private static bool ValidateC10(CardData card)
        {
            if (!TryPrepareEnemyTurn(
                    card, null, TestContentIds.C10, out BattleRuntimeState runtime,
                    out BattleRuntimeTrapInstallation installation,
                    out _))
            {
                return false;
            }

            BattleEventRecord abilityEvent = runtime.EventLog.Record(
                BattleEventType.CardPlayed,
                "EnemyAbility",
                "ENEMY-A",
                "ENEMY-A",
                "PLAYER");
            EnemyAbilityResolutionContext ability = new(
                "ABILITY-A", "ENEMY-A", false, true, true);
            bool valid = BattleRuntimeTrapEffectService.TryCancelEnemyAbility(
                runtime,
                installation,
                abilityEvent,
                ability,
                out bool cancelled,
                out bool returnedToHand);
            BattleEnemyStatusState enemy = runtime.EnemyStatuses.Find("ENEMY-A");
            return valid && cancelled && returnedToHand &&
                   installation.SourceTrap.Zone == CardZone.Hand &&
                   enemy != null && enemy.Weaken == 1;
        }

        private static bool TryPrepareEnemyTurn(
            CardData trapCard,
            CardData monsterCard,
            string suffix,
            out BattleRuntimeState runtime,
            out BattleRuntimeTrapInstallation installation,
            out BattleMonsterState monster)
        {
            runtime = null;
            installation = null;
            monster = null;
            BattleCardInstance trap = Instance(trapCard, 5, $"{suffix}-TRAP");
            List<BattleCardInstance> cards = new() { trap };
            BattleCardInstance ally = null;
            if (monsterCard != null)
            {
                ally = Instance(monsterCard, 1, $"{suffix}-ALLY");
                cards.Add(ally);
            }

            runtime = new BattleRuntimeState(cards, 35, 10);
            if (!runtime.TryAddEnemy(
                    "ENEMY-A", 5, 10, EnemyFieldPosition.Left, out _) ||
                !runtime.Turn.TryBeginBattle(out _) ||
                !runtime.Turn.TryConfirmStartingHand(
                    Array.Empty<string>(), out _, out _, out _))
            {
                return false;
            }

            if (ally != null &&
                (!runtime.Deck.Zones.TryMove(
                     ally.Ids.BattleCardId, CardZone.MonsterField, out _) ||
                 !runtime.TryRegisterFieldMonster(
                     ally.Ids.BattleCardId, out monster)))
            {
                return false;
            }

            if (!BattleRuntimeCardPlayService.TryPlay(
                    runtime,
                    trap.Ids.BattleCardId,
                    out BattleRuntimeCardPlayResult playResult,
                    out _,
                    out _) ||
                !BattleRuntimeTrapEffectService.TryRegisterInstallation(
                    runtime, playResult, out installation))
            {
                return false;
            }

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
                    $"OWNED-RUNTIME-35G-{suffix}",
                    $"BATTLE-RUNTIME-35G-{suffix}"),
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
