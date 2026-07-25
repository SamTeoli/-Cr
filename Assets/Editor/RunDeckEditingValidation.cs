using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class RunDeckEditingValidation
    {
        [MenuItem("Have a Break/Validate Run Deck Editing")]
        private static void ValidateFromMenu()
        {
            Debug.Log(Validate()
                ? "Run deck editing passed."
                : "Run deck editing failed.");
        }

        internal static bool Validate()
        {
            CardData c01 = FindCard(TestContentIds.C01);
            CardData c02 = FindCard(TestContentIds.C02);
            CardData c03 = FindCard(TestContentIds.C03);
            if (c01 == null || c02 == null || c03 == null)
            {
                return false;
            }

            RunCardInstance first = new(c01, "OWNED-DECK-C01", 2);
            RunCardInstance second = new(c02, "OWNED-DECK-C02", 3);
            RunCardInstance third = new(c03, "OWNED-DECK-C03", 4);
            RunOwnedCardState owned = new();
            if (!owned.TryAdd(first, out _) ||
                !owned.TryAdd(second, out _) ||
                !owned.TryAdd(third, out _) ||
                !RunDeckSelectionService.TryCreateDeck(
                    owned,
                    new[] { first.OwnedCardId, second.OwnedCardId },
                    out RunDeckState initial,
                    out _))
            {
                return false;
            }

            RunBattleState run = new(30, 30, 0);
            RunEncounterProgressState progress = new(
                run, owned, initial, new PlayerPermanentRewardState(),
                System.Array.Empty<string>(), 0);
            if (progress.OwnedCards.Count != 3 ||
                progress.RunDeck.Count != 2 ||
                progress.RunDeck.Cards[0] != first ||
                progress.RunDeck.Cards[1] != second ||
                progress.OwnedCards.Find(third.OwnedCardId) != third ||
                !ValidateSelectionViewModel(
                    progress, first, second, third))
            {
                return false;
            }

            RunDeckState successfulDeck = progress.RunDeck;
            bool missingRejected = !RunDeckSelectionService.TryReplaceDeck(
                progress, new[] { "OWNED-DECK-MISSING" }, out RunDeckFailure missingFailure);
            bool duplicateRejected = !RunDeckSelectionService.TryReplaceDeck(
                progress,
                new[] { first.OwnedCardId, first.OwnedCardId },
                out RunDeckFailure duplicateFailure);
            bool emptyRejected = !RunDeckSelectionService.TryReplaceDeck(
                progress, System.Array.Empty<string>(), out RunDeckFailure emptyFailure);

            RunEncounterProgressState endedProgress = new(
                new RunBattleState(30, 30, 0, runEnded: true),
                owned,
                successfulDeck,
                new PlayerPermanentRewardState(),
                System.Array.Empty<string>(),
                0);
            bool endedRejected = !RunDeckSelectionService.TryReplaceDeck(
                endedProgress,
                new[] { second.OwnedCardId },
                out RunDeckFailure endedFailure);

            return missingRejected && missingFailure == RunDeckFailure.CardNotFound &&
                   duplicateRejected && duplicateFailure == RunDeckFailure.DuplicateOwnedCardId &&
                   emptyRejected && emptyFailure == RunDeckFailure.InvalidDeck &&
                   endedRejected && endedFailure == RunDeckFailure.RunEnded &&
                   endedProgress.RunDeck == successfulDeck &&
                   progress.RunDeck == successfulDeck;
        }

        private static bool ValidateSelectionViewModel(
            RunEncounterProgressState progress,
            RunCardInstance first,
            RunCardInstance second,
            RunCardInstance third)
        {
            RunDeckSelectionViewModel selection = new();
            if (selection.Toggle(first.OwnedCardId))
            {
                return false;
            }

            selection.OpenFromDeck(progress.RunDeck);
            RunDeckSelectionOption[] initialOptions =
                selection.CreateOptions(progress.OwnedCards);
            if (!selection.IsOpen || selection.SelectedCount != 2 ||
                initialOptions.Length != 3 ||
                !initialOptions[0].IsSelected ||
                initialOptions[0].SelectionOrder != 1 ||
                !initialOptions[1].IsSelected ||
                initialOptions[1].SelectionOrder != 2 ||
                initialOptions[2].IsSelected ||
                initialOptions[2].SelectionOrder != 0 ||
                initialOptions[0].DisplayLabel !=
                $"[편성] {first.Card.DisplayName} · Lv.{first.CurrentLevel}")
            {
                return false;
            }

            if (!selection.Toggle(second.OwnedCardId) ||
                !selection.Toggle(third.OwnedCardId) ||
                selection.SelectedCount != 2 ||
                selection.SelectedOwnedCardIds[0] != first.OwnedCardId ||
                selection.SelectedOwnedCardIds[1] != third.OwnedCardId ||
                !selection.TryApply(progress, out RunDeckFailure applyFailure) ||
                applyFailure != RunDeckFailure.None ||
                selection.IsOpen || selection.SelectedCount != 0 ||
                progress.RunDeck.Count != 2 ||
                progress.RunDeck.Cards[0] != first ||
                progress.RunDeck.Cards[1] != third)
            {
                return false;
            }

            selection.OpenWithAllOwnedCards(progress.OwnedCards);
            RunDeckSelectionOption[] allOptions =
                selection.CreateOptions(progress.OwnedCards);
            if (!selection.IsOpen || selection.SelectedCount != 3 ||
                allOptions.Length != 3 ||
                !allOptions[0].IsSelected || allOptions[0].SelectionOrder != 1 ||
                !allOptions[1].IsSelected || allOptions[1].SelectionOrder != 2 ||
                !allOptions[2].IsSelected || allOptions[2].SelectionOrder != 3 ||
                !selection.TryCreateDeck(
                    progress.OwnedCards,
                    out RunDeckState allCardsDeck,
                    out RunDeckFailure createFailure) ||
                createFailure != RunDeckFailure.None ||
                allCardsDeck.Count != 3)
            {
                return false;
            }

            selection.Close();
            return !selection.IsOpen && selection.SelectedCount == 0 &&
                   selection.CreateOptions(null).Length == 0;
        }

        private static CardData FindCard(string cardId)
        {
            string[] guids = AssetDatabase.FindAssets("t:CardData");
            foreach (string guid in guids)
            {
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (card != null && card.CatalogCardId == cardId)
                {
                    return card;
                }
            }

            return null;
        }
    }
}
