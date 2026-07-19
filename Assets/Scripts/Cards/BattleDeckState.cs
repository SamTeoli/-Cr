using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleDeckState
    {
        public const int StartingHandSize = 5;

        [SerializeField] private BattleCardZoneState zones = new();
        [SerializeField] private List<string> drawPileOrder = new();
        [SerializeField] private uint shuffleState;
        [SerializeField] private bool firstPlayerTurnDrawPending = true;

        private BattleDeckState()
        {
        }

        public BattleDeckState(IEnumerable<BattleCardInstance> deckCards, int shuffleSeed)
        {
            if (deckCards == null)
            {
                throw new ArgumentNullException(nameof(deckCards));
            }

            shuffleState = NormalizeSeed(shuffleSeed);
            foreach (BattleCardInstance card in deckCards)
            {
                if (card == null)
                {
                    throw new ArgumentException("Deck contains a null card.", nameof(deckCards));
                }

                card.MoveTo(CardZone.DrawPile);
                if (!zones.TryAdd(card, out CardZoneMoveFailure failure))
                {
                    throw new ArgumentException($"Could not add deck card: {failure}.", nameof(deckCards));
                }

                drawPileOrder.Add(card.Ids.BattleCardId);
            }

            Shuffle(drawPileOrder);
        }

        public BattleCardZoneState Zones => zones;
        public IReadOnlyList<string> DrawPileOrder => drawPileOrder;
        public bool FirstPlayerTurnDrawPending => firstPlayerTurnDrawPending;

        public int DrawStartingHand()
        {
            int drawn = 0;
            for (int i = 0; i < StartingHandSize; i++)
            {
                if (!TryDraw(out _, out _))
                {
                    break;
                }

                drawn++;
            }

            return drawn;
        }

        public bool TryDrawAtPlayerTurnStart(out BattleCardInstance drawnCard, out CardDrawFailure failure)
        {
            if (firstPlayerTurnDrawPending)
            {
                firstPlayerTurnDrawPending = false;
                drawnCard = null;
                failure = CardDrawFailure.FirstTurnSkipped;
                return false;
            }

            return TryDraw(out drawnCard, out failure);
        }

        public bool TryDraw(out BattleCardInstance drawnCard, out CardDrawFailure failure)
        {
            drawnCard = null;
            if (!zones.HasCapacity(CardZone.Hand))
            {
                failure = CardDrawFailure.HandFull;
                return false;
            }

            if (drawPileOrder.Count == 0 && !TryRebuildDrawPile())
            {
                failure = CardDrawFailure.NoCardsAvailable;
                return false;
            }

            string battleCardId = drawPileOrder[0];
            BattleCardInstance card = zones.Find(battleCardId);
            if (card == null || !zones.TryMove(battleCardId, CardZone.Hand, out _))
            {
                failure = CardDrawFailure.ZoneMoveFailed;
                return false;
            }

            drawPileOrder.RemoveAt(0);
            drawnCard = card;
            failure = CardDrawFailure.None;
            return true;
        }

        public bool TryDiscard(string battleCardId, out CardZoneMoveFailure failure)
        {
            return zones.TryMove(battleCardId, CardZone.Graveyard, out failure);
        }

        public bool TryBanish(string battleCardId, out CardZoneMoveFailure failure)
        {
            bool moved = zones.TryMove(battleCardId, CardZone.Banished, out failure);
            if (moved)
            {
                drawPileOrder.RemoveAll(id => string.Equals(id, battleCardId, StringComparison.OrdinalIgnoreCase));
            }

            return moved;
        }

        private bool TryRebuildDrawPile()
        {
            List<BattleCardInstance> graveyard = zones.GetCards(CardZone.Graveyard);
            if (graveyard.Count == 0)
            {
                return false;
            }

            Shuffle(graveyard);

            drawPileOrder.Clear();
            for (int i = 0; i < graveyard.Count; i++)
            {
                BattleCardInstance card = graveyard[i];
                if (!zones.TryMove(card.Ids.BattleCardId, CardZone.DrawPile, out _))
                {
                    return false;
                }

                drawPileOrder.Add(card.Ids.BattleCardId);
            }

            return true;
        }

        private void Shuffle<T>(List<T> items)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int swapIndex = NextIndex(i + 1);
                T item = items[i];
                items[i] = items[swapIndex];
                items[swapIndex] = item;
            }
        }

        private int NextIndex(int exclusiveMaximum)
        {
            shuffleState ^= shuffleState << 13;
            shuffleState ^= shuffleState >> 17;
            shuffleState ^= shuffleState << 5;
            return (int)(shuffleState % (uint)exclusiveMaximum);
        }

        private static uint NormalizeSeed(int seed)
        {
            uint normalized = unchecked((uint)seed);
            return normalized == 0 ? 0x9E3779B9u : normalized;
        }
    }
}
