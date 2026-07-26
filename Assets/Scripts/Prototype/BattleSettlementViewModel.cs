using System;

namespace HaveABreak.Cards
{
    public enum BattleSettlementCommandFailure
    {
        None = 0,
        InvalidState = 1,
        SettlementFailed = 2,
        EncounterCompletionFailed = 3,
        GoldClaimFailed = 4,
        PermanentRewardCreateFailed = 5,
        PermanentRewardClaimFailed = 6
    }

    public sealed class BattleSettlementCommandResult
    {
        internal BattleSettlementCommandResult(
            bool succeeded,
            BattleSettlementCommandFailure failure,
            BattleOutcome outcome,
            int goldReward,
            bool goldClaimed,
            bool permanentRewardRequired,
            bool permanentRewardClaimed,
            bool activeEncounterCompleted,
            RunCampaignPhase campaignPhase,
            RunEncounterProgressFailure progressFailure,
            BattleRuntimeEncounterFlowFailure flowFailure,
            BattleRuntimeSessionFailure sessionFailure,
            BattleSettlementFailure settlementFailure,
            BattleRewardFailure rewardFailure,
            BattleVictoryPermanentRewardFailure permanentRewardFailure,
            string message)
        {
            Succeeded = succeeded;
            Failure = failure;
            Outcome = outcome;
            GoldReward = Math.Max(0, goldReward);
            GoldClaimed = goldClaimed;
            PermanentRewardRequired = permanentRewardRequired;
            PermanentRewardClaimed = permanentRewardClaimed;
            ActiveEncounterCompleted = activeEncounterCompleted;
            CampaignPhase = campaignPhase;
            ProgressFailure = progressFailure;
            FlowFailure = flowFailure;
            SessionFailure = sessionFailure;
            SettlementFailure = settlementFailure;
            RewardFailure = rewardFailure;
            PermanentRewardFailure = permanentRewardFailure;
            Message = message;
        }

        public bool Succeeded { get; }
        public BattleSettlementCommandFailure Failure { get; }
        public BattleOutcome Outcome { get; }
        public int GoldReward { get; }
        public bool GoldClaimed { get; }
        public bool PermanentRewardRequired { get; }
        public bool PermanentRewardClaimed { get; }
        public bool ActiveEncounterCompleted { get; }
        public RunCampaignPhase CampaignPhase { get; }
        public RunEncounterProgressFailure ProgressFailure { get; }
        public BattleRuntimeEncounterFlowFailure FlowFailure { get; }
        public BattleRuntimeSessionFailure SessionFailure { get; }
        public BattleSettlementFailure SettlementFailure { get; }
        public BattleRewardFailure RewardFailure { get; }
        public BattleVictoryPermanentRewardFailure PermanentRewardFailure { get; }
        public string Message { get; }
    }

    public sealed class BattleSettlementViewModel
    {
        public const string FinalBossPermanentRewardId =
            "PERMANENT-FIRST-RUN-CLEAR";

