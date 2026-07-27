using System;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class RuntimeGameScreenRouterValidation
    {
        [MenuItem("Have a Break/Tests/Validate Final UI Screen Router")]
        private static void RunFromMenu()
        {
            Validate();
        }

        internal static bool Validate()
        {
            bool valid = true;
            valid &= Expect(null, false, false, RuntimeGameScreen.Start);
            valid &= Expect(null, true, false, RuntimeGameScreen.RunPreparation);
            valid &= Expect(null, false, true, RuntimeGameScreen.Confirmation);
            valid &= Expect(
                RunCampaignPhase.NodeSelection,
                false,
                false,
                RuntimeGameScreen.NodeSelection);
            valid &= Expect(
                RunCampaignPhase.NodeResolution,
                false,
                false,
                RuntimeGameScreen.NodeResolution);
            valid &= Expect(
                RunCampaignPhase.Battle,
                false,
                false,
                RuntimeGameScreen.Battle);
            valid &= Expect(
                RunCampaignPhase.Reward,
                false,
                false,
                RuntimeGameScreen.Reward);
            valid &= Expect(
                RunCampaignPhase.Completed,
                false,
                false,
                RuntimeGameScreen.Completed);
            valid &= Expect(
                RunCampaignPhase.Defeated,
                false,
                false,
                RuntimeGameScreen.Defeated);

            valid &= Expect(
                RunCampaignPhase.Battle,
                true,
                true,
                RuntimeGameScreen.Confirmation);
            valid &= Expect(
                RunCampaignPhase.Battle,
                true,
                false,
                RuntimeGameScreen.RunPreparation);
            valid &= Enum.GetValues(typeof(RunCampaignPhase)).Length == 6;

            if (valid)
            {
                Debug.Log(
                    "Final UI screen router validation passed: start, " +
                    "confirmation, preparation, campaign phases, and priority.");
            }
            else
            {
                Debug.LogError(
                    "Final UI screen router validation failed. " +
                    "Check screen mappings and priority rules.");
            }

            return valid;
        }

        private static bool Expect(
            RunCampaignPhase? phase,
            bool preparing,
            bool confirming,
            RuntimeGameScreen expected)
        {
            return RuntimeGameScreenRouter.Resolve(
                       phase,
                       preparing,
                       confirming) == expected;
        }
    }
}
