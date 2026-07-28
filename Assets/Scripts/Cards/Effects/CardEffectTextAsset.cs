using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    public enum CardTextLocale
    {
        Korean,
        English,
        Japanese
    }

    [Serializable]
    public sealed class CardLevelEffectText
    {
        [SerializeField, Range(CardData.MinimumLevel, CardData.MaximumLevel)]
        private int level = CardData.MinimumLevel;
        [SerializeField, TextArea(2, 8)] private string rulesText;

        public int Level => level;
        public string RulesText => rulesText;
    }

    [Serializable]
    public sealed class LocalizedCardEffectText
    {
        [SerializeField] private CardTextLocale locale = CardTextLocale.Korean;
        [SerializeField, TextArea(2, 8)] private string rulesText;
        [SerializeField, TextArea(3, 12)] private string detailedRulesText;
        [SerializeField] private List<CardLevelEffectText> levels = new();

        public CardTextLocale Locale => locale;
        public string RulesText => rulesText;
        public string DetailedRulesText => detailedRulesText;
        public IReadOnlyList<CardLevelEffectText> Levels => levels;

        public string ResolveRulesText(int level)
        {
            if (level >= CardData.MinimumLevel &&
                level <= CardData.MaximumLevel)
            {
                for (int i = 0; i < levels.Count; i++)
                {
                    CardLevelEffectText entry = levels[i];
                    if (entry != null &&
                        entry.Level == level &&
                        !string.IsNullOrWhiteSpace(entry.RulesText))
                    {
                        return entry.RulesText;
                    }
                }
            }

            return rulesText;
        }
    }

    [CreateAssetMenu(
        menuName = "Have A Break/Cards/Card Effect Text",
        fileName = "CardEffectText")]
    public sealed class CardEffectTextAsset : ScriptableObject
    {
        [SerializeField] private string catalogCardId;
        [SerializeField] private List<LocalizedCardEffectText> localizations = new();

        public string CatalogCardId => catalogCardId;
        public IReadOnlyList<LocalizedCardEffectText> Localizations => localizations;

        public string ResolveRulesText(
            CardTextLocale locale,
            int level = 0)
        {
            LocalizedCardEffectText text = Find(locale) ?? Find(CardTextLocale.Korean);
            return text?.ResolveRulesText(level);
        }

        public string ResolveDetailedRulesText(CardTextLocale locale)
        {
            LocalizedCardEffectText text = Find(locale) ?? Find(CardTextLocale.Korean);
            return text?.DetailedRulesText;
        }

        public bool MatchesCard(string cardId)
        {
            return !string.IsNullOrWhiteSpace(cardId) &&
                   string.Equals(
                       catalogCardId?.Trim(),
                       cardId.Trim(),
                       StringComparison.OrdinalIgnoreCase);
        }

        private LocalizedCardEffectText Find(CardTextLocale locale)
        {
            for (int i = 0; i < localizations.Count; i++)
            {
                LocalizedCardEffectText candidate = localizations[i];
                if (candidate != null && candidate.Locale == locale)
                {
                    return candidate;
                }
            }

            return null;
        }

        private void OnValidate()
        {
            catalogCardId = catalogCardId?.Trim();
        }
    }

    public static class CardTextLocaleProvider
    {
        public static CardTextLocale Current =>
            Application.systemLanguage switch
            {
                SystemLanguage.English => CardTextLocale.English,
                SystemLanguage.Japanese => CardTextLocale.Japanese,
                _ => CardTextLocale.Korean
            };
    }
}
