using System;
using System.IO;

namespace HaveABreak.Cards
{
    public static class RunResumeService
    {
        public static bool TryLoad(
            string activeCheckpointPath,
            string runProgressPath,
            CardDatabase cardDatabase,
            EnchantDatabase enchantDatabase,
            EncounterDatabase encounterDatabase,
            PlayerPermanentRewardState permanentRewards,
            out RunEncounterProgressState progress,
            out RunResumeSource source,
            out EncounterData encounter,
            out RunResumeFailure failure,
            out ActiveBattleCheckpointFailure checkpointFailure,
            out RunProgressSaveFailure runProgressFailure)
        {
            progress = null;
            source = RunResumeSource.None;
            encounter = null;
            failure = RunResumeFailure.None;
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
                failure = RunResumeFailure.InvalidPath;
                return false;
            }

            if (File.Exists(normalizedCheckpointPath))
            {
                if (!ActiveBattleCheckpointService.TryLoad(
                        normalizedCheckpointPath,
                        cardDatabase,
                        enchantDatabase,
                        permanentRewards,
                        encounterDatabase,
                        out progress,
                        out encounter,
                        out checkpointFailure))
                {
                    progress = null;
                    encounter = null;
                    failure = RunResumeFailure.ActiveCheckpointLoadFailed;
                    return false;
                }

                source = RunResumeSource.ActiveBattleCheckpoint;
                return true;
            }

            if (!File.Exists(normalizedRunPath))
            {
                failure = RunResumeFailure.NotFound;
                return false;
            }

            if (!RunProgressSaveService.TryLoad(
                    normalizedRunPath,
                    cardDatabase,
                    enchantDatabase,
                    permanentRewards,
                    out progress,
                    out runProgressFailure))
            {
                progress = null;
                failure = RunResumeFailure.RunProgressLoadFailed;
                return false;
            }

            source = RunResumeSource.RunProgress;
            failure = RunResumeFailure.None;
            return true;
        }

        public static bool TryLoadDefault(
            CardDatabase cardDatabase,
            EnchantDatabase enchantDatabase,
            EncounterDatabase encounterDatabase,
            PlayerPermanentRewardState permanentRewards,
            out RunEncounterProgressState progress,
            out RunResumeSource source,
            out EncounterData encounter,
            out RunResumeFailure failure,
            out ActiveBattleCheckpointFailure checkpointFailure,
            out RunProgressSaveFailure runProgressFailure)
        {
            return TryLoad(
                ActiveBattleCheckpointService.DefaultPath,
                RunProgressSaveService.DefaultPath,
                cardDatabase,
                enchantDatabase,
                encounterDatabase,
                permanentRewards,
                out progress,
                out source,
                out encounter,
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
