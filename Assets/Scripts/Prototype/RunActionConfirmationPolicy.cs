namespace HaveABreak.Cards
{
    public static class RunActionConfirmationPolicy
    {
        public static bool ShouldConfirmNewRun(
            bool hasCurrentRun,
            bool saveInspectionSucceeded,
            RunSaveSlotState saveSlotState)
        {
            return hasCurrentRun ||
                   !saveInspectionSucceeded ||
                   saveSlotState != RunSaveSlotState.Empty;
        }

        public static bool ShouldConfirmContinue(
            bool hasCurrentRun,
            RunCampaignPhase phase)
        {
            return hasCurrentRun && phase == RunCampaignPhase.Battle;
        }
    }
}
