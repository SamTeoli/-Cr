namespace HaveABreak.Cards
{
    public enum PlayerPermanentRewardSaveFailure
    {
        None,
        InvalidState,
        InvalidPath,
        SerializationFailed,
        DirectoryCreationFailed,
        WriteFailed,
        NotFound,
        ReadFailed,
        InvalidData,
        UnsupportedVersion,
        DuplicateRewardId
    }
}
