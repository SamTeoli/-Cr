using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class RunDeckState
    {
        [SerializeField] private List<RunCardInstance> cards = new();

        public IReadOnlyList<RunCardInstance> Cards => cards;
        public int Count => cards.Count;

        public bool TryAdd(
            RunCardInstance card,
            out RunDeckFailure failure)
        {
            if (card?.Card == null || card.Enchants == null ||
                card.Enchants.Card != card.Card)
            {
                failure = RunDeckFailure.InvalidCard;
                return false;
            }

            if (string.IsNullOrWhiteSpace(card.OwnedCardId))
            {
                failure = RunDeckFailure.InvalidOwnedCardId;
                return false;
            }

            if (Find(card.OwnedCardId) != null)
            {
                failure = RunDeckFailure.DuplicateOwnedCardId;
                return false;
            }

            cards.Add(card);
            failure = RunDeckFailure.None;
            return true;
        }

        public bool TryRemove(
            string ownedCardId,
            out RunCardInstance removed,
            out RunDeckFailure failure)
        {
            removed = Find(ownedCardId);
            if (removed == null)
            {
                failure = RunDeckFailure.CardNotFound;
                return false;
            }

            cards.Remove(removed);
            failure = RunDeckFailure.None;
            return true;
        }

        public RunCardInstance Find(string ownedCardId)
        {
            if (string.IsNullOrWhiteSpace(ownedCardId))
            {
                return null;
            }

            return cards.Find(card => card != null && string.Equals(
                card.OwnedCardId,
                ownedCardId,
                StringComparison.OrdinalIgnoreCase));
        }
    }
}
