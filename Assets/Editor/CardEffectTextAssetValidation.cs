using System;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    public static class CardEffectTextAssetValidation
    {
        [MenuItem("Have A Break/Validation/Card Effect Text Assets")]
        public static void Validate()
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:CardData",
                new[] { "Assets/GameData/Cards" });
            if (guids.Length == 0)
            {
                throw new InvalidOperationException(
                    "No card assets were found.");
            }

            foreach (string guid in guids)
            {
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(
                    AssetDatabase.GUIDToAssetPath(guid));
                ValidateCard(card);
            }

            Debug.Log(
                $"Validated {guids.Length} authored card effect text assets.");
        }

        public static void ValidateFromCommandLine()
        {
            try
            {
                Validate();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void ValidateCard(CardData card)
        {
            if (card == null)
            {
                throw new InvalidOperationException("Card asset is null.");
            }

            CardEffectTextAsset textAsset = card.EffectTextAsset;
            if (textAsset == null)
            {
                throw new InvalidOperationException(
                    $"{card.CatalogCardId} has no effect text asset.");
            }

            if (!textAsset.MatchesCard(card.CatalogCardId))
            {
                throw new InvalidOperationException(
                    $"{card.CatalogCardId} references mismatched text asset " +
                    $"'{textAsset.CatalogCardId}'.");
            }

            string baseText = textAsset.ResolveRulesText(
                CardTextLocale.Korean);
            if (string.IsNullOrWhiteSpace(baseText) ||
                baseText != card.RulesText)
            {
                throw new InvalidOperationException(
                    $"{card.CatalogCardId} Korean base text is missing or " +
                    "does not match its migrated source.");
            }

            for (int level = CardData.MinimumLevel;
                 level <= CardData.MaximumLevel;
                 level++)
            {
                string expected = card.GetLevelData(level)?.RulesText;
                string actual = textAsset.ResolveRulesText(
                    CardTextLocale.Korean,
                    level);
                if (!string.Equals(
                        actual,
                        string.IsNullOrWhiteSpace(expected)
                            ? baseText
                            : expected,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"{card.CatalogCardId} level {level} text mismatch.");
                }
            }

            string presented = CardEffectTextFormatter.BuildCardRulesText(
                card,
                "INVALID FALLBACK",
                CardData.MinimumLevel);
            if (presented != textAsset.ResolveRulesText(
                    CardTextLocaleProvider.Current,
                    CardData.MinimumLevel))
            {
                throw new InvalidOperationException(
                    $"{card.CatalogCardId} does not present authored text.");
            }
        }
    }
}
