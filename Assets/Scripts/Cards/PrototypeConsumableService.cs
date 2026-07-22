using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    public enum PrototypeConsumableFailure
    {
        None,
        InvalidContext,
        InvalidItem,
        ItemNotOwned,
        BattleFinished,
        NoEffect,
        InvalidCard,
        SlotLimitReached,
        RecordFailed
    }

    public static class PrototypeConsumableCatalog
    {
        public const string HealingPotion = "ITEM-HEAL-5";
        public const string CleanseScroll = "ITEM-CLEANSE";
        public const string ManaBattery = "ITEM-MANA-2";
        public const string EnchantHammer = "ITEM-ENCHANT-HAMMER";
        public const string MutationScroll = "ITEM-MUTATION-SCROLL";

        private static ConsumableDatabase Database =>
            Resources.Load<RuntimePrototypeConfig>(
                "GameData/RuntimePrototypeConfig")?.ConsumableDatabase;

        public static IReadOnlyList<ConsumableData> All =>
            Database?.Consumables ?? Array.Empty<ConsumableData>();

        public static ConsumableData Find(string itemId)
        {
            return Database?.Find(itemId);
        }
    }

    public static class PrototypeConsumableService
    {
        public static bool TryUseInBattle(
            BattleRuntimeEncounterContext context,
            string itemId,
            out int appliedAmount,
            out PrototypeConsumableFailure failure)
        {
            appliedAmount = 0;
            if (context?.Runtime?.Player == null || context.RunState == null ||
                context.RunChanges == null)
            {
                failure = PrototypeConsumableFailure.InvalidContext;
                return false;
            }

            if (context.Session.IsFinished)
            {
                failure = PrototypeConsumableFailure.BattleFinished;
                return false;
            }

            ConsumableData item =
                PrototypeConsumableCatalog.Find(itemId);
            if (item == null ||
                item.Effect == ConsumableEffect.IncreaseEnchantSlot ||
                item.Effect == ConsumableEffect.ReplaceEnchant)
            {
                failure = PrototypeConsumableFailure.InvalidItem;
                return false;
            }

            if (!HasUnconsumedCopy(context, item.ItemId))
            {
                failure = PrototypeConsumableFailure.ItemNotOwned;
                return false;
            }

            appliedAmount = item.Effect switch
            {
                ConsumableEffect.HealPlayer =>
                    context.Runtime.Player.ApplyHealing(item.Amount),
                ConsumableEffect.ClearPlayerStatuses =>
                    context.Runtime.Player.Status.ClearAll(),
                ConsumableEffect.RestoreMana =>
                    context.Runtime.CardPlay.Mana.Restore(item.Amount),
                _ => 0
            };
            if (appliedAmount <= 0)
            {
                failure = PrototypeConsumableFailure.NoEffect;
                return false;
            }

            if (!context.RunChanges.RecordConsumedItem(item.ItemId))
            {
                failure = PrototypeConsumableFailure.RecordFailed;
                return false;
            }

            failure = PrototypeConsumableFailure.None;
            return true;
        }

        public static bool TryUseEnchantHammer(
            RunEncounterProgressState progress,
            string ownedCardId,
            out PrototypeConsumableFailure failure)
        {
            if (progress?.RunState == null || progress.RunState.RunEnded ||
                progress.OwnedCards == null ||
                progress.HasActiveEncounter)
            {
                failure = PrototypeConsumableFailure.InvalidContext;
                return false;
            }

            if (!Contains(progress.RunState.ConsumableItemIds,
                    PrototypeConsumableCatalog.EnchantHammer))
            {
                failure = PrototypeConsumableFailure.ItemNotOwned;
                return false;
            }

            RunCardInstance card = progress.OwnedCards.Find(ownedCardId);
            if (card == null)
            {
                failure = PrototypeConsumableFailure.InvalidCard;
                return false;
            }

            if (!card.Enchants.TryIncreaseSlotCount())
            {
                failure = PrototypeConsumableFailure.SlotLimitReached;
                return false;
            }

            progress.RunState.RemoveConsumableItem(
                PrototypeConsumableCatalog.EnchantHammer);
            failure = PrototypeConsumableFailure.None;
            return true;
        }

        public static bool TryUseMutationScroll(
            RunEncounterProgressState progress,
            string ownedCardId,
            int slotIndex,
            EnchantData replacement,
            out EnchantAttachmentFailure attachmentFailure,
            out PrototypeConsumableFailure failure)
        {
            attachmentFailure = EnchantAttachmentFailure.None;
            if (progress?.RunState == null || progress.RunState.RunEnded ||
                progress.OwnedCards == null ||
                progress.HasActiveEncounter)
            {
                failure = PrototypeConsumableFailure.InvalidContext;
                return false;
            }

            if (!Contains(progress.RunState.ConsumableItemIds,
                    PrototypeConsumableCatalog.MutationScroll))
            {
                failure = PrototypeConsumableFailure.ItemNotOwned;
                return false;
            }

            RunCardInstance card = progress.OwnedCards.Find(ownedCardId);
            if (card == null)
            {
                failure = PrototypeConsumableFailure.InvalidCard;
                return false;
            }

            if (!card.Enchants.TryReplace(
                    slotIndex, replacement, false, out attachmentFailure))
            {
                failure = PrototypeConsumableFailure.NoEffect;
                return false;
            }

            if (!progress.RunState.RemoveConsumableItem(
                    PrototypeConsumableCatalog.MutationScroll))
            {
                failure = PrototypeConsumableFailure.RecordFailed;
                return false;
            }

            failure = PrototypeConsumableFailure.None;
            return true;
        }

        private static bool HasUnconsumedCopy(
            BattleRuntimeEncounterContext context,
            string itemId)
        {
            int owned = Count(context.RunState.ConsumableItemIds, itemId);
            int consumed = Count(context.RunChanges.ConsumedItemIds, itemId);
            return owned > consumed;
        }

        private static int Count(IReadOnlyList<string> values, string itemId)
        {
            int count = 0;
            foreach (string value in values)
            {
                if (string.Equals(value, itemId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool Contains(IReadOnlyList<string> values, string itemId)
        {
            return Count(values, itemId) > 0;
        }
    }
}
