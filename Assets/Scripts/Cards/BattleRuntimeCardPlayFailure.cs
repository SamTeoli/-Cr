namespace HaveABreak.Cards
{
    public enum BattleRuntimeCardPlayFailure
    {
        None,
        InvalidRuntime,
        InvalidTurnPhase,
        PreviewFailed,
        BeginActionFailed,
        ConfirmFailed,
        MonsterRegistrationFailed,
        CompleteActionFailed
    }
}
