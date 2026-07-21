using System;
using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public enum PrototypeConsumableEffect
    {
        HealPlayer,
        ClearPlayerStatuses,
        RestoreMana,
        IncreaseEnchantSlot
    }

    [Serializable]
    public sealed class PrototypeConsumableDefinition
    {
        public PrototypeConsumableDefinition(
            string itemId,
            string displayName,
            string rulesText,
            PrototypeConsumableEffect effect,
            int amount,
            int shopPrice)
        {
            ItemId = itemId;
            DisplayName = displayName;
            RulesText = rulesText;
            Effect = effect;
            Amount = amount;
            ShopPrice = shopPrice;
        }

        public string ItemId { get; }
        public string DisplayName { get; }
        public string RulesText { get; }
        public PrototypeConsumableEffect Effect { get; }
        public int Amount { get; }
        public int ShopPrice { get; }
    }

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

        private static readonly PrototypeConsumableDefinition[] Items =
        {
            new(HealingPotion, "회복 포션", "플레이어 HP를 5 회복한다.",
                PrototypeConsumableEffect.HealPlayer, 5, 30),
            new(CleanseScroll, "해제 주문서", "플레이어의 상태이상을 모두 해제한다.",
                PrototypeConsumableEffect.ClearPlayerStatuses, 0, 35),
            new(ManaBattery, "비상 전지", "현재 마력을 2 회복한다.",
                PrototypeConsumableEffect.RestoreMana, 2, 35),
            new(EnchantHammer, "인첸트 망치", "전투 밖에서 카드의 인첸트 슬롯을 1칸 늘린다.",
                PrototypeConsumableEffect.IncreaseEnchantSlot, 1, 60)
        };

        public static IReadOnlyList<PrototypeConsumableDefinition> All => Items;

        public static PrototypeConsumableDefinition Find(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return null;
            }

            return Array.Find(Items, item => string.Equals(
                item.ItemId, itemId.Trim(), StringComparison.OrdinalIgnoreCase));
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

            PrototypeConsumableDefinition item =
                PrototypeConsumableCatalog.Find(itemId);
            if (item == null ||
                item.Effect == PrototypeConsumableEffect.IncreaseEnchantSlot)
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
                PrototypeConsumableEffect.HealPlayer =>
                    context.Runtime.Player.ApplyHealing(item.Amount),
                PrototypeConsumableEffect.ClearPlayerStatuses =>
                    context.Runtime.Player.Status.ClearAll(),
                PrototypeConsumableEffect.RestoreMana =>
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
            if (progress?.RunState == null || progress.RunDeck == null ||
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

            RunCardInstance card = progress.RunDeck.Find(ownedCardId);
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
