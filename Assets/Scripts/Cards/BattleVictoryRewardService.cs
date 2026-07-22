using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleVictoryRewardService
    {
        [SerializeField] private BattleSettlementService settlement;
        [SerializeField] private RunBattleState runState;
        [SerializeField] private BattleEncounterGrade encounterGrade;
        [SerializeField] private uint rewardSeed;
        [SerializeField] private int goldReward;
        [SerializeField] private bool goldClaimed;
        [NonSerialized] private BattleRewardConfig.GradeRule rewardRule;

        private BattleVictoryRewardService()
        {
        }

        public BattleVictoryRewardService(
            BattleSettlementService settlement,
            RunBattleState runState,
            BattleEncounterGrade encounterGrade,
            uint rewardSeed,
            BattleRewardConfig rewardConfig = null)
        {
            this.settlement = settlement ?? throw new ArgumentNullException(nameof(settlement));
            this.runState = runState ?? throw new ArgumentNullException(nameof(runState));
            this.encounterGrade = encounterGrade;
            this.rewardSeed = rewardSeed;
            rewardConfig?.TryGetRule(encounterGrade, out rewardRule);
            goldReward = rewardRule == null
                ? ResolveLegacyGoldReward(encounterGrade, rewardSeed)
                : rewardRule.GoldOptions[(int)(rewardSeed % (uint)rewardRule.GoldOptions.Count)];
        }

        public BattleEncounterGrade EncounterGrade => encounterGrade;
        public uint RewardSeed => rewardSeed;
        public int GoldReward => goldReward;
        public bool GoldClaimed => goldClaimed;
        public int EnchantChoiceCount => rewardRule?.EnchantChoiceCount ?? (encounterGrade == BattleEncounterGrade.FinalBoss ? 0 : 3);
        public CardRarity MinimumGuaranteedEnchantRarity =>
            rewardRule?.MinimumEnchantRarity ?? (encounterGrade == BattleEncounterGrade.Elite ||
            encounterGrade == BattleEncounterGrade.MidBoss
                ? CardRarity.Rare
                : CardRarity.Common);
        public int ConsumableItemRewardCount => rewardRule?.ConsumableItemRewardCount ?? (encounterGrade == BattleEncounterGrade.Elite ? 1 : 0);
        public bool GrantsFinalBossPermanentReward => rewardRule?.GrantsPermanentReward ?? (encounterGrade == BattleEncounterGrade.FinalBoss);

        public bool TryClaimGold(out BattleRewardFailure failure)
        {
            if (goldClaimed)
            {
                failure = BattleRewardFailure.AlreadyClaimed;
                return false;
            }

            if (!settlement.IsSettled)
            {
                failure = BattleRewardFailure.SettlementNotComplete;
                return false;
            }

            if (!settlement.RewardEligible)
            {
                failure = BattleRewardFailure.NotVictory;
                return false;
            }

            runState.AddRewardGold(goldReward);
            goldClaimed = true;
            failure = BattleRewardFailure.None;
            return true;
        }

        private static int ResolveLegacyGoldReward(BattleEncounterGrade grade, uint seed)
        {
            return grade switch
            {
                BattleEncounterGrade.Normal => new[] { 20, 25, 30 }[(int)(seed % 3u)],
                BattleEncounterGrade.Elite => new[] { 40, 45, 50, 55 }[(int)(seed % 4u)],
                BattleEncounterGrade.MidBoss => 60,
                BattleEncounterGrade.FinalBoss => 0,
                _ => 0
            };
        }
    }
}
