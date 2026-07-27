using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class FullRunEndToEndValidation
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
                destination = "FullRunMemoryCheckpoint";
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

        [MenuItem("Have a Break/Tests/Validate Full Run End To End")]
        private static void ValidateFromMenu()
        {
            Debug.Log(Validate()
                ? "Full run end-to-end validation passed."
                : "Full run end-to-end validation failed.");
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
                    "Full run validation failed: runtime config, cards, or enchants " +
                    "are not ready.");
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
                    "Full run validation failed while planning a deterministic " +
                    $"route: {planningFailure}");
                return false;
            }

            if (!TryExecuteRoute(
                    seed,
                    route,
                    config,
                    cards,
                    out string executionFailure,
                    out int battleCount,
                    out int enchantClaims,
                    out int consumableClaims))
            {
                Debug.LogError(
                    $"Full run validation failed for seed {seed}: " +
                    executionFailure);
                return false;
            }

            Debug.Log(
                $"Full run end-to-end validation passed: seed {seed}, " +
                $"{totalNodes} nodes, {battleCount} real battles, " +
                $"{enchantClaims} enchant rewards, {consumableClaims} consumable " +
                "rewards, midpoint resume, final boss permanent reward, and run " +
                "completion.");
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

            failure = lastFailure ??
                      "no route covered all required node categories";
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
            RunBattleState run = new(30, 30, 1000);
            HashSet<RunNodeType> visited = new();

            for (int step = 0; step < totalNodes + 2; step++)
            {
                if (campaign.Phase == RunCampaignPhase.Completed)
                {
                    break;
                }

                if (campaign.Phase != RunCampaignPhase.NodeSelection)
                {
                    failure =
                        $"planning reached unexpected phase {campaign.Phase}";
                    return false;
                }

                IReadOnlyList<RunNodeChoice> choices =
                    RunCampaignService.GetChoices(campaign);
                RunNodeChoice selected = SelectChoice(choices, visited);
                RunCampaignFailure selectFailure = RunCampaignFailure.None;
                bool selectedNode = selected != null &&
                                    RunCampaignService.TrySelectNode(
                                        campaign,
                                        selected.NodeId,
                                        out selectFailure);
                if (!selectedNode)
                {
                    failure = $"planning node selection failed: {selectFailure}";
                    return false;
                }

                route.Add(new PlannedNode(selected.NodeId, selected.NodeType));
                visited.Add(selected.NodeType);
                int completedBefore = campaign.CompletedNodeCount;
                if (!ResolvePlanningNode(campaign, run, out failure) ||
                    campaign.CompletedNodeCount != completedBefore + 1)
                {
                    failure ??=
                        $"planning node count did not advance at {selected.NodeId}";
                    return false;
                }
            }

            bool allRequired = RequiredNodeTypes.All(visited.Contains);
            bool complete = campaign.Phase == RunCampaignPhase.Completed &&
                            campaign.CompletedNodeCount == totalNodes &&
                            route.Count == totalNodes;
            if (!allRequired || !complete)
            {
                failure =
                    $"planned route incomplete: phase={campaign.Phase}, " +
                    $"completed={campaign.CompletedNodeCount}/{totalNodes}, " +
                    $"route={route.Count}, visited=" +
                    string.Join(",", visited.OrderBy(value => value));
                return false;
            }

            return true;
        }

        private static bool TryExecuteRoute(
            int seed,
            IReadOnlyList<PlannedNode> route,
            RuntimePrototypeConfig config,
            CardDatabase cards,
            out string failure,
            out int battleCount,
            out int enchantClaims,
            out int consumableClaims)
        {
            failure = null;
            battleCount = 0;
            enchantClaims = 0;
            consumableClaims = 0;
            PlayerPermanentRewardState permanentRewards = new();
            RunEncounterProgressState progress = CreateProgress(
                cards,
                config,
                permanentRewards);
            RunCampaignState campaign = new(seed);
            if (progress == null || route == null || route.Count == 0)
            {
                failure = "actual run setup failed";
                return false;
            }

            MemoryCheckpointWriter checkpointWriter = new();
            BattleStartViewModel battleStart = new(checkpointWriter);
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
                        $"invalid pre-node state at {planned.NodeId}: " +
                        $"phase={campaign.Phase}, active=" +
                        progress.HasActiveEncounter;
                    return false;
                }

                RunNodeChoice selected = RunCampaignService.GetChoices(campaign)
                    .FirstOrDefault(choice => choice != null && string.Equals(
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
                        $"actual node selection failed at {planned.NodeId}: " +
                        selectFailure;
                    return false;
                }

                int completedBefore = campaign.CompletedNodeCount;
                visited.Add(selected.NodeType);
                if (!ResolveActualNode(
                        campaign,
                        progress,
                        config,
                        battleStart,
                        settlement,
                        rewards,
                        ref battleCount,
                        ref enchantClaims,
                        ref consumableClaims,
                        out failure))
                {
                    failure = $"{selected.NodeId} [{selected.NodeType}] · {failure}";
                    return false;
                }

                if (campaign.CompletedNodeCount != completedBefore + 1)
                {
                    failure =
                        $"completed node count did not advance at {selected.NodeId}: " +
                        $"{campaign.CompletedNodeCount}/{completedBefore + 1}";
                    return false;
                }

                if (!resumed &&
                    campaign.CompletedNodeCount >= route.Count / 2)
                {
                    string json = JsonUtility.ToJson(campaign);
                    campaign = JsonUtility.FromJson<RunCampaignState>(json);
                    resumed = true;
                    if (campaign == null || progress.HasActiveEncounter ||
                        campaign.Phase != RunCampaignPhase.NodeSelection ||
                        campaign.CompletedNodeCount != completedBefore + 1 ||
                        campaign.SelectedNodePath.Count !=
                            campaign.CompletedNodeCount)
                    {
                        failure = "midpoint campaign resume failed";
                        return false;
                    }
                }
            }

            bool allRequired = RequiredNodeTypes.All(visited.Contains);
            bool pathValid = campaign.SelectedNodePath.Count == route.Count &&
                             campaign.SelectedNodePath
                                 .Distinct(StringComparer.OrdinalIgnoreCase)
                                 .Count() == route.Count;
            bool completed = campaign.Phase == RunCampaignPhase.Completed &&
                             campaign.CompletedNodeCount == route.Count &&
                             campaign.ActiveNode == null &&
                             !progress.HasActiveEncounter;
            bool battleLifecycleValid =
                battleCount > 0 &&
                progress.CompletedEncounterCount == battleCount &&
                checkpointWriter.CallCount == battleCount;
            bool permanentValid = permanentRewards.Contains(
                BattleSettlementViewModel.FinalBossPermanentRewardId);
            if (!allRequired || !pathValid || !completed || !resumed ||
                !battleLifecycleValid || !permanentValid)
            {
                failure =
                    $"final state invalid: phase={campaign.Phase}, " +
                    $"completed={campaign.CompletedNodeCount}/{route.Count}, " +
                    $"path={campaign.SelectedNodePath.Count}, resumed={resumed}, " +
                    $"battles={battleCount}, encounters=" +
                    $"{progress.CompletedEncounterCount}, checkpoints=" +
                    $"{checkpointWriter.CallCount}, permanent={permanentValid}, " +
                    $"visited={string.Join(",", visited.OrderBy(value => value))}";
                return false;
            }

            return true;
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

        private static bool ResolvePlanningNode(
            RunCampaignState campaign,
            RunBattleState run,
            out string failure)
        {
            failure = null;
            RunNodeChoice node = campaign.ActiveNode;
            if (node == null)
            {
                failure = "planning active node is missing";
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
                    return TryResolveEvent(
                        campaign,
                        run,
                        "planning",
                        out failure);

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
                    failure = $"unsupported planning node: {node.NodeType}";
                    return false;
            }
        }

        private static bool ResolveActualNode(
            RunCampaignState campaign,
            RunEncounterProgressState progress,
            RuntimePrototypeConfig config,
            BattleStartViewModel battleStart,
            BattleSettlementViewModel settlement,
            RunBattleRewardViewModel rewards,
            ref int battleCount,
            ref int enchantClaims,
            ref int consumableClaims,
            out string failure)
        {
            failure = null;
            RunNodeChoice node = campaign.ActiveNode;
            if (node == null)
            {
                failure = "active node is missing";
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
                        failure = $"shop resolution failed: {shopFailure}";
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
                        failure = $"rest resolution failed: {restFailure}";
                        return false;
                    }
                    return true;

                case RunNodeType.Battle:
                case RunNodeType.EliteBattle:
                case RunNodeType.MidBoss:
                case RunNodeType.FinalBoss:
                    if (!ResolveBattleNode(
                            campaign,
                            progress,
                            config,
                            battleStart,
                            settlement,
                            rewards,
                            ref enchantClaims,
                            ref consumableClaims,
                            out failure))
                    {
                        return false;
                    }
                    battleCount++;
                    return true;

                default:
                    failure = $"unsupported node type: {node.NodeType}";
                    return false;
            }
        }

        private static bool ResolveBattleNode(
            RunCampaignState campaign,
            RunEncounterProgressState progress,
            RuntimePrototypeConfig config,
            BattleStartViewModel battleStart,
            BattleSettlementViewModel settlement,
            RunBattleRewardViewModel rewards,
            ref int enchantClaims,
            ref int consumableClaims,
            out string failure)
        {
            failure = null;
            RunNodeType nodeType = campaign.ActiveNode.NodeType;
            BattleEncounterGrade expectedGrade =
                BattleStartViewModel.ResolveGrade(nodeType);
            BattleStartCommandResult started = battleStart.TryStart(
                campaign,
                progress,
                config);
            if (started == null || !started.Succeeded ||
                !started.BattleStarted || !started.StartedNewBattle ||
                !started.CheckpointSaved || started.Grade != expectedGrade ||
                started.Encounter == null ||
                started.Failure != BattleStartCommandFailure.None ||
                progress.ActiveEncounter == null)
            {
                failure =
                    "battle start failed: " + DescribeStart(started);
                return false;
            }

            BattleRuntimeEncounterContext context = progress.ActiveEncounter;
            if (!DefeatAllEnemies(context, out failure))
            {
                return false;
            }

            BattleSettlementCommandResult settled = settlement.TrySettle(
                campaign,
                progress);
            bool finalBoss = nodeType == RunNodeType.FinalBoss;
            if (settled == null || !settled.Succeeded ||
                settled.Failure != BattleSettlementCommandFailure.None ||
                settled.Outcome != BattleOutcome.Victory ||
                !settled.GoldClaimed ||
                settled.CampaignPhase != RunCampaignPhase.Reward ||
                finalBoss && (!settled.PermanentRewardRequired ||
                              !settled.PermanentRewardClaimed))
            {
                failure =
                    "battle settlement failed: " + DescribeSettlement(settled);
                return false;
            }

            return ClaimAndCompleteRewards(
                campaign,
                progress,
                config.EnchantDatabase,
                rewards,
                ref enchantClaims,
                ref consumableClaims,
                out failure);
        }

        private static bool DefeatAllEnemies(
            BattleRuntimeEncounterContext context,
            out string failure)
        {
            failure = null;
            BattleEnemyRuntimeState[] living = context?.Runtime?.Enemies
                .Where(enemy => enemy != null && enemy.IsAlive)
                .ToArray();
            if (context?.Session == null || living == null || living.Length == 0)
            {
                failure = "battle has no living enemies";
                return false;
            }

            foreach (BattleEnemyRuntimeState enemy in living)
            {
                int health = enemy.Vital.CurrentHealth;
                if (health <= 0 || enemy.Vital.ApplyDamage(health) <= 0 ||
                    !context.Runtime.LivingEnemies.TryRemove(enemy.EnemyId))
                {
                    failure = $"failed to defeat enemy {enemy.EnemyId}";
                    return false;
                }
            }

            if (!BattleRuntimeSessionService.TryFinalizeTerminalOutcome(
                    context.Session,
                    out BattleOutcome outcome,
                    out BattleRuntimeSessionFailure sessionFailure) ||
                outcome != BattleOutcome.Victory ||
                sessionFailure != BattleRuntimeSessionFailure.None ||
                !context.Session.IsFinished)
            {
                failure =
                    $"victory finalization failed: outcome={outcome}, " +
                    $"failure={sessionFailure}, finished=" +
                    context.Session.IsFinished;
                return false;
            }

            return true;
        }

        private static bool ClaimAndCompleteRewards(
            RunCampaignState campaign,
            RunEncounterProgressState progress,
            EnchantDatabase enchantDatabase,
            RunBattleRewardViewModel rewards,
            ref int enchantClaims,
            ref int consumableClaims,
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
                    failure =
                        "reward snapshot unavailable: " +
                        (snapshot?.ErrorText ?? "null snapshot");
                    return false;
                }

                if (!snapshot.EnchantRewardComplete)
                {
                    RunBattleEnchantRewardOption option =
                        snapshot.EnchantOptions.FirstOrDefault(value =>
                            value != null && value.CanClaim);
                    EnchantAttachmentFailure attachmentFailure =
                        EnchantAttachmentFailure.None;
                    BattleVictoryEnchantRewardFailure enchantFailure =
                        BattleVictoryEnchantRewardFailure.None;
                    bool claimed = option != null && rewards.TryClaimEnchant(
                        campaign,
                        progress,
                        enchantDatabase,
                        option.DefinitionId,
                        out _,
                        out _,
                        out attachmentFailure,
                        out enchantFailure);
                    if (!claimed ||
                        attachmentFailure != EnchantAttachmentFailure.None ||
                        enchantFailure !=
                            BattleVictoryEnchantRewardFailure.None)
                    {
                        failure =
                            $"enchant reward claim failed: attachment=" +
                            $"{attachmentFailure}, reward={enchantFailure}";
                        return false;
                    }

                    enchantClaims++;
                    continue;
                }

                if (!snapshot.ConsumableRewardComplete)
                {
                    RunBattleConsumableRewardOption option =
                        snapshot.ConsumableOptions.FirstOrDefault(value =>
                            value != null && value.CanClaim);
                    BattleVictoryConsumableRewardFailure itemFailure =
                        BattleVictoryConsumableRewardFailure.None;
                    bool claimed = option != null &&
                                   rewards.TryClaimConsumable(
                                       campaign,
                                       progress,
                                       option.ItemId,
                                       out _,
                                       out _,
                                       out itemFailure);
                    if (!claimed || itemFailure !=
                        BattleVictoryConsumableRewardFailure.None)
                    {
                        failure =
                            $"consumable reward claim failed: {itemFailure}";
                        return false;
                    }

                    consumableClaims++;
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
                    progress.HasActiveEncounter ||
                    campaign.ActiveNode != null ||
                    (campaign.Phase != RunCampaignPhase.NodeSelection &&
                     campaign.Phase != RunCampaignPhase.Completed))
                {
                    failure =
                        $"reward completion failed: canComplete=" +
                        $"{snapshot.CanComplete}, failure={completeFailure}, " +
                        $"active={progress.HasActiveEncounter}, " +
                        $"phase={campaign.Phase}";
                    return false;
                }

                return true;
            }

            failure = "reward processing exceeded safety limit";
            return false;
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
            if (choices == null || choices.Count == 0)
            {
                failure = $"{stage} event has no choices";
                return false;
            }

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

            failure = $"{stage} event resolution failed: {lastFailure}";
            return false;
        }

        private static RunEncounterProgressState CreateProgress(
            CardDatabase cards,
            RuntimePrototypeConfig config,
            PlayerPermanentRewardState permanentRewards)
        {
            if (cards == null || config?.RunStartProgressionConfig == null ||
                permanentRewards == null)
            {
                return null;
            }

            RunOwnedCardState owned = new();
            RunDeckState deck = new();
            int index = 0;
            foreach (CardData card in cards.Cards.Where(card => card != null))
            {
                RunCardInstance instance = new(
                    card,
                    $"OWNED-FULL-RUN-{++index:00}-{card.CatalogCardId}",
                    1);
                if (!owned.TryAdd(instance, out _) ||
                    !deck.TryAdd(instance, out RunDeckFailure deckFailure) ||
                    deckFailure != RunDeckFailure.None)
                {
                    return null;
                }
            }

            return new RunEncounterProgressState(
                config.RunStartProgressionConfig.CreateInitialRunState(),
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
                  $"battleStarted={result.BattleStarted}, new=" +
                  $"{result.StartedNewBattle}, checkpoint=" +
                  $"{result.CheckpointSaved}, grade={result.Grade}, " +
                  $"progress={result.ProgressFailure}, flow=" +
                  $"{result.FlowFailure}, deck={result.DeckFailure}, " +
                  $"bootstrap={result.BootstrapFailure}, session=" +
                  $"{result.SessionFailure}, redraw={result.RedrawFailure}, " +
                  $"turn={result.TurnFailure}, message={result.Message}";
        }

        private static string DescribeSettlement(
            BattleSettlementCommandResult result)
        {
            return result == null
                ? "result=null"
                : $"success={result.Succeeded}, failure={result.Failure}, " +
                  $"outcome={result.Outcome}, goldClaimed=" +
                  $"{result.GoldClaimed}, permanentRequired=" +
                  $"{result.PermanentRewardRequired}, permanentClaimed=" +
                  $"{result.PermanentRewardClaimed}, phase=" +
                  $"{result.CampaignPhase}, progress=" +
                  $"{result.ProgressFailure}, flow={result.FlowFailure}, " +
                  $"session={result.SessionFailure}, settlement=" +
                  $"{result.SettlementFailure}, reward=" +
                  $"{result.RewardFailure}, permanent=" +
                  $"{result.PermanentRewardFailure}, message={result.Message}";
        }
    }
}
