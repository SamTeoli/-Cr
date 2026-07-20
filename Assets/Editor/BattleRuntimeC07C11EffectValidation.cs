using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleRuntimeC07C11EffectValidation
    {
        [MenuItem("Have a Break/Validate Runtime C07 C11 Effects")]
        private static void ValidateFromMenu()
        {
            CardData c01 = FindCard("C01");
            CardData c07 = FindCard("C07");
            CardData c11 = FindCard("C11");
            bool cardsFound = c01 != null && c07 != null && c11 != null;
            bool c07Valid = cardsFound && ValidateC07(c01, c07);
            bool c11Valid = cardsFound && ValidateC11(c01, c11);
            bool valid = cardsFound && c07Valid && c11Valid;

            if (valid)
            {
                Debug.Log("Battle runtime C07 and C11 effects passed.");
            }
            else
            {
                Debug.LogError(
                    $"Battle runtime C07 and C11 effects failed. " +
                    $"cards={cardsFound}, C07={c07Valid}, C11={c11Valid}");
            }

            EditorUtility.DisplayDialog(
                "Battle Runtime C07 C11 Effects Validation",
                valid
                    ? "Battle runtime C07 and C11 effects passed."
                    : "Battle runtime C07 and C11 effects failed. Check the Console.",
                "OK");
        }

        private static bool ValidateC07(CardData c01, CardData c07)
        {
            BattleCardInstance source = Instance(c07, 5, "C07-SOURCE");
            BattleCardInstance selected = Instance(c01, 1, "C07-SELECTED");
            BattleCardInstance ally = Instance(c01, 1, "C07-ALLY");
            List<BattleCardInstance> cards = new() { source, selected, ally };
            for (int i = 0; i < 5; i++)
            {
                cards.Add(Instance(c01, 1, $"C07-DRAW-{i}"));
            }

            BattleRuntimeState runtime = Start(cards, 4);
            if (runtime == null ||
                source.Zone != CardZone.Hand ||
                selected.Zone != CardZone.Hand ||
                ally.Zone != CardZone.Hand ||
                !runtime.Deck.Zones.TryMove(
                    ally.Ids.BattleCardId, CardZone.MonsterField, out _) ||
                !runtime.TryRegisterFieldMonster(
                    ally.Ids.BattleCardId, out BattleMonsterState allyState) ||
                !BattleRuntimeCardPlayService.TryPlay(
                    runtime, source.Ids.BattleCardId,
                    out BattleRuntimeCardPlayResult playResult, out _, out _))
            {
                return false;
            }

            bool valid = BattleRuntimeC07EffectService.TryResolve(
                runtime,
                playResult,
                selected.Ids.BattleCardId,
                out BattleRuntimeC07EffectResult result);
            valid &= result != null &&
                     result.DrawnCount == 3 &&
                     result.Banished &&
                     result.DefendedMonsterCount == 1 &&
                     selected.Zone == CardZone.Banished &&
                     allyState.Defense == 2 &&
                     runtime.Deck.Zones.Count(CardZone.Hand) == 5;
            valid &= !BattleRuntimeC07EffectService.TryResolve(
                runtime, playResult, selected.Ids.BattleCardId, out _);
            return valid;
        }

        private static bool ValidateC11(CardData c01, CardData c11)
        {
            BattleCardInstance barrier = Instance(c11, 5, "C11-SOURCE");
            BattleCardInstance ally = Instance(c01, 1, "C11-ALLY");
            List<BattleCardInstance> cards = new() { barrier, ally };
            for (int i = 0; i < 6; i++)
            {
                cards.Add(Instance(c01, 1, $"C11-DRAW-{i}"));
            }

            BattleRuntimeState runtime = Start(cards, 2);
            if (runtime == null ||
                barrier.Zone != CardZone.Hand ||
                ally.Zone != CardZone.Hand ||
                !runtime.Deck.Zones.TryMove(
                    ally.Ids.BattleCardId, CardZone.MonsterField, out _) ||
                !runtime.TryRegisterFieldMonster(
                    ally.Ids.BattleCardId, out BattleMonsterState allyState) ||
                !BattleRuntimeCardPlayService.TryPlay(
                    runtime, barrier.Ids.BattleCardId,
                    out BattleRuntimeCardPlayResult playResult, out _, out _) ||
                playResult.Card.Zone != CardZone.SkillField)
            {
                return false;
            }

            int firstPlayerTurnEventIndex = runtime.EventLog.Events.Count;
            if (!BattleRuntimeTurnEffectService.TryEndPlayerTurn(
                    runtime,
                    firstPlayerTurnEventIndex,
                    out _,
                    out _))
            {
                return false;
            }

            bool valid = BattleRuntimePlayerTurnStartEffectService
                .TryCompleteEnemyTurnAndResolve(
                    runtime,
                    out BattleRuntimePlayerTurnStartEffectResult result,
                    out BattleTurnFailure failure);
            valid &= failure == BattleTurnFailure.None &&
                     result != null &&
                     result.ResolvedC11Count == 1 &&
                     result.DrawnCount == 2 &&
                     result.DefendedMonsterIds.Count == 1 &&
                     result.DefendedMonsterIds[0] == ally.Ids.BattleCardId &&
                     allyState.Defense == 1 &&
                     runtime.Deck.Zones.Count(CardZone.Hand) == 6 &&
                     runtime.Turn.PlayerTurnNumber == 2 &&
                     runtime.Turn.Phase == BattleTurnPhase.PlayerAction;
            return valid;
        }

        private static BattleRuntimeState Start(
            IEnumerable<BattleCardInstance> cards,
            int seed)
        {
            BattleRuntimeState runtime = new(cards, seed, 10);
            return runtime.Turn.TryBeginBattle(out _) &&
                   runtime.Turn.TryConfirmStartingHand(
                       Array.Empty<string>(), out _, out _, out _)
                ? runtime
                : null;
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
                    $"OWNED-RUNTIME-35F-{suffix}",
                    $"BATTLE-RUNTIME-35F-{suffix}"),
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
