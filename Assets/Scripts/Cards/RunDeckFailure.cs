namespace HaveABreak.Cards
{
    public enum RunDeckFailure
    {
        None,
        InvalidDeck,
        InvalidCard,
        InvalidOwnedCardId,
        DuplicateOwnedCardId,
        CardNotFound,
        InvalidBattleInstanceId,
        InvalidRuntime,
        EnchantRegistrationFailed
    }
}
