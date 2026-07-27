using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class FullRunPlayerActionEndToEndValidation
    {
        private sealed class PlannedNode
        {
            internal PlannedNode(string nodeId, RunNodeType nodeType)
            {
                NodeId = nodeId;
                NodeType = nodeType;
            }

            internal string NodeId { get; }
            internal RunNodeType NodeType { get; }
        }

        private sealed class MemoryCheckpointWriter :
            IBattleStartCheckpointWriter
        {
            internal int CallCount { get; private set; }

            public bool TrySave(
                RunCampaignState campaign,
                RunEncounterProgressState progress,
                out string destination,
                out RunCampaignFailure failure)
            {
                CallCount++;
                destination = "PlayerActionFullRunMemoryCheckpoint";
                failure = RunCampaignFailure.None;
                return campaign != null && progress?.ActiveEncounter != null;
            }
        }

        private static readonly RunNodeType[] RequiredNodeTypes =
        {
            RunNodeType.Battle,
            RunNodeType.EliteBattle,
            RunNodeType.Shop,
            RunNodeType.SituationEvent,
            RunNodeType.RestOrUpgrade,
            RunNodeType.MidBoss,
            RunNodeType.FinalBoss
        };

        [MenuItem("Have a Break/Tests/Validate Full Run Player Actions")]
        private static void ValidateFromMenu()
        {
            Debug.Log(Validate()
                ? "Full run player-action validation passed."
                : "Full run player-action validation failed.");
        }

        internal static bool Validate()
        {
            RuntimePrototypeConfig config = Resources.Load<RuntimePrototypeConfig>(
                "GameData/RuntimePrototypeConfig");
            CardDatabase cards = AssetDatabase.LoadAssetAtPath<CardDatabase>(
                "Assets/GameData/CardDatabase.asset");
            if (config == null || !config.IsReady ||
                config.RunStartProgressionConfig == null ||
                config.EnchantDatabase == null || cards == null)
            {
                Debug.LogError(
                    "Player-action full run requires ready runtime, card, and " +
                    "enchant data.");
                return false;
            }

            int totalNodes = config.RunStartProgressionConfig.TotalNodeCount;
            if (!TryFindRoute(
                    totalNodes,
                    out int seed,
                    out List<PlannedNode> route,
                    out string planningFailure))
            {
                Debug.LogError(
                    "Player-action full run route planning failed: " +
                    planningFailure);
                return false;
            }

            if (!TryExecute(
                    seed,
                    route,
                    config,
                    cards,
                    out string failure,
                    out int battles,
                    out int turns,
                    out int cardsPlayed,
                    out int attacks,
                    out int items))
            {
                Debug.LogError(
                    $"Player-action full run failed for seed {seed}: {failure}");
                return false;
            }

            Debug.Log(
                $"Full run player-action validation passed: seed {seed}, " +
                $"{totalNodes} nodes, {battles} battles, {turns} player turns, " +
                $"{cardsPlayed} cards, {attacks} attacks, {items} battle items, " +
                "midpoint resume, final boss reward, and run completion.");
            return true;
        }

        private static bool TryFindRoute(
            int totalNodes,
            out int seed,
            out List<PlannedNode> route,
            out string failure)
        {
            seed = -1;
            route = null;
            failure = null;
            string lastFailure = null;
            for (int candidate = 0; candidate < 512; candidate++)
            {
                if (!TryPlanRoute(
                        candidate,
                        totalNodes,
                        out List<PlannedNode> candidateRoute,
                        out lastFailure))
                {
                    continue;
                }

                seed = candidate;
                route = candidateRoute;
                return true;
            }

            failure = lastFailure ?? "no complete route found";
            return false;
        }

        private static bool TryPlanRoute(
            int seed,
            int totalNodes,
            out List<PlannedNode> route,
            out string failure)
        {
            route = new List<PlannedNode>();
            failure = null;
            RunCampaignState campaign = new(seed);
            RunBattleState run = new(999, 999, 1000);
            HashSet<RunNodeType> visited = new();

            for (int step = 0; step < totalNodes + 2; step++)
            {
                if (campaign.Phase == RunCampaignPhase.Completed)
                {
                    break;
                }

                if (campaign.Phase != RunCampaignPhase.NodeSelection)
                {
                    failure = $"unexpected planning phase {campaign.Phase}";
                    return false;
                }

                RunNodeChoice selected = SelectChoice(
                    RunCampaignService.GetChoices(campaign),
                    visited);
                RunCampaignFailure selectFailure = RunCampaignFailure.None;
                bool selectedNode = selected != null &&
                                    RunCampaignService.TrySelectNode(
                                        campaign,
                                        selected.NodeId,
                                        out selectFailure);
                if (!selectedNode)
                {
                    failure = $"planning selection failed: {selectFailure}";
                    return false;
                }

                route.Add(new PlannedNode(selected.NodeId, selected.NodeType));
                visited.Add(selected.NodeType);
                int completedBefore = campaign.CompletedNodeCount;
                if (!ResolvePlanningNode(campaign, run, out failure) ||
                    campaign.CompletedNodeCount != completedBefore + 1)
                {
                    failure ??= $"planning did not complete {selected.NodeId}";
                    return false;
                }
            }

            bool complete = campaign.Phase == RunCampaignPhase.Completed &&
                            campaign.CompletedNodeCount == totalNodes &&
                            route.Count == totalNodes;
            if (!complete || !RequiredNodeTypes.All(visited.Contains))
            {
                failure =
                    $"planned route incomplete: phase={campaign.Phase}, " +
                    $"completed={campaign.CompletedNodeCount}/{totalNodes}, " +
                    $"visited={string.Join(",", visited.OrderBy(value => value))}";
                return false;
            }

            return true;
        }

        private static bool TryExecute(
            int seed,
            IReadOnlyList<PlannedNode> route,
            RuntimePrototypeConfig config,
            CardDatabase cards,
            out string failure,
            out int battleCount,
            out int turns,
            out int cardsPlayed,
            out int attacks,
            out int items)
        {
            failure = null;
            battleCount = 0;
            turns = 0;
            cardsPlayed = 0;
            attacks = 0;
            items = 0;
            PlayerPermanentRewardState permanentRewards = new();
            RunEncounterProgressState progress = CreateProgress(
                cards,
                permanentRewards);
            RunCampaignState campaign = new(seed);
            if (progress == null || route == null || route.Count == 0)
            {
                failure = "player-action full run setup failed";
                return false;
            }

            MemoryCheckpointWriter checkpointWriter = new();
            BattleStartViewModel battleStart = new(checkpointWriter);
            BattleAutoplayViewModel autoplay = new();
            BattleSettlementViewModel settlement = new();
            RunBattleRewardViewModel rewards = new();
            HashSet<RunNodeType> visited = new();
            bool resumed = false;

            for (int index = 0; index < route.Count; index++)
            {
                PlannedNode planned = route[index];
                if (campaign.Phase != RunCampaignPhase.NodeSelection ||
                    progress.HasActiveEncounter)
                {
                    failure =
                        $"invalid pre-node state: phase={campaign.Phase}, " +
                        $"active={progress.HasActiveEncounter}";
                    return false;
                }

                RunNodeChoice selected = RunCampaignService.GetChoices(campaign)
                    .FirstOrDefault(choice => choice != null &&
                        string.Equals(
                            choice.NodeId,
                            planned.NodeId,
                            StringComparison.OrdinalIgnoreCase));
                RunCampaignFailure selectFailure = RunCampaignFailure.None;
                bool selectedNode = selected != null &&
                                    selected.NodeType == planned.NodeType &&
                                    RunCampaignService.TrySelectNode(
                                        campaign,
                                        selected.NodeId,
                                        out selectFailure);
                if (!selectedNode)
                {
                    failure =
                        $"node selection failed at {planned.NodeId}: " +
                        selectFailure;
                    return false;
                }

                visited.Add(selected.NodeType);
                int completedBefore = campaign.CompletedNodeCount;
                if (!ResolveActualNode(
                        campaign,
                        progress,
                        config,
                        battleStart,
                        autoplay,
                        settlement,
                        rewards,
                        ref battleCount,
                        ref turns,
                        ref cardsPlayed,
                        ref attacks,
                        ref items,
                        out failure))
                {
                    failure = $"{planned.NodeId} [{planned.NodeType}] · {failure}";
                    return false;
                }

                if (campaign.CompletedNodeCount != completedBefore + 1)
                {
                    failure =
                        $"node count did not advance at {planned.NodeId}: " +
                        $"{campaign.CompletedNodeCount}/{completedBefore + 1}";
                    return false;
                }

                if (!resumed && campaign.CompletedNodeCount >= route.Count / 2)
                {
                    string json = JsonUtility.ToJson(campaign);
                    campaign = JsonUtility.FromJson<RunCampaignState>(json);
                    resumed = true;
                    if (campaign == null || progress.HasActiveEncounter ||
                        campaign.Phase != RunCampaignPhase.NodeSelection ||
                        campaign.CompletedNodeCount != completedBefore + 1)
                    {
                        failure = "midpoint campaign resume failed";
                        return false;
                    }
                }
            }

            bool complete = campaign.Phase == RunCampaignPhase.Completed &&
                            campaign.CompletedNodeCount == route.Count &&
                            campaign.ActiveNode == null &&
                            !progress.HasActiveEncounter;
            bool pathValid = campaign.SelectedNodePath.Count == route.Count &&
                             campaign.SelectedNodePath
                                 .Distinct(StringComparer.OrdinalIgnoreCase)
                                 .Count() == route.Count;
            bool encountersValid = battleCount > 0 &&
                                   progress.CompletedEncounterCount == battleCount &&
                                   checkpointWriter.CallCount == battleCount;
            bool permanentValid = permanentRewards.Contains(
                BattleSettlementViewModel.FinalBossPermanentRewardId);
            if (!complete || !pathValid || !encountersValid || !permanentValid ||
                !resumed || !RequiredNodeTypes.All(visited.Contains) ||
                cardsPlayed <= 0 || attacks <= 0)
            {
                failure =
                    $"final state invalid: phase={campaign.Phase}, completed=" +
                    $"{campaign.CompletedNodeCount}/{route.Count}, path=" +
                    $"{campaign.SelectedNodePath.Count}, battles={battleCount}, " +
                    $"encounters={progress.CompletedEncounterCount}, checkpoints=" +
                    $"{checkpointWriter.CallCount}, turns={turns}, cards=" +
                    $"{cardsPlayed}, attacks={attacks}, items={items}, permanent=" +
                    $"{permanentValid}, resumed={resumed}";
                return false;
            }

            return true;
        }

        private static bool ResolveActualNode(
            RunCampaignState campaign,
            RunEncounterProgressState progress,
            RuntimePrototypeConfig config,
            BattleStartViewModel battleStart,
            BattleAutoplayViewModel autoplay,
            BattleSettlementViewModel settlement,
            RunBattleRewardViewModel rewards,
            ref int battleCount,
            ref int turns,
            ref int cardsPlayed,
            ref int attacks,
            ref int items,
            out string failure)
        {
            failure = null;
            RunNodeChoice node = campaign.ActiveNode;
            if (node == null)
            {
                failure = "active node missing";
                return false;
            }

            switch (node.NodeType)
            {
                case RunNodeType.Shop:
                    if (!RunCampaignService.TryLeaveShop(
                            campaign,
                            progress.RunState,
                            out RunCampaignFailure shopFailure))
                    {
                        failure = $"shop failed: {shopFailure}";
                        return false;
                    }
                    return true;

                case RunNodeType.SituationEvent:
                    return TryResolveEvent(
                        campaign,
                        progress.RunState,
                        "actual",
                        out failure);

                case RunNodeType.RestOrUpgrade:
                    if (!RunCampaignService.TryRest(
                            campaign,
                            progress.RunState,
                            out _,
                            out RunCampaignFailure restFailure))
                    {
                        failure = $"rest failed: {restFailure}";
                        return false;
                    }
                    return true;

                case RunNodeType.Battle:
                case RunNodeType.EliteBattle:
                case RunNodeType.MidBoss:
                case RunNodeType.FinalBoss:
                    if (!ResolveBattle(
                            campaign,
                            progress,
                            config,
                            battleStart,
                            autoplay,
                            settlement,
                            rewards,
                            ref turns,
                            ref cardsPlayed,
                            ref attacks,
                            ref items,
                            out failure))
                    {
                        return false;
                    }
                    battleCount++;
                    return true;

                default:
                    failure = $"unsupported node {node.NodeType}";
                    return false;
            }
        }

        private static bool ResolveBattle(
            RunCampaignState campaign,
            RunEncounterProgressState progress,
            RuntimePrototypeConfig config,
            BattleStartViewModel battleStart,
            BattleAutoplayViewModel autoplay,
            BattleSettlementViewModel settlement,
            RunBattleRewardViewModel rewards,
            ref int turns,
            ref int cardsPlayed,
            ref int attacks,
            ref int items,
            out string failure)
        {
            failure = null;
            RunNodeType nodeType = campaign.ActiveNode.NodeType;
            BattleStartCommandResult started = battleStart.TryStart(
                campaign,
                progress,
                config);
            BattleEncounterGrade expected =
                BattleStartViewModel.ResolveGrade(nodeType);
            if (started == null || !started.Succeeded ||
                !started.BattleStarted || !started.StartedNewBattle ||
                !started.CheckpointSaved || started.Grade != expected ||
                started.Encounter == null || progress.ActiveEncounter == null)
            {
                failure = "battle start failed: " + DescribeStart(started);
                return false;
            }

            BattleAutoplayCommandResult combat = autoplay.TryRun(
                progress,
                campaign,
                new BattleAutoplaySettings(
                    maximumPlayerTurns: 120,
                    maximumCardPlaysPerTurn: 32,
                    maximumAttacksPerTurn: 16,
                    maximumConsumablesPerTurn: 8,
                    maximumStalledTurns: 8,
                    useConsumables: true));
            if (combat == null || !combat.Succeeded ||
                combat.Failure != BattleAutoplayFailure.None ||
                combat.Outcome != BattleOutcome.Victory ||
                combat.CardsPlayed <= 0 || combat.AttacksResolved <= 0)
            {
                failure = "player-action combat failed: " + DescribeCombat(combat);
                return false;
            }

            turns += combat.PlayerTurnsCompleted;
            cardsPlayed += combat.CardsPlayed;
            attacks += combat.AttacksResolved;
            items += combat.ConsumablesUsed;

            BattleSettlementCommandResult settled = settlement.TrySettle(
                campaign,
                progress);
            bool finalBoss = nodeType == RunNodeType.FinalBoss;
            if (settled == null || !settled.Succeeded ||
                settled.Failure != BattleSettlementCommandFailure.None ||
                settled.Outcome != BattleOutcome.Victory ||
                !settled.GoldClaimed ||
                settled.CampaignPhase != RunCampaignPhase.Reward ||
                (finalBoss && (!settled.PermanentRewardRequired ||
                               !settled.PermanentRewardClaimed)))
            {
                failure = "settlement failed: " + DescribeSettlement(settled);
                return false;
            }

            return ClaimAndCompleteRewards(
                campaign,
                progress,
                config.EnchantDatabase,
                rewards,
                out failure);
        }

        private static bool ClaimAndCompleteRewards(
            RunCampaignState campaign,
            RunEncounterProgressState progress,
            EnchantDatabase enchantDatabase,
            RunBattleRewardViewModel rewards,
            out string failure)
        {
            failure = null;
            for (int pass = 0; pass < 8; pass++)
            {
                RunBattleRewardSnapshot snapshot = rewards.CreateSnapshot(
                    campaign,
                    progress,
                    enchantDatabase);
                if (snapshot == null || !snapshot.Available ||
                    !string.IsNullOrWhiteSpace(snapshot.ErrorText))
                {
                    failure = "reward snapshot failed: " +
                              (snapshot?.ErrorText ?? "null");
                    return false;
                }

                if (!snapshot.EnchantRewardComplete)
                {
                    RunBattleEnchantRewardOption option = snapshot.EnchantOptions
                        .FirstOrDefault(value => value?.CanClaim == true);
                    EnchantAttachmentFailure attachment =
                        EnchantAttachmentFailure.None;
                    BattleVictoryEnchantRewardFailure rewardFailure =
                        BattleVictoryEnchantRewardFailure.None;
                    bool claimed = option != null && rewards.TryClaimEnchant(
                        campaign,
                        progress,
                        enchantDatabase,
                        option.DefinitionId,
                        out _,
                        out _,
                        out attachment,
                        out rewardFailure);
                    if (!claimed || attachment != EnchantAttachmentFailure.None ||
                        rewardFailure != BattleVictoryEnchantRewardFailure.None)
                    {
                        failure =
                            $"enchant reward failed: {attachment}/{rewardFailure}";
                        return false;
                    }
                    continue;
                }

                if (!snapshot.ConsumableRewardComplete)
                {
                    RunBattleConsumableRewardOption option =
                        snapshot.ConsumableOptions.FirstOrDefault(value =>
                            value?.CanClaim == true);
                    BattleVictoryConsumableRewardFailure rewardFailure =
                        BattleVictoryConsumableRewardFailure.None;
                    bool claimed = option != null && rewards.TryClaimConsumable(
                        campaign,
                        progress,
                        option.ItemId,
                        out _,
                        out _,
                        out rewardFailure);
                    if (!claimed || rewardFailure !=
                        BattleVictoryConsumableRewardFailure.None)
                    {
                        failure = $"consumable reward failed: {rewardFailure}";
                        return false;
                    }
                    continue;
                }

                RunEncounterProgressFailure completeFailure =
                    RunEncounterProgressFailure.None;
                bool completed = snapshot.CanComplete && rewards.TryComplete(
                    campaign,
                    progress,
                    out _,
                    out completeFailure);
                if (!completed ||
                    completeFailure != RunEncounterProgressFailure.None ||
                    progress.HasActiveEncounter)
                {
                    failure =
                        $"reward completion failed: can={snapshot.CanComplete}, " +
                        $"failure={completeFailure}, active=" +
                        progress.HasActiveEncounter;
                    return false;
                }

                return true;
            }

            failure = "reward processing exceeded safety limit";
            return false;
        }

        private static bool ResolvePlanningNode(
            RunCampaignState campaign,
            RunBattleState run,
            out string failure)
        {
            failure = null;
            RunNodeChoice node = campaign.ActiveNode;
            if (node == null)
            {
                failure = "planning active node missing";
                return false;
            }

            switch (node.NodeType)
            {
                case RunNodeType.Shop:
                    if (!RunCampaignService.TryLeaveShop(
                            campaign,
                            run,
                            out RunCampaignFailure shopFailure))
                    {
                        failure = $"planning shop failed: {shopFailure}";
                        return false;
                    }
                    return true;

                case RunNodeType.SituationEvent:
                    return TryResolveEvent(campaign, run, "planning", out failure);

                case RunNodeType.RestOrUpgrade:
                    if (!RunCampaignService.TryRest(
                            campaign,
                            run,
                            out _,
                            out RunCampaignFailure restFailure))
                    {
                        failure = $"planning rest failed: {restFailure}";
                        return false;
                    }
                    return true;

                case RunNodeType.Battle:
                case RunNodeType.EliteBattle:
                case RunNodeType.MidBoss:
                case RunNodeType.FinalBoss:
                    RunCampaignService.MarkBattleReward(
                        campaign,
                        BattleOutcome.Victory);
                    RunCampaignService.CompleteBattleReward(campaign);
                    return campaign.Phase == RunCampaignPhase.NodeSelection ||
                           campaign.Phase == RunCampaignPhase.Completed;

                default:
                    failure = $"unsupported planning node {node.NodeType}";
                    return false;
            }
        }

        private static RunNodeChoice SelectChoice(
            IReadOnlyList<RunNodeChoice> choices,
            ISet<RunNodeType> visited)
        {
            if (choices == null || choices.Count == 0)
            {
                return null;
            }

            foreach (RunNodeType type in RequiredNodeTypes)
            {
                if (visited.Contains(type))
                {
                    continue;
                }

                RunNodeChoice missing = choices.FirstOrDefault(choice =>
                    choice != null && choice.NodeType == type);
                if (missing != null)
                {
                    return missing;
                }
            }

            return choices
                .Where(choice => choice != null)
                .OrderBy(choice => choice.NodeId, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }

        private static bool TryResolveEvent(
            RunCampaignState campaign,
            RunBattleState run,
            string stage,
            out string failure)
        {
            failure = null;
            IReadOnlyList<RunSituationEventChoice> choices =
                RunCampaignService.GetSituationEventChoices(campaign);
            RunCampaignFailure lastFailure = RunCampaignFailure.None;
            foreach (RunSituationEventChoice choice in choices
                         .Where(value => value != null)
                         .OrderBy(value => value.ChoiceId,
                             StringComparer.OrdinalIgnoreCase))
            {
                if (RunCampaignService.TryResolveSituationEvent(
                        campaign,
                        run,
                        choice.ChoiceId,
                        out _,
                        out lastFailure))
                {
                    return true;
                }
            }

            failure = $"{stage} event failed: {lastFailure}";
            return false;
        }

        private static RunEncounterProgressState CreateProgress(
            CardDatabase cards,
            PlayerPermanentRewardState permanentRewards)
        {
            RunOwnedCardState owned = new();
            RunDeckState deck = new();
            int index = 0;
            foreach (CardData card in cards.Cards.Where(card => card != null))
            {
                RunCardInstance instance = new(
                    card,
                    $"OWNED-PLAYER-ACTION-RUN-{++index:00}-" +
                    card.CatalogCardId,
                    1);
                if (!owned.TryAdd(instance, out _) ||
                    !deck.TryAdd(instance, out RunDeckFailure deckFailure) ||
                    deckFailure != RunDeckFailure.None)
                {
                    return null;
                }
            }

            return new RunEncounterProgressState(
                new RunBattleState(999, 999, 1000),
                owned,
                deck,
                permanentRewards,
                Array.Empty<string>(),
                0);
        }

        private static string DescribeStart(BattleStartCommandResult result)
        {
            return result == null
                ? "result=null"
                : $"success={result.Succeeded}, failure={result.Failure}, " +
                  $"started={result.BattleStarted}, new=" +
                  $"{result.StartedNewBattle}, checkpoint=" +
                  $"{result.CheckpointSaved}, grade={result.Grade}, message=" +
                  result.Message;
        }

        private static string DescribeCombat(BattleAutoplayCommandResult result)
        {
            return result == null
                ? "result=null"
                : $"success={result.Succeeded}, failure={result.Failure}, " +
                  $"outcome={result.Outcome}, turns=" +
                  $"{result.PlayerTurnsCompleted}, cards={result.CardsPlayed}, " +
                  $"attacks={result.AttacksResolved}, items=" +
                  $"{result.ConsumablesUsed}, health=" +
                  $"{result.FinalPlayerHealth}, enemies=" +
                  $"{result.LivingEnemyCount}, message={result.Message}";
        }

        private static string DescribeSettlement(
            BattleSettlementCommandResult result)
        {
            return result == null
                ? "result=null"
                : $"success={result.Succeeded}, failure={result.Failure}, " +
                  $"outcome={result.Outcome}, gold={result.GoldClaimed}, " +
                  $"permanent={result.PermanentRewardClaimed}, phase=" +
                  $"{result.CampaignPhase}, message={result.Message}";
        }
    }
}
