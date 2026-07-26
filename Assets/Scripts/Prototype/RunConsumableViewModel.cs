using System;
using System.Collections.Generic;
using System.Linq;

namespace HaveABreak.Cards
{
    public sealed class RunConsumableInventoryOption
    {
        internal RunConsumableInventoryOption(
            string itemId,
            ConsumableData item,
            int count)
        {
            ItemId = itemId;
            Item = item;
            Count = Math.Max(0, count);
        }

        public string ItemId { get; }
        public ConsumableData Item { get; }
        public string DisplayName => Item?.DisplayName ?? ItemId;
        public int Count { get; }
        public string DisplayLabel => Count > 1
            ? $"{DisplayName} ×{Count}"
            : DisplayName;
    }

    public sealed class RunEnchantHammerTargetOption
    {
        internal RunEnchantHammerTargetOption(
            RunCardInstance card,
            bool isSelected,
            bool hasHammer,
            bool runEnded)
        {
            Card = card ?? throw new ArgumentNullException(nameof(card));
            IsSelected = isSelected;
            HasHammer = hasHammer;
            RunEnded = runEnded;
        }

        public RunCardInstance Card { get; }
        public string OwnedCardId => Card.OwnedCardId;
        public string DisplayName => Card.Card.DisplayName;
        public int SlotCount => Card.Enchants.SlotCount;
        public int MaximumSlotCount => RunCardEnchantState.MaximumSlotCount;
        public bool IsSelected { get; }
        public bool HasHammer { get; }
        public bool RunEnded { get; }
        public bool CanUse => HasHammer && !RunEnded &&
                              SlotCount < MaximumSlotCount;
        public string DisplayLabel =>
            $"{DisplayName} · 슬롯 {SlotCount}/{MaximumSlotCount}";
        public string BlockReason => !HasHammer
            ? "인첸트 망치 없음"
            : RunEnded
                ? "종료된 런에서는 사용할 수 없음"
                : SlotCount >= MaximumSlotCount
                    ? "최대 슬롯 도달"
                    : null;
    }

    public sealed class RunMutationScrollOption
    {
        internal RunMutationScrollOption(
            RunCardInstance card,
            RunEnchantSlot slot,
            EnchantData replacement,
            bool hasScroll,
            bool runEnded)
        {
            Card = card ?? throw new ArgumentNullException(nameof(card));
            Slot = slot ?? throw new ArgumentNullException(nameof(slot));
            Replacement = replacement ??
                          throw new ArgumentNullException(nameof(replacement));
            HasScroll = hasScroll;
            RunEnded = runEnded;
        }

        public RunCardInstance Card { get; }
        public RunEnchantSlot Slot { get; }
        public EnchantData OriginalEnchant => Slot.Enchant;
        public EnchantData Replacement { get; }
        public string OwnedCardId => Card.OwnedCardId;
        public int SlotIndex => Slot.SlotIndex;
        public string ReplacementDefinitionId => Replacement.DefinitionId;
        public bool HasScroll { get; }
        public bool RunEnded { get; }
        public bool CanUse => HasScroll && !RunEnded &&
                              !Slot.IsEmpty && OriginalEnchant != null;
        public string DisplayText =>
            $"{Card.Card.DisplayName} · {OriginalEnchant?.DisplayName} → " +
            Replacement.DisplayName;
        public string BlockReason => !HasScroll
            ? "변이 주문서 없음"
            : RunEnded
                ? "종료된 런에서는 사용할 수 없음"
                : CanUse
                    ? null
                    : "변이할 인첸트 없음";
    }

    public sealed class RunConsumableViewModel
    {
        private string selectedHammerOwnedCardId;

        public string SelectedHammerOwnedCardId => selectedHammerOwnedCardId;

