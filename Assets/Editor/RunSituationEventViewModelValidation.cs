using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class RunSituationEventViewModelValidation
    {
        [MenuItem("Have a Break/Validate Run Situation Event ViewModel")]
        private static void ValidateFromMenu()
        {
            Debug.Log(Validate()
                ? "Run situation event ViewModel passed."
                : "Run situation event ViewModel failed.");
        }

        internal static bool Validate()
        {
            RunSituationEventViewModel selection = new();
            if (selection.CreateOptions(null).Length != 0)
            {
                return false;
            }

            RunCampaignState campaign = CampaignAtSituationEvent();
            RunBattleState run = new(30, 30, 0);
            if (campaign == null ||
                campaign.Phase != RunCampaignPhase.NodeResolution ||
                campaign.ActiveNode?.NodeType != RunNodeType.SituationEvent)
            {
                return false;
            }

            RunSituationEventOption[] options =
                selection.CreateOptions(campaign);
            if (options.Length == 0 ||
                string.IsNullOrWhiteSpace(campaign.ActiveSituationEventId))
            {
                return false;
            }

            foreach (RunSituationEventOption option in options)
            {
                if (option == null || option.Choice == null ||
                    string.IsNullOrWhiteSpace(option.ChoiceId) ||
                    string.IsNullOrWhiteSpace(option.DisplayText) ||
                    option.ChoiceId != option.Choice.ChoiceId ||
                    option.DisplayText != option.Choice.DisplayText)
                {
                    return false;
                }
            }

            if (selection.TryResolve(
                    campaign,
                    run,
                    "MISSING-EVENT-CHOICE",
                    out RunSituationEventOption missing,
                    out string missingResult,
                    out _) ||
                missing != null ||
                !string.IsNullOrEmpty(missingResult) ||
                campaign.CompletedNodeCount != 0 ||
                campaign.Phase != RunCampaignPhase.NodeResolution)
            {
                return false;
            }

            string resolvedEventId = campaign.ActiveSituationEventId;
            RunSituationEventOption expected = options[0];
            if (!selection.TryResolve(
                    campaign,
                    run,
                    expected.ChoiceId,
                    out RunSituationEventOption selected,
                    out string result,
                    out RunCampaignFailure failure) ||
                failure != RunCampaignFailure.None ||
                selected == null ||
                selected.ChoiceId != expected.ChoiceId ||
                selected.DisplayText != expected.DisplayText ||
                string.IsNullOrWhiteSpace(result) ||
                campaign.CompletedNodeCount != 1 ||
                campaign.Phase != RunCampaignPhase.NodeSelection ||
                campaign.ActiveNode != null ||
                campaign.ResolvedSituationEventIds.Count != 1 ||
                !string.Equals(
                    campaign.ResolvedSituationEventIds[0],
                    resolvedEventId,
                    StringComparison.OrdinalIgnoreCase) ||
                selection.CreateOptions(campaign).Length != 0)
            {
                return false;
            }

            return true;
        }

        private static RunCampaignState CampaignAtSituationEvent()
        {
            for (int seed = 0; seed < 1000; seed++)
            {
                RunCampaignState campaign = new(seed);
                RunNodeChoice choice = RunCampaignService.GetChoices(campaign)
                    .FirstOrDefault(value =>
                        value.NodeType == RunNodeType.SituationEvent);
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
