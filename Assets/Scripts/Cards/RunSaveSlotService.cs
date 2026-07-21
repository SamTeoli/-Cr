using System;
using System.IO;

namespace HaveABreak.Cards
{
    public static class RunSaveSlotService
    {
        public static bool TryInspect(
            string activeCheckpointPath,
            string runProgressPath,
            CardDatabase cardDatabase,
            EnchantDatabase enchantDatabase,
            EncounterDatabase encounterDatabase,
            PlayerPermanentRewardState permanentRewards,
            out RunSaveSlotInfo info,
            out RunSaveSlotInspectionFailure failure)
        {
            info = null;
            if (!TryNormalizePath(
                    activeCheckpointPath,
                    out string normalizedCheckpointPath) ||
                !TryNormalizePath(
                    runProgressPath,
                    out string normalizedRunPath) ||
                string.Equals(
                    normalizedCheckpointPath,
                    normalizedRunPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                failure = RunSaveSlotInspectionFailure.InvalidPath;
                return false;
            }

            bool hasCheckpoint = File.Exists(normalizedCheckpointPath);
            bool hasRunProgress = File.Exists(normalizedRunPath);
            if (!hasCheckpoint && !hasRunProgress)
            {
                info = CreateInfo(
                    RunSaveSlotState.Empty,
                    RunResumeSource.None,
                    null,
                    null,
                    null,
                    RunResumeFailure.None,
                    ActiveBattleCheckpointFailure.None,
                    RunProgressSaveFailure.None);
                failure = RunSaveSlotInspectionFailure.None;
                return true;
            }

            ActiveBattleCheckpointInfo checkpointInfo = null;
            if (hasCheckpoint)
            {
                ActiveBattleCheckpointService.TryReadInfo(
                    normalizedCheckpointPath,
                    out checkpointInfo,
                    out _);
            }

            bool loaded = RunResumeService.TryLoad(
                normalizedCheckpointPath,
                normalizedRunPath,
                cardDatabase,
                enchantDatabase,
                encounterDatabase,
                permanentRewards,
                out RunEncounterProgressState progress,
                out RunResumeSource source,
                out EncounterData encounter,
                out RunResumeFailure resumeFailure,
                out ActiveBattleCheckpointFailure checkpointFailure,
                out RunProgressSaveFailure runProgressFailure);
            RunSaveSlotState state = loaded
                ? source == RunResumeSource.ActiveBattleCheckpoint
                    ? RunSaveSlotState.ActiveBattleCheckpoint
                    : RunSaveSlotState.RunProgress
                : hasCheckpoint
                    ? RunSaveSlotState.InvalidActiveBattleCheckpoint
                    : RunSaveSlotState.InvalidRunProgress;
            info = CreateInfo(
                state,
                source,
                progress,
                encounter,
                checkpointInfo,
                resumeFailure,
                checkpointFailure,
                runProgressFailure);
            failure = RunSaveSlotInspectionFailure.None;
            return true;
        }

        public static bool TryInspectDefault(
            CardDatabase cardDatabase,
            EnchantDatabase enchantDatabase,
            EncounterDatabase encounterDatabase,
            PlayerPermanentRewardState permanentRewards,
            out RunSaveSlotInfo info,
            out RunSaveSlotInspectionFailure failure)
        {
            return TryInspect(
                ActiveBattleCheckpointService.DefaultPath,
                RunProgressSaveService.DefaultPath,
                cardDatabase,
                enchantDatabase,
                encounterDatabase,
                permanentRewards,
                out info,
                out failure);
        }

        private static RunSaveSlotInfo CreateInfo(
            RunSaveSlotState state,
            RunResumeSource source,
            RunEncounterProgressState progress,
            EncounterData encounter,
            ActiveBattleCheckpointInfo checkpointInfo,
            RunResumeFailure resumeFailure,
            ActiveBattleCheckpointFailure checkpointFailure,
            RunProgressSaveFailure runProgressFailure)
        {
            return new RunSaveSlotInfo(
                state,
                source,
                progress,
                encounter,
                checkpointInfo,
                resumeFailure,
                checkpointFailure,
                runProgressFailure);
        }

        private static bool TryNormalizePath(
            string path,
            out string normalizedPath)
        {
            normalizedPath = null;
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            try
            {
                normalizedPath = Path.GetFullPath(path.Trim());
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
