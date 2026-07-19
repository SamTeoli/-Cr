using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    public abstract class CardData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string catalogCardId = "C01";
        [SerializeField] private string displayName = "New Card";
        [SerializeField] private CardRarity rarity = CardRarity.Common;
        [SerializeField, Min(0)] private int manaCost;

        [Header("Presentation")]
        [SerializeField, TextArea(2, 6)] private string rulesText;
        [SerializeField, TextArea(3, 10)] private string detailedRulesText;
        [SerializeField] private Sprite artwork;

        [Header("Collection")]
        [SerializeField, Min(1)] private int baseEnchantSlots = 1;
        [SerializeField] private string role;
        [SerializeField] private string sourceVersion;
        [SerializeField] private string sourceDocument;

        [Header("Levels 1-5")]
        [SerializeField] private List<CardLevelData> levels = new();

        [Header("Effects")]
        [SerializeField] private List<CardEffectData> effects = new();

        public string CatalogCardId => catalogCardId;
        public string DisplayName => displayName;
        public CardRarity Rarity => rarity;
        public int ManaCost => manaCost;
        public string RulesText => rulesText;
        public string DetailedRulesText => detailedRulesText;
        public Sprite Artwork => artwork;
        public int BaseEnchantSlots => baseEnchantSlots;
        public string Role => role;
        public string SourceVersion => sourceVersion;
        public string SourceDocument => sourceDocument;
        public IReadOnlyList<CardLevelData> Levels => levels;
        public IReadOnlyList<CardEffectData> Effects => effects;
        public abstract CardType CardType { get; }

#if UNITY_EDITOR
        public void EditorInitialize(string id, string cardName, CardRarity cardRarity, int cost)
        {
            catalogCardId = id;
            displayName = cardName;
            rarity = cardRarity;
            manaCost = Mathf.Max(0, cost);
        }
#endif

        protected virtual void OnValidate()
        {
            catalogCardId = catalogCardId?.Trim();
            displayName = displayName?.Trim();
            manaCost = Mathf.Max(0, manaCost);
        }
    }
}
