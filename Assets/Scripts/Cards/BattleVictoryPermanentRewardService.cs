using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleVictoryPermanentRewardService
    {
        [SerializeField] private BattleSettlementService settlement;
        [SerializeField] private BattleVictoryRewardService rewardRules;
        [SerializeField] private PlayerPermanentRewardState permanentRewards;
        [SerializeField] private string claimedRewardId;

        private BattleVictoryPermanentRewardService()
        {
        }

        private BattleVictoryPermanentRewardService(
            BattleSettlementService settlement,
            BattleVictoryRewardService rewardRules,
            PlayerPermanentRewardState permanentRewards)
        {
            this.settlement = settlement;
            this.rewardRules = rewardRules;
            this.permanentRewards = permanentRewards;
        }

        public bool Claimed => !string.IsNullOrEmpty(claimedRewardId);
        public string ClaimedRewardId => claimedRewardId;

        public static bool TryCreate(
            RunEncounterProgressState progress,
            out BattleVictoryPermanentRewardService service,
            out BattleVictoryPermanentRewardFailure failure)
        {
            service = null;
            BattleRuntimeEncounterContext context =
                progress?.ActiveEncounter;
            if (context?.Settlement == null ||
                context.VictoryRewards == null)
            {
                failure = BattleVictoryPermanentRewardFailure
                    .InvalidEncounterContext;
                return false;
            }

            PlayerPermanentRewardState permanentRewards =
                progress.PermanentRewards;
            if (permanentRewards == null)
            {
                failure = BattleVictoryPermanentRewardFailure
                    .InvalidRewardState;
                return false;
            }

            if (context.VictoryPermanentRewards != null)
            {
                failure = BattleVictoryPermanentRewardFailure
                    .AlreadyCreated;
                return false;
            }

            if (!context.Settlement.IsSettled)
            {
                failure = BattleVictoryPermanentRewardFailure
                    .SettlementNotComplete;
                return false;
            }

            if (!context.Settlement.RewardEligible)
            {
                failure = BattleVictoryPermanentRewardFailure.NotVictory;
                return false;
            }

            if (!context.VictoryRewards.GrantsFinalBossPermanentReward)
            {
                failure = BattleVictoryPermanentRewardFailure.NotFinalBoss;
                return false;
            }

            BattleVictoryPermanentRewardService created = new(
                context.Settlement,
                context.VictoryRewards,
                permanentRewards);
            if (!context.TrySetVictoryPermanentRewards(created))
            {
                failure = BattleVictoryPermanentRewardFailure
                    .AlreadyCreated;
                return false;
            }

            service = created;
            failure = BattleVictoryPermanentRewardFailure.None;
            return true;
        }

        public bool TryClaim(
            string rewardId,
            out BattleVictoryPermanentRewardFailure failure)
        {
            if (Claimed)
            {
                failure = BattleVictoryPermanentRewardFailure
                    .AlreadyClaimed;
                return false;
            }

            if (!settlement.IsSettled)
            {
                failure = BattleVictoryPermanentRewardFailure
                    .SettlementNotComplete;
                return false;
            }

            if (!settlement.RewardEligible)
            {
                failure = BattleVictoryPermanentRewardFailure.NotVictory;
                return false;
            }

            if (!rewardRules.GrantsFinalBossPermanentReward)
            {
                failure = BattleVictoryPermanentRewardFailure.NotFinalBoss;
                return false;
            }

            if (string.IsNullOrWhiteSpace(rewardId))
            {
                failure = BattleVictoryPermanentRewardFailure
                    .InvalidRewardId;
                return false;
            }

            if (!permanentRewards.TryAdd(rewardId))
            {
                failure = BattleVictoryPermanentRewardFailure
                    .RewardAlreadyOwned;
                return false;
            }

            claimedRewardId = rewardId.Trim();
            failure = BattleVictoryPermanentRewardFailure.None;
            return true;
        }
    }
}
