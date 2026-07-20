using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleVictoryConsumableRewardService
    {
        [SerializeField] private BattleSettlementService settlement;
        [SerializeField] private BattleVictoryRewardService rewardRules;
        [SerializeField] private RunBattleState runState;
        [SerializeField] private List<string> claimedItemIds = new();

        private BattleVictoryConsumableRewardService()
        {
        }

        private BattleVictoryConsumableRewardService(
            BattleSettlementService settlement,
            BattleVictoryRewardService rewardRules,
            RunBattleState runState)
        {
            this.settlement = settlement;
            this.rewardRules = rewardRules;
            this.runState = runState;
        }

        public int RequiredItemCount =>
            rewardRules.ConsumableItemRewardCount;
        public IReadOnlyList<string> ClaimedItemIds => claimedItemIds;
        public bool Claimed =>
            claimedItemIds.Count >= RequiredItemCount;

        public static bool TryCreate(
            BattleRuntimeEncounterContext context,
            out BattleVictoryConsumableRewardService service,
            out BattleVictoryConsumableRewardFailure failure)
        {
            service = null;
            if (context?.Settlement == null ||
                context.VictoryRewards == null ||
                context.RunState == null)
            {
                failure = BattleVictoryConsumableRewardFailure
                    .InvalidEncounterContext;
                return false;
            }

            if (context.VictoryConsumableRewards != null)
            {
                failure = BattleVictoryConsumableRewardFailure
                    .AlreadyCreated;
                return false;
            }

            if (!context.Settlement.IsSettled)
            {
                failure = BattleVictoryConsumableRewardFailure
                    .SettlementNotComplete;
                return false;
            }

            if (!context.Settlement.RewardEligible)
            {
                failure =
                    BattleVictoryConsumableRewardFailure.NotVictory;
                return false;
            }

            if (context.VictoryRewards.ConsumableItemRewardCount <= 0)
            {
                failure = BattleVictoryConsumableRewardFailure
                    .NoConsumableReward;
                return false;
            }

            BattleVictoryConsumableRewardService created = new(
                context.Settlement,
                context.VictoryRewards,
                context.RunState);
            if (!context.TrySetVictoryConsumableRewards(created))
            {
                failure = BattleVictoryConsumableRewardFailure
                    .AlreadyCreated;
                return false;
            }

            service = created;
            failure = BattleVictoryConsumableRewardFailure.None;
            return true;
        }

        public bool TryClaim(
            string itemId,
            out BattleVictoryConsumableRewardFailure failure)
        {
            if (Claimed)
            {
                failure = BattleVictoryConsumableRewardFailure
                    .AlreadyClaimed;
                return false;
            }

            if (!settlement.IsSettled)
            {
                failure = BattleVictoryConsumableRewardFailure
                    .SettlementNotComplete;
                return false;
            }

            if (!settlement.RewardEligible)
            {
                failure =
                    BattleVictoryConsumableRewardFailure.NotVictory;
                return false;
            }

            if (!runState.TryAddRewardConsumableItem(itemId))
            {
                failure = BattleVictoryConsumableRewardFailure.InvalidItemId;
                return false;
            }

            claimedItemIds.Add(itemId.Trim());
            failure = BattleVictoryConsumableRewardFailure.None;
            return true;
        }
    }
}
