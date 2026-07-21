namespace HaveABreak.Cards
{
    public sealed class RunSaveSlotInfo
    {
        internal RunSaveSlotInfo(
            RunSaveSlotState state,
            RunResumeSource source,
            RunEncounterProgressState progress,
            EncounterData encounter,
            ActiveBattleCheckpointInfo checkpointInfo,
            RunResumeFailure resumeFailure,
            ActiveBattleCheckpointFailure checkpointFailure,
            RunProgressSaveFailure runProgressFailure)
        {
            State = state;
            Source = source;
            CanResume = state == RunSaveSlotState.RunProgress ||
                        state ==
                        RunSaveSlotState.ActiveBattleCheckpoint;
            MaximumHealth = progress?.RunState?.MaximumHealth ?? 0;
            CurrentHealth = progress?.RunState?.CurrentHealth ?? 0;
            Gold = progress?.RunState?.Gold ?? 0;
            CompletedEncounterCount =
                progress?.CompletedEncounterCount ?? 0;
            BattleInstanceId = progress?.ActiveEncounter?.StartParameters
                ?.BattleInstanceId ?? checkpointInfo?.BattleInstanceId;
            EncounterId = encounter?.EncounterId ??
                progress?.ActiveEncounter?.StartParameters?.EncounterId ??
                checkpointInfo?.EncounterId;
            ResumeFailure = resumeFailure;
            CheckpointFailure = checkpointFailure;
            RunProgressFailure = runProgressFailure;
        }

        public RunSaveSlotState State { get; }
        public bool CanResume { get; }
        public RunResumeSource Source { get; }
        public int MaximumHealth { get; }
        public int CurrentHealth { get; }
        public int Gold { get; }
        public int CompletedEncounterCount { get; }
        public string BattleInstanceId { get; }
        public string EncounterId { get; }
        public RunResumeFailure ResumeFailure { get; }
        public ActiveBattleCheckpointFailure CheckpointFailure { get; }
        public RunProgressSaveFailure RunProgressFailure { get; }
    }
}
