namespace HaveABreak.Cards
{
    public sealed class BattleRuntimeSessionRoundResult
    {
        internal BattleRuntimeSessionRoundResult(
            BattleRuntimeRoundResult round,
            BattleRuntimeSessionState session)
        {
            Round = round;
            CompletedRoundCount = session.CompletedRoundCount;
            Outcome = session.Outcome;
            PlayerTurnEventStartIndex =
                session.PlayerTurnEventStartIndex;
        }

        public BattleRuntimeRoundResult Round { get; }
        public int CompletedRoundCount { get; }
        public BattleOutcome Outcome { get; }
        public int PlayerTurnEventStartIndex { get; }
        public bool PlayerTurnStarted => Round.PlayerTurnStarted;
    }
}
