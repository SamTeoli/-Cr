using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleRuntimeCardPlayServiceValidation
    {
        [MenuItem("Have a Break/Validate Battle Runtime Card Play Events")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            EditorUtility.DisplayDialog(
                "Battle Runtime Card Play Events Validation",
                valid
                    ? "Battle runtime card play events passed."
                    : "Battle runtime card play events failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            CardDatabase database = AssetDatabase.LoadAssetAtPath<CardDatabase>(
                "Assets/GameData/CardDatabase.asset");
            if (database == null || database.Cards.Count != 12)
            {
                Debug.LogError("Runtime card play validation requires C01-C12.");
                return false;
            }

            List<BattleCardInstance> cards = database.Cards
                .OrderBy(card => card.CatalogCardId, StringComparer.OrdinalIgnoreCase)
                .Select((card, index) => new BattleCardInstance(
                    card,
                    new CardInstanceIds(
                        card.CatalogCardId,
                        $"OWNED-PLAY-{index + 1:D2}",
                        $"BATTLE-PLAY-{index + 1:D2}"),
                    1,
                    CardZone.DrawPile))
                .ToList();
            BattleRuntimeState runtime = new(cards, 351);
            bool valid = runtime.Turn.TryBeginBattle(out _) &&
                         runtime.Turn.TryConfirmStartingHand(
                             Array.Empty<string>(), out _, out _, out _);

            foreach (BattleCardInstance handCard in
                     runtime.Deck.Zones.GetCards(CardZone.Hand))
            {
                valid &= runtime.Deck.Zones.TryMove(
                    handCard.Ids.BattleCardId, CardZone.Graveyard, out _);
            }

            BattleCardInstance c01 = cards.First(card =>
                card.SourceCard.CatalogCardId == TestContentIds.C01);
            BattleCardInstance c05 = cards.First(card =>
                card.SourceCard.CatalogCardId == TestContentIds.C05);
            valid &= runtime.Deck.Zones.TryMove(
                c01.Ids.BattleCardId, CardZone.Hand, out _);
            valid &= runtime.Deck.Zones.TryMove(
                c05.Ids.BattleCardId, CardZone.Hand, out _);

            valid &= BattleRuntimeCardPlayService.TryPlay(
                runtime, c01.Ids.BattleCardId,
                out BattleRuntimeCardPlayResult monsterResult,
                out BattleRuntimeCardPlayFailure monsterFailure,
                out CardPlayFailure monsterCardFailure);
            valid &= monsterFailure == BattleRuntimeCardPlayFailure.None &&
                     monsterCardFailure == CardPlayFailure.None &&
                     monsterResult.Card == c01 &&
                     monsterResult.MonsterWasSummoned &&
                     monsterResult.PlayedEvent.EventType == BattleEventType.CardPlayed &&
                     monsterResult.SummonedEvent.EventType == BattleEventType.MonsterSummoned &&
                     monsterResult.SummonedEvent.ParentEventId ==
                     monsterResult.PlayedEvent.EventId &&
                     runtime.Monsters.Find(c01.Ids.BattleCardId) ==
                     monsterResult.SummonedMonster &&
                     c01.Zone == CardZone.MonsterField &&
                     runtime.Turn.Phase == BattleTurnPhase.PlayerAction;

            valid &= BattleRuntimeCardPlayService.TryPlay(
                runtime, c05.Ids.BattleCardId,
                out BattleRuntimeCardPlayResult skillResult,
                out BattleRuntimeCardPlayFailure skillFailure,
                out CardPlayFailure skillCardFailure);
            valid &= skillFailure == BattleRuntimeCardPlayFailure.None &&
                     skillCardFailure == CardPlayFailure.None &&
                     !skillResult.MonsterWasSummoned &&
                     skillResult.PlayedEvent.EventType == BattleEventType.CardPlayed &&
                     c05.Zone == CardZone.Graveyard &&
                     runtime.Turn.Phase == BattleTurnPhase.PlayerAction;

            int playedCount = runtime.EventLog.Events.Count(item =>
                item.EventType == BattleEventType.CardPlayed);
            int summonedCount = runtime.EventLog.Events.Count(item =>
                item.EventType == BattleEventType.MonsterSummoned);
            valid &= playedCount == 2 && summonedCount == 1;

            if (valid)
            {
                Debug.Log("Battle runtime card play events passed.");
            }
            else
            {
                Debug.LogError("Battle runtime card play events failed.");
            }

            return valid;
        }
    }
}
