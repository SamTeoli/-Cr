using System;
using System.Collections.Generic;
using System.Linq;

namespace HaveABreak.Cards
{
    public sealed class RunBattleEnchantRewardOption
    {
        internal RunBattleEnchantRewardOption(
            EnchantData enchant,
            RunCardInstance targetCard,
            int targetSlotIndex,
            bool rewardClaimed,
            bool isSelected)
        {
            Enchant = enchant ?? throw new ArgumentNullException(nameof(enchant));
            TargetCard = targetCard;
            TargetSlotIndex = targetSlotIndex;
            RewardClaimed = rewardClaimed;
            IsSelected = isSelected;
        }

        public EnchantData Enchant { get; }
        public RunCardInstance TargetCard { get; }
        public int TargetSlotIndex { get; }
        public string DefinitionId => Enchant.DefinitionId;
        public string DisplayName => Enchant.DisplayName;
        public bool RewardClaimed { get; }
        public bool IsSelected { get; }
        public bool HasTarget => TargetCard != null && TargetSlotIndex >= 0;
        public bool CanClaim => !RewardClaimed && HasTarget;
        public string DisplayText =>
            $"{Enchant.DisplayName} [{Enchant.Rarity}] · {Enchant.RulesText}";
        public string TargetLabel => TargetCard == null
            ? null
            : $"{TargetCard.Card.DisplayName} · 슬롯 {TargetSlotIndex + 1}";
        public string BlockReason => RewardClaimed
            ? IsSelected
                ? "선택 완료"
                : "다른 인첸트 선택 완료"
            : HasTarget
                ? null
                : "장착 가능한 런 덱 카드 없음";
    }

