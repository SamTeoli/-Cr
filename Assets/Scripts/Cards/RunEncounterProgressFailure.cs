namespace HaveABreak.Cards
{
    public enum RunEncounterProgressFailure
    {
        None,
        InvalidState,
        RunEnded,
        EncounterAlreadyActive,
        BattleInstanceAlreadyUsed,
        BeginFailed,
        NoActiveEncounter,
        SettlementFailed,
        SettlementNotComplete,
        GoldRewardPending,
        EnchantRewardPending,
        ConsumableRewardPending,
        PermanentRewardPending
    }
}
