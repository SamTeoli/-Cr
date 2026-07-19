using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class CardPlayPreview
    {
        [SerializeField] private string battleCardId;
        [SerializeField] private CardType cardType;
        [SerializeField] private CardZone destination;
        [SerializeField] private int manaCost;

        public CardPlayPreview(string battleCardId, CardType cardType, CardZone destination, int manaCost)
        {
            this.battleCardId = battleCardId;
            this.cardType = cardType;
            this.destination = destination;
            this.manaCost = Mathf.Max(0, manaCost);
        }

        public string BattleCardId => battleCardId;
        public CardType CardType => cardType;
        public CardZone Destination => destination;
        public int ManaCost => manaCost;
    }
}
