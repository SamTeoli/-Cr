using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    // Shared contracts for the prototype save, resume, and checkpoint path.
    // These plain C# types share one file because Unity asset GUIDs are not
    // used to serialize them independently.
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

    public sealed class ActiveBattleCheckpointInfo
    {
        public ActiveBattleCheckpointInfo(
            string battleInstanceId,
            string encounterId,
            int shuffleSeed,
            int maximumMana,
            uint rewardSeed)
        {
            BattleInstanceId = battleInstanceId;
            EncounterId = encounterId;
            ShuffleSeed = shuffleSeed;
            MaximumMana = maximumMana;
            RewardSeed = rewardSeed;
        }

        public string BattleInstanceId { get; }
        public string EncounterId { get; }
        public int ShuffleSeed { get; }
        public int MaximumMana { get; }
        public uint RewardSeed { get; }
    }

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

    [Serializable]
    public sealed class PlayerPermanentRewardState
    {
        [SerializeField] private List<string> rewardIds = new();

        public IReadOnlyList<string> RewardIds =>
            rewardIds ??= new List<string>();

        public bool Contains(string rewardId)
        {
            if (string.IsNullOrWhiteSpace(rewardId))
            {
                return false;
            }

            string normalized = rewardId.Trim();
            rewardIds ??= new List<string>();
            return rewardIds.Exists(id => string.Equals(
                id,
                normalized,
                StringComparison.OrdinalIgnoreCase));
        }

        internal bool TryAdd(string rewardId)
        {
            if (string.IsNullOrWhiteSpace(rewardId) || Contains(rewardId))
            {
                return false;
            }

            rewardIds ??= new List<string>();
            rewardIds.Add(rewardId.Trim());
            return true;
        }
    }

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

    [Serializable]
    public sealed class RunEncounterProgressState
    {
        [SerializeField] private RunBattleState runState;
        [SerializeField] private RunDeckState runDeck;
        [SerializeField] private PlayerPermanentRewardState permanentRewards;
        [SerializeField] private List<string> usedBattleInstanceIds = new();
        [SerializeField] private int completedEncounterCount;
        [NonSerialized] private BattleRuntimeEncounterContext activeEncounter;

        private RunEncounterProgressState()
        {
        }

        public RunEncounterProgressState(
            RunBattleState runState,
            RunDeckState runDeck)
            : this(runState, runDeck, new PlayerPermanentRewardState())
        {
        }

        public RunEncounterProgressState(
            RunBattleState runState,
            RunDeckState runDeck,
            PlayerPermanentRewardState permanentRewards)
            : this(
                runState,
                runDeck,
                permanentRewards,
                Array.Empty<string>(),
                0)
        {
        }

        public RunEncounterProgressState(
            RunBattleState runState,
            RunDeckState runDeck,
            PlayerPermanentRewardState permanentRewards,
            IEnumerable<string> completedBattleInstanceIds,
            int completedEncounterCount)
        {
            this.runState = runState ??
                throw new ArgumentNullException(nameof(runState));
            this.runDeck = runDeck ??
                throw new ArgumentNullException(nameof(runDeck));
            this.permanentRewards = permanentRewards ??
                throw new ArgumentNullException(nameof(permanentRewards));
            if (completedEncounterCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(completedEncounterCount));
            }

            if (completedBattleInstanceIds == null)
            {
                throw new ArgumentNullException(
                    nameof(completedBattleInstanceIds));
            }

            foreach (string battleInstanceId in completedBattleInstanceIds)
            {
                if (string.IsNullOrWhiteSpace(battleInstanceId) ||
                    HasUsedBattleInstanceId(battleInstanceId))
                {
                    throw new ArgumentException(
                        "Completed battle instance IDs must be non-empty and unique.",
                        nameof(completedBattleInstanceIds));
                }

                usedBattleInstanceIds.Add(battleInstanceId.Trim());
            }

            if (usedBattleInstanceIds.Count != completedEncounterCount)
            {
                throw new ArgumentException(
                    "Completed encounter count must match completed battle IDs.",
                    nameof(completedEncounterCount));
            }

            this.completedEncounterCount = completedEncounterCount;
        }

        public RunBattleState RunState => runState;
        public RunDeckState RunDeck => runDeck;
        public PlayerPermanentRewardState PermanentRewards =>
            permanentRewards ??= new PlayerPermanentRewardState();
        public BattleRuntimeEncounterContext ActiveEncounter =>
            activeEncounter;
        public bool HasActiveEncounter => activeEncounter != null;
        public int CompletedEncounterCount => completedEncounterCount;
        public IReadOnlyList<string> UsedBattleInstanceIds =>
            usedBattleInstanceIds ??= new List<string>();

        internal bool HasUsedBattleInstanceId(string battleInstanceId)
        {
            if (string.IsNullOrWhiteSpace(battleInstanceId))
            {
                return false;
            }

            usedBattleInstanceIds ??= new List<string>();
            return usedBattleInstanceIds.Exists(id => string.Equals(
                id,
                battleInstanceId.Trim(),
                StringComparison.OrdinalIgnoreCase));
        }

        internal void SetActiveEncounter(
            string battleInstanceId,
            BattleRuntimeEncounterContext context)
        {
            activeEncounter = context ??
                throw new ArgumentNullException(nameof(context));
            usedBattleInstanceIds ??= new List<string>();
            usedBattleInstanceIds.Add(battleInstanceId.Trim());
        }

        internal void CompleteActiveEncounter()
        {
            activeEncounter = null;
            completedEncounterCount++;
        }
    }

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

    public enum RunResumeFailure
    {
        None,
        InvalidPath,
        NotFound,
        ActiveCheckpointLoadFailed,
        RunProgressLoadFailed
    }

    public enum RunResumeSource
    {
        None,
        RunProgress,
        ActiveBattleCheckpoint
    }

    public enum RunSaveDestination
    {
        None,
        RunProgress,
        ActiveBattleCheckpoint
    }

    public enum RunSaveFailure
    {
        None,
        InvalidPath,
        ActiveCheckpointSaveFailed,
        RunProgressSaveFailed,
        CheckpointClearFailed
    }

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

    public enum RunSaveSlotInspectionFailure
    {
        None,
        InvalidPath
    }

    public enum RunSaveSlotState
    {
        Empty,
        RunProgress,
        ActiveBattleCheckpoint,
        InvalidRunProgress,
        InvalidActiveBattleCheckpoint
    }
}
