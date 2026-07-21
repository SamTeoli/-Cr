namespace HaveABreak.Cards
{
    public enum ActiveBattleCheckpointFailure
    {
        None,
        InvalidProgress,
        UnsafeCheckpoint,
        MissingStartParameters,
        CheckpointMismatch,
        InvalidCardDatabase,
        InvalidEnchantDatabase,
        InvalidEncounter,
        InvalidPath,
        SerializationFailed,
        DirectoryCreationFailed,
        WriteFailed,
        DeleteFailed,
        NotFound,
        ReadFailed,
        InvalidData,
        UnsupportedVersion,
        BaseProgressFailed,
        RestoreBeginFailed
    }
}
