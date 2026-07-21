using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

public static class BattleRuntimeTurnEffectServiceValidation
{
    [MenuItem("Have a Break/Validate Runtime C03 C04 Turn Effects")]
    public static void Validate()
    {
        CardData c03 = FindCard(TestContentIds.C03);
        CardData c04 = FindCard(TestContentIds.C04);
        if (c03 == null || c04 == null)
        {
            Fail("C03 or C04 card data was not found.");
            return;
        }

        BattleCardInstance c03Instance = CreateInstance(c03, TestContentIds.C03);
        BattleCardInstance c04Instance = CreateInstance(c04, TestContentIds.C04);
        BattleRuntimeState runtime = new BattleRuntimeState(
            new[] { c03Instance, c04Instance }, 35);

        if (!runtime.Turn.TryBeginBattle(out BattleTurnFailure beginFailure))
        {
            Fail($"Could not begin runtime battle. turn={beginFailure}");
            return;
        }

        if (!runtime.Turn.TryConfirmStartingHand(
                Array.Empty<string>(), out _, out _, out BattleTurnFailure handFailure))
        {
            Fail($"Could not confirm runtime starting hand. hand={handFailure}");
            return;
        }

        if (!runtime.Deck.Zones.TryMove(
                c03Instance.Ids.BattleCardId, CardZone.MonsterField, out _) ||
            !runtime.Deck.Zones.TryMove(
                c04Instance.Ids.BattleCardId, CardZone.MonsterField, out _) ||
            !runtime.TryRegisterFieldMonster(
                c03Instance.Ids.BattleCardId, out BattleMonsterState c03Monster) ||
            !runtime.TryRegisterFieldMonster(
                c04Instance.Ids.BattleCardId, out BattleMonsterState c04Monster))
        {
            Fail("Could not register C03 and C04 on the monster field.");
            return;
        }

        int firstPlayerTurnEventIndex = runtime.EventLog.Events.Count;
        if (!BattleRuntimeTurnEffectService.TryEndPlayerTurn(
                runtime,
                firstPlayerTurnEventIndex,
                out BattleRuntimeTurnEffectResult turnEndResult,
                out BattleTurnFailure endFailure) ||
            turnEndResult.ResolvedC03Count != 1 ||
            turnEndResult.TotalDefenseGained != 3 ||
            c03Monster.Defense != 3 ||
            runtime.Turn.Phase != BattleTurnPhase.EnemyTurn ||
            runtime.CardPlay.Mana.CurrentMana != 0)
        {
            Fail($"C03 player turn end connection failed. failure={endFailure}");
            return;
        }

        BattleEventRecord commandEvent = runtime.EventLog.Record(
            BattleEventType.CardPlayed,
            "EnemyMoveCommand",
            "ENEMY-A",
            "ENEMY-A",
            "ENEMY-A");
        BattleEventRecord movedEvent = runtime.EventLog.Record(
            BattleEventType.EnemyMoved,
            "EnemyMoved",
            "ENEMY-A",
            "ENEMY-A",
            "ENEMY-A",
            parentEventId: commandEvent.EventId,
            beforeValue: 0,
            afterValue: 1);

        if (!BattleRuntimeTurnEffectService.TryResolveEnemyMoved(
                runtime,
                movedEvent,
                out BattleRuntimeTurnEffectResult moveResult) ||
            moveResult.ResolvedC04Count != 1 ||
            moveResult.TotalAttackEnhancement != 1 ||
            c04Monster.AttackEnhancement != 1)
        {
            Fail("C04 enemy movement connection failed.");
            return;
        }

        BattleEventRecord secondMovedEvent = runtime.EventLog.Record(
            BattleEventType.EnemyMoved,
            "EnemyMoved",
            "ENEMY-B",
            "ENEMY-B",
            "ENEMY-B",
            parentEventId: commandEvent.EventId,
            beforeValue: 0,
            afterValue: 1);
        if (!BattleRuntimeTurnEffectService.TryResolveEnemyMoved(
                runtime,
                secondMovedEvent,
                out BattleRuntimeTurnEffectResult duplicateResult) ||
            duplicateResult.ResolvedC04Count != 0 ||
            duplicateResult.TotalAttackEnhancement != 0 ||
            c04Monster.AttackEnhancement != 1)
        {
            Fail("C04 movement command duplicate prevention failed.");
            return;
        }

        Debug.Log("Battle runtime C03 and C04 turn effects passed.");
        EditorUtility.DisplayDialog(
            "Battle Runtime Turn Effects Validation",
            "Battle runtime C03 and C04 turn effects passed.",
            "OK");
    }

    private static CardData FindCard(string catalogCardId)
    {
        return AssetDatabase.FindAssets("t:CardData")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<CardData>)
            .FirstOrDefault(card => card != null &&
                string.Equals(card.CatalogCardId, catalogCardId, StringComparison.OrdinalIgnoreCase));
    }

    private static BattleCardInstance CreateInstance(CardData card, string suffix)
    {
        return new BattleCardInstance(
            card,
            new CardInstanceIds(
                card.CatalogCardId,
                $"OWNED-RUNTIME-35D-{suffix}",
                $"BATTLE-RUNTIME-35D-{suffix}"),
            1,
            CardZone.DrawPile);
    }

    private static void Fail(string message)
    {
        Debug.LogError(message);
        EditorUtility.DisplayDialog("Battle Runtime Turn Effects Validation", message, "OK");
    }
}
