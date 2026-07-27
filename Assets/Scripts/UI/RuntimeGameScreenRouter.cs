using System;

namespace HaveABreak.Cards
{
    public enum RuntimeGameScreen
    {
        Start,
        Confirmation,
        RunPreparation,
        NodeSelection,
        NodeResolution,
        Battle,
        Reward,
        Completed,
        Defeated
    }

    public static class RuntimeGameScreenRouter
    {
        public static RuntimeGameScreen Resolve(
            RunCampaignPhase? campaignPhase,
            bool isPreparingRun,
            bool isAwaitingConfirmation)
        {
            if (isAwaitingConfirmation)
            {
                return RuntimeGameScreen.Confirmation;
            }

            if (isPreparingRun)
            {
                return RuntimeGameScreen.RunPreparation;
            }

            if (!campaignPhase.HasValue)
            {
                return RuntimeGameScreen.Start;
            }

            return campaignPhase.Value switch
            {
                RunCampaignPhase.NodeSelection => RuntimeGameScreen.NodeSelection,
                RunCampaignPhase.NodeResolution => RuntimeGameScreen.NodeResolution,
                RunCampaignPhase.Battle => RuntimeGameScreen.Battle,
                RunCampaignPhase.Reward => RuntimeGameScreen.Reward,
                RunCampaignPhase.Completed => RuntimeGameScreen.Completed,
                RunCampaignPhase.Defeated => RuntimeGameScreen.Defeated,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(campaignPhase),
                    campaignPhase,
                    "지원하지 않는 런 캠페인 단계입니다.")
            };
        }
    }
}
