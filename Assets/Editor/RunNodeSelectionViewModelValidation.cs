using System;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class RunNodeSelectionViewModelValidation
    {
        [MenuItem("Have a Break/Validate Run Node Selection ViewModel")]
        private static void ValidateFromMenu()
        {
            Debug.Log(Validate()
                ? "Run node selection ViewModel passed."
                : "Run node selection ViewModel failed.");
        }

        internal static bool Validate()
        {
            RunNodeSelectionViewModel selection = new();
            if (selection.CreateOptions(null).Length != 0)
            {
                return false;
            }

            RunCampaignState campaign = new(20260722);
            RunNodeSelectionOption[] options =
                selection.CreateOptions(campaign);
            if (options.Length < 2 || options.Length > 4)
            {
                return false;
            }

            for (int index = 0; index < options.Length; index++)
            {
                RunNodeSelectionOption option = options[index];
                if (option == null || option.Choice == null ||
                    string.IsNullOrWhiteSpace(option.NodeId) ||
                    string.IsNullOrWhiteSpace(option.DisplayName) ||
                    option.NodeType != option.Choice.NodeType ||
                    option.IsBattle != option.Choice.IsBattle ||
                    option.PreviousNodeId != option.Choice.PreviousNodeId ||
                    option.InlineLabel !=
                        $"{option.DisplayName}  ·  {option.NodeId}" ||
                    option.StackedLabel !=
                        $"{option.DisplayName}\n{option.NodeId}")
                {
                    return false;
                }
            }

            if (selection.TrySelect(
                    campaign,
                    "MISSING-NODE",
                    out RunNodeSelectionOption missing,
                    out _) ||
                missing != null ||
                campaign.ActiveNode != null)
            {
                return false;
            }

            RunNodeSelectionOption expected = options[0];
            if (!selection.TrySelect(
                    campaign,
                    expected.NodeId,
                    out RunNodeSelectionOption selected,
                    out RunCampaignFailure failure) ||
                failure != RunCampaignFailure.None ||
                selected == null ||
                !string.Equals(
                    selected.NodeId,
                    expected.NodeId,
                    StringComparison.OrdinalIgnoreCase) ||
                selected.DisplayName != expected.DisplayName ||
                selected.NodeType != expected.NodeType ||
                selected.IsBattle != expected.IsBattle ||
                campaign.ActiveNode == null ||
                !string.Equals(
                    campaign.ActiveNode.NodeId,
                    expected.NodeId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return selection.CreateOptions(campaign).Length == 0;
        }
    }
}
