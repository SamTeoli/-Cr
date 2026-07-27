using System;
using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public sealed class RunDeckSelectionOption
    {
        internal RunDeckSelectionOption(
            RunCardInstance card,
            bool isSelected,
            int selectionOrder)
        {
            Card = card ?? throw new ArgumentNullException(nameof(card));
            IsSelected = isSelected;
            SelectionOrder = selectionOrder;
        }

        public RunCardInstance Card { get; }
        public string OwnedCardId => Card.OwnedCardId;
        public string DisplayName => Card.Card.DisplayName;
        public int CurrentLevel => Card.CurrentLevel;
        public bool IsSelected { get; }
        public int SelectionOrder { get; }
        public string DisplayLabel =>
            $"{(IsSelected ? $"[순서 {SelectionOrder}]" : "[보유]")} " +
            $"{DisplayName} · Lv.{CurrentLevel}";
    }

    public sealed class RunDeckSelectionViewModel
    {
        private readonly List<string> selectedOwnedCardIds = new();

        public bool IsOpen { get; private set; }
        public int SelectedCount => selectedOwnedCardIds.Count;
        public IReadOnlyList<string> SelectedOwnedCardIds =>
            selectedOwnedCardIds.ToArray();

        public void OpenFromDeck(RunDeckState deck)
        {
            Open(deck?.Cards);
        }

        public void OpenWithAllOwnedCards(RunOwnedCardState ownedCards)
        {
            Open(ownedCards?.Cards);
        }

        public void Close()
        {
            selectedOwnedCardIds.Clear();
            IsOpen = false;
        }

        public bool IsSelected(string ownedCardId)
        {
            return IndexOfSelected(ownedCardId) >= 0;
        }

        public bool Toggle(string ownedCardId)
        {
            if (!IsOpen || string.IsNullOrWhiteSpace(ownedCardId))
            {
                return false;
            }

            string normalized = ownedCardId.Trim();
            int selectedIndex = IndexOfSelected(normalized);
            if (selectedIndex >= 0)
            {
                selectedOwnedCardIds.RemoveAt(selectedIndex);
            }
            else
            {
                selectedOwnedCardIds.Add(normalized);
            }

            return true;
        }

        public RunDeckSelectionOption[] CreateOptions(
            RunOwnedCardState ownedCards)
        {
            if (ownedCards == null)
            {
                return Array.Empty<RunDeckSelectionOption>();
            }

            List<RunDeckSelectionOption> options = new();
            foreach (RunCardInstance card in ownedCards.Cards)
            {
                if (card == null)
                {
                    continue;
                }

                int selectedIndex = IndexOfSelected(card.OwnedCardId);
                options.Add(new RunDeckSelectionOption(
                    card,
                    selectedIndex >= 0,
                    selectedIndex >= 0 ? selectedIndex + 1 : 0));
            }

            return options.ToArray();
        }

        public bool TryCreateDeck(
            RunOwnedCardState ownedCards,
            out RunDeckState deck,
            out RunDeckFailure failure)
        {
            return RunDeckSelectionService.TryCreateDeck(
                ownedCards,
                selectedOwnedCardIds.ToArray(),
                out deck,
                out failure);
        }

        public bool TryApply(
            RunEncounterProgressState progress,
            out RunDeckFailure failure)
        {
            if (!IsOpen)
            {
                failure = RunDeckFailure.InvalidDeck;
                return false;
            }

            if (!RunDeckSelectionService.TryReplaceDeck(
                    progress,
                    selectedOwnedCardIds.ToArray(),
                    out failure))
            {
                return false;
            }

            Close();
            return true;
        }

        private void Open(IEnumerable<RunCardInstance> cards)
        {
            selectedOwnedCardIds.Clear();
            if (cards != null)
            {
                foreach (RunCardInstance card in cards)
                {
                    AddSelection(card?.OwnedCardId);
                }
            }

            IsOpen = true;
        }

        private void AddSelection(string ownedCardId)
        {
            if (string.IsNullOrWhiteSpace(ownedCardId))
            {
                return;
            }

            string normalized = ownedCardId.Trim();
            if (IndexOfSelected(normalized) < 0)
            {
                selectedOwnedCardIds.Add(normalized);
            }
        }

        private int IndexOfSelected(string ownedCardId)
        {
            if (string.IsNullOrWhiteSpace(ownedCardId))
            {
                return -1;
            }

            string normalized = ownedCardId.Trim();
            return selectedOwnedCardIds.FindIndex(value =>
                string.Equals(
                    value,
                    normalized,
                    StringComparison.OrdinalIgnoreCase));
        }
    }
}
