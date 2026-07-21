using System;
using System.IO;

namespace HaveABreak.Cards
{
    public static class ActiveBattleCheckpointLifecycleService
    {
        public static bool TryCompleteAndClear(
            RunEncounterProgressState progress,
            string checkpointPath,
            out RunEncounterProgressFailure progressFailure,
            out ActiveBattleCheckpointFailure checkpointFailure)
        {
            progressFailure = RunEncounterProgressFailure.None;
            checkpointFailure = ActiveBattleCheckpointFailure.None;
            BattleRuntimeEncounterContext context = progress?.ActiveEncounter;
            BattleEncounterStartParameters parameters =
                context?.StartParameters;
            if (parameters == null)
            {
                progressFailure = RunEncounterProgressFailure.InvalidState;
                checkpointFailure =
                    ActiveBattleCheckpointFailure.InvalidProgress;
                return false;
            }

            if (!IsValidPath(checkpointPath))
            {
                checkpointFailure =
                    ActiveBattleCheckpointFailure.InvalidPath;
                return false;
            }

            if (ActiveBattleCheckpointService.Exists(checkpointPath))
            {
                if (!ActiveBattleCheckpointService.TryReadInfo(
                        checkpointPath,
                        out ActiveBattleCheckpointInfo info,
                        out checkpointFailure) ||
                    !string.Equals(
                        info.BattleInstanceId,
                        parameters.BattleInstanceId,
                        StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(
                        info.EncounterId,
                        parameters.EncounterId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (checkpointFailure ==
                        ActiveBattleCheckpointFailure.None)
                    {
                        checkpointFailure =
                            ActiveBattleCheckpointFailure.CheckpointMismatch;
                    }

                    return false;
                }
            }

            if (!RunEncounterProgressService.TryCompleteActive(
                    progress,
                    out progressFailure))
            {
                return false;
            }

            return ActiveBattleCheckpointService.TryClear(
                checkpointPath,
                out checkpointFailure);
        }

        public static bool TryCompleteAndClearDefault(
            RunEncounterProgressState progress,
            out RunEncounterProgressFailure progressFailure,
            out ActiveBattleCheckpointFailure checkpointFailure)
        {
            return TryCompleteAndClear(
                progress,
                ActiveBattleCheckpointService.DefaultPath,
                out progressFailure,
                out checkpointFailure);
        }

        private static bool IsValidPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            try
            {
                Path.GetFullPath(path.Trim());
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
