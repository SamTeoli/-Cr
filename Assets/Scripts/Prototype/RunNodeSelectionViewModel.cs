using System;
using System.Collections.Generic;
using System.Linq;

namespace HaveABreak.Cards
{
    public sealed class RunNodeSelectionOption
    {
        internal RunNodeSelectionOption(RunNodeChoice choice)
        {
            Choice = choice ?? throw new ArgumentNullException(nameof(choice));
        }

        public RunNodeChoice Choice { get; }
        public string NodeId => Choice.NodeId;
        public string DisplayName => Choice.DisplayName;
        public RunNodeType NodeType => Choice.NodeType;
        public bool IsBattle => Choice.IsBattle;
        public string PreviousNodeId => Choice.PreviousNodeId;
        public string InlineLabel => $"{DisplayName}  ·  {NodeId}";
        public string StackedLabel => $"{DisplayName}\n{NodeId}";
    }

    public sealed class RunNodeSelectionViewModel
    {
        public RunNodeSelectionOption[] CreateOptions(
            RunCampaignState campaign)
        {
            if (campaign == null ||
                campaign.Phase != RunCampaignPhase.NodeSelection)
            {
                return Array.Empty<RunNodeSelectionOption>();
            }

            IReadOnlyList<RunNodeChoice> choices =
                RunCampaignService.GetChoices(campaign);
            if (choices == null || choices.Count == 0)
            {
                return Array.Empty<RunNodeSelectionOption>();
            }

            return choices
                .Where(choice => choice != null)
                .Select(choice => new RunNodeSelectionOption(choice))
                .ToArray();
        }

        public bool TrySelect(
            RunCampaignState campaign,
            string nodeId,
            out RunNodeSelectionOption selected,
            out RunCampaignFailure failure)
        {
            selected = null;
            if (campaign == null || string.IsNullOrWhiteSpace(nodeId))
            {
                failure = default;
                return false;
            }

            string normalized = nodeId.Trim();
            RunNodeSelectionOption option = CreateOptions(campaign)
                .FirstOrDefault(value => string.Equals(
                    value.NodeId,
                    normalized,
                    StringComparison.OrdinalIgnoreCase));

            if (!RunCampaignService.TrySelectNode(
                    campaign,
                    normalized,
                    out failure))
            {
                return false;
            }

            selected = option;
            if (selected == null && campaign.ActiveNode != null)
            {
                selected = new RunNodeSelectionOption(campaign.ActiveNode);
            }

            return selected != null;
        }
    }
}
