using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    public sealed class CardDataEditorWindow : EditorWindow
    {
        private const string DefaultCardFolder = "Assets/GameData/Cards";
        private const string DatabasePath = "Assets/GameData/CardDatabase.asset";

        private CardType cardType;
        private CardRarity rarity;
        private string catalogCardId = "C01";
        private string displayName = "New Card";
        private int manaCost;
        private Vector2 scroll;

        [MenuItem("Have a Break/Card Data Tools")]
        public static void ShowWindow()
        {
            GetWindow<CardDataEditorWindow>("Card Data Tools");
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Create Card", EditorStyles.boldLabel);
            cardType = (CardType)EditorGUILayout.EnumPopup("Card Type", cardType);
            catalogCardId = EditorGUILayout.TextField("Catalog Card ID", catalogCardId);
            displayName = EditorGUILayout.TextField("Display Name", displayName);
            rarity = (CardRarity)EditorGUILayout.EnumPopup("Rarity", rarity);
            manaCost = EditorGUILayout.IntField("Mana Cost", manaCost);

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(catalogCardId) ||
                                               string.IsNullOrWhiteSpace(displayName)))
            {
                if (GUILayout.Button("Create Card Asset", GUILayout.Height(30)))
                {
                    CreateCardAsset();
                }
            }

            EditorGUILayout.Space(16);
            EditorGUILayout.LabelField("Database", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Searches every CardData asset in the project, validates IDs, and updates the central database.",
                MessageType.Info);

            if (GUILayout.Button("Validate Cards"))
            {
                ValidateCards(true);
            }

            if (GUILayout.Button("Validate Level Resolution"))
            {
                ValidateLevelResolution(true);
            }

            if (GUILayout.Button("Rebuild Card Database", GUILayout.Height(30)))
            {
                RebuildDatabase();
            }

            EditorGUILayout.EndScrollView();
        }

        private void CreateCardAsset()
        {
            EnsureFolder(DefaultCardFolder);

            if (FindAllCards().Any(card => string.Equals(
                    card.CatalogCardId, catalogCardId.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                EditorUtility.DisplayDialog("Duplicate ID", $"'{catalogCardId.Trim()}' already exists.", "OK");
                return;
            }

            CardData card = cardType switch
            {
                CardType.Monster => CreateInstance<MonsterCardData>(),
                CardType.Skill => CreateInstance<SkillCardData>(),
                CardType.Trap => CreateInstance<TrapCardData>(),
                CardType.Barrier => CreateInstance<BarrierCardData>(),
                _ => throw new ArgumentOutOfRangeException()
            };

            card.EditorInitialize(catalogCardId.Trim(), displayName.Trim(), rarity, manaCost);
            string safeName = SanitizeFileName($"{catalogCardId.Trim()}_{displayName.Trim()}");
            string path = AssetDatabase.GenerateUniqueAssetPath($"{DefaultCardFolder}/{safeName}.asset");
            AssetDatabase.CreateAsset(card, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = card;
            EditorGUIUtility.PingObject(card);
        }

        private static void RebuildDatabase()
        {
            List<CardData> cards = FindAllCards()
                .OrderBy(card => card.CatalogCardId, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!ValidateCards(false))
            {
                EditorUtility.DisplayDialog("Validation Failed", "Fix card errors shown in the Console first.", "OK");
                return;
            }

            EnsureFolder("Assets/GameData");
            CardDatabase database = AssetDatabase.LoadAssetAtPath<CardDatabase>(DatabasePath);
            if (database == null)
            {
                database = CreateInstance<CardDatabase>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }

            database.EditorSetCards(cards);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Selection.activeObject = database;
            EditorGUIUtility.PingObject(database);
            Debug.Log($"Card database rebuilt with {cards.Count} card(s).", database);
        }

        private static bool ValidateCards(bool showDialog)
        {
            List<CardData> cards = FindAllCards();
            bool valid = true;

            foreach (CardData card in cards)
            {
                if (string.IsNullOrWhiteSpace(card.CatalogCardId))
                {
                    Debug.LogError($"Card ID is empty: {AssetDatabase.GetAssetPath(card)}", card);
                    valid = false;
                }
                else if (!Regex.IsMatch(card.CatalogCardId, @"^C\d{2,}$", RegexOptions.IgnoreCase))
                {
                    Debug.LogError($"Card ID must use the C01 format: {card.CatalogCardId}", card);
                    valid = false;
                }

                if (string.IsNullOrWhiteSpace(card.DisplayName))
                {
                    Debug.LogError($"Card name is empty: {AssetDatabase.GetAssetPath(card)}", card);
                    valid = false;
                }

                if (card.Levels.Count > 0)
                {
                    if (card.Levels.Any(item => item == null))
                    {
                        Debug.LogError($"Card contains an empty level entry: {card.CatalogCardId}", card);
                        valid = false;
                        continue;
                    }

                    int[] levels = card.Levels.Select(item => item.Level).OrderBy(level => level).ToArray();
                    if (!levels.SequenceEqual(new[] { 1, 2, 3, 4, 5 }))
                    {
                        Debug.LogError($"Card must contain exactly one entry for levels 1-5: {card.CatalogCardId}", card);
                        valid = false;
                    }
                }
            }

            foreach (IGrouping<string, CardData> duplicate in cards
                         .Where(card => !string.IsNullOrWhiteSpace(card.CatalogCardId))
                         .GroupBy(card => card.CatalogCardId, StringComparer.OrdinalIgnoreCase)
                         .Where(group => group.Count() > 1))
            {
                Debug.LogError($"Duplicate card ID '{duplicate.Key}': " +
                               string.Join(", ", duplicate.Select(card => AssetDatabase.GetAssetPath(card))));
                valid = false;
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog("Card Validation",
                    valid ? $"Validation passed ({cards.Count} card(s))." : "Validation failed. Check the Console.",
                    "OK");
            }

            return valid;
        }

        private static bool ValidateLevelResolution(bool showDialog)
        {
            Dictionary<string, CardData> cards = FindAllCards()
                .Where(card => !string.IsNullOrWhiteSpace(card.CatalogCardId))
                .ToDictionary(card => card.CatalogCardId, StringComparer.OrdinalIgnoreCase);

            bool valid = true;
            valid &= ValidateResolvedCard(cards, "C01", 5, 3, 4, 8, "이동 성공 시에도 이 몬스터가 방어 1을 얻습니다.");
            valid &= ValidateResolvedCard(cards, "C05", 3, 0, 0, 0, "효과 동일");
            valid &= ValidateResolvedCard(cards, "C10", 2, 1, 0, 0, "효과 동일");
            valid &= ValidateResolvedCard(cards, "C12", 5, 1, 0, 0, "취약 부여 후 피해 1");

            if (cards.TryGetValue("C01", out CardData clampCard))
            {
                valid &= ValidateClampedLevel(clampCard, 0, CardData.MinimumLevel);
                valid &= ValidateClampedLevel(clampCard, 6, CardData.MaximumLevel);
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Card Level Validation",
                    valid ? "Level resolution passed." : "Level resolution failed. Check the Console.",
                    "OK");
            }

            return valid;
        }

        private static bool ValidateResolvedCard(
            IReadOnlyDictionary<string, CardData> cards,
            string cardId,
            int level,
            int expectedCost,
            int expectedAttack,
            int expectedHealth,
            string expectedRulesText)
        {
            if (!cards.TryGetValue(cardId, out CardData card))
            {
                Debug.LogError($"Level validation card not found: {cardId}");
                return false;
            }

            ResolvedCardData resolved = card.ResolveLevel(level);
            bool valid = resolved.Level == level &&
                         resolved.ManaCost == expectedCost &&
                         resolved.Attack == expectedAttack &&
                         resolved.Health == expectedHealth &&
                         string.Equals(resolved.RulesText, expectedRulesText, StringComparison.Ordinal);

            if (!valid)
            {
                Debug.LogError(
                    $"Level resolution mismatch: {cardId} L{level} " +
                    $"(cost {resolved.ManaCost}, attack {resolved.Attack}, health {resolved.Health}, rules '{resolved.RulesText}')",
                    card);
            }

            return valid;
        }

        private static bool ValidateClampedLevel(CardData card, int requestedLevel, int expectedLevel)
        {
            ResolvedCardData resolved = card.ResolveLevel(requestedLevel);
            bool valid = resolved.Level == expectedLevel && resolved.WasLevelClamped;
            if (!valid)
            {
                Debug.LogError(
                    $"Level clamp mismatch: {card.CatalogCardId} requested {requestedLevel}, resolved {resolved.Level}.",
                    card);
            }

            return valid;
        }

        private static List<CardData> FindAllCards()
        {
            return AssetDatabase.FindAssets("t:CardData")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => AssetDatabase.LoadAssetAtPath<CardData>(path))
                .Where(card => card != null)
                .ToList();
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] segments = folderPath.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = $"{current}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }
                current = next;
            }
        }

        private static string SanitizeFileName(string value)
        {
            foreach (char invalid in System.IO.Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }
            return value;
        }
    }
}
