using System;
using System.IO;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class PlayerPermanentRewardSaveServiceValidation
    {
        [MenuItem("Have a Break/Validate Permanent Reward Save Load")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            if (valid)
            {
                Debug.Log("Permanent reward save and load passed.");
            }
            else
            {
                Debug.LogError("Permanent reward save and load failed.");
            }

            EditorUtility.DisplayDialog(
                "Permanent Reward Save Load Validation",
                valid
                    ? "Permanent reward save and load passed."
                    : "Permanent reward save and load failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            if (string.IsNullOrWhiteSpace(
                    PlayerPermanentRewardSaveService.DefaultPath) ||
                !ValidateSerializationFailures())
            {
                return false;
            }

            string directory = Path.Combine(
                Path.GetTempPath(),
                $"HaveABreak-PermanentReward-{Guid.NewGuid():N}");
            string path = Path.Combine(directory, "save.json");
            try
            {
                return ValidateFileRoundTrip(path);
            }
            finally
            {
                TryDeleteDirectory(directory);
            }
        }

        private static bool ValidateSerializationFailures()
        {
            bool nullSerialized =
                PlayerPermanentRewardSaveService.TrySerialize(
                    null,
                    out string nullJson,
                    out PlayerPermanentRewardSaveFailure nullFailure);
            bool corruptLoaded =
                PlayerPermanentRewardSaveService.TryDeserialize(
                    "{",
                    out PlayerPermanentRewardState corruptState,
                    out PlayerPermanentRewardSaveFailure corruptFailure);
            bool unsupportedLoaded =
                PlayerPermanentRewardSaveService.TryDeserialize(
                    "{\"schemaVersion\":2,\"rewardIds\":[]}",
                    out PlayerPermanentRewardState unsupportedState,
                    out PlayerPermanentRewardSaveFailure unsupportedFailure);
            bool duplicateLoaded =
                PlayerPermanentRewardSaveService.TryDeserialize(
                    "{\"schemaVersion\":1,\"rewardIds\":[\"R-A\",\"r-a\"]}",
                    out PlayerPermanentRewardState duplicateState,
                    out PlayerPermanentRewardSaveFailure duplicateFailure);
            bool blankLoaded =
                PlayerPermanentRewardSaveService.TryDeserialize(
                    "{\"schemaVersion\":1,\"rewardIds\":[\" \"]}",
                    out PlayerPermanentRewardState blankState,
                    out PlayerPermanentRewardSaveFailure blankFailure);

            return !nullSerialized && nullJson == null &&
                   nullFailure ==
                   PlayerPermanentRewardSaveFailure.InvalidState &&
                   !corruptLoaded && corruptState == null &&
                   corruptFailure ==
                   PlayerPermanentRewardSaveFailure.InvalidData &&
                   !unsupportedLoaded && unsupportedState == null &&
                   unsupportedFailure ==
                   PlayerPermanentRewardSaveFailure.UnsupportedVersion &&
                   !duplicateLoaded && duplicateState == null &&
                   duplicateFailure ==
                   PlayerPermanentRewardSaveFailure.DuplicateRewardId &&
                   !blankLoaded && blankState == null &&
                   blankFailure ==
                   PlayerPermanentRewardSaveFailure.InvalidData;
        }

        private static bool ValidateFileRoundTrip(string path)
        {
            bool missingLoaded = PlayerPermanentRewardSaveService.TryLoad(
                path,
                out PlayerPermanentRewardState missingState,
                out PlayerPermanentRewardSaveFailure missingFailure);
            if (missingLoaded || missingState != null ||
                missingFailure !=
                PlayerPermanentRewardSaveFailure.NotFound ||
                !TryCreateState(
                    new[] { "TEST-PERMANENT-50", "TEST-PERMANENT-51" },
                    out PlayerPermanentRewardState source))
            {
                return false;
            }

            bool saved = PlayerPermanentRewardSaveService.TrySave(
                source,
                path,
                out PlayerPermanentRewardSaveFailure saveFailure);
            bool loaded = PlayerPermanentRewardSaveService.TryLoad(
                path,
                out PlayerPermanentRewardState restored,
                out PlayerPermanentRewardSaveFailure loadFailure);
            if (!saved ||
                saveFailure != PlayerPermanentRewardSaveFailure.None ||
                !loaded || restored == null ||
                loadFailure != PlayerPermanentRewardSaveFailure.None ||
                restored.RewardIds.Count != 2 ||
                restored.RewardIds[0] != "TEST-PERMANENT-50" ||
                restored.RewardIds[1] != "TEST-PERMANENT-51" ||
                File.Exists(path + ".tmp") || File.Exists(path + ".bak"))
            {
                return false;
            }

            RunEncounterProgressState nextRun = new(
                new RunBattleState(30, 30, 0),
                new RunDeckState(),
                restored);
            if (nextRun.PermanentRewards != restored ||
                !nextRun.PermanentRewards.Contains("TEST-PERMANENT-50") ||
                !nextRun.PermanentRewards.Contains("TEST-PERMANENT-51"))
            {
                return false;
            }

            if (!TryCreateState(
                    new[] { "TEST-PERMANENT-52" },
                    out PlayerPermanentRewardState replacement) ||
                !PlayerPermanentRewardSaveService.TrySave(
                    replacement,
                    path,
                    out PlayerPermanentRewardSaveFailure replaceFailure) ||
                replaceFailure != PlayerPermanentRewardSaveFailure.None ||
                !PlayerPermanentRewardSaveService.TryLoad(
                    path,
                    out PlayerPermanentRewardState replaced,
                    out PlayerPermanentRewardSaveFailure reloadFailure))
            {
                return false;
            }

            return reloadFailure == PlayerPermanentRewardSaveFailure.None &&
                   replaced != null && replaced.RewardIds.Count == 1 &&
                   replaced.RewardIds[0] == "TEST-PERMANENT-52" &&
                   !File.Exists(path + ".tmp") &&
                   !File.Exists(path + ".bak");
        }

        private static bool TryCreateState(
            string[] rewardIds,
            out PlayerPermanentRewardState state)
        {
            string quotedIds = string.Join(
                ",",
                Array.ConvertAll(rewardIds, id => $"\"{id}\""));
            return PlayerPermanentRewardSaveService.TryDeserialize(
                $"{{\"schemaVersion\":1,\"rewardIds\":[{quotedIds}]}}",
                out state,
                out PlayerPermanentRewardSaveFailure failure) &&
                   failure == PlayerPermanentRewardSaveFailure.None;
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Could not delete validation directory: {exception.Message}");
            }
        }
    }
}
