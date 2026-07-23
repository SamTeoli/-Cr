using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public static class RunDeckSelectionService
    {
        public static bool TryReplaceDeck(
            RunEncounterProgressState progress,
            IEnumerable<string> selectedOwnedCardIds,
            out RunDeckFailure failure)
        {
            if (progress?.RunState == null || progress.OwnedCards == null)
            {
                failure = RunDeckFailure.InvalidDeck;
                return false;
            }

            if (progress.RunState.RunEnded)
            {
                failure = RunDeckFailure.RunEnded;
                return false;
            }

            if (progress.HasActiveEncounter)
            {
                failure = RunDeckFailure.BattleLocked;
                return false;
            }

            if (!TryCreateDeck(
                    progress.OwnedCards,
                    selectedOwnedCardIds,
                    out RunDeckState replacement,
                    out failure))
            {
                return false;
            }

            progress.ReplaceRunDeck(replacement);
            failure = RunDeckFailure.None;
            return true;
        }

        public static bool TryCreateDeck(
            RunOwnedCardState ownedCards,
            IEnumerable<string> selectedOwnedCardIds,
            out RunDeckState runDeck,
            out RunDeckFailure failure)
        {
            runDeck = new RunDeckState();
            if (ownedCards == null || selectedOwnedCardIds == null)
            {
                failure = RunDeckFailure.InvalidDeck;
                return false;
            }

            foreach (string ownedCardId in selectedOwnedCardIds)
            {
                RunCardInstance card = ownedCards.Find(ownedCardId);
                if (card == null)
                {
                    failure = RunDeckFailure.CardNotFound;
                    runDeck = null;
                    return false;
                }

                if (!runDeck.TryAdd(card, out failure))
                {
                    runDeck = null;
                    return false;
                }
            }

            if (runDeck.Count == 0)
            {
                failure = RunDeckFailure.InvalidDeck;
                runDeck = null;
                return false;
            }

            failure = RunDeckFailure.None;
            return true;
        }
    }
}