        public BattleSettlementCommandResult TrySettle(
            RunCampaignState campaign,
            RunEncounterProgressState progress)
        {
            BattleRuntimeEncounterContext context = progress?.ActiveEncounter;
            BattleRuntimeSessionState session = context?.Session;
            if (campaign == null || progress == null || context == null ||
                session == null || campaign.Phase != RunCampaignPhase.Battle ||
                !session.IsFinished)
            {
                return Failure(
                    BattleSettlementCommandFailure.InvalidState,
                    context,
                    campaign,
                    "정산 실패: 종료된 활성 전투가 없습니다.");
            }

            RunEncounterProgressFailure progressFailure =
                RunEncounterProgressFailure.None;
            BattleRuntimeEncounterFlowFailure flowFailure =
                BattleRuntimeEncounterFlowFailure.None;
            BattleRuntimeSessionFailure sessionFailure =
                BattleRuntimeSessionFailure.None;
            BattleSettlementFailure settlementFailure =
                BattleSettlementFailure.None;
            if (!context.Settlement.IsSettled &&
                !RunEncounterProgressService.TrySettleActive(
                    progress,
                    out progressFailure,
                    out flowFailure,
                    out sessionFailure,
                    out settlementFailure))
            {
                return Failure(
                    BattleSettlementCommandFailure.SettlementFailed,
                    context,
                    campaign,
                    $"정산 실패: {progressFailure} / {flowFailure} / " +
                    $"{sessionFailure} / {settlementFailure}",
                    progressFailure,
                    flowFailure,
                    sessionFailure,
                    settlementFailure);
            }

            BattleOutcome outcome = context.Settlement.SettledOutcome;
            if (outcome == BattleOutcome.Defeat)
            {
                bool completed = RunEncounterProgressService.TryCompleteActive(
                    progress,
                    out progressFailure);
                if (!completed)
                {
                    return Failure(
                        BattleSettlementCommandFailure.EncounterCompletionFailed,
                        context,
                        campaign,
                        $"패배 정산 완료 처리 실패: {progressFailure}",
                        progressFailure);
                }

                RunCampaignService.MarkBattleReward(
                    campaign,
                    BattleOutcome.Defeat);
                return Success(
                    context,
                    campaign,
                    true,
                    false,
                    "패배 정산 완료 · 런 종료");
            }

            if (outcome != BattleOutcome.Victory ||
                context.VictoryRewards == null)
            {
                return Failure(
                    BattleSettlementCommandFailure.InvalidState,
                    context,
                    campaign,
                    $"정산 실패: 지원하지 않는 전투 결과 {outcome}");
            }

            BattleRewardFailure rewardFailure = BattleRewardFailure.None;
            if (!context.VictoryRewards.GoldClaimed &&
                !context.VictoryRewards.TryClaimGold(out rewardFailure))
            {
                return Failure(
                    BattleSettlementCommandFailure.GoldClaimFailed,
                    context,
                    campaign,
                    $"골드 보상 실패: {rewardFailure}",
                    rewardFailure: rewardFailure);
            }

            bool permanentRequired =
                context.VictoryRewards.GrantsFinalBossPermanentReward;
            bool permanentClaimed = !permanentRequired;
            BattleVictoryPermanentRewardFailure permanentFailure =
                BattleVictoryPermanentRewardFailure.None;
            if (permanentRequired)
            {
                BattleVictoryPermanentRewardService permanent =
                    context.VictoryPermanentRewards;
                if (permanent == null &&
                    !BattleVictoryPermanentRewardService.TryCreate(
                        progress,
                        out permanent,
                        out permanentFailure))
                {
                    return Failure(
                        BattleSettlementCommandFailure
                            .PermanentRewardCreateFailed,
                        context,
                        campaign,
                        $"영구 보상 생성 실패: {permanentFailure}",
                        rewardFailure: rewardFailure,
                        permanentFailure: permanentFailure);
                }

                if (permanent == null)
                {
                    return Failure(
                        BattleSettlementCommandFailure
                            .PermanentRewardCreateFailed,
                        context,
                        campaign,
                        "영구 보상 생성 실패: 보상 서비스를 찾을 수 없습니다.",
                        rewardFailure: rewardFailure);
                }

                if (!permanent.Claimed &&
                    !permanent.TryClaim(
                        FinalBossPermanentRewardId,
                        out permanentFailure))
                {
                    return Failure(
                        BattleSettlementCommandFailure
                            .PermanentRewardClaimFailed,
                        context,
                        campaign,
                        $"영구 보상 수령 실패: {permanentFailure}",
                        rewardFailure: rewardFailure,
                        permanentFailure: permanentFailure);
                }

                permanentClaimed = permanent.Claimed;
            }

            RunCampaignService.MarkBattleReward(
                campaign,
                BattleOutcome.Victory);
            return Success(
                context,
                campaign,
                false,
                permanentClaimed,
                $"승리 정산 완료 · 골드 " +
                $"{context.VictoryRewards.GoldReward} 획득");
        }

        private static BattleSettlementCommandResult Success(
            BattleRuntimeEncounterContext context,
            RunCampaignState campaign,
            bool activeEncounterCompleted,
            bool permanentRewardClaimed,
            string message)
        {
            var rewards = context?.VictoryRewards;
            return new BattleSettlementCommandResult(
                true,
                BattleSettlementCommandFailure.None,
                context?.Settlement?.SettledOutcome ?? BattleOutcome.Ongoing,
                rewards?.GoldReward ?? 0,
                rewards?.GoldClaimed == true,
                rewards?.GrantsFinalBossPermanentReward == true,
                permanentRewardClaimed,
                activeEncounterCompleted,
                campaign?.Phase ?? RunCampaignPhase.NodeSelection,
                RunEncounterProgressFailure.None,
                BattleRuntimeEncounterFlowFailure.None,
                BattleRuntimeSessionFailure.None,
                BattleSettlementFailure.None,
                BattleRewardFailure.None,
                BattleVictoryPermanentRewardFailure.None,
                message);
        }

        private static BattleSettlementCommandResult Failure(
            BattleSettlementCommandFailure failure,
            BattleRuntimeEncounterContext context,
            RunCampaignState campaign,
            string message,
            RunEncounterProgressFailure progressFailure =
                RunEncounterProgressFailure.None,
            BattleRuntimeEncounterFlowFailure flowFailure =
                BattleRuntimeEncounterFlowFailure.None,
            BattleRuntimeSessionFailure sessionFailure =
                BattleRuntimeSessionFailure.None,
            BattleSettlementFailure settlementFailure =
                BattleSettlementFailure.None,
            BattleRewardFailure rewardFailure = BattleRewardFailure.None,
            BattleVictoryPermanentRewardFailure permanentFailure =
                BattleVictoryPermanentRewardFailure.None)
        {
            var rewards = context?.VictoryRewards;
            return new BattleSettlementCommandResult(
                false,
                failure,
                context?.Settlement?.SettledOutcome ?? BattleOutcome.Ongoing,
                rewards?.GoldReward ?? 0,
                rewards?.GoldClaimed == true,
                rewards?.GrantsFinalBossPermanentReward == true,
                context?.VictoryPermanentRewards?.Claimed == true,
                false,
                campaign?.Phase ?? RunCampaignPhase.NodeSelection,
                progressFailure,
                flowFailure,
                sessionFailure,
                settlementFailure,
                rewardFailure,
                permanentFailure,
                message);
        }
    }
}
