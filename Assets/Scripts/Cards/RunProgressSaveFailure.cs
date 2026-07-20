namespace HaveABreak.Cards
{
    public enum RunProgressSaveFailure
    {
        None,
        InvalidProgress,
        ActiveEncounterNotSupported,
        InvalidCardDatabase,
        InvalidEnchantDatabase,
        InvalidPath,
        SerializationFailed,
        DirectoryCreationFailed,
        WriteFailed,
        NotFound,
        ReadFailed,
        InvalidData,
        UnsupportedVersion,
        DuplicateBattleInstanceId,
        MissingCard,
        DuplicateOwnedCardId,
        MissingEnchant,
        EnchantRestoreFailed
    }
}
