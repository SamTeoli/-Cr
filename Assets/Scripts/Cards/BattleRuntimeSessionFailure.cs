namespace HaveABreak.Cards
{
    public enum BattleRuntimeSessionFailure
    {
        None,
        InvalidSession,
        AlreadyStarted,
        NotStarted,
        BattleFinished,
        BeginBattleFailed,
        StartingHandConfirmationFailed,
        RoundResolutionFailed
    }
}