        public RunConsumableInventoryOption[] CreateInventoryOptions(
            RunEncounterProgressState progress)
        {
            IReadOnlyList<string> itemIds =
                progress?.RunState?.ConsumableItemIds;
            if (itemIds == null || itemIds.Count == 0)
            {
                return Array.Empty<RunConsumableInventoryOption>();
            }

            List<RunConsumableInventoryOption> options = new();
            HashSet<string> added = new(StringComparer.OrdinalIgnoreCase);
            foreach (string rawId in itemIds)
            {
                if (string.IsNullOrWhiteSpace(rawId))
                {
                    continue;
                }

                string itemId = rawId.Trim();
                if (!added.Add(itemId))
                {
                    continue;
                }

                int count = itemIds.Count(value => string.Equals(
                    value,
                    itemId,
                    StringComparison.OrdinalIgnoreCase));
                options.Add(new RunConsumableInventoryOption(
                    itemId,
                    PrototypeConsumableCatalog.Find(itemId),
                    count));
            }

            return options.ToArray();
        }

        public RunEnchantHammerTargetOption[] CreateHammerTargets(
            RunEncounterProgressState progress,
            string preferredOwnedCardId = null)
        {
            if (progress?.OwnedCards == null ||
                progress.OwnedCards.Count == 0 ||
                !HasItem(progress, PrototypeConsumableCatalog.EnchantHammer))
            {
                selectedHammerOwnedCardId = null;
                return Array.Empty<RunEnchantHammerTargetOption>();
            }

            IReadOnlyList<RunCardInstance> cards = progress.OwnedCards.Cards;
            EnsureHammerSelection(cards, preferredOwnedCardId);
            bool runEnded = progress.RunState.RunEnded;
            return cards
                .Where(card => card != null)
                .Select(card => new RunEnchantHammerTargetOption(
                    card,
                    string.Equals(
                        card.OwnedCardId,
                        selectedHammerOwnedCardId,
                        StringComparison.OrdinalIgnoreCase),
                    true,
                    runEnded))
                .ToArray();
        }

        public RunEnchantHammerTargetOption SelectedHammerTarget(
            RunEncounterProgressState progress,
            string preferredOwnedCardId = null)
        {
            return CreateHammerTargets(progress, preferredOwnedCardId)
                .FirstOrDefault(option => option.IsSelected);
        }

        public bool SelectHammerTarget(
            RunEncounterProgressState progress,
            string ownedCardId)
        {
            if (string.IsNullOrWhiteSpace(ownedCardId))
            {
                return false;
            }

            RunEnchantHammerTargetOption option = CreateHammerTargets(progress)
                .FirstOrDefault(value => string.Equals(
                    value.OwnedCardId,
                    ownedCardId.Trim(),
                    StringComparison.OrdinalIgnoreCase));
            if (option == null)
            {
                return false;
            }

            selectedHammerOwnedCardId = option.OwnedCardId;
            return true;
        }

        public RunEnchantHammerTargetOption CycleHammerTarget(
            RunEncounterProgressState progress,
            string preferredOwnedCardId = null)
        {
            RunEnchantHammerTargetOption[] options =
                CreateHammerTargets(progress, preferredOwnedCardId);
            if (options.Length == 0)
            {
                return null;
            }

            int selectedIndex = Array.FindIndex(
                options,
                option => option.IsSelected);
            int nextIndex = (selectedIndex + 1 + options.Length) % options.Length;
            selectedHammerOwnedCardId = options[nextIndex].OwnedCardId;
            return new RunEnchantHammerTargetOption(
                options[nextIndex].Card,
                true,
                options[nextIndex].HasHammer,
                options[nextIndex].RunEnded);
        }

        public bool TryUseEnchantHammer(
            RunEncounterProgressState progress,
            string preferredOwnedCardId,
            out RunEnchantHammerTargetOption used,
            out string result,
            out PrototypeConsumableFailure failure)
        {
            used = SelectedHammerTarget(progress, preferredOwnedCardId);
            result = null;
            failure = default;
            if (used == null || !used.CanUse)
            {
                return false;
            }

            if (!PrototypeConsumableService.TryUseEnchantHammer(
                    progress,
                    used.OwnedCardId,
                    out failure))
            {
                return false;
            }

            selectedHammerOwnedCardId = used.OwnedCardId;
            used = new RunEnchantHammerTargetOption(
                used.Card,
                true,
                HasItem(progress, PrototypeConsumableCatalog.EnchantHammer),
                progress.RunState.RunEnded);
            result = "인첸트 슬롯을 1칸 늘렸습니다.";
            return true;
        }