    public sealed class RunBattleConsumableRewardOption
    {
        internal RunBattleConsumableRewardOption(
            ConsumableData item,
            bool rewardClaimed,
            bool isSelected)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));
            RewardClaimed = rewardClaimed;
            IsSelected = isSelected;
        }

        public ConsumableData Item { get; }
        public string ItemId => Item.ItemId;
        public string DisplayName => Item.DisplayName;
        public bool RewardClaimed { get; }
        public bool IsSelected { get; }
        public bool CanClaim => !RewardClaimed;
        public string DisplayText => $"{Item.DisplayName} · {Item.RulesText}";
        public string BlockReason => RewardClaimed
            ? IsSelected
                ? "수령 완료"
                : "소모아이템 보상 수령 완료"
            : null;
    }

    public sealed class RunBattleRewardSnapshot
    {
        internal RunBattleRewardSnapshot(
            bool available,
            int goldReward,
            bool goldClaimed,
            RunBattleEnchantRewardOption[] enchantOptions,
            RunBattleConsumableRewardOption[] consumableOptions,
            bool enchantRewardComplete,
            bool consumableRewardComplete,
            string errorText)
        {
            Available = available;
            GoldReward = goldReward;
            GoldClaimed = goldClaimed;
            EnchantOptions = enchantOptions ??
                             Array.Empty<RunBattleEnchantRewardOption>();
            ConsumableOptions = consumableOptions ??
                                Array.Empty<RunBattleConsumableRewardOption>();
            EnchantRewardComplete = enchantRewardComplete;
            ConsumableRewardComplete = consumableRewardComplete;
            ErrorText = errorText;
        }

        public bool Available { get; }
        public int GoldReward { get; }
        public bool GoldClaimed { get; }
        public RunBattleEnchantRewardOption[] EnchantOptions { get; }
        public RunBattleConsumableRewardOption[] ConsumableOptions { get; }
        public bool EnchantRewardComplete { get; }
        public bool ConsumableRewardComplete { get; }
        public string ErrorText { get; }
        public string GoldLabel => GoldClaimed
            ? $"골드 {GoldReward} 수령 완료"
            : $"골드 {GoldReward} 대기 중";
        public bool CanComplete => Available && GoldClaimed &&
                                   EnchantRewardComplete &&
                                   ConsumableRewardComplete &&
                                   string.IsNullOrWhiteSpace(ErrorText);
    }

    public sealed class RunBattleRewardViewModel
    {
        public RunBattleRewardSnapshot CreateSnapshot(
            RunCampaignState campaign,
            RunEncounterProgressState progress,
            EnchantDatabase enchantDatabase)
        {
            BattleRuntimeEncounterContext context = progress?.ActiveEncounter;
            if (!IsAvailable(campaign, context))
            {
                return EmptySnapshot("정산된 승리 전투가 없습니다.");
            }

            List<string> errors = new();
            if (!EnsureEnchantRewards(
                    context,
                    progress,
                    enchantDatabase,
                    out BattleVictoryEnchantRewardFailure enchantFailure))
            {
                errors.Add($"인첸트 보상 생성 실패: {enchantFailure}");
            }
            if (!EnsureConsumableRewards(
                    context,
                    out BattleVictoryConsumableRewardFailure consumableFailure))
            {
                errors.Add($"소모아이템 보상 생성 실패: {consumableFailure}");
            }

            BattleVictoryEnchantRewardService enchantRewards =
                context.VictoryEnchantRewards;
            BattleVictoryConsumableRewardService consumableRewards =
                context.VictoryConsumableRewards;
            RunBattleEnchantRewardOption[] enchantOptions = enchantRewards == null
                ? Array.Empty<RunBattleEnchantRewardOption>()
                : enchantRewards.OfferedChoices
                    .Where(enchant => enchant != null)
                    .Select(enchant => CreateEnchantOption(
                        progress,
                        enchant,
                        enchantRewards))
                    .ToArray();
            RunBattleConsumableRewardOption[] consumableOptions =
                context.VictoryRewards.ConsumableItemRewardCount <= 0
                    ? Array.Empty<RunBattleConsumableRewardOption>()
                    : PrototypeConsumableCatalog.All
                        .Where(item => item != null)
                        .Take(3)
                        .Select(item => new RunBattleConsumableRewardOption(
                            item,
                            consumableRewards?.Claimed == true,
                            consumableRewards?.ClaimedItemIds?.Any(id =>
                                string.Equals(
                                    id,
                                    item.ItemId,
                                    StringComparison.OrdinalIgnoreCase)) == true))
                        .ToArray();

            bool enchantComplete =
                context.VictoryRewards.EnchantChoiceCount <= 0 ||
                enchantRewards?.Claimed == true;
            bool consumableComplete =
                context.VictoryRewards.ConsumableItemRewardCount <= 0 ||
                consumableRewards?.Claimed == true;
            return new RunBattleRewardSnapshot(
                true,
                context.VictoryRewards.GoldReward,
                context.VictoryRewards.GoldClaimed,
                enchantOptions,
                consumableOptions,
                enchantComplete,
                consumableComplete,
                errors.Count == 0 ? null : string.Join("\n", errors));
        }

        public bool TryClaimEnchant(
            RunCampaignState campaign,
            RunEncounterProgressState progress,
            EnchantDatabase enchantDatabase,
            string definitionId,
            out RunBattleEnchantRewardOption claimed,
            out string result,
            out EnchantAttachmentFailure attachmentFailure,
            out BattleVictoryEnchantRewardFailure failure)
        {
            claimed = null;
            result = null;
            attachmentFailure = default;
            BattleRuntimeEncounterContext context = progress?.ActiveEncounter;
            if (!IsAvailable(campaign, context) ||
                string.IsNullOrWhiteSpace(definitionId) ||
                !EnsureEnchantRewards(
                    context,
                    progress,
                    enchantDatabase,
                    out failure))
            {
                return false;
            }

            BattleVictoryEnchantRewardService rewards =
                context.VictoryEnchantRewards;
            RunBattleEnchantRewardOption option = rewards?.OfferedChoices
                .Where(enchant => enchant != null)
                .Select(enchant => CreateEnchantOption(progress, enchant, rewards))
                .FirstOrDefault(value => string.Equals(
                    value.DefinitionId,
                    definitionId.Trim(),
                    StringComparison.OrdinalIgnoreCase));
            if (option == null || !option.CanClaim)
            {
                failure = default;
                return false;
            }

            if (!rewards.TryClaim(
                    option.DefinitionId,
                    option.TargetCard.OwnedCardId,
                    option.TargetSlotIndex,
                    out attachmentFailure,
                    out failure))
            {
                return false;
            }

            claimed = new RunBattleEnchantRewardOption(
                option.Enchant,
                option.TargetCard,
                option.TargetSlotIndex,
                true,
                true);
            result = $"{option.TargetCard.Card.DisplayName}에 " +
                     $"{option.Enchant.DisplayName} 장착.";
            return true;
        }

        public bool TryClaimConsumable(
            RunCampaignState campaign,
            RunEncounterProgressState progress,
            string itemId,
            out RunBattleConsumableRewardOption claimed,
            out string result,
            out BattleVictoryConsumableRewardFailure failure)
        {
            claimed = null;
            result = null;
            BattleRuntimeEncounterContext context = progress?.ActiveEncounter;
            if (!IsAvailable(campaign, context) ||
                string.IsNullOrWhiteSpace(itemId) ||
                !EnsureConsumableRewards(context, out failure))
            {
                return false;
            }

            ConsumableData item = PrototypeConsumableCatalog.All
                .Where(value => value != null)
                .Take(3)
                .FirstOrDefault(value => string.Equals(
                    value.ItemId,
                    itemId.Trim(),
                    StringComparison.OrdinalIgnoreCase));
            if (item == null || context.VictoryConsumableRewards?.Claimed == true)
            {
                failure = default;
                return false;
            }

            if (!context.VictoryConsumableRewards.TryClaim(
                    item.ItemId,
                    out failure))
            {
                return false;
            }

            claimed = new RunBattleConsumableRewardOption(
                item,
                context.VictoryConsumableRewards.Claimed,
                true);
            result = $"{item.DisplayName} 보상 수령 완료.";
            return true;
        }

        public bool TryComplete(
            RunCampaignState campaign,
            RunEncounterProgressState progress,
            out string result,
            out RunEncounterProgressFailure failure)
        {
            result = null;
            BattleRuntimeEncounterContext context = progress?.ActiveEncounter;
            if (!IsAvailable(campaign, context))
            {
                failure = default;
                return false;
            }

            if (!RunEncounterProgressService.TryCompleteActive(
                    progress,
                    out failure))
            {
                return false;
            }

            RunCampaignService.CompleteBattleReward(campaign);
            result = "보상 완료 · 다음 노드를 선택하세요.";
            return true;
        }

        private static RunBattleEnchantRewardOption CreateEnchantOption(
            RunEncounterProgressState progress,
            EnchantData enchant,
            BattleVictoryEnchantRewardService rewards)
        {
            bool selected = rewards?.Claimed == true &&
                            rewards.ClaimedEnchant != null &&
                            rewards.ClaimedEnchant.MatchesDefinition(enchant);
            if (rewards?.Claimed == true)
            {
                return new RunBattleEnchantRewardOption(
                    enchant,
                    null,
                    -1,
                    true,
                    selected);
            }

            TryFindEnchantTarget(
                progress,
                enchant,
                out RunCardInstance target,
                out int slotIndex);
            return new RunBattleEnchantRewardOption(
                enchant,
                target,
                slotIndex,
                false,
                false);
        }

        private static bool EnsureEnchantRewards(
            BattleRuntimeEncounterContext context,
            RunEncounterProgressState progress,
            EnchantDatabase enchantDatabase,
            out BattleVictoryEnchantRewardFailure failure)
        {
            failure = BattleVictoryEnchantRewardFailure.None;
            if (context?.VictoryRewards == null ||
                context.VictoryRewards.EnchantChoiceCount <= 0)
            {
                return true;
            }
            if (context.VictoryEnchantRewards != null)
            {
                return true;
            }
            if (progress?.RunDeck == null || enchantDatabase == null)
            {
                failure = default;
                return false;
            }

            List<EnchantData> choices = enchantDatabase.Enchants
                .Where(enchant => enchant != null &&
                                  TryFindEnchantTarget(
                                      progress,
                                      enchant,
                                      out _,
                                      out _))
                .OrderByDescending(enchant =>
                    (int)enchant.Rarity >=
                    (int)context.VictoryRewards.MinimumGuaranteedEnchantRarity)
                .ThenBy(enchant => enchant.DefinitionId)
                .Take(context.VictoryRewards.EnchantChoiceCount)
                .ToList();
            return BattleVictoryEnchantRewardService.TryCreate(
                context,
                progress.RunDeck,
                choices,
                out _,
                out failure);
        }

        private static bool EnsureConsumableRewards(
            BattleRuntimeEncounterContext context,
            out BattleVictoryConsumableRewardFailure failure)
        {
            failure = BattleVictoryConsumableRewardFailure.None;
            if (context?.VictoryRewards == null ||
                context.VictoryRewards.ConsumableItemRewardCount <= 0)
            {
                return true;
            }
            if (context.VictoryConsumableRewards != null)
            {
                return true;
            }

            return BattleVictoryConsumableRewardService.TryCreate(
                context,
                out _,
                out failure);
        }

        private static bool TryFindEnchantTarget(
            RunEncounterProgressState progress,
            EnchantData enchant,
            out RunCardInstance target,
            out int slotIndex)
        {
            target = null;
            slotIndex = -1;
            if (progress?.RunDeck == null || enchant == null)
            {
                return false;
            }

            foreach (RunCardInstance card in progress.RunDeck.Cards)
            {
                if (card?.Enchants == null)
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

        private static bool IsAvailable(
            RunCampaignState campaign,
            BattleRuntimeEncounterContext context)
        {
            return campaign != null &&
                   campaign.Phase == RunCampaignPhase.Reward &&
                   context != null &&
                   context.Settlement?.IsSettled == true &&
                   context.Settlement.SettledOutcome == BattleOutcome.Victory;
        }

        private static RunBattleRewardSnapshot EmptySnapshot(string errorText)
        {
            return new RunBattleRewardSnapshot(
                false,
                0,
                false,
                Array.Empty<RunBattleEnchantRewardOption>(),
                Array.Empty<RunBattleConsumableRewardOption>(),
                false,
                false,
                errorText);
        }
    }
}
