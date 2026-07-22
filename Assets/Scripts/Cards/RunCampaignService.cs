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

        private RunNodeChoice()
        {
        }

        public RunNodeChoice(string id, RunNodeType type, string name)
        {
            nodeId = id;
            nodeType = type;
            displayName = name;
        }

        public string NodeId => nodeId;
        public RunNodeType NodeType => nodeType;
        public string DisplayName => displayName;
        public bool IsBattle => nodeType == RunNodeType.Battle ||
                                nodeType == RunNodeType.EliteBattle ||
                                nodeType == RunNodeType.MidBoss ||
                                nodeType == RunNodeType.FinalBoss;
    }

    [Serializable]
    public sealed class RunCampaignState
    {
        public const int GeneralNodeCount = 10;
        public const int MidBossIndex = 4;
        public const int FinalBossIndex = 11;

        [SerializeField] private int seed;
        [SerializeField] private int completedNodeCount;
        [SerializeField] private int shopRerollCount;
        [SerializeField] private RunCampaignPhase phase;
        [SerializeField] private RunNodeChoice activeNode;
        [SerializeField] private List<RunShopProductSlot> shopSlots = new();
        [SerializeField] private List<RunSituationEventChoice> eventChoices = new();

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
        public int ShopRerollCount => shopRerollCount;
        public RunCampaignPhase Phase => phase;
        public RunNodeChoice ActiveNode => activeNode;
        public IReadOnlyList<RunShopProductSlot> ShopSlots =>
            shopSlots ??= new List<RunShopProductSlot>();
        public IReadOnlyList<RunSituationEventChoice> EventChoices =>
            eventChoices ??= new List<RunSituationEventChoice>();
        public bool IsFinished => phase == RunCampaignPhase.Completed ||
                                  phase == RunCampaignPhase.Defeated;

        internal void Select(RunNodeChoice choice)
        {
            activeNode = choice;
            shopRerollCount = 0;
            shopSlots ??= new List<RunShopProductSlot>();
            eventChoices ??= new List<RunSituationEventChoice>();
            shopSlots.Clear();
            eventChoices.Clear();
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
            phase = finalBoss
                ? RunCampaignPhase.Completed
                : RunCampaignPhase.NodeSelection;
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
    }

    public static class RunCampaignService
    {
        private static readonly RunNodeType[] Rotation =
        {
            RunNodeType.Battle,
            RunNodeType.Shop,
            RunNodeType.SituationEvent,
            RunNodeType.RestOrUpgrade,
            RunNodeType.EliteBattle
        };

        public static IReadOnlyList<RunNodeChoice> GetChoices(
            RunCampaignState campaign)
        {
            if (campaign == null || campaign.IsFinished ||
                campaign.Phase != RunCampaignPhase.NodeSelection)
            {
                return Array.Empty<RunNodeChoice>();
            }

            int index = campaign.CompletedNodeCount;
            if (index == RunCampaignState.MidBossIndex)
            {
                return new[] { Choice(index, 0, RunNodeType.MidBoss) };
            }

            if (index == RunCampaignState.FinalBossIndex)
            {
                return new[] { Choice(index, 0, RunNodeType.FinalBoss) };
            }

            List<RunNodeChoice> choices = new();
            int count = index == 0 ? 3 : 2 + PositiveMod(campaign.Seed + index, 3);
            count = Mathf.Clamp(count, 2, 4);
            for (int i = 0; i < count; i++)
            {
                int rotationIndex = PositiveMod(
                    campaign.Seed + index * 3 + i * 2,
                    Rotation.Length);
                RunNodeType type = Rotation[rotationIndex];
                if (index < 2 && type == RunNodeType.EliteBattle)
                {
                    type = RunNodeType.Battle;
                }

                choices.Add(Choice(index, i, type));
            }

            return choices;
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
                int offset = PositiveMod(
                    campaign.Seed + campaign.CompletedNodeCount * 17, 3);
                RunSituationEventChoice[] pool =
                {
                    new("EVENT-GOLD", RunSituationEffect.GainGold, 20,
                        "버려진 매표기를 조사한다 · 골드 20"),
                    new("EVENT-DAMAGE", RunSituationEffect.TakeDamage, 3,
                        "무너진 통로를 통과한다 · HP 3 피해"),
                    new("EVENT-HEALTH", RunSituationEffect.IncreaseMaximumHealth, 2,
                        "안전한 객실에서 쉰다 · 최대 HP +2")
                };
                List<RunSituationEventChoice> ordered = new();
                for (int i = 0; i < pool.Length; i++)
                    ordered.Add(pool[(offset + i) % pool.Length]);
                campaign.SetEventChoices(ordered);
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

            FinishNonBattleNode(campaign, run);
            failure = RunCampaignFailure.None;
            return true;
        }

        public static IReadOnlyList<RunShopProductSlot> GetShopSlots(
            RunCampaignState campaign,
            IEnumerable<ConsumableData> consumables,
            IEnumerable<EnchantData> enchants)
        {
            if (campaign == null || campaign.ActiveNode?.NodeType != RunNodeType.Shop)
                return Array.Empty<RunShopProductSlot>();
            if (campaign.ShopSlots.Count > 0) return campaign.ShopSlots;

            int seed = campaign.Seed + campaign.CompletedNodeCount * 31 +
                       campaign.ShopRerollCount * 101;
            List<RunShopProductSlot> slots = new();
            int index = 0;
            foreach (ConsumableData item in Rotate(consumables, seed, 3))
                slots.Add(new RunShopProductSlot(
                    $"SHOP-R{campaign.ShopRerollCount}-C-{index++ + 1}",
                    RunShopProductType.Consumable, item.ItemId, item.ShopPrice));
            index = 0;
            foreach (EnchantData enchant in Rotate(enchants, seed + 7, 4))
                slots.Add(new RunShopProductSlot(
                    $"SHOP-R{campaign.ShopRerollCount}-E-{index++ + 1}",
                    RunShopProductType.Enchant, enchant.DefinitionId,
                    EnchantShopPrice(enchant)));
            campaign.SetShopSlots(slots);
            return campaign.ShopSlots;
        }

        public static bool TryRest(
            RunCampaignState campaign,
            RunBattleState run,
            out int healed,
            out RunCampaignFailure failure)
        {
            healed = 0;
            if (!ValidateNode(campaign, run, RunNodeType.RestOrUpgrade,
                    out failure))
            {
                return false;
            }

            healed = run.ApplyHealing(
                Mathf.CeilToInt(run.MaximumHealth * 0.3f));
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
            if (progress?.RunState == null || progress.RunDeck == null)
            {
                failure = RunCampaignFailure.InvalidState;
                return false;
            }

            if (!ValidateNode(campaign, progress.RunState,
                    RunNodeType.RestOrUpgrade, out failure))
            {
                return false;
            }

            RunCardInstance card = progress.RunDeck.Find(ownedCardId);
            if (card == null)
            {
                failure = RunCampaignFailure.InvalidCard;
                return false;
            }

            card.SetLevel(card.CurrentLevel + 1);
            FinishNonBattleNode(campaign, progress.RunState);
            failure = RunCampaignFailure.None;
            return true;
        }

        public static int GetShopRerollCost(RunCampaignState campaign)
        {
            return 10 + (campaign?.ShopRerollCount ?? 0) * 5;
        }

        public static bool TryRerollShop(
            RunCampaignState campaign,
            RunBattleState run,
            out RunCampaignFailure failure)
        {
            if (!ValidateNode(campaign, run, RunNodeType.Shop, out failure))
            {
                return false;
            }

            if (!run.TrySpendGold(GetShopRerollCost(campaign)))
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

            if (!IsAvailableShopProduct(campaign,
                    RunShopProductType.Consumable, itemId))
            {
                failure = RunCampaignFailure.InvalidChoice;
                return false;
            }

            if (!run.TrySpendGold(item.ShopPrice))
            {
                failure = RunCampaignFailure.InsufficientGold;
                return false;
            }

            run.TryAddRewardConsumableItem(item.ItemId);
            MarkPurchasedSlot(campaign, RunShopProductType.Consumable, itemId);
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
            if (progress?.RunState == null || progress.RunDeck == null)
            {
                failure = RunCampaignFailure.InvalidState;
                return false;
            }

            if (!ValidateNode(campaign, progress.RunState,
                    RunNodeType.Shop, out failure))
            {
                return false;
            }

            RunCardInstance card = progress.RunDeck.Find(ownedCardId);
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

            if (!IsAvailableShopProduct(campaign,
                    RunShopProductType.Enchant, enchant.DefinitionId))
            {
                failure = RunCampaignFailure.InvalidChoice;
                return false;
            }

            if (!progress.RunState.TrySpendGold(price))
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

            MarkPurchasedSlot(campaign, RunShopProductType.Enchant,
                enchant.DefinitionId);
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
                });
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

        private static int EnchantShopPrice(EnchantData enchant)
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
