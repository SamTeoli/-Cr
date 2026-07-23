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

            if (!RunDeckSelectionService.TryReplaceDeck(
                    progress,
                    new[] { third.OwnedCardId, first.OwnedCardId },
                    out RunDeckFailure successFailure) ||
                successFailure != RunDeckFailure.None ||
                progress.RunDeck.Count != 2 ||
                progress.RunDeck.Cards[0] != third ||
                progress.RunDeck.Cards[1] != first)
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

        private static CardData FindCard(string cardId)
        {
            string[] guids = AssetDatabase.FindAssets("t:CardData");
            foreach (string guid in guids)
            {
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (card != null && card.CardId == cardId)
                {
                    return card;
                }
            }

            return null;
        }
    }
}
