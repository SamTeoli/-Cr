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
        InvalidEncounterDatabase,
        InvalidEncounter,
        EncounterNotFound,
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
