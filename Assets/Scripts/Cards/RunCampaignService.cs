using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HaveABreak.Cards
{
    public enum RunNodeType
    {
        Battle,
        EliteBattle,
        Shop,
        SituationEvent,
        RestOrUpgrade,
        MidBoss,
        FinalBoss
    }

    public enum RunCampaignPhase
    {
        NodeSelection,
        NodeResolution,
        Battle,
        Reward,
        Completed,
        Defeated
    }

    public enum RunCampaignFailure
    {
        None,
        InvalidState,
        InvalidChoice,
        InvalidPhase,
        RunEnded,
        InsufficientGold,
        InvalidCard,
        InvalidEnchant,
        AttachmentFailed,
        SaveFailed,
        LoadFailed,
        UnsupportedVersion
    }

    public enum RunShopProductType
    {
        Consumable,
        Enchant
    }

    public enum RunSituationEffect
    {
        GainGold,
        TakeDamage,
        IncreaseMaximumHealth
    }

    [Serializable]
    public sealed class RunShopProductSlot
    {
        [SerializeField] private string slotId;
        [SerializeField] private RunShopProductType productType;
        [SerializeField] private string contentId;
        [SerializeField] private int price;
        [SerializeField] private bool purchased;

        public RunShopProductSlot(string id, RunShopProductType type,
            string productContentId, int productPrice)
        {
            slotId = id;
            productType = type;
            contentId = productContentId;
            price = Mathf.Max(0, productPrice);
        }

        public string SlotId => slotId;
        public RunShopProductType ProductType => productType;
        public string ContentId => contentId;
        public int Price => price;
        public bool Purchased => purchased;
        internal void MarkPurchased() => purchased = true;
    }

    [Serializable]
    public sealed class RunSituationEventChoice
    {
        [SerializeField] private string choiceId;
        [SerializeField] private RunSituationEffect effect;
        [SerializeField] private int value;
        [SerializeField] private string displayText;

        public RunSituationEventChoice(string id, RunSituationEffect choiceEffect,
            int effectValue, string text)
        {
            choiceId = id;
            effect = choiceEffect;
            value = effectValue;
            displayText = text;
        }

        public string ChoiceId => choiceId;
        public RunSituationEffect Effect => effect;
        public int Value => value;
        public string DisplayText => displayText;
    }

    [Serializable]
    public sealed class RunNodeChoice
    {
        [SerializeField] private string nodeId;
        [SerializeField] private RunNodeType nodeType;
        [SerializeField] private string displayName;
        [SerializeField] private string previousNodeId;

        private RunNodeChoice()
        {
        }

        public RunNodeChoice(string id, RunNodeType type, string name,
            string connectedFromNodeId = null)
        {
            nodeId = id;
            nodeType = type;
            displayName = name;
            previousNodeId = connectedFromNodeId;
        }

        public string NodeId => nodeId;
        public RunNodeType NodeType => nodeType;
        public string DisplayName => displayName;
        public string PreviousNodeId => previousNodeId;
        public bool IsBattle => nodeType == RunNodeType.Battle ||
                                nodeType == RunNodeType.EliteBattle ||
                                nodeType == RunNodeType.MidBoss ||
                                nodeType == RunNodeType.FinalBoss;
    }

    [Serializable]
    public sealed class RunCampaignState
    {
        [SerializeField] private int seed;
        [SerializeField] private int completedNodeCount;
        [SerializeField] private int shopRerollCount;
        [SerializeField] private RunCampaignPhase phase;
        [SerializeField] private RunNodeChoice activeNode;
        [SerializeField] private List<RunShopProductSlot> shopSlots = new();
        [SerializeField] private List<RunSituationEventChoice> eventChoices = new();
        [SerializeField] private string activeSituationEventId;
        [SerializeField] private List<string> resolvedSituationEventIds = new();
        [SerializeField] private List<RunNodeChoice> availableNodeChoices = new();
        [SerializeField] private List<string> selectedNodePath = new();

        private RunCampaignState()
        {
        }

        public RunCampaignState(int seed)
        {
            this.seed = seed;
            phase = RunCampaignPhase.NodeSelection;
        }

        public int Seed => seed;
        public int CompletedNodeCount => completedNodeCount;
        public int Act => Mathf.Clamp(completedNodeCount / 4 + 1, 1, 3);
        public int GetAct(RunStartProgressionConfig config) =>
            config == null ? Act : config.GetAct(completedNodeCount);
        public int ShopRerollCount => shopRerollCount;
        public RunCampaignPhase Phase => phase;
        public RunNodeChoice ActiveNode => activeNode;
        public IReadOnlyList<RunShopProductSlot> ShopSlots =>
            shopSlots ??= new List<RunShopProductSlot>();
        public IReadOnlyList<RunSituationEventChoice> EventChoices =>
            eventChoices ??= new List<RunSituationEventChoice>();
        public string ActiveSituationEventId => activeSituationEventId;
        public IReadOnlyList<string> ResolvedSituationEventIds =>
            resolvedSituationEventIds ??= new List<string>();
        public IReadOnlyList<RunNodeChoice> AvailableNodeChoices =>
            availableNodeChoices ??= new List<RunNodeChoice>();
        public IReadOnlyList<string> SelectedNodePath =>
            selectedNodePath ??= new List<string>();
        public bool IsFinished => phase == RunCampaignPhase.Completed ||
                                  phase == RunCampaignPhase.Defeated;

        internal void Select(RunNodeChoice choice)
        {
            activeNode = choice;
            selectedNodePath ??= new List<string>();
            selectedNodePath.Add(choice.NodeId);
            availableNodeChoices?.Clear();
            shopRerollCount = 0;
            shopSlots ??= new List<RunShopProductSlot>();
            eventChoices ??= new List<RunSituationEventChoice>();
            shopSlots.Clear();
            eventChoices.Clear();
            activeSituationEventId = null;
            phase = choice.IsBattle
                ? RunCampaignPhase.Battle
                : RunCampaignPhase.NodeResolution;
        }

        internal void EnterReward()
        {
            phase = RunCampaignPhase.Reward;
        }

        internal void CompleteNode()
        {
            completedNodeCount++;
            bool finalBoss = activeNode?.NodeType == RunNodeType.FinalBoss;
            activeNode = null;
            shopRerollCount = 0;
            shopSlots?.Clear();
            eventChoices?.Clear();
            activeSituationEventId = null;
            phase = finalBoss
                ? RunCampaignPhase.Completed
                : RunCampaignPhase.NodeSelection;
        }

        internal void SetAvailableNodeChoices(IEnumerable<RunNodeChoice> values)
        {
            availableNodeChoices = values == null
                ? new List<RunNodeChoice>()
                : new List<RunNodeChoice>(values);
        }

        internal void MarkDefeated()
        {
            phase = RunCampaignPhase.Defeated;
        }

        internal void IncrementShopReroll()
        {
            shopRerollCount++;
            shopSlots?.Clear();
        }

        internal void SetShopSlots(IEnumerable<RunShopProductSlot> values)
        {
            shopSlots = values == null
                ? new List<RunShopProductSlot>()
                : new List<RunShopProductSlot>(values);
        }

        internal void SetEventChoices(IEnumerable<RunSituationEventChoice> values)
        {
            eventChoices = values == null
                ? new List<RunSituationEventChoice>()
                : new List<RunSituationEventChoice>(values);
        }

        internal void SetSituationEvent(
            string eventId, IEnumerable<RunSituationEventChoice> values)
        {
            activeSituationEventId = eventId?.Trim();
            SetEventChoices(values);
        }

        internal bool HasResolvedSituationEvent(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId)) return false;
            resolvedSituationEventIds ??= new List<string>();
            return resolvedSituationEventIds.Exists(value => string.Equals(
                value, eventId.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        internal void RecordResolvedSituationEvent(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId) ||
                HasResolvedSituationEvent(eventId)) return;
            resolvedSituationEventIds.Add(eventId.Trim());
        }
    }

    public static class RunCampaignService
    {
        private static SituationEventDatabase SituationEvents =>
            Resources.Load<RuntimePrototypeConfig>(
                "GameData/RuntimePrototypeConfig")?.SituationEventDatabase;
        private static RunNodeGenerationConfig NodeGeneration =>
            Resources.Load<RuntimePrototypeConfig>(
                "GameData/RuntimePrototypeConfig")?.RunNodeGenerationConfig;

        public static IReadOnlyList<RunNodeChoice> GetChoices(
            RunCampaignState campaign)
        {
            if (campaign == null || campaign.IsFinished ||
                campaign.Phase != RunCampaignPhase.NodeSelection)
            {
                return Array.Empty<RunNodeChoice>();
            }

            if (campaign.AvailableNodeChoices.Count > 0)
            {
                return campaign.AvailableNodeChoices;
            }

            int index = campaign.CompletedNodeCount;
            RunNodeGenerationConfig generation = NodeGeneration;
            if (generation == null || generation.GetValidationErrors().Count > 0)
                return Array.Empty<RunNodeChoice>();
            if (index == generation.MidBossIndex)
            {
                campaign.SetAvailableNodeChoices(new[]
                    { Choice(campaign, index, 0, RunNodeType.MidBoss) });
                return campaign.AvailableNodeChoices;
            }

            if (index == generation.FinalBossIndex)
            {
                campaign.SetAvailableNodeChoices(new[]
                    { Choice(campaign, index, 0, RunNodeType.FinalBoss) });
                return campaign.AvailableNodeChoices;
            }

            List<RunNodeChoice> choices = new();
            int count = index == 0
                ? generation.OpeningChoiceCount
                : generation.MinimumChoiceCount + PositiveMod(
                    campaign.Seed + index,
                    generation.MaximumChoiceCount -
                    generation.MinimumChoiceCount + 1);
            count = Mathf.Clamp(count, generation.MinimumChoiceCount,
                generation.MaximumChoiceCount);
            for (int i = 0; i < count; i++)
            {
                int rotationIndex = PositiveMod(
                    campaign.Seed + index * 3 + i * 2,
                    generation.GeneralNodePool.Count);
                RunNodeType type = generation.GeneralNodePool[rotationIndex];
                if (index < generation.EliteUnlockIndex &&
                    type == RunNodeType.EliteBattle)
                {
                    type = RunNodeType.Battle;
                }

                choices.Add(Choice(campaign, index, i, type));
            }
            campaign.SetAvailableNodeChoices(choices);
            return campaign.AvailableNodeChoices;
        }

        public static bool TrySelectNode(
            RunCampaignState campaign,
            string nodeId,
            out RunCampaignFailure failure)
        {
            if (campaign == null || campaign.IsFinished)
            {
                failure = RunCampaignFailure.InvalidState;
                return false;
            }

            foreach (RunNodeChoice choice in GetChoices(campaign))
            {
                if (string.Equals(choice.NodeId, nodeId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    campaign.Select(choice);
                    failure = RunCampaignFailure.None;
                    return true;
                }
            }

            failure = RunCampaignFailure.InvalidChoice;
            return false;
        }

        public static bool TryResolveSituationEvent(
            RunCampaignState campaign,
            RunBattleState run,
            out string result,
            out RunCampaignFailure failure)
        {
            result = null;
            if (!ValidateNode(campaign, run, RunNodeType.SituationEvent,
                    out failure))
            {
                return false;
            }

            IReadOnlyList<RunSituationEventChoice> choices =
                GetSituationEventChoices(campaign);
            return TryResolveSituationEvent(campaign, run,
                choices.Count > 0 ? choices[0].ChoiceId : null,
                out result, out failure);
        }

        public static IReadOnlyList<RunSituationEventChoice> GetSituationEventChoices(
            RunCampaignState campaign)
        {
            if (campaign == null || campaign.ActiveNode?.NodeType !=
                RunNodeType.SituationEvent)
            {
                return Array.Empty<RunSituationEventChoice>();
            }

            if (campaign.EventChoices.Count == 0)
            {
                IReadOnlyList<SituationEventData> events = SituationEvents?.Events;
                if (events == null || events.Count == 0)
                    return Array.Empty<RunSituationEventChoice>();

                List<SituationEventData> eligible = new();
                foreach (SituationEventData candidate in events)
                {
                    if (candidate == null ||
                        !candidate.IsAvailableAt(campaign.CompletedNodeCount) ||
                        (!candidate.AllowRepeatInRun &&
                         campaign.HasResolvedSituationEvent(candidate.EventId)))
                    {
                        continue;
                    }
                    eligible.Add(candidate);
                }
                if (eligible.Count == 0)
                    return Array.Empty<RunSituationEventChoice>();

                SituationEventData selected = eligible[PositiveMod(
                    campaign.Seed + campaign.CompletedNodeCount * 17,
                    eligible.Count)];
                List<RunSituationEventChoice> choices = new();
                foreach (SituationEventChoiceData choice in selected.Choices)
                    choices.Add(new RunSituationEventChoice(choice.ChoiceId,
                        choice.Effect, choice.Value, choice.DisplayText));
                campaign.SetSituationEvent(selected.EventId, choices);
            }

            return campaign.EventChoices;
        }

        public static bool TryResolveSituationEvent(
            RunCampaignState campaign,
            RunBattleState run,
            string choiceId,
            out string result,
            out RunCampaignFailure failure)
        {
            result = null;
            if (!ValidateNode(campaign, run, RunNodeType.SituationEvent,
                    out failure)) return false;

            RunSituationEventChoice choice = null;
            foreach (RunSituationEventChoice candidate in
                     GetSituationEventChoices(campaign))
            {
                if (string.Equals(candidate.ChoiceId, choiceId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    choice = candidate;
                    break;
                }
            }

            if (choice == null)
            {
                failure = RunCampaignFailure.InvalidChoice;
                return false;
            }

            switch (choice.Effect)
            {
                case RunSituationEffect.GainGold:
                    run.AddRewardGold(choice.Value);
                    result = $"골드 {choice.Value}을 얻었습니다.";
                    break;
                case RunSituationEffect.TakeDamage:
                    int damage = run.ApplyDamage(choice.Value);
                    result = $"HP {damage}를 잃었습니다.";
                    break;
                default:
                    run.IncreaseMaximumHealth(choice.Value);
                    result = $"최대 HP가 {choice.Value} 증가했습니다.";
                    break;
            }

            campaign.RecordResolvedSituationEvent(
                campaign.ActiveSituationEventId);
            FinishNonBattleNode(campaign, run);
            failure = RunCampaignFailure.None;
            return true;
        }

        public static IReadOnlyList<RunShopProductSlot> GetShopSlots(
            RunCampaignState campaign,
            IEnumerable<ConsumableData> consumables,
            IEnumerable<EnchantData> enchants)
        {
            return GetShopSlots(campaign, consumables, enchants, null);
        }

        public static IReadOnlyList<RunShopProductSlot> GetShopSlots(
            RunCampaignState campaign,
            IEnumerable<ConsumableData> consumables,
            IEnumerable<EnchantData> enchants,
            ShopEconomyConfig config)
        {
            if (campaign == null || campaign.ActiveNode?.NodeType != RunNodeType.Shop)
                return Array.Empty<RunShopProductSlot>();
            if (campaign.ShopSlots.Count > 0) return campaign.ShopSlots;

            int seed = campaign.Seed + campaign.CompletedNodeCount * 31 +
                       campaign.ShopRerollCount * 101;
            List<RunShopProductSlot> slots = new();
            int index = 0;
            int consumableCount = config?.ConsumableOfferCount ?? 3;
            int enchantCount = config?.EnchantOfferCount ?? 4;
            foreach (ConsumableData item in Rotate(
                         consumables, seed, consumableCount))
                slots.Add(new RunShopProductSlot(
                    $"SHOP-R{campaign.ShopRerollCount}-C-{index++ + 1}",
                    RunShopProductType.Consumable, item.ItemId, item.ShopPrice));
            index = 0;
            foreach (EnchantData enchant in Rotate(
                         enchants, seed + 7, enchantCount))
                slots.Add(new RunShopProductSlot(
                    $"SHOP-R{campaign.ShopRerollCount}-E-{index++ + 1}",
                    RunShopProductType.Enchant, enchant.DefinitionId,
                    config?.GetEnchantPrice(enchant.Rarity) ??
                    LegacyEnchantShopPrice(enchant)));
            campaign.SetShopSlots(slots);
            return campaign.ShopSlots;
        }

        public static bool TryRest(
            RunCampaignState campaign,
            RunBattleState run,
            out int healed,
            out RunCampaignFailure failure)
        {
            return TryRest(campaign, run, null, out healed, out failure);
        }

        public static bool TryRest(
            RunCampaignState campaign,
            RunBattleState run,
            RestUpgradeConfig config,
            out int healed,
            out RunCampaignFailure failure)
        {
            healed = 0;
            if (!ValidateNode(campaign, run, RunNodeType.RestOrUpgrade,
                    out failure))
            {
                return false;
            }

            healed = run.ApplyHealing(config == null
                ? Mathf.CeilToInt(run.MaximumHealth * 0.3f)
                : config.GetHealingAmount(run.MaximumHealth));
            FinishNonBattleNode(campaign, run);
            failure = RunCampaignFailure.None;
            return true;
        }

        public static bool TryUpgrade(
            RunCampaignState campaign,
            RunEncounterProgressState progress,
            string ownedCardId,
            out RunCampaignFailure failure)
        {
            return TryUpgrade(campaign, progress, ownedCardId, null, out failure);
        }

        public static bool TryUpgrade(
            RunCampaignState campaign,
            RunEncounterProgressState progress,
            string ownedCardId,
            RestUpgradeConfig config,
            out RunCampaignFailure failure)
        {
            if (progress?.RunState == null || progress.OwnedCards == null)
            {
                failure = RunCampaignFailure.InvalidState;
                return false;
            }

            if (!ValidateNode(campaign, progress.RunState,
                    RunNodeType.RestOrUpgrade, out failure))
            {
                return false;
            }

            RunCardInstance card = progress.OwnedCards.Find(ownedCardId);
            if (card == null)
            {
                failure = RunCampaignFailure.InvalidCard;
                return false;
            }

            card.SetLevel(card.CurrentLevel +
                          (config?.UpgradeLevelIncrease ?? 1));
            FinishNonBattleNode(campaign, progress.RunState);
            failure = RunCampaignFailure.None;
            return true;
        }

        public static int GetShopRerollCost(RunCampaignState campaign)
        {
            return 10 + (campaign?.ShopRerollCount ?? 0) * 5;
        }

        public static int GetShopRerollCost(
            RunCampaignState campaign,
            ShopEconomyConfig config)
        {
            return config?.GetRerollCost(campaign?.ShopRerollCount ?? 0) ?? 0;
        }

        public static bool TryRerollShop(
            RunCampaignState campaign,
            RunBattleState run,
            out RunCampaignFailure failure)
        {
            return TryRerollShop(campaign, run, null, out failure);
        }

        public static bool TryRerollShop(
            RunCampaignState campaign,
            RunBattleState run,
            ShopEconomyConfig config,
            out RunCampaignFailure failure)
        {
            if (!ValidateNode(campaign, run, RunNodeType.Shop, out failure))
            {
                return false;
            }

            int cost = config == null
                ? GetShopRerollCost(campaign)
                : GetShopRerollCost(campaign, config);
            if (!run.TrySpendGold(cost))
            {
                failure = RunCampaignFailure.InsufficientGold;
                return false;
            }

            campaign.IncrementShopReroll();
            failure = RunCampaignFailure.None;
            return true;
        }

        public static bool TryBuyConsumable(
            RunCampaignState campaign,
            RunBattleState run,
            string itemId,
            out RunCampaignFailure failure)
        {
            if (!ValidateNode(campaign, run, RunNodeType.Shop, out failure))
            {
                return false;
            }

            ConsumableData item =
                PrototypeConsumableCatalog.Find(itemId);
            if (item == null)
            {
                failure = RunCampaignFailure.InvalidChoice;
                return false;
            }

            RunShopProductSlot slot = FindAvailableShopProduct(
                campaign, RunShopProductType.Consumable, itemId);
            if (campaign.ShopSlots.Count > 0 && slot == null)
            {
                failure = RunCampaignFailure.InvalidChoice;
                return false;
            }

            int price = slot?.Price ?? item.ShopPrice;
            if (!run.CanSpendGold(price))
            {
                failure = RunCampaignFailure.InsufficientGold;
                return false;
            }

            if (!run.TryAddRewardConsumableItem(item.ItemId) ||
                !run.TrySpendGold(price))
            {
                run.RemoveConsumableItem(item.ItemId);
                failure = RunCampaignFailure.InvalidChoice;
                return false;
            }
            MarkPurchasedSlot(campaign, RunShopProductType.Consumable, itemId);
            failure = RunCampaignFailure.None;
            return true;
        }

        public static bool TryBuyConsumableSlot(
            RunCampaignState campaign,
            RunBattleState run,
            string slotId,
            out RunCampaignFailure failure)
        {
            if (!ValidateNode(campaign, run, RunNodeType.Shop, out failure))
                return false;

            RunShopProductSlot slot = FindAvailableShopSlot(
                campaign, slotId, RunShopProductType.Consumable);
            ConsumableData item = slot == null
                ? null
                : PrototypeConsumableCatalog.Find(slot.ContentId);
            if (slot == null || item == null)
            {
                failure = RunCampaignFailure.InvalidChoice;
                return false;
            }

            if (!run.CanSpendGold(slot.Price))
            {
                failure = RunCampaignFailure.InsufficientGold;
                return false;
            }

            if (!run.TryAddRewardConsumableItem(item.ItemId) ||
                !run.TrySpendGold(slot.Price))
            {
                run.RemoveConsumableItem(item.ItemId);
                failure = RunCampaignFailure.InvalidChoice;
                return false;
            }
            slot.MarkPurchased();
            failure = RunCampaignFailure.None;
            return true;
        }

        public static bool TryBuyEnchant(
            RunCampaignState campaign,
            RunEncounterProgressState progress,
            EnchantData enchant,
            string ownedCardId,
            int slotIndex,
            int price,
            out EnchantAttachmentFailure attachmentFailure,
            out RunCampaignFailure failure)
        {
            attachmentFailure = EnchantAttachmentFailure.None;
            if (progress?.RunState == null || progress.OwnedCards == null)
            {
                failure = RunCampaignFailure.InvalidState;
                return false;
            }

            if (!ValidateNode(campaign, progress.RunState,
                    RunNodeType.Shop, out failure))
            {
                return false;
            }

            RunCardInstance card = progress.OwnedCards.Find(ownedCardId);
            if (card == null)
            {
                failure = RunCampaignFailure.InvalidCard;
                return false;
            }

            if (enchant == null || !card.Enchants.CanAttach(
                    enchant, slotIndex, out attachmentFailure))
            {
                failure = RunCampaignFailure.InvalidEnchant;
                return false;
            }

            RunShopProductSlot shopSlot = FindAvailableShopProduct(
                campaign, RunShopProductType.Enchant, enchant.DefinitionId);
            if (campaign.ShopSlots.Count > 0 && shopSlot == null)
            {
                failure = RunCampaignFailure.InvalidChoice;
                return false;
            }

            int transactionPrice = shopSlot?.Price ?? price;
            if (!progress.RunState.CanSpendGold(transactionPrice))
            {
                failure = RunCampaignFailure.InsufficientGold;
                return false;
            }

            if (!card.Enchants.TryAttach(
                    enchant, slotIndex, false, out attachmentFailure))
            {
                failure = RunCampaignFailure.AttachmentFailed;
                return false;
            }

            if (!progress.RunState.TrySpendGold(transactionPrice))
            {
                card.Enchants.TryRemove(slotIndex, false, out _);
                failure = RunCampaignFailure.InsufficientGold;
                return false;
            }

            MarkPurchasedSlot(campaign, RunShopProductType.Enchant,
                enchant.DefinitionId);
            failure = RunCampaignFailure.None;
            return true;
        }

        public static bool TryBuyEnchantSlot(
            RunCampaignState campaign,
            RunEncounterProgressState progress,
            EnchantData enchant,
            string shopSlotId,
            string ownedCardId,
            int enchantSlotIndex,
            out EnchantAttachmentFailure attachmentFailure,
            out RunCampaignFailure failure)
        {
            attachmentFailure = EnchantAttachmentFailure.None;
            if (progress?.RunState == null || progress.OwnedCards == null)
            {
                failure = RunCampaignFailure.InvalidState;
                return false;
            }

            if (!ValidateNode(campaign, progress.RunState,
                    RunNodeType.Shop, out failure)) return false;

            RunShopProductSlot shopSlot = FindAvailableShopSlot(
                campaign, shopSlotId, RunShopProductType.Enchant);
            if (shopSlot == null || enchant == null ||
                !string.Equals(shopSlot.ContentId, enchant.DefinitionId,
                    StringComparison.OrdinalIgnoreCase))
            {
                failure = RunCampaignFailure.InvalidChoice;
                return false;
            }

            RunCardInstance card = progress.OwnedCards.Find(ownedCardId);
            if (card == null)
            {
                failure = RunCampaignFailure.InvalidCard;
                return false;
            }

            if (!card.Enchants.CanAttach(enchant, enchantSlotIndex,
                    out attachmentFailure))
            {
                failure = RunCampaignFailure.InvalidEnchant;
                return false;
            }

            if (!progress.RunState.CanSpendGold(shopSlot.Price))
            {
                failure = RunCampaignFailure.InsufficientGold;
                return false;
            }

            if (!card.Enchants.TryAttach(enchant, enchantSlotIndex, false,
                    out attachmentFailure))
            {
                failure = RunCampaignFailure.AttachmentFailed;
                return false;
            }

            if (!progress.RunState.TrySpendGold(shopSlot.Price))
            {
                card.Enchants.TryRemove(enchantSlotIndex, false, out _);
                failure = RunCampaignFailure.InsufficientGold;
                return false;
            }

            shopSlot.MarkPurchased();
            failure = RunCampaignFailure.None;
            return true;
        }

        public static bool TryLeaveShop(
            RunCampaignState campaign,
            RunBattleState run,
            out RunCampaignFailure failure)
        {
            if (!ValidateNode(campaign, run, RunNodeType.Shop, out failure))
            {
                return false;
            }

            campaign.CompleteNode();
            failure = RunCampaignFailure.None;
            return true;
        }

        public static void MarkBattleReward(
            RunCampaignState campaign,
            BattleOutcome outcome)
        {
            if (campaign == null)
            {
                return;
            }

            if (outcome == BattleOutcome.Defeat)
            {
                campaign.MarkDefeated();
            }
            else if (outcome == BattleOutcome.Victory)
            {
                campaign.EnterReward();
            }
        }

        public static void CompleteBattleReward(RunCampaignState campaign)
        {
            if (campaign?.Phase == RunCampaignPhase.Reward)
            {
                campaign.CompleteNode();
            }
        }

        private static RunNodeChoice Choice(
            RunCampaignState campaign,
            int nodeIndex,
            int branch,
            RunNodeType type)
        {
            return new RunNodeChoice(
                $"NODE-{nodeIndex + 1:00}-{branch + 1}",
                type,
                type switch
                {
                    RunNodeType.Battle => "전투",
                    RunNodeType.EliteBattle => "엘리트 전투",
                    RunNodeType.Shop => "상점",
                    RunNodeType.SituationEvent => "상황 이벤트",
                    RunNodeType.RestOrUpgrade => "회복 · 강화",
                    RunNodeType.MidBoss => "중간보스",
                    _ => "보스"
                },
                campaign.SelectedNodePath.Count > 0
                    ? campaign.SelectedNodePath[campaign.SelectedNodePath.Count - 1]
                    : null);
        }

        private static bool ValidateNode(
            RunCampaignState campaign,
            RunBattleState run,
            RunNodeType expected,
            out RunCampaignFailure failure)
        {
            if (campaign == null || run == null || campaign.ActiveNode == null)
            {
                failure = RunCampaignFailure.InvalidState;
                return false;
            }

            if (run.RunEnded)
            {
                campaign.MarkDefeated();
                failure = RunCampaignFailure.RunEnded;
                return false;
            }

            if (campaign.Phase != RunCampaignPhase.NodeResolution ||
                campaign.ActiveNode.NodeType != expected)
            {
                failure = RunCampaignFailure.InvalidPhase;
                return false;
            }

            failure = RunCampaignFailure.None;
            return true;
        }

        private static void FinishNonBattleNode(
            RunCampaignState campaign,
            RunBattleState run)
        {
            if (run.RunEnded)
            {
                campaign.MarkDefeated();
            }
            else
            {
                campaign.CompleteNode();
            }
        }

        private static int PositiveMod(int value, int divisor)
        {
            int result = value % divisor;
            return result < 0 ? result + divisor : result;
        }

        private static IEnumerable<T> Rotate<T>(
            IEnumerable<T> source, int seed, int count) where T : class
        {
            List<T> values = source == null
                ? new List<T>()
                : new List<T>(source);
            values.RemoveAll(value => value == null);
            if (values.Count == 0) yield break;
            int start = PositiveMod(seed, values.Count);
            int take = Mathf.Min(count, values.Count);
            for (int i = 0; i < take; i++)
                yield return values[(start + i) % values.Count];
        }

        private static int LegacyEnchantShopPrice(EnchantData enchant)
        {
            return enchant.Rarity switch
            {
                CardRarity.Legendary => 120,
                CardRarity.Rare => 80,
                _ => 45
            };
        }

        private static void MarkPurchasedSlot(
            RunCampaignState campaign, RunShopProductType type, string contentId)
        {
            if (campaign == null) return;
            foreach (RunShopProductSlot slot in campaign.ShopSlots)
            {
                if (!slot.Purchased && slot.ProductType == type &&
                    string.Equals(slot.ContentId, contentId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    slot.MarkPurchased();
                    return;
                }
            }
        }

        private static bool IsAvailableShopProduct(
            RunCampaignState campaign, RunShopProductType type, string contentId)
        {
            if (campaign == null || campaign.ShopSlots.Count == 0) return true;
            foreach (RunShopProductSlot slot in campaign.ShopSlots)
            {
                if (!slot.Purchased && slot.ProductType == type &&
                    string.Equals(slot.ContentId, contentId,
                        StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static RunShopProductSlot FindAvailableShopProduct(
            RunCampaignState campaign, RunShopProductType type, string contentId)
        {
            if (campaign == null) return null;
            foreach (RunShopProductSlot slot in campaign.ShopSlots)
            {
                if (!slot.Purchased && slot.ProductType == type &&
                    string.Equals(slot.ContentId, contentId,
                        StringComparison.OrdinalIgnoreCase)) return slot;
            }
            return null;
        }

        private static RunShopProductSlot FindAvailableShopSlot(
            RunCampaignState campaign, string slotId, RunShopProductType type)
        {
            if (campaign == null || string.IsNullOrWhiteSpace(slotId)) return null;
            foreach (RunShopProductSlot slot in campaign.ShopSlots)
            {
                if (!slot.Purchased && slot.ProductType == type &&
                    string.Equals(slot.SlotId, slotId,
                        StringComparison.OrdinalIgnoreCase)) return slot;
            }
            return null;
        }
    }

    public static class IntegratedRunSaveService
    {
        private const int SchemaVersion = 1;
        private const string FileName = "integrated-run-campaign.json";

        [Serializable]
        private sealed class CampaignSaveData
        {
            [SerializeField] private int schemaVersion;
            [SerializeField] private RunCampaignState campaign;

            public CampaignSaveData()
            {
            }

            public CampaignSaveData(RunCampaignState value)
            {
                schemaVersion = SchemaVersion;
                campaign = value;
            }

            public int Version => schemaVersion;
            public RunCampaignState Campaign => campaign;
        }

        public static string DefaultPath => Path.Combine(
            Application.persistentDataPath, FileName);

        public static bool TrySave(
            RunCampaignState campaign,
            RunEncounterProgressState progress,
            out RunSaveDestination destination,
            out RunCampaignFailure failure)
        {
            destination = RunSaveDestination.None;
            if (campaign == null || progress == null)
            {
                failure = RunCampaignFailure.InvalidState;
                return false;
            }

            if (!RunSaveService.TrySaveDefault(
                    progress,
                    out destination,
                    out _,
                    out _,
                    out _))
            {
                failure = RunCampaignFailure.SaveFailed;
                return false;
            }

            if (!PlayerPermanentRewardSaveService.TrySaveDefault(
                    progress.PermanentRewards, out _, out _))
            {
                failure = RunCampaignFailure.SaveFailed;
                return false;
            }

            if (!TryWriteCampaignAtomically(campaign))
            {
                failure = RunCampaignFailure.SaveFailed;
                return false;
            }

            failure = RunCampaignFailure.None;
            return true;
        }

        private static bool TryWriteCampaignAtomically(
            RunCampaignState campaign)
        {
            string temporaryPath = DefaultPath + ".tmp";
            string backupPath = DefaultPath + ".bak";
            try
            {
                string directory = Path.GetDirectoryName(DefaultPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(
                    temporaryPath,
                    JsonUtility.ToJson(new CampaignSaveData(campaign), true));
                if (File.Exists(DefaultPath))
                {
                    TryDeleteFile(backupPath);
                    File.Replace(temporaryPath, DefaultPath, backupPath);
                    TryDeleteFile(backupPath);
                }
                else
                {
                    File.Move(temporaryPath, DefaultPath);
                }

                return true;
            }
            catch (Exception)
            {
                try
                {
                    if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
                }
                catch (Exception)
                {
                    // The original save failure is the actionable result.
                }

                return false;
            }
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception)
            {
                // Backup cleanup must not invalidate a completed save.
            }
        }

        public static bool TryLoad(
            CardDatabase cardDatabase,
            EnchantDatabase enchantDatabase,
            EncounterDatabase encounterDatabase,
            PlayerPermanentRewardState permanentRewards,
            out RunCampaignState campaign,
            out RunEncounterProgressState progress,
            out EncounterData activeEncounter,
            out RunResumeSource source,
            out RunCampaignFailure failure)
        {
            campaign = null;
            progress = null;
            activeEncounter = null;
            source = RunResumeSource.None;
            CampaignSaveData data;
            try
            {
                if (!File.Exists(DefaultPath))
                {
                    failure = RunCampaignFailure.LoadFailed;
                    return false;
                }

                data = JsonUtility.FromJson<CampaignSaveData>(
                    File.ReadAllText(DefaultPath));
            }
            catch (Exception)
            {
                failure = RunCampaignFailure.LoadFailed;
                return false;
            }

            if (data == null || data.Version != SchemaVersion ||
                data.Campaign == null)
            {
                failure = data != null && data.Version != SchemaVersion
                    ? RunCampaignFailure.UnsupportedVersion
                    : RunCampaignFailure.LoadFailed;
                return false;
            }

            if (!RunResumeService.TryLoadDefault(
                    cardDatabase,
                    enchantDatabase,
                    encounterDatabase,
                    permanentRewards,
                    out progress,
                    out source,
                    out activeEncounter,
                    out _,
                    out _,
                    out _))
            {
                progress = null;
                failure = RunCampaignFailure.LoadFailed;
                return false;
            }

            campaign = data.Campaign;
            failure = RunCampaignFailure.None;
            return true;
        }
    }
}
