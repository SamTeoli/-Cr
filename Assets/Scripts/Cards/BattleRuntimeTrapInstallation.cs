namespace HaveABreak.Cards
{
    public sealed class BattleRuntimeTrapInstallation
    {
        internal BattleRuntimeTrapInstallation(
            BattleCardInstance sourceTrap,
            string playedEventId,
            int eligibleEnemyTurn)
        {
            SourceTrap = sourceTrap;
            PlayedEventId = playedEventId;
            EligibleEnemyTurn = eligibleEnemyTurn;
        }

        public BattleCardInstance SourceTrap { get; }
        public string PlayedEventId { get; }
        public int EligibleEnemyTurn { get; }
    }
}
