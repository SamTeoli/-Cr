using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class RunCardInstance
    {
        [SerializeField] private CardData card;
        [SerializeField] private string ownedCardId;
        [SerializeField, Range(CardData.MinimumLevel, CardData.MaximumLevel)]
        private int currentLevel = CardData.MinimumLevel;
        [SerializeField] private RunCardEnchantState enchants;

        private RunCardInstance()
        {
        }

        public RunCardInstance(
            CardData card,
            string ownedCardId,
            int level = CardData.MinimumLevel)
        {
            this.card = card ??
                throw new ArgumentNullException(nameof(card));
            if (string.IsNullOrWhiteSpace(ownedCardId))
            {
                throw new ArgumentException(
                    "Owned card ID is required.",
                    nameof(ownedCardId));
            }

            this.ownedCardId = ownedCardId.Trim();
            currentLevel = Mathf.Clamp(
                level,
                CardData.MinimumLevel,
                CardData.MaximumLevel);
            enchants = new RunCardEnchantState(card);
        }

        public CardData Card => card;
        public string CatalogCardId => card?.CatalogCardId;
        public string OwnedCardId => ownedCardId;
        public int CurrentLevel => currentLevel;
        public RunCardEnchantState Enchants => enchants;

        public void SetLevel(int level)
        {
            currentLevel = Mathf.Clamp(
                level,
                CardData.MinimumLevel,
                CardData.MaximumLevel);
        }
    }
}
