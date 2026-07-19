using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class TestEnchantCompatibilityBuilder
    {
        private static readonly Dictionary<string, EnchantCompatibilityTag[]> Tags = new(
            StringComparer.OrdinalIgnoreCase)
        {
            ["C01"] = new[]
            {
                EnchantCompatibilityTag.MainEffectCompletion,
                EnchantCompatibilityTag.FixedSingleEnemyTarget
            },
            ["C02"] = new[] { EnchantCompatibilityTag.MainEffectCompletion },
            ["C03"] = new[] { EnchantCompatibilityTag.MainEffectCompletion },
            ["C04"] = new[] { EnchantCompatibilityTag.MainEffectCompletion },
            ["C05"] = new[]
            {
                EnchantCompatibilityTag.MainEffectCompletion,
                EnchantCompatibilityTag.NormalGraveyardAfterResolution,
                EnchantCompatibilityTag.FixedSingleEnemyTarget
            },
            ["C06"] = new[]
            {
                EnchantCompatibilityTag.MainEffectCompletion,
                EnchantCompatibilityTag.NormalGraveyardAfterResolution,
                EnchantCompatibilityTag.FixedSingleEnemyTarget
            },
            ["C07"] = new[]
            {
                EnchantCompatibilityTag.MainEffectCompletion,
                EnchantCompatibilityTag.NormalGraveyardAfterResolution
            },
            ["C08"] = new[]
            {
                EnchantCompatibilityTag.MainEffectCompletion,
                EnchantCompatibilityTag.EnemyAffectingEffect,
                EnchantCompatibilityTag.NormalGraveyardAfterResolution
            },
            ["C09"] = new[]
            {
                EnchantCompatibilityTag.MainEffectCompletion,
                EnchantCompatibilityTag.NormalGraveyardAfterResolution
            },
            ["C10"] = new[]
            {
                EnchantCompatibilityTag.MainEffectCompletion,
                EnchantCompatibilityTag.EnemyAffectingEffect,
                EnchantCompatibilityTag.NormalGraveyardAfterResolution
            },
            ["C11"] = new[]
            {
                EnchantCompatibilityTag.MainEffectCompletion,
                EnchantCompatibilityTag.NumericRepeatingEffect
            },
            ["C12"] = new[]
            {
                EnchantCompatibilityTag.MainEffectCompletion,
                EnchantCompatibilityTag.EnemyAffectingEffect,
                EnchantCompatibilityTag.NumericRepeatingEffect
            }
        };

        private static readonly Dictionary<string, string[]> ExpectedCompatibleCards = new(
            StringComparer.OrdinalIgnoreCase)
        {
            ["E01"] = new[] { "C01", "C02", "C03", "C04" },
            ["E02"] = new[] { "C01", "C02", "C03", "C04" },
            ["E03"] = new[]
                { "C01", "C02", "C03", "C04", "C05", "C06", "C07", "C08", "C09", "C10", "C11", "C12" },
            ["E04"] = new[] { "C06" },
            ["E05"] = new[] { "C08", "C10" },
            ["E06"] = new[] { "C11", "C12" },
            ["E07"] = new[] { "C05", "C06", "C07", "C08", "C09", "C10" },
            ["E08"] = new[] { "C01", "C05", "C06" }
        };

        public static void ApplyTags(bool showDialog)
        {
            Dictionary<string, CardData> cards = FindCards();
            bool complete = true;
            foreach (KeyValuePair<string, EnchantCompatibilityTag[]> pair in Tags)
            {
                string cardId = pair.Key;
                EnchantCompatibilityTag[] tags = pair.Value;
                if (!cards.TryGetValue(cardId, out CardData card))
                {
                    complete = false;
                    Debug.LogError($"Missing card for enchant compatibility tags: {cardId}");
                    continue;
                }

                card.EditorSetEnchantCompatibilityTags(tags);
                EditorUtility.SetDirty(card);
            }

            AssetDatabase.SaveAssets();
            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Enchant Compatibility Tags",
                    complete
                        ? "Applied enchant compatibility tags to C01-C12."
                        : "Some cards were missing. Check the Console.",
                    "OK");
            }
        }

        public static bool Validate(bool showDialog)
        {
            Dictionary<string, CardData> cards = FindCards();
            EnchantDatabase database = AssetDatabase.LoadAssetAtPath<EnchantDatabase>(
                "Assets/GameData/EnchantDatabase.asset");
            bool valid = database != null && Tags.Keys.All(cards.ContainsKey);

            foreach (KeyValuePair<string, EnchantCompatibilityTag[]> pair in Tags)
            {
                string cardId = pair.Key;
                EnchantCompatibilityTag[] expectedTags = pair.Value;
                if (!cards.TryGetValue(cardId, out CardData card))
                {
                    valid = false;
                    continue;
                }

                bool tagsMatch = card.EnchantCompatibilityTags.SequenceEqual(expectedTags);
                valid &= tagsMatch;
                if (!tagsMatch)
                {
                    Debug.LogError($"Enchant compatibility tag mismatch: {cardId}", card);
                }
            }

            if (database != null)
            {
                foreach (KeyValuePair<string, string[]> pair in ExpectedCompatibleCards)
                {
                    string enchantId = pair.Key;
                    string[] expectedCardIds = pair.Value;
                    EnchantData enchant = database.Find(enchantId);
                    if (enchant == null)
                    {
                        valid = false;
                        Debug.LogError($"Missing enchant for compatibility validation: {enchantId}");
                        continue;
                    }

                    string[] actualCardIds = cards.Values
                        .Where(card => EnchantCompatibilityEvaluator.IsCompatible(enchant, card))
                        .Select(card => card.CatalogCardId)
                        .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                    bool matrixMatches = actualCardIds.SequenceEqual(expectedCardIds);
                    valid &= matrixMatches;
                    if (!matrixMatches)
                    {
                        Debug.LogError(
                            $"Compatibility mismatch for {enchantId}. " +
                            $"Expected [{string.Join(", ", expectedCardIds)}], " +
                            $"actual [{string.Join(", ", actualCardIds)}].");
                    }
                }
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Enchant Compatibility Validation",
                    valid
                        ? "Enchant compatibility passed."
                        : "Enchant compatibility failed. Check the Console.",
                    "OK");
            }

            return valid;
        }

        private static Dictionary<string, CardData> FindCards()
        {
            return AssetDatabase.FindAssets("t:CardData")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => AssetDatabase.LoadAssetAtPath<CardData>(path))
                .Where(card => card != null && !string.IsNullOrWhiteSpace(card.CatalogCardId))
                .ToDictionary(card => card.CatalogCardId, StringComparer.OrdinalIgnoreCase);
        }
    }
}
