using System;
using System.Collections.Generic;
using System.Linq;

namespace HaveABreak.Cards
{
    public sealed class RunRestUpgradeCardOption
    {
        internal RunRestUpgradeCardOption(
            RunCardInstance card,
            bool isSelected)
        {
            Card = card ?? throw new ArgumentNullException(nameof(card));
            IsSelected = isSelected;
        }

        public RunCardInstance Card { get; }
        public string OwnedCardId => Card.OwnedCardId;
        public string DisplayName => Card.Card.DisplayName;
        public int CurrentLevel => Card.CurrentLevel;
        public bool IsSelected { get; }
        public string DisplayLabel => $"{DisplayName} · 레벨 {CurrentLevel}";
    }

    public sealed class RunRestUpgradeViewModel
    {
        private string selectedOwnedCardId;

        public string SelectedOwnedCardId => selectedOwnedCardId;

        public string RestButtonLabel(RestUpgradeConfig rules)
        {
            return rules == null
                ? "회복"
                : $"최대 HP의 {rules.HealingRatio * 100f:0.#}% 회복";
        }

        public string UpgradeButtonLabel(RestUpgradeConfig rules)
        {
            return rules == null
                ? "선택 카드 강화"
                : $"선택 카드 {rules.UpgradeLevelIncrease}레벨 강화";
        }

        public RunRestUpgradeCardOption[] CreateCardOptions(
            RunCampaignState campaign,
            RunEncounterProgressState progress,
            string preferredOwnedCardId = null)
        {
            if (!IsAvailable(campaign) || progress?.OwnedCards == null)
            {
                return Array.Empty<RunRestUpgradeCardOption>();
            }

            IReadOnlyList<RunCardInstance> cards = progress.OwnedCards.Cards;
            EnsureSelection(cards, preferredOwnedCardId);
            return cards
                .Where(card => card != null)
                .Select(card => new RunRestUpgradeCardOption(
                    card,
                    string.Equals(
                        card.OwnedCardId,
                        selectedOwnedCardId,
                        StringComparison.OrdinalIgnoreCase)))
                .ToArray();
        }

        public RunRestUpgradeCardOption SelectedCard(
            RunCampaignState campaign,
            RunEncounterProgressState progress,
            string preferredOwnedCardId = null)
        {
            return CreateCardOptions(campaign, progress, preferredOwnedCardId)
                .FirstOrDefault(option => option.IsSelected);
        }

        public bool SelectCard(
            RunCampaignState campaign,
            RunEncounterProgressState progress,
            string ownedCardId)
        {
            if (string.IsNullOrWhiteSpace(ownedCardId))
            {
                return false;
            }

            RunRestUpgradeCardOption option = CreateCardOptions(campaign, progress)
                .FirstOrDefault(value => string.Equals(
                    value.OwnedCardId,
                    ownedCardId.Trim(),
                    StringComparison.OrdinalIgnoreCase));
            if (option == null)
            {
                return false;
            }

            selectedOwnedCardId = option.OwnedCardId;
            return true;
        }

        public RunRestUpgradeCardOption CycleCard(
            RunCampaignState campaign,
            RunEncounterProgressState progress,
            string preferredOwnedCardId = null)
        {
            RunRestUpgradeCardOption[] options =
                CreateCardOptions(campaign, progress, preferredOwnedCardId);
            if (options.Length == 0)
            {
                return null;
            }

            int selectedIndex = Array.FindIndex(options, option => option.IsSelected);
            int nextIndex = (selectedIndex + 1 + options.Length) % options.Length;
            selectedOwnedCardId = options[nextIndex].OwnedCardId;
            return new RunRestUpgradeCardOption(options[nextIndex].Card, true);
        }

        public bool TryRest(
            RunCampaignState campaign,
            RunBattleState run,
            RestUpgradeConfig rules,
            out int healed,
            out string result,
            out RunCampaignFailure failure)
        {
            healed = 0;
            result = null;
            if (!IsAvailable(campaign) || run == null || rules == null)
            {
                failure = default;
                return false;
            }

            if (!RunCampaignService.TryRest(
                    campaign,
                    run,
                    rules,
                    out healed,
                    out failure))
            {
                return false;
            }

            result = $"HP를 {healed} 회복했습니다.";
            Reset();
            return true;
        }

        public bool TryUpgrade(
            RunCampaignState campaign,
            RunEncounterProgressState progress,
            RestUpgradeConfig rules,
            string preferredOwnedCardId,
            out RunRestUpgradeCardOption upgraded,
            out string result,
            out RunCampaignFailure failure)
        {
            upgraded = SelectedCard(campaign, progress, preferredOwnedCardId);
            result = null;
            if (upgraded == null || rules == null)
            {
                failure = default;
                return false;
            }

            if (!RunCampaignService.TryUpgrade(
                    campaign,
                    progress,
                    upgraded.OwnedCardId,
                    rules,
                    out failure))
            {
                return false;
            }

            upgraded = new RunRestUpgradeCardOption(upgraded.Card, true);
            result = $"{upgraded.DisplayName} 카드를 " +
                     $"{rules.UpgradeLevelIncrease}레벨 강화했습니다.";
            Reset();
            return true;
        }

        public void Reset()
        {
            selectedOwnedCardId = null;
        }

        private static bool IsAvailable(RunCampaignState campaign)
        {
            return campaign != null &&
                   campaign.Phase == RunCampaignPhase.NodeResolution &&
                   campaign.ActiveNode?.NodeType == RunNodeType.RestOrUpgrade;
        }

        private void EnsureSelection(
            IReadOnlyList<RunCardInstance> cards,
            string preferredOwnedCardId)
        {
            if (cards == null || cards.Count == 0)
            {
                selectedOwnedCardId = null;
                return;
            }

            RunCardInstance preferred = cards.FirstOrDefault(card =>
                card != null && string.Equals(
                    card.OwnedCardId,
                    preferredOwnedCardId,
                    StringComparison.OrdinalIgnoreCase));
            if (preferred != null)
            {
                selectedOwnedCardId = preferred.OwnedCardId;
                return;
            }

            RunCardInstance current = cards.FirstOrDefault(card =>
                card != null && string.Equals(
                    card.OwnedCardId,
                    selectedOwnedCardId,
                    StringComparison.OrdinalIgnoreCase));
            if (current != null)
            {
                selectedOwnedCardId = current.OwnedCardId;
                return;
            }

            selectedOwnedCardId = cards.FirstOrDefault(card => card != null)?.OwnedCardId;
        }
    }
}
