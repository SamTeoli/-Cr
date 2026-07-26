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
            if (config == null || !config.IsReady ||
                config.RunStartProgressionConfig == null)
            {
                Debug.LogError(
                    "Full run validation failed: runtime prototype config is not ready.");
                return false;
            }

            int totalNodes = config.RunStartProgressionConfig.TotalNodeCount;
            for (int seed = 0; seed < 512; seed++)
            {
                if (!TryCompleteRun(seed, totalNodes, out string failure))
                {
                    continue;
                }

                Debug.Log(
                    $"Full run end-to-end validation passed: seed {seed}, " +
                    $"{totalNodes} nodes, all node categories, midpoint resume, " +
                    "final boss, and run completion.");
                return true;
            }

            Debug.LogError(
                "Full run validation failed: no deterministic seed completed all " +
                "required node categories within the search range.");
            return false;
        }

        private static bool TryCompleteRun(
            int seed,
            int totalNodes,
            out string failure)
        {
            failure = null;
            RunCampaignState campaign = new(seed);
            RunBattleState run = new(30, 30, 1000);
            HashSet<RunNodeType> visited = new();
            bool resumed = false;

            for (int step = 0; step < totalNodes + 2; step++)
            {
                if (campaign.Phase == RunCampaignPhase.Completed)
                {
                    break;
                }

                if (campaign.Phase != RunCampaignPhase.NodeSelection)
                {
                    failure = $"unexpected phase before selection: {campaign.Phase}";
                    return false;
                }

                IReadOnlyList<RunNodeChoice> choices =
                    RunCampaignService.GetChoices(campaign);
                RunNodeChoice selected = SelectChoice(choices, visited);
                if (selected == null || !RunCampaignService.TrySelectNode(
                        campaign,
                        selected.NodeId,
                        out RunCampaignFailure selectFailure))
                {
                    failure = $"node selection failed: {selectFailure}";
                    return false;
                }

                int completedBefore = campaign.CompletedNodeCount;
                visited.Add(selected.NodeType);
                if (!ResolveActiveNode(campaign, run, out failure))
                {
                    return false;
                }

                if (campaign.CompletedNodeCount != completedBefore + 1)
                {
                    failure =
                        $"completed node count did not advance at {selected.NodeId}";
                    return false;
                }

                if (!resumed && campaign.CompletedNodeCount >= totalNodes / 2)
                {
                    string json = JsonUtility.ToJson(campaign);
                    campaign = JsonUtility.FromJson<RunCampaignState>(json);
                    resumed = true;
                    if (campaign == null ||
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
            bool pathValid = campaign.SelectedNodePath.Count == totalNodes &&
                             campaign.SelectedNodePath
                                 .Distinct(StringComparer.OrdinalIgnoreCase)
                                 .Count() == totalNodes;
            bool completed = campaign.Phase == RunCampaignPhase.Completed &&
                             campaign.CompletedNodeCount == totalNodes &&
                             campaign.ActiveNode == null;
            if (!allRequired || !pathValid || !completed || !resumed)
            {
                failure =
                    $"final state invalid: phase={campaign.Phase}, " +
                    $"completed={campaign.CompletedNodeCount}/{totalNodes}, " +
                    $"path={campaign.SelectedNodePath.Count}, resumed={resumed}, " +
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

        private static bool ResolveActiveNode(
            RunCampaignState campaign,
            RunBattleState run,
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
                            run,
                            out RunCampaignFailure shopFailure))
                    {
                        failure = $"shop resolution failed: {shopFailure}";
                        return false;
                    }
                    return true;

                case RunNodeType.SituationEvent:
                    IReadOnlyList<RunSituationEventChoice> eventChoices =
                        RunCampaignService.GetSituationEventChoices(campaign);
                    RunSituationEventChoice eventChoice = eventChoices
                        .OrderBy(choice => choice.ChoiceId,
                            StringComparer.OrdinalIgnoreCase)
                        .FirstOrDefault();
                    if (eventChoice == null ||
                        !RunCampaignService.TryResolveSituationEvent(
                            campaign,
                            run,
                            eventChoice.ChoiceId,
                            out _,
                            out RunCampaignFailure eventFailure))
                    {
                        failure = $"event resolution failed: {eventFailure}";
                        return false;
                    }
                    return true;

                case RunNodeType.RestOrUpgrade:
                    if (!RunCampaignService.TryRest(
                            campaign,
                            run,
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
                    RunCampaignService.MarkBattleReward(
                        campaign,
                        BattleOutcome.Victory);
                    RunCampaignService.CompleteBattleReward(campaign);
                    return campaign.Phase == RunCampaignPhase.NodeSelection ||
                           campaign.Phase == RunCampaignPhase.Completed;

                default:
                    failure = $"unsupported node type: {node.NodeType}";
                    return false;
            }
        }
    }
}
