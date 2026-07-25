using System;
using System.Collections.Generic;
using System.Linq;

namespace HaveABreak.Cards
{
    public sealed class RunShopProductOption
    {
        internal RunShopProductOption(
            RunShopProductSlot slot,
            ConsumableData consumable,
            EnchantData enchant,
            RunCardInstance targetCard,
            int targetSlotIndex)
        {
            Slot = slot ?? throw new ArgumentNullException(nameof(slot));
            Consumable = consumable;
            Enchant = enchant;
            TargetCard = targetCard;
            TargetSlotIndex = targetSlotIndex;
        }

        public RunShopProductSlot Slot { get; }
        public ConsumableData Consumable { get; }
        public EnchantData Enchant { get; }
        public RunCardInstance TargetCard { get; }
        public int TargetSlotIndex { get; }
        public string SlotId => Slot.SlotId;
        public string ContentId => Slot.ContentId;
        public RunShopProductType ProductType => Slot.ProductType;
        public int Price => Slot.Price;
        public bool Purchased => Slot.Purchased;
        public bool HasEnchantTarget =>
            ProductType != RunShopProductType.Enchant ||
            (TargetCard != null && TargetSlotIndex >= 0);
        public bool CanPurchase => !Purchased && HasEnchantTarget;
        public string SectionLabel =>
            ProductType == RunShopProductType.Consumable
                ? "소모아이템"
                : "인첸트";
        public string DisplayName => ProductType switch
        {
            RunShopProductType.Consumable => Consumable?.DisplayName ?? ContentId,
            RunShopProductType.Enchant => Enchant?.DisplayName ?? ContentId,
            _ => ContentId
        };
        public string DisplayText => ProductType switch
        {
            RunShopProductType.Consumable when Consumable != null =>
                $"{Consumable.DisplayName} · {Consumable.RulesText}",
            RunShopProductType.Enchant when Enchant != null =>
                $"{Enchant.DisplayName} [{Enchant.Rarity}] · {Enchant.RulesText}",
            _ => DisplayName
        };
        public string PurchaseButtonLabel => Purchased ? "판매 완료" : $"{Price}G";
        public string TargetLabel => TargetCard == null
            ? null
            : $"{TargetCard.Card.DisplayName} · 슬롯 {TargetSlotIndex + 1}";
        public string BlockReason => Purchased
            ? "판매 완료"
            : HasEnchantTarget
                ? null
                : "장착 가능한 카드 없음";
    }

    public sealed class RunShopViewModel
    {
        public RunShopProductOption[] CreateOptions(
            RunCampaignState campaign,
            RunEncounterProgressState progress,
            EnchantDatabase enchantDatabase,
            ShopEconomyConfig economy)
        {
            if (!IsAvailable(campaign) || progress == null ||
                enchantDatabase == null || economy == null)
            {
                return Array.Empty<RunShopProductOption>();
            }

            IReadOnlyList<RunShopProductSlot> slots =
                RunCampaignService.GetShopSlots(
                    campaign,
                    PrototypeConsumableCatalog.All,
                    enchantDatabase.Enchants,
                    economy);
            if (slots == null || slots.Count == 0)
            {
                return Array.Empty<RunShopProductOption>();
            }

            List<RunShopProductOption> options = new();
            foreach (RunShopProductSlot slot in slots)
            {
                if (slot == null)
                {
                    continue;
                }

                switch (slot.ProductType)
                {
                    case RunShopProductType.Consumable:
                    {
                        ConsumableData item =
                            PrototypeConsumableCatalog.Find(slot.ContentId);
                        if (item != null)
                        {
                            options.Add(new RunShopProductOption(
                                slot,
                                item,
                                null,
                                null,
                                -1));
                        }
                        break;
                    }
                    case RunShopProductType.Enchant:
                    {
                        EnchantData enchant = enchantDatabase.Find(slot.ContentId);
                        if (enchant == null)
                        {
                            break;
                        }

                        TryFindEnchantTarget(
                            progress,
                            enchant,
                            out RunCardInstance target,
                            out int targetSlotIndex);
                        options.Add(new RunShopProductOption(
                            slot,
                            null,
                            enchant,
                            target,
                            targetSlotIndex));
                        break;
                    }
                }
            }

            return options.ToArray();
        }

