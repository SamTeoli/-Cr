using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HaveABreak.Cards
{
    public static class PlayerPermanentRewardSaveService
    {
        private const int CurrentSchemaVersion = 1;
        private const string DefaultFileName =
            "player-permanent-rewards.json";

        [Serializable]
        private sealed class SaveData
        {
            [SerializeField] private int schemaVersion;
            [SerializeField] private List<string> rewardIds = new();

            public SaveData()
            {
            }

            public SaveData(IEnumerable<string> ids)
            {
                schemaVersion = CurrentSchemaVersion;
                rewardIds = ids == null
                    ? new List<string>()
                    : new List<string>(ids);
            }

            public int SchemaVersion => schemaVersion;
            public IReadOnlyList<string> RewardIds => rewardIds;
        }

        public static string DefaultPath => Path.Combine(
            Application.persistentDataPath,
            DefaultFileName);

        public static bool TrySerialize(
            PlayerPermanentRewardState state,
            out string json,
            out PlayerPermanentRewardSaveFailure failure)
        {
            json = null;
            if (state == null)
            {
                failure = PlayerPermanentRewardSaveFailure.InvalidState;
                return false;
            }

            try
            {
                json = JsonUtility.ToJson(
                    new SaveData(state.RewardIds),
                    true);
            }
            catch (Exception)
            {
                failure =
                    PlayerPermanentRewardSaveFailure.SerializationFailed;
                return false;
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                failure =
                    PlayerPermanentRewardSaveFailure.SerializationFailed;
                return false;
            }

            failure = PlayerPermanentRewardSaveFailure.None;
            return true;
        }

        public static bool TryDeserialize(
            string json,
            out PlayerPermanentRewardState state,
            out PlayerPermanentRewardSaveFailure failure)
        {
            state = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                failure = PlayerPermanentRewardSaveFailure.InvalidData;
                return false;
            }

            SaveData data;
            try
            {
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception)
            {
                failure = PlayerPermanentRewardSaveFailure.InvalidData;
                return false;
            }

            if (data == null)
            {
                failure = PlayerPermanentRewardSaveFailure.InvalidData;
                return false;
            }

            if (data.SchemaVersion != CurrentSchemaVersion)
            {
                failure =
                    PlayerPermanentRewardSaveFailure.UnsupportedVersion;
                return false;
            }

            if (data.RewardIds == null)
            {
                failure = PlayerPermanentRewardSaveFailure.InvalidData;
                return false;
            }

            PlayerPermanentRewardState restored = new();
            foreach (string rewardId in data.RewardIds)
            {
                if (string.IsNullOrWhiteSpace(rewardId))
                {
                    failure = PlayerPermanentRewardSaveFailure.InvalidData;
                    return false;
                }

                if (!restored.TryAdd(rewardId))
                {
                    failure =
                        PlayerPermanentRewardSaveFailure.DuplicateRewardId;
                    return false;
                }
            }

            state = restored;
            failure = PlayerPermanentRewardSaveFailure.None;
            return true;
        }

        public static bool TrySaveDefault(
            PlayerPermanentRewardState state,
            out string path,
            out PlayerPermanentRewardSaveFailure failure)
        {
            path = DefaultPath;
            return TrySave(state, path, out failure);
        }

        public static bool TryLoadDefault(
            out PlayerPermanentRewardState state,
            out string path,
            out PlayerPermanentRewardSaveFailure failure)
        {
            path = DefaultPath;
            return TryLoad(path, out state, out failure);
        }

        public static bool TrySave(
            PlayerPermanentRewardState state,
            string path,
            out PlayerPermanentRewardSaveFailure failure)
        {
            if (!TryNormalizePath(path, out string normalizedPath))
            {
                failure = PlayerPermanentRewardSaveFailure.InvalidPath;
                return false;
            }

            if (!TrySerialize(state, out string json, out failure))
            {
                return false;
            }

            string directory = Path.GetDirectoryName(normalizedPath);
            try
            {
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            catch (Exception)
            {
                failure = PlayerPermanentRewardSaveFailure
                    .DirectoryCreationFailed;
                return false;
            }

            string temporaryPath = normalizedPath + ".tmp";
            string backupPath = normalizedPath + ".bak";
            try
            {
                File.WriteAllText(temporaryPath, json);
                if (File.Exists(normalizedPath))
                {
                    if (File.Exists(backupPath))
                    {
                        File.Delete(backupPath);
                    }

                    File.Replace(
                        temporaryPath,
                        normalizedPath,
                        backupPath);
                    TryDeleteFile(backupPath);
                }
                else
                {
                    File.Move(temporaryPath, normalizedPath);
                }
            }
            catch (Exception)
            {
                TryDeleteFile(temporaryPath);
                failure = PlayerPermanentRewardSaveFailure.WriteFailed;
                return false;
            }

            failure = PlayerPermanentRewardSaveFailure.None;
            return true;
        }

        public static bool TryLoad(
            string path,
            out PlayerPermanentRewardState state,
            out PlayerPermanentRewardSaveFailure failure)
        {
            state = null;
            if (!TryNormalizePath(path, out string normalizedPath))
            {
                failure = PlayerPermanentRewardSaveFailure.InvalidPath;
                return false;
            }

            if (!File.Exists(normalizedPath))
            {
                failure = PlayerPermanentRewardSaveFailure.NotFound;
                return false;
            }

            string json;
            try
            {
                json = File.ReadAllText(normalizedPath);
            }
            catch (Exception)
            {
                failure = PlayerPermanentRewardSaveFailure.ReadFailed;
                return false;
            }

            return TryDeserialize(json, out state, out failure);
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

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
