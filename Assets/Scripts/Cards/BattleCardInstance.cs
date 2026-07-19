using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleCardInstance
    {
        [SerializeField] private CardData sourceCard;
        [SerializeField] private CardInstanceIds ids;
        [SerializeField, Range(CardData.MinimumLevel, CardData.MaximumLevel)] private int currentLevel = 1;
        [SerializeField] private CardZone zone = CardZone.DrawPile;

        private BattleCardInstance()
        {
        }

        public BattleCardInstance(CardData sourceCard, CardInstanceIds ids, int level, CardZone startingZone)
        {
            if (sourceCard == null)
            {
                throw new ArgumentNullException(nameof(sourceCard));
            }

            if (!ids.IsValid)
            {
                throw new ArgumentException("Card instance IDs are incomplete.", nameof(ids));
            }

            if (!string.Equals(sourceCard.CatalogCardId, ids.CatalogCardId, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Catalog card ID does not match the source card.", nameof(ids));
            }

            this.sourceCard = sourceCard;
            this.ids = ids;
            currentLevel = Mathf.Clamp(level, CardData.MinimumLevel, CardData.MaximumLevel);
            zone = startingZone;
        }

        public CardData SourceCard => sourceCard;
        public CardInstanceIds Ids => ids;
        public int CurrentLevel => currentLevel;
        public CardZone Zone => zone;
        public bool IsTemporary => ids.IsTemporary;
        public ResolvedCardData Resolved => sourceCard.ResolveLevel(currentLevel);

        public void SetLevel(int level)
        {
            currentLevel = Mathf.Clamp(level, CardData.MinimumLevel, CardData.MaximumLevel);
        }

        public void MoveTo(CardZone destination)
        {
            zone = destination;
        }
    }
}
