namespace HaveABreak.Cards
{
    public sealed class ActiveBattleCheckpointInfo
    {
        public ActiveBattleCheckpointInfo(
            string battleInstanceId,
            string encounterId,
            int shuffleSeed,
            int maximumMana,
            uint rewardSeed)
        {
            BattleInstanceId = battleInstanceId;
            EncounterId = encounterId;
            ShuffleSeed = shuffleSeed;
            MaximumMana = maximumMana;
            RewardSeed = rewardSeed;
        }

        public string BattleInstanceId { get; }
        public string EncounterId { get; }
        public int ShuffleSeed { get; }
        public int MaximumMana { get; }
        public uint RewardSeed { get; }
    }
}
