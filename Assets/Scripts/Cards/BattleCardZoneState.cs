using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleCardZoneState
    {
        public const int MaximumHandSize = 10;
        public const int MaximumMonsterFieldSize = 3;
        public const int MaximumSkillFieldSize = 3;

        [SerializeField] private List<BattleCardInstance> cards = new();

        public IReadOnlyList<BattleCardInstance> Cards => cards;

        public bool TryAdd(BattleCardInstance card, out CardZoneMoveFailure failure)
        {
            if (card == null)
            {
                failure = CardZoneMoveFailure.NullCard;
                return false;
            }

            if (!card.Ids.IsValid)
            {
                failure = CardZoneMoveFailure.InvalidIds;
                return false;
            }

            if (Find(card.Ids.BattleCardId) != null)
            {
                failure = CardZoneMoveFailure.DuplicateBattleCardId;
                return false;
            }

            if (!HasCapacity(card.Zone))
            {
                failure = CardZoneMoveFailure.DestinationFull;
                return false;
            }

            cards.Add(card);
            failure = CardZoneMoveFailure.None;
            return true;
        }

        public bool TryMove(string battleCardId, CardZone destination, out CardZoneMoveFailure failure)
        {
            BattleCardInstance card = Find(battleCardId);
            if (card == null)
            {
                failure = CardZoneMoveFailure.CardNotFound;
                return false;
            }

            if (card.Zone == destination)
            {
                failure = CardZoneMoveFailure.None;
                return true;
            }

            if (!HasCapacity(destination))
            {
                failure = CardZoneMoveFailure.DestinationFull;
                return false;
            }

            card.MoveTo(destination);
            failure = CardZoneMoveFailure.None;
            return true;
        }

        public bool TryRemove(string battleCardId, out BattleCardInstance removed)
        {
            removed = Find(battleCardId);
            return removed != null && cards.Remove(removed);
        }

        public BattleCardInstance Find(string battleCardId)
        {
            if (string.IsNullOrWhiteSpace(battleCardId))
            {
                return null;
            }

            return cards.Find(card => card != null &&
                string.Equals(card.Ids.BattleCardId, battleCardId, StringComparison.OrdinalIgnoreCase));
        }

        public List<BattleCardInstance> GetCards(CardZone zone)
        {
            return cards.FindAll(card => card != null && card.Zone == zone);
        }

        public int Count(CardZone zone)
        {
            int count = 0;
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] != null && cards[i].Zone == zone)
                {
                    count++;
                }
            }

            return count;
        }

        public bool HasCapacity(CardZone zone)
        {
            int limit = GetCapacity(zone);
            return limit < 0 || Count(zone) < limit;
        }

        public static int GetCapacity(CardZone zone)
        {
            return zone switch
            {
                CardZone.Hand => MaximumHandSize,
                CardZone.MonsterField => MaximumMonsterFieldSize,
                CardZone.SkillField => MaximumSkillFieldSize,
                _ => -1
            };
        }
    }
}
