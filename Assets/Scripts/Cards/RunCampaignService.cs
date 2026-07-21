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
        public bool IsFinished => phase == RunCampaignPhase.Completed ||
                                  phase == RunCampaignPhase.Defeated;

        internal void Select(RunNodeChoice choice)
        {
            activeNode = choice;
            shopRerollCount = 0;
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

            int outcome = PositiveMod(
                campaign.Seed + campaign.CompletedNodeCount * 17, 3);
            if (outcome == 0)
            {
                run.AddRewardGold(20);
                result = "버려진 매표기에서 골드 20을 얻었습니다.";
            }
            else if (outcome == 1)
            {
                int damage = run.ApplyDamage(3);
                result = $"무너진 통로를 지나며 HP {damage}를 잃었습니다.";
            }
            else
            {
                run.IncreaseMaximumHealth(2);
                result = "안전한 객실을 찾아 최대 HP가 2 증가했습니다.";
            }

            FinishNonBattleNode(campaign, run);
            failure = RunCampaignFailure.None;
            return true;
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

            PrototypeConsumableDefinition item =
                PrototypeConsumableCatalog.Find(itemId);
            if (item == null)
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

            try
            {
                File.WriteAllText(DefaultPath,
                    JsonUtility.ToJson(new CampaignSaveData(campaign), true));
            }
            catch (Exception)
            {
                failure = RunCampaignFailure.SaveFailed;
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

            failure = RunCampaignFailure.None;
            return true;
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
