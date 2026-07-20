namespace HaveABreak.Cards
{
    public enum ActiveBattleCheckpointFailure
    {
        None,
        InvalidProgress,
        UnsafeCheckpoint,
        MissingStartParameters,
        InvalidCardDatabase,
        InvalidEnchantDatabase,
        InvalidEncounter,
        InvalidPath,
        SerializationFailed,
        DirectoryCreationFailed,
        WriteFailed,
        NotFound,
        ReadFailed,
        InvalidData,
        UnsupportedVersion,
        BaseProgressFailed,
        RestoreBeginFailed
    }
}
