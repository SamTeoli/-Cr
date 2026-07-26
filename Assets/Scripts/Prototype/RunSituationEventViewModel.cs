using System;
using System.Collections.Generic;
using System.Linq;

namespace HaveABreak.Cards
{
    public sealed class RunSituationEventOption
    {
        internal RunSituationEventOption(RunSituationEventChoice choice)
        {
            Choice = choice ?? throw new ArgumentNullException(nameof(choice));
        }

        public RunSituationEventChoice Choice { get; }
        public string ChoiceId => Choice.ChoiceId;
        public string DisplayText => Choice.DisplayText;
    }

    public sealed class RunSituationEventViewModel
    {
        public RunSituationEventOption[] CreateOptions(
            RunCampaignState campaign)
        {
            if (campaign == null ||
                campaign.Phase != RunCampaignPhase.NodeResolution ||
                campaign.ActiveNode?.NodeType != RunNodeType.SituationEvent)
            {
                return Array.Empty<RunSituationEventOption>();
            }

            IReadOnlyList<RunSituationEventChoice> choices =
                RunCampaignService.GetSituationEventChoices(campaign);
            if (choices == null || choices.Count == 0)
            {
                return Array.Empty<RunSituationEventOption>();
            }

            return choices
                .Where(choice => choice != null)
                .Select(choice => new RunSituationEventOption(choice))
                .ToArray();
        }

        public bool TryResolve(
            RunCampaignState campaign,
            RunBattleState run,
            string choiceId,
            out RunSituationEventOption selected,
            out string result,
            out RunCampaignFailure failure)
        {
            selected = null;
            result = null;
            if (campaign == null || run == null ||
                string.IsNullOrWhiteSpace(choiceId))
            {
                failure = default;
                return false;
            }

            string normalized = choiceId.Trim();
            RunSituationEventOption option = CreateOptions(campaign)
                .FirstOrDefault(value => string.Equals(
                    value.ChoiceId,
                    normalized,
                    StringComparison.OrdinalIgnoreCase));
            if (option == null)
            {
                failure = default;
                return false;
            }

            if (!RunCampaignService.TryResolveSituationEvent(
                    campaign,
                    run,
                    normalized,
                    out result,
                    out failure))
            {
                return false;
            }

            selected = option;
            return true;
        }
    }
}
