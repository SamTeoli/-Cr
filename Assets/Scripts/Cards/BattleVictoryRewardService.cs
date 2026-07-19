using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleVictoryRewardService
    {
        private static readonly int[] NormalGoldOptions = { 20, 25, 30 };
        private static readonly int[] EliteGoldOptions = { 40, 45, 50, 55 };

        [SerializeField] private BattleSettlementService settlement;
        [SerializeField] private RunBattleState runState;
        [SerializeField] private BattleEncounterGrade encounterGrade;
        [SerializeField] private uint rewardSeed;
        [SerializeField] private int goldReward;
        [SerializeField] private bool goldClaimed;

        private BattleVictoryRewardService()
        {
        }

        public BattleVictoryRewardService(
            BattleSettlementService settlement,
            RunBattleState runState,
            BattleEncounterGrade encounterGrade,
            uint rewardSeed)
        {
            this.settlement = settlement ?? throw new ArgumentNullException(nameof(settlement));
            this.runState = runState ?? throw new ArgumentNullException(nameof(runState));
            this.encounterGrade = encounterGrade;
            this.rewardSeed = rewardSeed;
            goldReward = ResolveGoldReward(encounterGrade, rewardSeed);
        }

        public BattleEncounterGrade EncounterGrade => encounterGrade;
        public uint RewardSeed => rewardSeed;
        public int GoldReward => goldReward;
        public bool GoldClaimed => goldClaimed;
        public int EnchantChoiceCount => encounterGrade == BattleEncounterGrade.FinalBoss ? 0 : 3;
        public CardRarity MinimumGuaranteedEnchantRarity =>
            encounterGrade == BattleEncounterGrade.Elite ||
            encounterGrade == BattleEncounterGrade.MidBoss
                ? CardRarity.Rare
                : CardRarity.Common;
        public int ConsumableItemRewardCount => encounterGrade == BattleEncounterGrade.Elite ? 1 : 0;
        public bool GrantsFinalBossPermanentReward => encounterGrade == BattleEncounterGrade.FinalBoss;

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

        private static int ResolveGoldReward(BattleEncounterGrade grade, uint seed)
        {
            return grade switch
            {
                BattleEncounterGrade.Normal => NormalGoldOptions[(int)(seed % (uint)NormalGoldOptions.Length)],
                BattleEncounterGrade.Elite => EliteGoldOptions[(int)(seed % (uint)EliteGoldOptions.Length)],
                BattleEncounterGrade.MidBoss => 60,
                BattleEncounterGrade.FinalBoss => 0,
                _ => 0
            };
        }
    }
}
