using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class RunShopViewModelValidation
    {
        [MenuItem("Have a Break/Validate Run Shop ViewModel")]
        private static void ValidateFromMenu()
        {
            Debug.Log(Validate()
                ? "Run shop ViewModel passed."
                : "Run shop ViewModel failed.");
        }

        internal static bool Validate()
        {
            CardDatabase cards = AssetDatabase.LoadAssetAtPath<CardDatabase>(
                "Assets/GameData/CardDatabase.asset");
            EnchantDatabase enchants = AssetDatabase.LoadAssetAtPath<EnchantDatabase>(
                "Assets/GameData/EnchantDatabase.asset");
            ShopEconomyConfig economy = AssetDatabase.LoadAssetAtPath<ShopEconomyConfig>(
                "Assets/GameData/ShopEconomyConfig.asset");
            if (cards == null || enchants == null || economy == null)
            {
                return false;
            }

            RunShopViewModel shop = new();
            if (shop.CreateOptions(null, null, enchants, economy).Length != 0 ||
                shop.GetRerollCost(null, economy) != 0)
            {
                return false;
            }

            RunCampaignState campaign = CampaignAtShop();
            RunEncounterProgressState progress = CreateProgress(cards, 2000);
            if (campaign == null || progress == null)
            {
                return false;
            }

            RunShopProductOption[] options = shop.CreateOptions(
                campaign,
                progress,
                enchants,
                economy);
            if (options.Length != economy.ConsumableOfferCount +
                    economy.EnchantOfferCount ||
                options.Count(option => option.ProductType ==
                    RunShopProductType.Consumable) != economy.ConsumableOfferCount ||
                options.Count(option => option.ProductType ==
                    RunShopProductType.Enchant) != economy.EnchantOfferCount ||
                options.Any(option => option == null || option.Slot == null ||
                    string.IsNullOrWhiteSpace(option.SlotId) ||
                    string.IsNullOrWhiteSpace(option.ContentId) ||
                    string.IsNullOrWhiteSpace(option.DisplayName) ||
                    string.IsNullOrWhiteSpace(option.DisplayText) ||
                    option.Price <= 0 || option.Purchased ||
                    option.PurchaseButtonLabel != $"{option.Price}G"))
            {
                return false;
            }

            foreach (RunShopProductOption option in options)
            {
                if (option.ProductType == RunShopProductType.Consumable)
                {
                    if (option.Consumable == null || option.Enchant != null ||
                        option.SectionLabel != "소모아이템" ||
                        option.DisplayText !=
                            $"{option.Consumable.DisplayName} · {option.Consumable.RulesText}" ||
                        !option.CanPurchase || option.TargetCard != null ||
                        option.TargetSlotIndex != -1)
                    {
                        return false;
                    }
                }
                else if (option.ProductType == RunShopProductType.Enchant)
                {
                    if (option.Enchant == null || option.Consumable != null ||
                        option.SectionLabel != "인첸트" ||
                        option.DisplayText !=
                            $"{option.Enchant.DisplayName} [{option.Enchant.Rarity}] · " +
                            option.Enchant.RulesText ||
                        option.HasEnchantTarget !=
                            (option.TargetCard != null && option.TargetSlotIndex >= 0))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            int originalGold = progress.RunState.Gold;
            if (shop.TryBuy(
                    campaign,
                    progress,
                    enchants,
                    economy,
                    "MISSING-SHOP-SLOT",
                    out _,
                    out _,
                    out _,
                    out _) ||
                progress.RunState.Gold != originalGold)
            {
                return false;
            }

            RunShopProductOption consumable = options.FirstOrDefault(option =>
                option.ProductType == RunShopProductType.Consumable);
            if (consumable == null)
            {
                return false;
            }

            int consumableCountBefore = progress.RunState.ConsumableItemIds.Count(value =>
                string.Equals(
                    value,
                    consumable.ContentId,
                    StringComparison.OrdinalIgnoreCase));
            int goldBeforeConsumable = progress.RunState.Gold;
            if (!shop.TryBuy(
                    campaign,
                    progress,
                    enchants,
                    economy,
                    consumable.SlotId,
                    out RunShopProductOption purchasedConsumable,
                    out string consumableResult,
                    out _,
                    out RunCampaignFailure consumableFailure) ||
                consumableFailure != RunCampaignFailure.None ||
                purchasedConsumable == null || !purchasedConsumable.Purchased ||
                purchasedConsumable.PurchaseButtonLabel != "판매 완료" ||
                progress.RunState.Gold != goldBeforeConsumable - consumable.Price ||
                progress.RunState.ConsumableItemIds.Count(value =>
                    string.Equals(
                        value,
                        consumable.ContentId,
                        StringComparison.OrdinalIgnoreCase)) != consumableCountBefore + 1 ||
                string.IsNullOrWhiteSpace(consumableResult) ||
                shop.TryBuy(
                    campaign,
                    progress,
                    enchants,
                    economy,
                    consumable.SlotId,
                    out _,
                    out _,
                    out _,
                    out _))
            {
                return false;
            }

            RunShopProductOption enchant = shop.CreateOptions(
                    campaign,
                    progress,
                    enchants,
                    economy)
                .FirstOrDefault(option =>
                    option.ProductType == RunShopProductType.Enchant &&
                    option.CanPurchase);
            if (enchant == null || enchant.TargetCard == null ||
                enchant.TargetSlotIndex < 0 ||
                string.IsNullOrWhiteSpace(enchant.TargetLabel))
            {
                return false;
            }

            RunCardInstance targetCard = enchant.TargetCard;
            int targetSlotIndex = enchant.TargetSlotIndex;
            int goldBeforeEnchant = progress.RunState.Gold;
            if (!shop.TryBuy(
                    campaign,
                    progress,
                    enchants,
                    economy,
                    enchant.SlotId,
                    out RunShopProductOption purchasedEnchant,
                    out string enchantResult,
                    out EnchantAttachmentFailure attachmentFailure,
                    out RunCampaignFailure enchantFailure) ||
                enchantFailure != RunCampaignFailure.None ||
                attachmentFailure != EnchantAttachmentFailure.None ||
                purchasedEnchant == null || !purchasedEnchant.Purchased ||
                progress.RunState.Gold != goldBeforeEnchant - enchant.Price ||
                targetCard.Enchants.Slots[targetSlotIndex].Enchant == null ||
                !targetCard.Enchants.Slots[targetSlotIndex].Enchant.MatchesDefinition(
                    enchant.Enchant) ||
                string.IsNullOrWhiteSpace(enchantResult))
            {
                return false;
            }

            string[] slotIdsBeforeReroll = shop.CreateOptions(
                    campaign,
                    progress,
                    enchants,
                    economy)
                .Select(option => option.SlotId)
                .ToArray();
            int rerollCost = shop.GetRerollCost(campaign, economy);
            int goldBeforeReroll = progress.RunState.Gold;
            if (rerollCost != economy.GetRerollCost(0) ||
                !shop.TryReroll(
                    campaign,
                    progress.RunState,
                    economy,
                    out int paidGold,
                    out string rerollResult,
                    out RunCampaignFailure rerollFailure) ||
                rerollFailure != RunCampaignFailure.None ||
                paidGold != rerollCost ||
                progress.RunState.Gold != goldBeforeReroll - rerollCost ||
                string.IsNullOrWhiteSpace(rerollResult))
            {
                return false;
            }

            RunShopProductOption[] rerolled = shop.CreateOptions(
                campaign,
                progress,
                enchants,
                economy);
            if (rerolled.Length != options.Length ||
                rerolled.Any(option => option.Purchased) ||
                rerolled.Select(option => option.SlotId)
                    .SequenceEqual(slotIdsBeforeReroll) ||
                shop.GetRerollCost(campaign, economy) != economy.GetRerollCost(1))
            {
                return false;
            }

            if (!shop.TryLeave(
                    campaign,
                    progress.RunState,
                    out string leaveResult,
                    out RunCampaignFailure leaveFailure) ||
                leaveFailure != RunCampaignFailure.None ||
                string.IsNullOrWhiteSpace(leaveResult) ||
                campaign.CompletedNodeCount != 1 ||
                campaign.Phase != RunCampaignPhase.NodeSelection ||
                campaign.ActiveNode != null ||
                shop.CreateOptions(campaign, progress, enchants, economy).Length != 0)
            {
                return false;
            }

            RunCampaignState unavailable = new(20260725);
            return !shop.TryReroll(
                       unavailable,
                       progress.RunState,
                       economy,
                       out _,
                       out _,
                       out _) &&
                   !shop.TryLeave(
                       unavailable,
                       progress.RunState,
                       out _,
                       out _);
        }

        private static RunEncounterProgressState CreateProgress(
            CardDatabase database,
            int gold)
        {
            RunOwnedCardState owned = new();
            RunDeckState deck = new();
            int index = 0;
            foreach (CardData data in database.Cards.Where(card => card != null))
            {
                RunCardInstance card = new(
                    data,
                    $"SHOP-OWNED-{++index:00}",
                    1);
                if (!owned.TryAdd(card, out _) || !deck.TryAdd(card, out _))
                {
                    return null;
                }
            }

            if (owned.Count == 0)
            {
                return null;
            }

            return new RunEncounterProgressState(
                new RunBattleState(30, 30, gold),
                owned,
                deck,
                new PlayerPermanentRewardState(),
                Array.Empty<string>(),
                0);
        }

        private static RunCampaignState CampaignAtShop()
        {
            for (int seed = 0; seed < 1000; seed++)
            {
                RunCampaignState campaign = new(seed);
                RunNodeChoice choice = RunCampaignService.GetChoices(campaign)
                    .FirstOrDefault(value => value.NodeType == RunNodeType.Shop);
                if (choice != null && RunCampaignService.TrySelectNode(
                        campaign,
                        choice.NodeId,
                        out _))
                {
                    return campaign;
                }
            }

            return null;
        }
    }
}
