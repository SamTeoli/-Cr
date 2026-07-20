namespace HaveABreak.Cards
{
    public enum BattleRuntimeEncounterFlowFailure
    {
        None,
        InvalidContext,
        BootstrapFailed,
        SessionBeginFailed,
        SessionNotFinished,
        SettlementFailed,
        RunDeckSnapshotFailed,
        EnchantRegistrationFailed
    }
}