        public RunMutationScrollOption[] CreateMutationOptions(
            RunEncounterProgressState progress,
            IEnumerable<EnchantData> enchantData)
        {
            if (progress?.OwnedCards == null || enchantData == null ||
                !HasItem(progress, PrototypeConsumableCatalog.MutationScroll))
            {
                return Array.Empty<RunMutationScrollOption>();
            }

            bool runEnded = progress.RunState.RunEnded;
            EnchantData[] replacements = enchantData
                .Where(enchant => enchant != null)
                .ToArray();
            List<RunMutationScrollOption> options = new();
            foreach (RunCardInstance card in progress.OwnedCards.Cards)
            {
                if (card?.Enchants == null)
                {
                    continue;
                }

                foreach (RunEnchantSlot slot in card.Enchants.Slots)
                {
                    if (slot == null || slot.IsEmpty || slot.Enchant == null)
                    {
                        continue;
                    }

                    foreach (EnchantData replacement in replacements)
                    {
                        if (replacement.MatchesDefinition(slot.Enchant) ||
                            !replacement.IsCompatible(card.Card.CardType))
                        {
                            continue;
                        }

                        options.Add(new RunMutationScrollOption(
                            card,
                            slot,
                            replacement,
                            true,
                            runEnded));
                    }
                }
            }

            return options.ToArray();
        }

        public bool TryUseMutationScroll(
            RunEncounterProgressState progress,
            IEnumerable<EnchantData> enchantData,
            string ownedCardId,
            int slotIndex,
            string replacementDefinitionId,
            out RunMutationScrollOption used,
            out string result,
            out EnchantAttachmentFailure attachmentFailure,
            out PrototypeConsumableFailure failure)
        {
            used = null;
            result = null;
            attachmentFailure = default;
            failure = default;
            if (string.IsNullOrWhiteSpace(ownedCardId) ||
                string.IsNullOrWhiteSpace(replacementDefinitionId))
            {
                return false;
            }

            used = CreateMutationOptions(progress, enchantData)
                .FirstOrDefault(option =>
                    option.SlotIndex == slotIndex &&
                    string.Equals(
                        option.OwnedCardId,
                        ownedCardId.Trim(),
                        StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(
                        option.ReplacementDefinitionId,
                        replacementDefinitionId.Trim(),
                        StringComparison.OrdinalIgnoreCase));
            if (used == null || !used.CanUse)
            {
                return false;
            }

            if (!PrototypeConsumableService.TryUseMutationScroll(
                    progress,
                    used.OwnedCardId,
                    used.SlotIndex,
                    used.Replacement,
                    out attachmentFailure,
                    out failure))
            {
                return false;
            }

            result = $"{used.Replacement.DisplayName}(으)로 변이했습니다.";
            return true;
        }

        public void Reset()
        {
            selectedHammerOwnedCardId = null;
        }

        private static bool HasItem(
            RunEncounterProgressState progress,
            string itemId)
        {
            return progress?.RunState?.ConsumableItemIds != null &&
                   progress.RunState.ConsumableItemIds.Any(value =>
                       string.Equals(
                           value,
                           itemId,
                           StringComparison.OrdinalIgnoreCase));
        }

        private void EnsureHammerSelection(
            IReadOnlyList<RunCardInstance> cards,
            string preferredOwnedCardId)
        {
            RunCardInstance preferred = cards.FirstOrDefault(card =>
                card != null && string.Equals(
                    card.OwnedCardId,
                    preferredOwnedCardId,
                    StringComparison.OrdinalIgnoreCase));
            if (preferred != null)
            {
                selectedHammerOwnedCardId = preferred.OwnedCardId;
                return;
            }

            RunCardInstance current = cards.FirstOrDefault(card =>
                card != null && string.Equals(
                    card.OwnedCardId,
                    selectedHammerOwnedCardId,
                    StringComparison.OrdinalIgnoreCase));
            if (current != null)
            {
                selectedHammerOwnedCardId = current.OwnedCardId;
                return;
            }

            selectedHammerOwnedCardId =
                cards.FirstOrDefault(card => card != null)?.OwnedCardId;
        }
    }
}
