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

            if (GUILayout.Button("Validate Card Play And Mana"))
            {
                ValidateCardPlayAndMana(true);
            }

            if (GUILayout.Button("Validate Starting Hand Redraw"))
            {
                ValidateStartingHandRedraw(true);
            }

            if (GUILayout.Button("Validate Battle Turn Flow"))
            {
                ValidateBattleTurnFlow(true);
            }

            if (GUILayout.Button("Validate Battle Event Queue"))
            {
                ValidateBattleEventQueue(true);
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

        private static bool ValidateCardPlayAndMana(bool showDialog)
        {
            Dictionary<string, CardData> cards = FindAllCards()
                .Where(card => !string.IsNullOrWhiteSpace(card.CatalogCardId))
                .ToDictionary(card => card.CatalogCardId, StringComparer.OrdinalIgnoreCase);

            bool hasC01 = cards.TryGetValue("C01", out CardData c01);
            bool hasC05 = cards.TryGetValue("C05", out CardData c05);
            bool hasC08 = cards.TryGetValue("C08", out CardData c08);
            bool hasC11 = cards.TryGetValue("C11", out CardData c11);
            bool valid = hasC01 && hasC05 && hasC08 && hasC11;
            if (!valid)
            {
                Debug.LogError("Card play validation requires C01, C05, C08 and C11.");
            }
            else
            {
                BattleCardPlayState monsterPlay = CreateValidationPlayState(5, 500, c01);
                BattleCardInstance monster = monsterPlay.Deck.Zones.GetCards(CardZone.Hand)[0];
                valid &= monsterPlay.TryPreviewPlay(monster.Ids.BattleCardId, out CardPlayPreview monsterPreview, out _) &&
                         monsterPlay.Mana.CurrentMana == 5 &&
                         monster.Zone == CardZone.Hand;
                valid &= monsterPlay.TryConfirmPlay(monsterPreview, out CardPlayFailure monsterFailure) &&
                         monsterFailure == CardPlayFailure.None &&
                         monster.Zone == CardZone.MonsterField &&
                         monsterPlay.Mana.CurrentMana == 2;

                BattleCardPlayState skillPlay = CreateValidationPlayState(2, 510, c05);
                BattleCardInstance skill = skillPlay.Deck.Zones.GetCards(CardZone.Hand)[0];
                valid &= skillPlay.TryPreviewPlay(skill.Ids.BattleCardId, out CardPlayPreview skillPreview, out _);
                valid &= skillPlay.TryConfirmPlay(skillPreview, out _) &&
                         skill.Zone == CardZone.Graveyard &&
                         skillPlay.Deck.Zones.Count(CardZone.SkillField) == 0 &&
                         skillPlay.Mana.CurrentMana == 1;

                BattleCardPlayState trapPlay = CreateValidationPlayState(1, 520, c08);
                BattleCardInstance trap = trapPlay.Deck.Zones.GetCards(CardZone.Hand)[0];
                valid &= trapPlay.TryPreviewPlay(trap.Ids.BattleCardId, out CardPlayPreview trapPreview, out _);
                valid &= trapPlay.TryConfirmPlay(trapPreview, out _) &&
                         trap.Zone == CardZone.SkillField &&
                         trapPlay.Mana.CurrentMana == 0;

                BattleCardPlayState barrierPlay = CreateValidationPlayState(5, 530, c11, c11);
                List<BattleCardInstance> barriers = barrierPlay.Deck.Zones.GetCards(CardZone.Hand);
                valid &= barrierPlay.TryPreviewPlay(barriers[0].Ids.BattleCardId, out CardPlayPreview barrierPreview, out _);
                valid &= barrierPlay.TryConfirmPlay(barrierPreview, out _);
                int manaBeforeDuplicate = barrierPlay.Mana.CurrentMana;
                valid &= !barrierPlay.TryPreviewPlay(barriers[1].Ids.BattleCardId, out _, out CardPlayFailure duplicateFailure) &&
                         duplicateFailure == CardPlayFailure.DuplicateBarrier &&
                         barrierPlay.Mana.CurrentMana == manaBeforeDuplicate &&
                         barriers[1].Zone == CardZone.Hand;

                BattleCardPlayState insufficient = CreateValidationPlayState(2, 540, c01);
                BattleCardInstance expensive = insufficient.Deck.Zones.GetCards(CardZone.Hand)[0];
                valid &= !insufficient.TryPreviewPlay(expensive.Ids.BattleCardId, out _, out CardPlayFailure manaFailure) &&
                         manaFailure == CardPlayFailure.NotEnoughMana &&
                         insufficient.Mana.CurrentMana == 2 &&
                         expensive.Zone == CardZone.Hand;

                BattleCardPlayState stale = CreateValidationPlayState(5, 550, c01, c01, c01, c01);
                List<BattleCardInstance> monsterHand = stale.Deck.Zones.GetCards(CardZone.Hand);
                BattleCardInstance staleTarget = monsterHand[0];
                valid &= stale.TryPreviewPlay(staleTarget.Ids.BattleCardId, out CardPlayPreview stalePreview, out _);
                for (int i = 1; i <= BattleCardZoneState.MaximumMonsterFieldSize; i++)
                {
                    valid &= stale.Deck.Zones.TryMove(monsterHand[i].Ids.BattleCardId, CardZone.MonsterField, out _);
                }

                valid &= !stale.TryConfirmPlay(stalePreview, out CardPlayFailure staleFailure) &&
                         staleFailure == CardPlayFailure.DestinationFull &&
                         stale.Mana.CurrentMana == 5 &&
                         staleTarget.Zone == CardZone.Hand;

                BattleManaState turnMana = new(BattleManaState.DefaultMaximumMana);
                turnMana.EndPlayerTurn();
                valid &= turnMana.CurrentMana == 0;
                turnMana.StartPlayerTurn();
                valid &= turnMana.CurrentMana == BattleManaState.DefaultMaximumMana;

                CardInstanceIds temporaryIds = new(c05.CatalogCardId, null, "BATTLE-TEMP-PLAY", "INSTANT-TEMP-PLAY");
                BattleCardInstance temporarySkill = new(c05, temporaryIds, 1, CardZone.DrawPile);
                BattleDeckState temporaryDeck = new(new[] { temporarySkill }, 77);
                temporaryDeck.DrawStartingHand();
                BattleCardPlayState temporaryPlay = new(temporaryDeck, 1);
                valid &= temporaryPlay.TryPreviewPlay(temporarySkill.Ids.BattleCardId, out CardPlayPreview temporaryPreview, out _);
                valid &= temporaryPlay.TryConfirmPlay(temporaryPreview, out _) &&
                         temporarySkill.Zone == CardZone.Banished;
            }

            if (!valid)
            {
                Debug.LogError("Card play and mana validation failed.");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Card Play And Mana Validation",
                    valid ? "Card play and mana passed." : "Card play and mana failed. Check the Console.",
                    "OK");
            }

            return valid;
        }

        private static BattleCardPlayState CreateValidationPlayState(
            int maximumMana,
            int startingSequence,
            params CardData[] sources)
        {
            List<BattleCardInstance> instances = new(sources.Length);
            for (int i = 0; i < sources.Length; i++)
            {
                instances.Add(CreateValidationInstance(sources[i], startingSequence + i, CardZone.DrawPile));
            }

            BattleDeckState deck = new(instances, startingSequence);
            deck.DrawStartingHand();
            return new BattleCardPlayState(deck, maximumMana);
        }

        private static bool ValidateStartingHandRedraw(bool showDialog)
        {
            Dictionary<string, CardData> cards = FindAllCards()
                .Where(card => !string.IsNullOrWhiteSpace(card.CatalogCardId))
                .ToDictionary(card => card.CatalogCardId, StringComparer.OrdinalIgnoreCase);

            bool hasC01 = cards.TryGetValue("C01", out CardData c01);
            bool hasC05 = cards.TryGetValue("C05", out CardData c05);
            bool valid = hasC01 && hasC05;
            if (!valid)
            {
                Debug.LogError("Starting hand redraw validation requires C01 and C05.");
            }
            else
            {
                BattleDeckState redrawDeck = new(CreateValidationDeck(c01, c05, 10, 600), 123);
                valid &= redrawDeck.DrawStartingHand() == BattleDeckState.StartingHandSize;
                List<BattleCardInstance> originalHand = redrawDeck.Zones.GetCards(CardZone.Hand);
                List<string> selected = originalHand.Take(2).Select(card => card.Ids.BattleCardId).ToList();
                valid &= redrawDeck.StartingHandRedrawAvailable;
                valid &= redrawDeck.TryRedrawStartingHand(
                    selected,
                    out List<BattleCardInstance> replacements,
                    out StartingHandRedrawFailure redrawFailure) &&
                         redrawFailure == StartingHandRedrawFailure.None &&
                         replacements.Count == 2 &&
                         redrawDeck.Zones.Count(CardZone.Hand) == 5 &&
                         redrawDeck.Zones.Count(CardZone.DrawPile) == 5 &&
                         redrawDeck.Zones.Count(CardZone.RedrawHolding) == 0 &&
                         !redrawDeck.StartingHandRedrawAvailable;

                valid &= selected.All(id => redrawDeck.Zones.Find(id).Zone == CardZone.DrawPile);
                valid &= replacements.All(card => !selected.Contains(card.Ids.BattleCardId));
                valid &= !redrawDeck.TryRedrawStartingHand(
                    selected,
                    out _,
                    out StartingHandRedrawFailure secondFailure) &&
                         secondFailure == StartingHandRedrawFailure.NotAvailable;
                valid &= !redrawDeck.TryDrawAtPlayerTurnStart(out _, out CardDrawFailure firstTurnFailure) &&
                         firstTurnFailure == CardDrawFailure.FirstTurnSkipped;

                BattleDeckState firstDeterministic = new(CreateValidationDeck(c01, c05, 10, 700), 456);
                BattleDeckState secondDeterministic = new(CreateValidationDeck(c01, c05, 10, 700), 456);
                firstDeterministic.DrawStartingHand();
                secondDeterministic.DrawStartingHand();
                List<string> firstSelection = firstDeterministic.Zones.GetCards(CardZone.Hand)
                    .Take(3).Select(card => card.Ids.BattleCardId).ToList();
                List<string> secondSelection = secondDeterministic.Zones.GetCards(CardZone.Hand)
                    .Take(3).Select(card => card.Ids.BattleCardId).ToList();
                valid &= firstDeterministic.TryRedrawStartingHand(firstSelection, out List<BattleCardInstance> firstReplacements, out _);
                valid &= secondDeterministic.TryRedrawStartingHand(secondSelection, out List<BattleCardInstance> secondReplacements, out _);
                valid &= firstReplacements.Select(card => card.Ids.BattleCardId)
                    .SequenceEqual(secondReplacements.Select(card => card.Ids.BattleCardId));
                valid &= firstDeterministic.DrawPileOrder.SequenceEqual(secondDeterministic.DrawPileOrder);

                BattleDeckState zeroSelection = new(CreateValidationDeck(c01, c05, 10, 800), 789);
                zeroSelection.DrawStartingHand();
                valid &= zeroSelection.TryRedrawStartingHand(Array.Empty<string>(), out List<BattleCardInstance> noReplacements, out _) &&
                         noReplacements.Count == 0 &&
                         !zeroSelection.StartingHandRedrawAvailable &&
                         zeroSelection.Zones.Count(CardZone.Hand) == 5;

                BattleDeckState duplicateSelection = new(CreateValidationDeck(c01, c05, 10, 900), 987);
                duplicateSelection.DrawStartingHand();
                string duplicateId = duplicateSelection.Zones.GetCards(CardZone.Hand)[0].Ids.BattleCardId;
                valid &= !duplicateSelection.TryRedrawStartingHand(
                    new[] { duplicateId, duplicateId },
                    out _,
                    out StartingHandRedrawFailure duplicateFailure) &&
                         duplicateFailure == StartingHandRedrawFailure.DuplicateCardId &&
                         duplicateSelection.StartingHandRedrawAvailable &&
                         duplicateSelection.Zones.Count(CardZone.Hand) == 5;

                BattleDeckState insufficientDeck = new(CreateValidationDeck(c01, c05, 5, 1000), 654);
                insufficientDeck.DrawStartingHand();
                string insufficientId = insufficientDeck.Zones.GetCards(CardZone.Hand)[0].Ids.BattleCardId;
                valid &= !insufficientDeck.TryRedrawStartingHand(
                    new[] { insufficientId },
                    out _,
                    out StartingHandRedrawFailure insufficientFailure) &&
                         insufficientFailure == StartingHandRedrawFailure.NotEnoughCards &&
                         insufficientDeck.StartingHandRedrawAvailable &&
                         insufficientDeck.Zones.Count(CardZone.Hand) == 5;
            }

            if (!valid)
            {
                Debug.LogError("Starting hand redraw validation failed.");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Starting Hand Redraw Validation",
                    valid ? "Starting hand redraw passed." : "Starting hand redraw failed. Check the Console.",
                    "OK");
            }

            return valid;
        }

        private static bool ValidateBattleTurnFlow(bool showDialog)
        {
            Dictionary<string, CardData> cards = FindAllCards()
                .Where(card => !string.IsNullOrWhiteSpace(card.CatalogCardId))
                .ToDictionary(card => card.CatalogCardId, StringComparer.OrdinalIgnoreCase);

            bool hasC01 = cards.TryGetValue("C01", out CardData c01);
            bool hasC05 = cards.TryGetValue("C05", out CardData c05);
            bool valid = hasC01 && hasC05;
            if (!valid)
            {
                Debug.LogError("Battle turn validation requires C01 and C05.");
            }
            else
            {
                BattleTurnState turns = CreateValidationTurnState(c01, c05, 12, 1100);
                valid &= turns.Phase == BattleTurnPhase.BattleSetup &&
                         turns.PlayerTurnNumber == 0 &&
                         !turns.CanAcceptPlayerAction;
                valid &= !turns.TryEndPlayerTurn(out BattleTurnFailure setupFailure) &&
                         setupFailure == BattleTurnFailure.InvalidPhase &&
                         turns.Phase == BattleTurnPhase.BattleSetup;

                valid &= turns.TryBeginBattle(out _) &&
                         turns.Phase == BattleTurnPhase.StartingHandRedraw &&
                         turns.CardPlay.Deck.Zones.Count(CardZone.Hand) == 5;
                valid &= turns.TryConfirmStartingHand(
                    Array.Empty<string>(),
                    out List<BattleCardInstance> replacements,
                    out StartingHandRedrawFailure redrawFailure,
                    out BattleTurnFailure firstTurnFailure) &&
                         replacements.Count == 0 &&
                         redrawFailure == StartingHandRedrawFailure.None &&
                         firstTurnFailure == BattleTurnFailure.None &&
                         turns.Phase == BattleTurnPhase.PlayerAction &&
                         turns.PlayerTurnNumber == 1 &&
                         turns.CardPlay.Mana.CurrentMana == BattleManaState.DefaultMaximumMana &&
                         turns.LastTurnDrawFailure == CardDrawFailure.FirstTurnSkipped &&
                         turns.CardPlay.Deck.Zones.Count(CardZone.Hand) == 5;

                valid &= turns.TryBeginPlayerAction(out _) &&
                         turns.Phase == BattleTurnPhase.PlayerActionResolving &&
                         !turns.CanAcceptPlayerAction;
                valid &= !turns.TryEndPlayerTurn(out BattleTurnFailure resolvingFailure) &&
                         resolvingFailure == BattleTurnFailure.InvalidPhase &&
                         turns.CardPlay.Mana.CurrentMana == BattleManaState.DefaultMaximumMana;
                valid &= turns.TryCompletePlayerAction(out _) && turns.CanAcceptPlayerAction;

                valid &= turns.CardPlay.Mana.TrySpend(3) && turns.CardPlay.Mana.CurrentMana == 2;
                valid &= turns.TryEndPlayerTurn(out _) &&
                         turns.Phase == BattleTurnPhase.EnemyTurn &&
                         turns.CardPlay.Mana.CurrentMana == 0 &&
                         turns.CardPlay.Deck.Zones.Count(CardZone.Hand) == 5;
                valid &= turns.TryCompleteEnemyTurn(out _) &&
                         turns.Phase == BattleTurnPhase.PlayerAction &&
                         turns.PlayerTurnNumber == 2 &&
                         turns.CardPlay.Mana.CurrentMana == BattleManaState.DefaultMaximumMana &&
                         turns.LastTurnDrawFailure == CardDrawFailure.None &&
                         turns.CardPlay.Deck.Zones.Count(CardZone.Hand) == 6;

                valid &= !turns.TryConfirmStartingHand(
                    Array.Empty<string>(),
                    out _,
                    out _,
                    out BattleTurnFailure repeatedRedrawFailure) &&
                         repeatedRedrawFailure == BattleTurnFailure.InvalidPhase &&
                         turns.Phase == BattleTurnPhase.PlayerAction;

                BattleTurnState fullHandTurns = CreateValidationTurnState(c01, c05, 15, 1200);
                fullHandTurns.TryBeginBattle(out _);
                fullHandTurns.TryConfirmStartingHand(Array.Empty<string>(), out _, out _, out _);
                while (fullHandTurns.CardPlay.Deck.Zones.Count(CardZone.Hand) < BattleCardZoneState.MaximumHandSize)
                {
                    valid &= fullHandTurns.CardPlay.Deck.TryDraw(out _, out _);
                }

                valid &= fullHandTurns.TryEndPlayerTurn(out _);
                valid &= fullHandTurns.TryCompleteEnemyTurn(out _) &&
                         fullHandTurns.Phase == BattleTurnPhase.PlayerAction &&
                         fullHandTurns.PlayerTurnNumber == 2 &&
                         fullHandTurns.LastTurnDrawFailure == CardDrawFailure.HandFull &&
                         fullHandTurns.CardPlay.Deck.Zones.Count(CardZone.Hand) == BattleCardZoneState.MaximumHandSize;

                BattleTurnState emptyDeckTurns = CreateValidationTurnState(c01, c05, 5, 1300);
                emptyDeckTurns.TryBeginBattle(out _);
                emptyDeckTurns.TryConfirmStartingHand(Array.Empty<string>(), out _, out _, out _);
                emptyDeckTurns.TryEndPlayerTurn(out _);
                valid &= emptyDeckTurns.TryCompleteEnemyTurn(out _) &&
                         emptyDeckTurns.Phase == BattleTurnPhase.PlayerAction &&
                         emptyDeckTurns.PlayerTurnNumber == 2 &&
                         emptyDeckTurns.LastTurnDrawFailure == CardDrawFailure.NoCardsAvailable;
            }

            if (!valid)
            {
                Debug.LogError("Battle turn flow validation failed.");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Battle Turn Flow Validation",
                    valid ? "Battle turn flow passed." : "Battle turn flow failed. Check the Console.",
                    "OK");
            }

            return valid;
        }

        private static BattleTurnState CreateValidationTurnState(
            CardData firstSource,
            CardData secondSource,
            int cardCount,
            int startingSequence)
        {
            BattleDeckState deck = new(
                CreateValidationDeck(firstSource, secondSource, cardCount, startingSequence),
                startingSequence);
            BattleCardPlayState cardPlay = new(deck, BattleManaState.DefaultMaximumMana);
            return new BattleTurnState(cardPlay);
        }

        private static bool ValidateBattleEventQueue(bool showDialog)
        {
            bool valid = true;
            BattleEventLog eventLog = new();
            BattleEventRecord cardPlayed = eventLog.Record(
                BattleEventType.CardPlayed,
                "CardPlayConfirmed",
                "C05",
                "BATTLE-QUEUE-001",
                "ENEMY-001",
                randomState: 123u);
            BattleEventRecord cardMoved = eventLog.Record(
                BattleEventType.CardMoved,
                "SkillResolved",
                "C05",
                "BATTLE-QUEUE-001",
                null,
                parentEventId: cardPlayed.EventId,
                sourceEffectId: "EFFECT-MAIN",
                hasZoneChange: true,
                fromZone: CardZone.SkillField,
                toZone: CardZone.Graveyard);

            valid &= cardPlayed.EventId == "EVENT-000001" &&
                     cardMoved.EventId == "EVENT-000002" &&
                     cardMoved.ParentEventId == cardPlayed.EventId &&
                     cardMoved.HasZoneChange &&
                     cardMoved.FromZone == CardZone.SkillField &&
                     cardMoved.ToZone == CardZone.Graveyard &&
                     cardPlayed.RandomState == 123u &&
                     eventLog.Find(cardMoved.EventId) == cardMoved;

            BattleEffectQueue queue = new();
            BattleEffectCommand pre = CreateValidationEffect(
                "PRE", cardPlayed, EffectProcessingStage.PreModification, true);
            BattleEffectCommand responseFirst = CreateValidationEffect(
                "RESPONSE-1", cardPlayed, EffectProcessingStage.Response, true);
            BattleEffectCommand responseSecond = CreateValidationEffect(
                "RESPONSE-2", cardPlayed, EffectProcessingStage.Response, false);
            BattleEffectCommand main = CreateValidationEffect(
                "MAIN", cardPlayed, EffectProcessingStage.MainEffect, true);
            BattleEffectCommand sourceOptional = CreateValidationEffect(
                "AFTER-SOURCE-OPTIONAL",
                cardPlayed,
                EffectProcessingStage.Aftermath,
                false,
                AftermathEffectPriority.SourceCard);
            BattleEffectCommand systemRequired = CreateValidationEffect(
                "AFTER-SYSTEM",
                cardPlayed,
                EffectProcessingStage.Aftermath,
                true,
                AftermathEffectPriority.SystemRequired);
            BattleEffectCommand sourceRequired = CreateValidationEffect(
                "AFTER-SOURCE-REQUIRED",
                cardPlayed,
                EffectProcessingStage.Aftermath,
                true,
                AftermathEffectPriority.SourceCard);
            BattleEffectCommand cleanup = CreateValidationEffect(
                "AFTER-CLEANUP",
                cardPlayed,
                EffectProcessingStage.Aftermath,
                true,
                AftermathEffectPriority.Cleanup);

            valid &= queue.TryRegister(pre, cardPlayed, out _);
            valid &= queue.TryRegister(responseFirst, cardPlayed, out _);
            valid &= queue.TryRegister(responseSecond, cardPlayed, out _);
            valid &= queue.TryRegister(main, cardPlayed, out _);
            valid &= queue.TryRegister(sourceOptional, cardPlayed, out _);
            valid &= queue.TryRegister(systemRequired, cardPlayed, out _);
            valid &= queue.TryRegister(sourceRequired, cardPlayed, out _);
            valid &= queue.TryRegister(cleanup, cardPlayed, out _);

            BattleEffectCommand duplicate = CreateValidationEffect(
                "PRE", cardPlayed, EffectProcessingStage.PreModification, true);
            valid &= !queue.TryRegister(duplicate, cardPlayed, out EffectQueueFailure duplicateFailure) &&
                     duplicateFailure == EffectQueueFailure.DuplicateForEvent &&
                     queue.Count == 8;

            string[] expectedOrder =
            {
                "PRE",
                "RESPONSE-2",
                "RESPONSE-1",
                "MAIN",
                "AFTER-SYSTEM",
                "AFTER-SOURCE-REQUIRED",
                "AFTER-SOURCE-OPTIONAL",
                "AFTER-CLEANUP"
            };
            for (int i = 0; i < expectedOrder.Length; i++)
            {
                valid &= queue.TryDequeue(out BattleEffectCommand next) &&
                         next.EffectId == expectedOrder[i];
            }

            valid &= !queue.TryDequeue(out _) && queue.Count == 0;
            valid &= !queue.TryRegister(duplicate, cardPlayed, out EffectQueueFailure processedDuplicateFailure) &&
                     processedDuplicateFailure == EffectQueueFailure.DuplicateForEvent;

            BattleEventRecord loopEvent = eventLog.Record(
                BattleEventType.CardMoved,
                "EffectCreatedSameEvent",
                "C05",
                "BATTLE-QUEUE-001",
                null,
                sourceEffectId: "EFFECT-LOOP");
            BattleEffectCommand loop = new(
                "EFFECT-LOOP",
                "C05",
                loopEvent.EventId,
                EffectProcessingStage.Aftermath,
                true,
                BattleEventType.CardMoved);
            valid &= !queue.TryRegister(loop, loopEvent, out EffectQueueFailure loopFailure) &&
                     loopFailure == EffectQueueFailure.SelfRepeatBlocked;

            BattleEffectQueue limitedRepeatQueue = new();
            BattleEffectCommand repeatOne = new(
                "LIMITED-REPEAT", "C05", cardMoved.EventId, EffectProcessingStage.Aftermath,
                true, BattleEventType.CardMoved, allowRepeatedTrigger: true, maximumRegistrationsPerEvent: 2);
            BattleEffectCommand repeatTwo = new(
                "LIMITED-REPEAT", "C05", cardMoved.EventId, EffectProcessingStage.Aftermath,
                true, BattleEventType.CardMoved, allowRepeatedTrigger: true, maximumRegistrationsPerEvent: 2);
            BattleEffectCommand repeatThree = new(
                "LIMITED-REPEAT", "C05", cardMoved.EventId, EffectProcessingStage.Aftermath,
                true, BattleEventType.CardMoved, allowRepeatedTrigger: true, maximumRegistrationsPerEvent: 2);
            valid &= limitedRepeatQueue.TryRegister(repeatOne, cardMoved, out _);
            valid &= limitedRepeatQueue.TryRegister(repeatTwo, cardMoved, out _);
            valid &= !limitedRepeatQueue.TryRegister(repeatThree, cardMoved, out EffectQueueFailure repeatFailure) &&
                     repeatFailure == EffectQueueFailure.DuplicateForEvent;

            if (!valid)
            {
                Debug.LogError("Battle event queue validation failed.");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Battle Event Queue Validation",
                    valid ? "Battle event queue passed." : "Battle event queue failed. Check the Console.",
                    "OK");
            }

            return valid;
        }

        private static BattleEffectCommand CreateValidationEffect(
            string effectId,
            BattleEventRecord sourceEvent,
            EffectProcessingStage stage,
            bool required,
            AftermathEffectPriority priority = AftermathEffectPriority.SourceCard)
        {
            return new BattleEffectCommand(
                effectId,
                "C05",
                sourceEvent.EventId,
                stage,
                required,
                sourceEvent.EventType,
                priority);
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
