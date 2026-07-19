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

            if (GUILayout.Button("Validate Battle Card Instances"))
            {
                ValidateBattleCardInstances(true);
            }

            if (GUILayout.Button("Validate Battle Card Zones"))
            {
                ValidateBattleCardZones(true);
            }

            if (GUILayout.Button("Validate Battle Deck Draw"))
            {
                ValidateBattleDeckDraw(true);
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

        private static bool ValidateBattleCardInstances(bool showDialog)
        {
            Dictionary<string, CardData> cards = FindAllCards()
                .Where(card => !string.IsNullOrWhiteSpace(card.CatalogCardId))
                .ToDictionary(card => card.CatalogCardId, StringComparer.OrdinalIgnoreCase);

            bool valid = true;
            if (!cards.TryGetValue("C01", out CardData c01) || !cards.TryGetValue("C05", out CardData c05))
            {
                Debug.LogError("Battle card validation requires C01 and C05.");
                valid = false;
            }
            else
            {
                CardInstanceIds ownedIds = new(c01.CatalogCardId, "OWN-001", "BATTLE-001");
                BattleCardInstance owned = new(c01, ownedIds, 5, CardZone.Hand);
                owned.MoveTo(CardZone.MonsterField);
                ResolvedCardData ownedResolved = owned.Resolved;

                valid &= owned.Ids.IsValid &&
                         !owned.IsTemporary &&
                         owned.Zone == CardZone.MonsterField &&
                         owned.CurrentLevel == 5 &&
                         ownedResolved.Attack == 4 &&
                         ownedResolved.Health == 8;

                CardInstanceIds temporaryIds = new(c05.CatalogCardId, null, "BATTLE-002", "INSTANT-001");
                BattleCardInstance temporary = new(c05, temporaryIds, 0, CardZone.Hand);
                temporary.MoveTo(CardZone.Banished);

                valid &= temporary.Ids.IsValid &&
                         temporary.IsTemporary &&
                         temporary.CurrentLevel == CardData.MinimumLevel &&
                         temporary.Zone == CardZone.Banished;
            }

            if (!valid)
            {
                Debug.LogError("Battle card instance validation failed.");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Battle Card Validation",
                    valid ? "Battle card instances passed." : "Battle card instances failed. Check the Console.",
                    "OK");
            }

            return valid;
        }

        private static bool ValidateBattleCardZones(bool showDialog)
        {
            Dictionary<string, CardData> cards = FindAllCards()
                .Where(card => !string.IsNullOrWhiteSpace(card.CatalogCardId))
                .ToDictionary(card => card.CatalogCardId, StringComparer.OrdinalIgnoreCase);

            bool hasC01 = cards.TryGetValue("C01", out CardData c01);
            bool hasC05 = cards.TryGetValue("C05", out CardData c05);
            bool valid = hasC01 && hasC05;
            if (!valid)
            {
                Debug.LogError("Battle card zone validation requires C01 and C05.");
            }
            else
            {
                BattleCardZoneState zones = new();
                BattleCardInstance first = CreateValidationInstance(c01, 1, CardZone.DrawPile);
                valid &= zones.TryAdd(first, out CardZoneMoveFailure addFailure) &&
                         addFailure == CardZoneMoveFailure.None;
                valid &= zones.TryMove(first.Ids.BattleCardId, CardZone.Hand, out CardZoneMoveFailure moveFailure) &&
                         moveFailure == CardZoneMoveFailure.None &&
                         zones.Count(CardZone.Hand) == 1 &&
                         zones.Count(CardZone.DrawPile) == 0;

                valid &= !zones.TryAdd(first, out CardZoneMoveFailure duplicateFailure) &&
                         duplicateFailure == CardZoneMoveFailure.DuplicateBattleCardId;

                for (int i = 2; i <= BattleCardZoneState.MaximumHandSize; i++)
                {
                    valid &= zones.TryAdd(CreateValidationInstance(c05, i, CardZone.Hand), out _);
                }

                BattleCardInstance overflow = CreateValidationInstance(c05, 11, CardZone.Hand);
                valid &= !zones.TryAdd(overflow, out CardZoneMoveFailure handFailure) &&
                         handFailure == CardZoneMoveFailure.DestinationFull &&
                         zones.Count(CardZone.Hand) == BattleCardZoneState.MaximumHandSize;

                BattleCardZoneState field = new();
                for (int i = 1; i <= BattleCardZoneState.MaximumMonsterFieldSize; i++)
                {
                    valid &= field.TryAdd(CreateValidationInstance(c01, i + 20, CardZone.MonsterField), out _);
                }

                valid &= !field.TryAdd(CreateValidationInstance(c01, 24, CardZone.MonsterField), out CardZoneMoveFailure fieldFailure) &&
                         fieldFailure == CardZoneMoveFailure.DestinationFull;
            }

            if (!valid)
            {
                Debug.LogError("Battle card zone validation failed.");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Battle Card Zone Validation",
                    valid ? "Battle card zones passed." : "Battle card zones failed. Check the Console.",
                    "OK");
            }

            return valid;
        }

        private static BattleCardInstance CreateValidationInstance(CardData source, int sequence, CardZone zone)
        {
            string suffix = sequence.ToString("D3");
            CardInstanceIds ids = new(source.CatalogCardId, $"OWN-{suffix}", $"BATTLE-{suffix}");
            return new BattleCardInstance(source, ids, 1, zone);
        }

        private static bool ValidateBattleDeckDraw(bool showDialog)
        {
            Dictionary<string, CardData> cards = FindAllCards()
                .Where(card => !string.IsNullOrWhiteSpace(card.CatalogCardId))
                .ToDictionary(card => card.CatalogCardId, StringComparer.OrdinalIgnoreCase);

            bool hasC01 = cards.TryGetValue("C01", out CardData c01);
            bool hasC05 = cards.TryGetValue("C05", out CardData c05);
            bool valid = hasC01 && hasC05;
            if (!valid)
            {
                Debug.LogError("Battle deck validation requires C01 and C05.");
            }
            else
            {
                List<BattleCardInstance> twelveCards = CreateValidationDeck(c01, c05, 12, 100);
                BattleDeckState deck = new(twelveCards, 12345);
                BattleDeckState sameSeedDeck = new(CreateValidationDeck(c01, c05, 12, 100), 12345);
                valid &= deck.DrawPileOrder.SequenceEqual(sameSeedDeck.DrawPileOrder);
                valid &= deck.DrawStartingHand() == BattleDeckState.StartingHandSize;
                valid &= deck.Zones.Count(CardZone.Hand) == 5 && deck.Zones.Count(CardZone.DrawPile) == 7;

                valid &= !deck.TryDrawAtPlayerTurnStart(out _, out CardDrawFailure firstTurnFailure) &&
                         firstTurnFailure == CardDrawFailure.FirstTurnSkipped &&
                         deck.Zones.Count(CardZone.Hand) == 5;

                valid &= deck.TryDrawAtPlayerTurnStart(out _, out CardDrawFailure secondTurnFailure) &&
                         secondTurnFailure == CardDrawFailure.None &&
                         deck.Zones.Count(CardZone.Hand) == 6;

                BattleDeckState fullHandDeck = new(CreateValidationDeck(c01, c05, 11, 200), 7);
                for (int i = 0; i < BattleCardZoneState.MaximumHandSize; i++)
                {
                    valid &= fullHandDeck.TryDraw(out _, out _);
                }

                string retainedTop = fullHandDeck.DrawPileOrder[0];
                valid &= !fullHandDeck.TryDraw(out _, out CardDrawFailure handFullFailure) &&
                         handFullFailure == CardDrawFailure.HandFull &&
                         fullHandDeck.DrawPileOrder.Count == 1 &&
                         string.Equals(fullHandDeck.DrawPileOrder[0], retainedTop, StringComparison.OrdinalIgnoreCase);

                BattleDeckState recycleDeck = new(CreateValidationDeck(c01, c05, 2, 300), 999);
                valid &= recycleDeck.TryDraw(out BattleCardInstance first, out _);
                valid &= recycleDeck.TryDraw(out BattleCardInstance second, out _);
                valid &= recycleDeck.TryDiscard(first.Ids.BattleCardId, out _);
                valid &= recycleDeck.TryDiscard(second.Ids.BattleCardId, out _);
                valid &= recycleDeck.Zones.Count(CardZone.DrawPile) == 0 &&
                         recycleDeck.Zones.Count(CardZone.Graveyard) == 2;
                valid &= recycleDeck.TryDraw(out _, out CardDrawFailure recycleFailure) &&
                         recycleFailure == CardDrawFailure.None &&
                         recycleDeck.Zones.Count(CardZone.Graveyard) == 0 &&
                         recycleDeck.Zones.Count(CardZone.DrawPile) == 1;
            }

            if (!valid)
            {
                Debug.LogError("Battle deck draw validation failed.");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Battle Deck Draw Validation",
                    valid ? "Battle deck draw passed." : "Battle deck draw failed. Check the Console.",
                    "OK");
            }

            return valid;
        }

        private static List<BattleCardInstance> CreateValidationDeck(
            CardData firstSource,
            CardData secondSource,
            int count,
            int startingSequence)
        {
            List<BattleCardInstance> result = new(count);
            for (int i = 0; i < count; i++)
            {
                CardData source = i % 2 == 0 ? firstSource : secondSource;
                result.Add(CreateValidationInstance(source, startingSequence + i, CardZone.DrawPile));
            }

            return result;
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