        public int GetRerollCost(
            RunCampaignState campaign,
            ShopEconomyConfig economy)
        {
            return IsAvailable(campaign) && economy != null
                ? RunCampaignService.GetShopRerollCost(campaign, economy)
                : 0;
        }

        public bool TryBuy(
            RunCampaignState campaign,
            RunEncounterProgressState progress,
            EnchantDatabase enchantDatabase,
            ShopEconomyConfig economy,
            string slotId,
            out RunShopProductOption purchased,
            out string result,
            out EnchantAttachmentFailure attachmentFailure,
            out RunCampaignFailure failure)
        {
            purchased = null;
            result = null;
            attachmentFailure = default;
            if (string.IsNullOrWhiteSpace(slotId))
            {
                failure = default;
                return false;
            }

            RunShopProductOption option = CreateOptions(
                    campaign,
                    progress,
                    enchantDatabase,
                    economy)
                .FirstOrDefault(value => string.Equals(
                    value.SlotId,
                    slotId.Trim(),
                    StringComparison.OrdinalIgnoreCase));
            if (option == null || !option.CanPurchase)
            {
                failure = default;
                return false;
            }

            switch (option.ProductType)
            {
                case RunShopProductType.Consumable:
                    if (!RunCampaignService.TryBuyConsumableSlot(
                            campaign,
                            progress.RunState,
                            option.SlotId,
                            out failure))
                    {
                        return false;
                    }
                    result = $"{option.DisplayName} 구매 완료.";
                    break;

                case RunShopProductType.Enchant:
                    if (!RunCampaignService.TryBuyEnchantSlot(
                            campaign,
                            progress,
                            option.Enchant,
                            option.SlotId,
                            option.TargetCard.OwnedCardId,
                            option.TargetSlotIndex,
                            out attachmentFailure,
                            out failure))
                    {
                        return false;
                    }
                    result = $"{option.TargetCard.Card.DisplayName}에 " +
                             $"{option.Enchant.DisplayName} 장착.";
                    break;

                default:
                    failure = default;
                    return false;
            }

            purchased = option;
            return true;
        }

        public bool TryReroll(
            RunCampaignState campaign,
            RunBattleState run,
            ShopEconomyConfig economy,
            out int paidGold,
            out string result,
            out RunCampaignFailure failure)
        {
            paidGold = 0;
            result = null;
            if (!IsAvailable(campaign) || run == null || economy == null)
            {
                failure = default;
                return false;
            }

            paidGold = GetRerollCost(campaign, economy);
            if (!RunCampaignService.TryRerollShop(
                    campaign,
                    run,
                    economy,
                    out failure))
            {
                paidGold = 0;
                return false;
            }

            result = "상점 상품을 다시 생성했습니다.";
            return true;
        }

        public bool TryLeave(
            RunCampaignState campaign,
            RunBattleState run,
            out string result,
            out RunCampaignFailure failure)
        {
            result = null;
            if (!IsAvailable(campaign) || run == null)
            {
                failure = default;
                return false;
            }

            if (!RunCampaignService.TryLeaveShop(
                    campaign,
                    run,
                    out failure))
            {
                return false;
            }

            result = "상점을 나왔습니다.";
            return true;
        }

        private static bool TryFindEnchantTarget(
            RunEncounterProgressState progress,
            EnchantData enchant,
            out RunCardInstance target,
            out int slotIndex)
        {
            target = null;
            slotIndex = -1;
            if (progress?.OwnedCards == null || enchant == null)
            {
                return false;
            }

            foreach (RunCardInstance card in progress.OwnedCards.Cards)
            {
                if (card == null)
                {
                    continue;
                }

                for (int index = 0; index < card.Enchants.SlotCount; index++)
                {
                    if (!card.Enchants.CanAttach(enchant, index, out _))
                    {
                        continue;
                    }

                    target = card;
                    slotIndex = index;
                    return true;
                }
            }

            return false;
        }

        private static bool IsAvailable(RunCampaignState campaign)
        {
            return campaign != null &&
                   campaign.Phase == RunCampaignPhase.NodeResolution &&
                   campaign.ActiveNode?.NodeType == RunNodeType.Shop;
        }
    }
}
