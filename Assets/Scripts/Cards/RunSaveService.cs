using System;
using System.IO;

namespace HaveABreak.Cards
{
    public static class RunSaveService
    {
        public static bool TrySave(
            RunEncounterProgressState progress,
            string activeCheckpointPath,
            string runProgressPath,
            out RunSaveDestination destination,
            out RunSaveFailure failure,
            out ActiveBattleCheckpointFailure checkpointFailure,
            out RunProgressSaveFailure runProgressFailure)
        {
            destination = RunSaveDestination.None;
            failure = RunSaveFailure.None;
            checkpointFailure = ActiveBattleCheckpointFailure.None;
            runProgressFailure = RunProgressSaveFailure.None;
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
                failure = RunSaveFailure.InvalidPath;
                return false;
            }

            if (progress?.HasActiveEncounter == true)
            {
                if (!ActiveBattleCheckpointService.TrySave(
                        progress,
                        normalizedCheckpointPath,
                        out checkpointFailure))
                {
                    failure = RunSaveFailure.ActiveCheckpointSaveFailed;
                    return false;
                }

                destination = RunSaveDestination.ActiveBattleCheckpoint;
                return true;
            }

            if (!RunProgressSaveService.TrySave(
                    progress,
                    normalizedRunPath,
                    out runProgressFailure))
            {
                failure = RunSaveFailure.RunProgressSaveFailed;
                return false;
            }

            destination = RunSaveDestination.RunProgress;
            if (!ActiveBattleCheckpointService.TryClear(
                    normalizedCheckpointPath,
                    out checkpointFailure))
            {
                failure = RunSaveFailure.CheckpointClearFailed;
                return false;
            }

            return true;
        }

        public static bool TrySaveDefault(
            RunEncounterProgressState progress,
            out RunSaveDestination destination,
            out RunSaveFailure failure,
            out ActiveBattleCheckpointFailure checkpointFailure,
            out RunProgressSaveFailure runProgressFailure)
        {
            return TrySave(
                progress,
                ActiveBattleCheckpointService.DefaultPath,
                RunProgressSaveService.DefaultPath,
                out destination,
                out failure,
                out checkpointFailure,
                out runProgressFailure);
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
