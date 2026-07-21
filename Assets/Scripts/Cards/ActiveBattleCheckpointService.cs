using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HaveABreak.Cards
{
    public static class ActiveBattleCheckpointService
    {
        private const int CurrentSchemaVersion = 1;
        private const string DefaultFileName =
            "active-battle-checkpoint.json";

        [Serializable]
        private sealed class CheckpointData
        {
            [SerializeField] private int schemaVersion;
            [SerializeField] private string battleInstanceId;
            [SerializeField] private string encounterId;
            [SerializeField] private int shuffleSeed;
            [SerializeField] private int maximumMana;
            [SerializeField] private List<string> startingHandRedrawIds =
                new();
            [SerializeField] private uint rewardSeed;
            [SerializeField] private string runProgressJson;

            public CheckpointData()
            {
            }

            public CheckpointData(
                BattleEncounterStartParameters parameters,
                string baseProgressJson)
            {
                schemaVersion = CurrentSchemaVersion;
                battleInstanceId = parameters.BattleInstanceId;
                encounterId = parameters.EncounterId;
                shuffleSeed = parameters.ShuffleSeed;
                maximumMana = parameters.MaximumMana;
                startingHandRedrawIds = new List<string>(
                    parameters.StartingHandRedrawIds);
                rewardSeed = parameters.RewardSeed;
                runProgressJson = baseProgressJson;
            }

            public int SchemaVersion => schemaVersion;
            public string BattleInstanceId => battleInstanceId;
            public string EncounterId => encounterId;
            public int ShuffleSeed => shuffleSeed;
            public int MaximumMana => maximumMana;
            public IReadOnlyList<string> StartingHandRedrawIds =>
                startingHandRedrawIds;
            public uint RewardSeed => rewardSeed;
            public string RunProgressJson => runProgressJson;
        }

        public static string DefaultPath => Path.Combine(
            Application.persistentDataPath,
            DefaultFileName);

        public static bool Exists(string path)
        {
            return TryNormalizePath(path, out string normalizedPath) &&
                   File.Exists(normalizedPath);
        }

        public static bool DefaultExists => Exists(DefaultPath);

        public static bool TryReadInfo(
            string path,
            out ActiveBattleCheckpointInfo info,
            out ActiveBattleCheckpointFailure failure)
        {
            info = null;
            if (!TryNormalizePath(path, out string normalizedPath))
            {
                failure = ActiveBattleCheckpointFailure.InvalidPath;
                return false;
            }

            if (!TryReadData(normalizedPath, out CheckpointData data,
                    out failure))
            {
                return false;
            }

            info = new ActiveBattleCheckpointInfo(
                data.BattleInstanceId,
                data.EncounterId,
                data.ShuffleSeed,
                data.MaximumMana,
                data.RewardSeed);
            failure = ActiveBattleCheckpointFailure.None;
            return true;
        }

        public static bool TryReadDefaultInfo(
            out ActiveBattleCheckpointInfo info,
            out ActiveBattleCheckpointFailure failure)
        {
            return TryReadInfo(DefaultPath, out info, out failure);
        }

        public static bool CanCreate(
            RunEncounterProgressState progress,
            out ActiveBattleCheckpointFailure failure)
        {
            BattleRuntimeEncounterContext context =
                progress?.ActiveEncounter;
            if (progress?.RunState == null || progress.RunDeck == null ||
                context?.Runtime == null || context.Session == null)
            {
                failure = ActiveBattleCheckpointFailure.InvalidProgress;
                return false;
            }

            if (context.StartParameters == null)
            {
                failure =
                    ActiveBattleCheckpointFailure.MissingStartParameters;
                return false;
            }

            BattleRuntimeState runtime = context.Runtime;
            bool safe = context.Session.Started &&
                        context.Session.CompletedRoundCount == 0 &&
                        context.Session.Outcome == BattleOutcome.Ongoing &&
                        runtime.Turn.Phase == BattleTurnPhase.PlayerAction &&
                        runtime.Turn.PlayerTurnNumber == 1 &&
                        runtime.CardPlay.Mana.CurrentMana ==
                        runtime.CardPlay.Mana.MaximumMana &&
                        runtime.EventLog.Events.Count == 0 &&
                        context.PendingSettlementEffects.Count == 0 &&
                        context.RunChanges.ConsumedItemIds.Count == 0 &&
                        context.RunChanges.GoldDelta == 0 &&
                        !context.Settlement.IsSettled &&
                        !context.VictoryRewards.GoldClaimed &&
                        IsInitialRuntimeState(context);
            if (!safe)
            {
                failure = ActiveBattleCheckpointFailure.UnsafeCheckpoint;
                return false;
            }

            failure = ActiveBattleCheckpointFailure.None;
            return true;
        }

        public static bool TrySerialize(
            RunEncounterProgressState progress,
            out string json,
            out ActiveBattleCheckpointFailure failure)
        {
            json = null;
            if (!CanCreate(progress, out failure))
            {
                return false;
            }

            BattleEncounterStartParameters parameters =
                progress.ActiveEncounter.StartParameters;
            if (!RunProgressSaveService.TrySerializeCheckpointBase(
                    progress,
                    parameters.BattleInstanceId,
                    out string baseProgressJson,
                    out _))
            {
                failure = ActiveBattleCheckpointFailure.BaseProgressFailed;
                return false;
            }

            try
            {
                json = JsonUtility.ToJson(
                    new CheckpointData(parameters, baseProgressJson),
                    true);
            }
            catch (Exception)
            {
                failure =
                    ActiveBattleCheckpointFailure.SerializationFailed;
                return false;
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                failure =
                    ActiveBattleCheckpointFailure.SerializationFailed;
                return false;
            }

            failure = ActiveBattleCheckpointFailure.None;
            return true;
        }

        public static bool TryDeserialize(
            string json,
            CardDatabase cardDatabase,
            EnchantDatabase enchantDatabase,
            PlayerPermanentRewardState permanentRewards,
            EncounterData encounter,
            out RunEncounterProgressState progress,
            out ActiveBattleCheckpointFailure failure)
        {
            progress = null;
            if (cardDatabase == null)
            {
                failure =
                    ActiveBattleCheckpointFailure.InvalidCardDatabase;
                return false;
            }

            if (enchantDatabase == null)
            {
                failure =
                    ActiveBattleCheckpointFailure.InvalidEnchantDatabase;
                return false;
            }

            if (encounter == null || permanentRewards == null)
            {
                failure = ActiveBattleCheckpointFailure.InvalidEncounter;
                return false;
            }

            CheckpointData data;
            try
            {
                data = string.IsNullOrWhiteSpace(json)
                    ? null
                    : JsonUtility.FromJson<CheckpointData>(json);
            }
            catch (Exception)
            {
                data = null;
            }

            if (data == null)
            {
                failure = ActiveBattleCheckpointFailure.InvalidData;
                return false;
            }

            if (data.SchemaVersion != CurrentSchemaVersion)
            {
                failure =
                    ActiveBattleCheckpointFailure.UnsupportedVersion;
                return false;
            }

            if (!ValidateData(data) || !string.Equals(
                    data.EncounterId,
                    encounter.EncounterId,
                    StringComparison.OrdinalIgnoreCase))
            {
                failure = ActiveBattleCheckpointFailure.InvalidData;
                return false;
            }

            if (!RunProgressSaveService.TryDeserialize(
                    data.RunProgressJson,
                    cardDatabase,
                    enchantDatabase,
                    permanentRewards,
                    out RunEncounterProgressState restored,
                    out _))
            {
                failure = ActiveBattleCheckpointFailure.BaseProgressFailed;
                return false;
            }

            bool begun = RunEncounterProgressService.TryBegin(
                restored,
                data.BattleInstanceId,
                encounter,
                data.ShuffleSeed,
                data.MaximumMana,
                data.StartingHandRedrawIds,
                data.RewardSeed,
                out BattleRuntimeEncounterContext context,
                out RunEncounterProgressFailure progressFailure,
                out BattleRuntimeEncounterFlowFailure flowFailure,
                out RunDeckFailure runDeckFailure,
                out BattleRuntimeBootstrapFailure bootstrapFailure,
                out BattleRuntimeSessionFailure sessionFailure,
                out StartingHandRedrawFailure redrawFailure,
                out BattleTurnFailure turnFailure,
                out List<string> validationErrors);
            if (!begun || context == null ||
                progressFailure != RunEncounterProgressFailure.None ||
                flowFailure != BattleRuntimeEncounterFlowFailure.None ||
                runDeckFailure != RunDeckFailure.None ||
                bootstrapFailure != BattleRuntimeBootstrapFailure.None ||
                sessionFailure != BattleRuntimeSessionFailure.None ||
                redrawFailure != StartingHandRedrawFailure.None ||
                turnFailure != BattleTurnFailure.None ||
                validationErrors.Count != 0 ||
                !CanCreate(restored, out _))
            {
                failure = ActiveBattleCheckpointFailure.RestoreBeginFailed;
                return false;
            }

            progress = restored;
            failure = ActiveBattleCheckpointFailure.None;
            return true;
        }

        public static bool TrySave(
            RunEncounterProgressState progress,
            string path,
            out ActiveBattleCheckpointFailure failure)
        {
            if (!TryNormalizePath(path, out string normalizedPath))
            {
                failure = ActiveBattleCheckpointFailure.InvalidPath;
                return false;
            }

            if (!TrySerialize(progress, out string json, out failure))
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
                failure =
                    ActiveBattleCheckpointFailure.DirectoryCreationFailed;
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
                failure = ActiveBattleCheckpointFailure.WriteFailed;
                return false;
            }

            failure = ActiveBattleCheckpointFailure.None;
            return true;
        }

        public static bool TrySaveDefault(
            RunEncounterProgressState progress,
            out string path,
            out ActiveBattleCheckpointFailure failure)
        {
            path = DefaultPath;
            return TrySave(progress, path, out failure);
        }

        public static bool TryLoad(
            string path,
            CardDatabase cardDatabase,
            EnchantDatabase enchantDatabase,
            PlayerPermanentRewardState permanentRewards,
            EncounterData encounter,
            out RunEncounterProgressState progress,
            out ActiveBattleCheckpointFailure failure)
        {
            progress = null;
            if (!TryNormalizePath(path, out string normalizedPath))
            {
                failure = ActiveBattleCheckpointFailure.InvalidPath;
                return false;
            }

            if (!File.Exists(normalizedPath))
            {
                failure = ActiveBattleCheckpointFailure.NotFound;
                return false;
            }

            string json;
            try
            {
                json = File.ReadAllText(normalizedPath);
            }
            catch (Exception)
            {
                failure = ActiveBattleCheckpointFailure.ReadFailed;
                return false;
            }

            return TryDeserialize(
                json,
                cardDatabase,
                enchantDatabase,
                permanentRewards,
                encounter,
                out progress,
                out failure);
        }

        public static bool TryLoad(
            string path,
            CardDatabase cardDatabase,
            EnchantDatabase enchantDatabase,
            PlayerPermanentRewardState permanentRewards,
            EncounterDatabase encounterDatabase,
            out RunEncounterProgressState progress,
            out EncounterData encounter,
            out ActiveBattleCheckpointFailure failure)
        {
            progress = null;
            encounter = null;
            if (encounterDatabase == null ||
                encounterDatabase.GetValidationErrors().Count != 0)
            {
                failure =
                    ActiveBattleCheckpointFailure.InvalidEncounterDatabase;
                return false;
            }

            if (!TryReadInfo(
                    path,
                    out ActiveBattleCheckpointInfo info,
                    out failure))
            {
                return false;
            }

            if (!encounterDatabase.TryGetEncounter(
                    info.EncounterId,
                    out encounter))
            {
                failure = ActiveBattleCheckpointFailure.EncounterNotFound;
                return false;
            }

            if (!TryLoad(
                    path,
                    cardDatabase,
                    enchantDatabase,
                    permanentRewards,
                    encounter,
                    out progress,
                    out failure))
            {
                encounter = null;
                return false;
            }

            return true;
        }

        public static bool TryLoadDefault(
            CardDatabase cardDatabase,
            EnchantDatabase enchantDatabase,
            PlayerPermanentRewardState permanentRewards,
            EncounterData encounter,
            out RunEncounterProgressState progress,
            out string path,
            out ActiveBattleCheckpointFailure failure)
        {
            path = DefaultPath;
            return TryLoad(
                path,
                cardDatabase,
                enchantDatabase,
                permanentRewards,
                encounter,
                out progress,
                out failure);
        }

        public static bool TryLoadDefault(
            CardDatabase cardDatabase,
            EnchantDatabase enchantDatabase,
            PlayerPermanentRewardState permanentRewards,
            EncounterDatabase encounterDatabase,
            out RunEncounterProgressState progress,
            out EncounterData encounter,
            out string path,
            out ActiveBattleCheckpointFailure failure)
        {
            path = DefaultPath;
            return TryLoad(
                path,
                cardDatabase,
                enchantDatabase,
                permanentRewards,
                encounterDatabase,
                out progress,
                out encounter,
                out failure);
        }

        public static bool TryClear(
            string path,
            out ActiveBattleCheckpointFailure failure)
        {
            if (!TryNormalizePath(path, out string normalizedPath))
            {
                failure = ActiveBattleCheckpointFailure.InvalidPath;
                return false;
            }

            try
            {
                DeleteIfPresent(normalizedPath);
                DeleteIfPresent(normalizedPath + ".tmp");
                DeleteIfPresent(normalizedPath + ".bak");
            }
            catch (Exception)
            {
                failure = ActiveBattleCheckpointFailure.DeleteFailed;
                return false;
            }

            failure = ActiveBattleCheckpointFailure.None;
            return true;
        }

        public static bool TryClearDefault(
            out ActiveBattleCheckpointFailure failure)
        {
            return TryClear(DefaultPath, out failure);
        }

        private static bool IsInitialRuntimeState(
            BattleRuntimeEncounterContext context)
        {
            BattleRuntimeState runtime = context.Runtime;
            BattleCardZoneState zones = runtime.Deck.Zones;
            if (runtime.Player.CurrentHealth != context.RunState.CurrentHealth ||
                runtime.Monsters.Monsters.Count != 0 ||
                runtime.TrapInstallations.Count != 0 ||
                zones.Count(CardZone.MonsterField) != 0 ||
                zones.Count(CardZone.SkillField) != 0 ||
                zones.Count(CardZone.Graveyard) != 0 ||
                zones.Count(CardZone.Banished) != 0 ||
                zones.Count(CardZone.RedrawHolding) != 0 ||
                zones.Count(CardZone.Hand) +
                zones.Count(CardZone.DrawPile) != zones.Cards.Count ||
                context.Encounter?.EnemySlots == null ||
                runtime.LivingEnemies.Count !=
                context.Encounter.EnemySlots.Count)
            {
                return false;
            }

            foreach (EncounterEnemySlot slot in context.Encounter.EnemySlots)
            {
                BattleEnemyRuntimeState enemy = runtime.FindEnemy(
                    slot.EnemyInstanceId);
                if (enemy == null || slot.Enemy == null ||
                    enemy.Attack != slot.Enemy.Attack ||
                    enemy.Vital.CurrentHealth != slot.Enemy.MaximumHealth ||
                    runtime.EnemyPositions.FindPosition(enemy.EnemyId) !=
                    slot.Position)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateData(CheckpointData data)
        {
            if (string.IsNullOrWhiteSpace(data.BattleInstanceId) ||
                string.IsNullOrWhiteSpace(data.EncounterId) ||
                data.MaximumMana < 0 ||
                data.MaximumMana > BattleManaState.MaximumManaLimit ||
                data.StartingHandRedrawIds == null ||
                string.IsNullOrWhiteSpace(data.RunProgressJson))
            {
                return false;
            }

            HashSet<string> unique = new(StringComparer.OrdinalIgnoreCase);
            foreach (string cardId in data.StartingHandRedrawIds)
            {
                if (string.IsNullOrWhiteSpace(cardId) ||
                    !unique.Add(cardId.Trim()))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryReadData(
            string normalizedPath,
            out CheckpointData data,
            out ActiveBattleCheckpointFailure failure)
        {
            data = null;
            if (!File.Exists(normalizedPath))
            {
                failure = ActiveBattleCheckpointFailure.NotFound;
                return false;
            }

            string json;
            try
            {
                json = File.ReadAllText(normalizedPath);
            }
            catch (Exception)
            {
                failure = ActiveBattleCheckpointFailure.ReadFailed;
                return false;
            }

            try
            {
                data = string.IsNullOrWhiteSpace(json)
                    ? null
                    : JsonUtility.FromJson<CheckpointData>(json);
            }
            catch (Exception)
            {
                data = null;
            }

            if (data == null || !ValidateData(data))
            {
                failure = ActiveBattleCheckpointFailure.InvalidData;
                return false;
            }

            if (data.SchemaVersion != CurrentSchemaVersion)
            {
                failure = ActiveBattleCheckpointFailure.UnsupportedVersion;
                return false;
            }

            failure = ActiveBattleCheckpointFailure.None;
            return true;
        }

        private static void DeleteIfPresent(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
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
