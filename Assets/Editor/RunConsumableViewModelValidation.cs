using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class RunConsumableViewModelValidation
    {
        [MenuItem("Have a Break/Validate Run Consumable ViewModel")]
        private static void ValidateFromMenu()
        {
            Debug.Log(Validate()
                ? "Run consumable ViewModel passed."
                : "Run consumable ViewModel failed.");
        }

        internal static bool Validate()
        {
            CardData card = AssetDatabase.FindAssets("t:CardData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<CardData>)
                .FirstOrDefault(value => value != null &&
                    value.CatalogCardId == TestContentIds.C01);
            EnchantData original = CreateEnchant(
                "VALIDATION-RUN-CONSUMABLE-A",
                CardType.Monster);
            EnchantData replacement = CreateEnchant(
                "VALIDATION-RUN-CONSUMABLE-B",
                CardType.Monster);
            try
            {
                RunEncounterProgressState progress =
                    CreateProgress(card, original);
                if (progress == null)
                {
                    return false;
                }

                RunConsumableViewModel viewModel = new();
                RunConsumableInventoryOption[] inventory =
                    viewModel.CreateInventoryOptions(progress);
                RunConsumableInventoryOption hammer = inventory.FirstOrDefault(
                    option => string.Equals(
                        option.ItemId,
                        PrototypeConsumableCatalog.EnchantHammer,
                        StringComparison.OrdinalIgnoreCase));
                RunConsumableInventoryOption scroll = inventory.FirstOrDefault(
                    option => string.Equals(
                        option.ItemId,
                        PrototypeConsumableCatalog.MutationScroll,
                        StringComparison.OrdinalIgnoreCase));
                if (inventory.Length != 2 || hammer == null || scroll == null ||
                    hammer.Count != 1 || scroll.Count != 2 ||
                    string.IsNullOrWhiteSpace(hammer.DisplayLabel) ||
                    !scroll.DisplayLabel.EndsWith("×2", StringComparison.Ordinal))
                {
                    return false;
                }

                RunCardInstance first = progress.OwnedCards.Cards[0];
                RunCardInstance second = progress.OwnedCards.Cards[1];
                RunEnchantHammerTargetOption[] targets =
                    viewModel.CreateHammerTargets(
                        progress,
                        second.OwnedCardId);
                if (targets.Length != 2 || targets[0].IsSelected ||
                    !targets[1].IsSelected ||
                    viewModel.SelectedHammerOwnedCardId != second.OwnedCardId ||
                    targets.Any(option =>
                        option.Card == null ||
                        string.IsNullOrWhiteSpace(option.DisplayLabel) ||
                        option.MaximumSlotCount !=
                            RunCardEnchantState.MaximumSlotCount) ||
                    viewModel.SelectHammerTarget(
                        progress,
                        "MISSING-HAMMER-TARGET") ||
                    viewModel.SelectedHammerOwnedCardId != second.OwnedCardId)
                {
                    return false;
                }

                RunEnchantHammerTargetOption cycled =
                    viewModel.CycleHammerTarget(
                        progress,
                        second.OwnedCardId);
                if (cycled == null ||
                    cycled.OwnedCardId != first.OwnedCardId ||
                    viewModel.SelectedHammerOwnedCardId != first.OwnedCardId)
                {
                    return false;
                }

                int slotsBefore = first.Enchants.SlotCount;
                if (!viewModel.TryUseEnchantHammer(
                        progress,
                        first.OwnedCardId,
                        out RunEnchantHammerTargetOption usedHammer,
                        out string hammerResult,
                        out PrototypeConsumableFailure hammerFailure) ||
                    hammerFailure != PrototypeConsumableFailure.None ||
                    usedHammer == null ||
                    usedHammer.OwnedCardId != first.OwnedCardId ||
                    first.Enchants.SlotCount != slotsBefore + 1 ||
                    string.IsNullOrWhiteSpace(hammerResult) ||
                    progress.RunState.ConsumableItemIds.Any(itemId =>
                        string.Equals(
                            itemId,
                            PrototypeConsumableCatalog.EnchantHammer,
                            StringComparison.OrdinalIgnoreCase)) ||
                    viewModel.CreateHammerTargets(progress).Length != 0)
                {
                    return false;
                }

                EnchantData[] mutationPool = { original, replacement };
                RunMutationScrollOption[] mutationOptions =
                    viewModel.CreateMutationOptions(progress, mutationPool);
                RunMutationScrollOption expected = mutationOptions.FirstOrDefault(
                    option => option.OwnedCardId == first.OwnedCardId &&
                              option.SlotIndex == 0 &&
                              option.Replacement == replacement);
                if (expected == null || !expected.CanUse ||
                    expected.OriginalEnchant != original ||
                    string.IsNullOrWhiteSpace(expected.DisplayText))
                {
                    return false;
                }

                int scrollCountBefore = progress.RunState.ConsumableItemIds.Count(
                    itemId => string.Equals(
                        itemId,
                        PrototypeConsumableCatalog.MutationScroll,
                        StringComparison.OrdinalIgnoreCase));
                if (viewModel.TryUseMutationScroll(
                        progress,
                        mutationPool,
                        first.OwnedCardId,
                        0,
                        "MISSING-REPLACEMENT",
                        out _,
                        out _,
                        out _,
                        out _) ||
                    progress.RunState.ConsumableItemIds.Count(itemId =>
                        string.Equals(
                            itemId,
                            PrototypeConsumableCatalog.MutationScroll,
                            StringComparison.OrdinalIgnoreCase)) != scrollCountBefore)
                {
                    return false;
                }

                if (!viewModel.TryUseMutationScroll(
                        progress,
                        mutationPool,
                        first.OwnedCardId,
                        0,
                        replacement.DefinitionId,
                        out RunMutationScrollOption usedMutation,
                        out string mutationResult,
                        out EnchantAttachmentFailure attachmentFailure,
                        out PrototypeConsumableFailure mutationFailure) ||
                    attachmentFailure != EnchantAttachmentFailure.None ||
                    mutationFailure != PrototypeConsumableFailure.None ||
                    usedMutation == null ||
                    usedMutation.Replacement != replacement ||
                    first.Enchants.Slots[0].Enchant != replacement ||
                    progress.RunState.ConsumableItemIds.Count(itemId =>
                        string.Equals(
                            itemId,
                            PrototypeConsumableCatalog.MutationScroll,
                            StringComparison.OrdinalIgnoreCase)) != scrollCountBefore - 1 ||
                    string.IsNullOrWhiteSpace(mutationResult) ||
                    viewModel.TryUseMutationScroll(
                        progress,
                        mutationPool,
                        first.OwnedCardId,
                        0,
                        replacement.DefinitionId,
                        out _,
                        out _,
                        out _,
                        out _))
                {
                    return false;
                }

                RunConsumableInventoryOption[] remaining =
                    viewModel.CreateInventoryOptions(progress);
                RunConsumableInventoryOption remainingScroll =
                    remaining.FirstOrDefault(option => string.Equals(
                        option.ItemId,
                        PrototypeConsumableCatalog.MutationScroll,
                        StringComparison.OrdinalIgnoreCase));
                viewModel.Reset();
                return remaining.Length == 1 &&
                       remainingScroll?.Count == 1 &&
                       viewModel.SelectedHammerOwnedCardId == null &&
                       viewModel.CreateInventoryOptions(null).Length == 0;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(original);
                UnityEngine.Object.DestroyImmediate(replacement);
            }
        }

        private static RunEncounterProgressState CreateProgress(
            CardData card,
            EnchantData original)
        {
            if (card == null || original == null)
            {
                return null;
            }

            RunCardInstance first = new(
                card,
                "RUN-CONSUMABLE-OWNED-01");
            RunCardInstance second = new(
                card,
                "RUN-CONSUMABLE-OWNED-02");
            RunOwnedCardState owned = new();
            RunDeckState deck = new();
            if (!owned.TryAdd(first, out _) ||
                !owned.TryAdd(second, out _) ||
                !deck.TryAdd(first, out _) ||
                !deck.TryAdd(second, out _) ||
                !first.Enchants.TryAttach(
                    original,
                    0,
                    false,
                    out EnchantAttachmentFailure attachmentFailure) ||
                attachmentFailure != EnchantAttachmentFailure.None)
            {
                return null;
            }

            return new RunEncounterProgressState(
                new RunBattleState(
                    30,
                    30,
                    0,
                    new[]
                    {
                        PrototypeConsumableCatalog.EnchantHammer,
                        PrototypeConsumableCatalog.MutationScroll,
                        PrototypeConsumableCatalog.MutationScroll
                    }),
                owned,
                deck,
                new PlayerPermanentRewardState(),
                Array.Empty<string>(),
                0);
        }

        private static EnchantData CreateEnchant(
            string id,
            CardType cardType)
        {
            EnchantData enchant =
                ScriptableObject.CreateInstance<EnchantData>();
            enchant.EditorInitialize(
                id,
                id,
                CardRarity.Common,
                new[] { cardType });
            return enchant;
        }
    }
}
