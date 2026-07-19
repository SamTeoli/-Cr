using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleStateBasedChecker
    {
        [SerializeField] private BattleDeckState deck;
        [SerializeField] private BattleMonsterRegistry monsters;
        [SerializeField] private BattleEventLog eventLog;

        private BattleStateBasedChecker()
        {
        }

        public BattleStateBasedChecker(
            BattleDeckState deck,
            BattleMonsterRegistry monsters,
            BattleEventLog eventLog)
        {
            this.deck = deck ?? throw new ArgumentNullException(nameof(deck));
            this.monsters = monsters ?? throw new ArgumentNullException(nameof(monsters));
            this.eventLog = eventLog ?? throw new ArgumentNullException(nameof(eventLog));
        }

        public bool TryResolveMonsterDestruction(
            string parentEventId,
            out List<BattleEventRecord> destructionEvents,
            out StateBasedCheckFailure failure)
        {
            destructionEvents = new List<BattleEventRecord>();
            if (!string.IsNullOrWhiteSpace(parentEventId) && eventLog.Find(parentEventId) == null)
            {
                failure = StateBasedCheckFailure.ParentEventNotFound;
                return false;
            }

            List<BattleCardInstance> fieldOrder = deck.Zones.GetCards(CardZone.MonsterField);
            List<BattleMonsterState> candidates = new();
            for (int i = 0; i < fieldOrder.Count; i++)
            {
                BattleMonsterState monster = monsters.Find(fieldOrder[i].Ids.BattleCardId);
                if (monster != null && monster.IsDestructionCandidate)
                {
                    candidates.Add(monster);
                }
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                BattleMonsterState monster = candidates[i];
                BattleCardInstance card = monster.Card;
                CardZone destination = card.IsTemporary ? CardZone.Banished : CardZone.Graveyard;
                if (!deck.Zones.TryMove(card.Ids.BattleCardId, destination, out _))
                {
                    failure = StateBasedCheckFailure.ZoneMoveFailed;
                    return false;
                }

                BattleEventRecord destroyed = eventLog.Record(
                    BattleEventType.MonsterDestroyed,
                    "StateBasedDestruction",
                    card.SourceCard.CatalogCardId,
                    card.Ids.BattleCardId,
                    card.Ids.BattleCardId,
                    parentEventId: parentEventId,
                    hasZoneChange: true,
                    fromZone: CardZone.MonsterField,
                    toZone: destination,
                    beforeValue: 0,
                    afterValue: 0);
                destructionEvents.Add(destroyed);
                monsters.TryRemove(card.Ids.BattleCardId, out _);
            }

            failure = StateBasedCheckFailure.None;
            return true;
        }
    }
}
