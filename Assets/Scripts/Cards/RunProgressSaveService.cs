using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HaveABreak.Cards
{
    public static class RunProgressSaveService
    {
        private const int CurrentSchemaVersion = 2;
        private const string DefaultFileName = "run-progress.json";

        [Serializable]
        private sealed class SaveData
        {
            [SerializeField] private int schemaVersion;
            [SerializeField] private int maximumHealth;
            [SerializeField] private int currentHealth;
            [SerializeField] private int gold;
            [SerializeField] private List<string> consumableItemIds = new();
            [SerializeField] private bool runEnded;
            [SerializeField] private int completedEncounterCount;
            [SerializeField] private List<string> completedBattleInstanceIds =
                new();
            [SerializeField] private List<CardSaveData> cards = new();
            [SerializeField] private List<string> deckOwnedCardIds = new();

            public SaveData()
            {
            }

            public SaveData(
                RunEncounterProgressState progress,
                string excludedBattleInstanceId = null)
            {
                schemaVersion = CurrentSchemaVersion;
                RunBattleState run = progress.RunState;
                maximumHealth = run.MaximumHealth;
                currentHealth = run.CurrentHealth;
                gold = run.Gold;
                consumableItemIds = new List<string>(
                    run.ConsumableItemIds);
                runEnded = run.RunEnded;
                completedEncounterCount = progress.CompletedEncounterCount;
                completedBattleInstanceIds = new List<string>();
                foreach (string battleId in progress.UsedBattleInstanceIds)
                {
                    if (!string.Equals(
                            battleId,
                            excludedBattleInstanceId,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        completedBattleInstanceIds.Add(battleId);
                    }
                }
                cards = new List<CardSaveData>();
                foreach (RunCardInstance card in progress.OwnedCards.Cards)
                {
                    cards.Add(new CardSaveData(card));
                }
                deckOwnedCardIds = new List<string>();
                foreach (RunCardInstance card in progress.RunDeck.Cards)
                {
                    deckOwnedCardIds.Add(card.OwnedCardId);
                }
            }

            public int SchemaVersion => schemaVersion;
            public int MaximumHealth => maximumHealth;
            public int CurrentHealth => currentHealth;
            public int Gold => gold;
            public IReadOnlyList<string> ConsumableItemIds =>
                consumableItemIds;
            public bool RunEnded => runEnded;
            public int CompletedEncounterCount => completedEncounterCount;
            public IReadOnlyList<string> CompletedBattleInstanceIds =>
                completedBattleInstanceIds;
            public IReadOnlyList<CardSaveData> Cards => cards;
            public IReadOnlyList<string> DeckOwnedCardIds => deckOwnedCardIds;
        }

        [Serializable]
        private sealed class CardSaveData
        {
            [SerializeField] private string catalogCardId;
            [SerializeField] private string ownedCardId;
            [SerializeField] private int level;
            [SerializeField] private int enchantSlotCount;
            [SerializeField] private List<EnchantSaveData> enchants = new();

            public CardSaveData()
            {
            }

            public CardSaveData(RunCardInstance card)
            {
                catalogCardId = card.CatalogCardId;
                ownedCardId = card.OwnedCardId;
                level = card.CurrentLevel;
                enchantSlotCount = card.Enchants.SlotCount;
                enchants = new List<EnchantSaveData>();
                foreach (RunEnchantSlot slot in card.Enchants.Slots)
                {
                    if (!slot.IsEmpty)
                    {
                        enchants.Add(new EnchantSaveData(slot));
                    }
                }
            }

            public string CatalogCardId => catalogCardId;
            public string OwnedCardId => ownedCardId;
            public int Level => level;
            public int EnchantSlotCount => enchantSlotCount;
            public IReadOnlyList<EnchantSaveData> Enchants => enchants;
        }

        [Serializable]
        private sealed class EnchantSaveData
        {
            [SerializeField] private int slotIndex;
            [SerializeField] private string definitionId;
            [SerializeField] private int attachmentOrder;
            [SerializeField] private bool active;

            public EnchantSaveData()
            {
            }

            public EnchantSaveData(RunEnchantSlot slot)
            {
                slotIndex = slot.SlotIndex;
                definitionId = slot.Enchant.DefinitionId;
                attachmentOrder = slot.AttachmentOrder;
                active = slot.Active;
            }

            public int SlotIndex => slotIndex;
            public string DefinitionId => definitionId;
            public int AttachmentOrder => attachmentOrder;
            public bool Active => active;
        }

        public static string DefaultPath => Path.Combine(
            Application.persistentDataPath,
            DefaultFileName);

        public static bool TrySerialize(
            RunEncounterProgressState progress,
            out string json,
            out RunProgressSaveFailure failure)
        {
            json = null;
            if (progress?.RunState == null || progress.RunDeck == null)
            {
                failure = RunProgressSaveFailure.InvalidProgress;
                return false;
            }

            if (progress.HasActiveEncounter)
            {
                failure =
                    RunProgressSaveFailure.ActiveEncounterNotSupported;
                return false;
            }

            try
            {
                json = JsonUtility.ToJson(new SaveData(progress), true);
            }
            catch (Exception)
            {
                failure = RunProgressSaveFailure.SerializationFailed;
                return false;
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                failure = RunProgressSaveFailure.SerializationFailed;
                return false;
            }

            failure = RunProgressSaveFailure.None;
            return true;
        }

        internal static bool TrySerializeCheckpointBase(
            RunEncounterProgressState progress,
            string activeBattleInstanceId,
            out string json,
            out RunProgressSaveFailure failure)
        {
            json = null;
            if (progress?.RunState == null || progress.RunDeck == null ||
                !progress.HasActiveEncounter ||
                string.IsNullOrWhiteSpace(activeBattleInstanceId) ||
                progress.UsedBattleInstanceIds.Count !=
                progress.CompletedEncounterCount + 1)
            {
                failure = RunProgressSaveFailure.InvalidProgress;
                return false;
            }

            int matchingIds = 0;
            foreach (string battleId in progress.UsedBattleInstanceIds)
            {
                if (string.Equals(
                        battleId,
                        activeBattleInstanceId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    matchingIds++;
                }
            }

            if (matchingIds != 1)
            {
                failure = RunProgressSaveFailure.InvalidProgress;
                return false;
            }

            try
            {
                json = JsonUtility.ToJson(
                    new SaveData(progress, activeBattleInstanceId),
                    true);
            }
            catch (Exception)
            {
                failure = RunProgressSaveFailure.SerializationFailed;
                return false;
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                failure = RunProgressSaveFailure.SerializationFailed;
                return false;
            }

            failure = RunProgressSaveFailure.None;
            return true;
        }

        public static bool TryDeserialize(
            string json,
            CardDatabase cardDatabase,
            EnchantDatabase enchantDatabase,
            PlayerPermanentRewardState permanentRewards,
            out RunEncounterProgressState progress,
            out RunProgressSaveFailure failure)
        {
            progress = null;
            if (cardDatabase == null)
            {
                failure = RunProgressSaveFailure.InvalidCardDatabase;
                return false;
            }

            if (enchantDatabase == null)
            {
                failure = RunProgressSaveFailure.InvalidEnchantDatabase;
                return false;
            }

            if (permanentRewards == null || string.IsNullOrWhiteSpace(json))
            {
                failure = RunProgressSaveFailure.InvalidData;
                return false;
            }

            SaveData data;
            try
            {
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception)
            {
                failure = RunProgressSaveFailure.InvalidData;
                return false;
            }

            if (data == null)
            {
                failure = RunProgressSaveFailure.InvalidData;
                return false;
            }

            if (data.SchemaVersion < 1 ||
                data.SchemaVersion > CurrentSchemaVersion)
            {
                failure = RunProgressSaveFailure.UnsupportedVersion;
                return false;
            }

            if (!ValidateRunData(data))
            {
                failure = RunProgressSaveFailure.InvalidData;
                return false;
            }

            if (!TryRestoreBattleIds(
                    data,
                    out List<string> battleIds,
                    out failure) ||
                !TryRestoreOwnedCards(
                    data,
                    cardDatabase,
                    enchantDatabase,
                    out RunOwnedCardState ownedCards,
                    out failure) ||
                !TryRestoreSelectedDeck(
                    data, ownedCards, out RunDeckState runDeck,
                    out failure))
            {
                return false;
            }

            try
            {
                RunBattleState runState = new(
                    data.MaximumHealth,
                    data.CurrentHealth,
                    data.Gold,
                    data.ConsumableItemIds,
                    data.RunEnded);
                progress = new RunEncounterProgressState(
                    runState,
                    ownedCards,
                    runDeck,
                    permanentRewards,
                    battleIds,
                    data.CompletedEncounterCount);
            }
            catch (Exception)
            {
                progress = null;
                failure = RunProgressSaveFailure.InvalidData;
                return false;
            }

            failure = RunProgressSaveFailure.None;
            return true;
        }

        public static bool TrySaveDefault(
            RunEncounterProgressState progress,
            out string path,
            out RunProgressSaveFailure failure)
        {
            path = DefaultPath;
            return TrySave(progress, path, out failure);
        }

        public static bool TryLoadDefault(
            CardDatabase cardDatabase,
            EnchantDatabase enchantDatabase,
            PlayerPermanentRewardState permanentRewards,
            out RunEncounterProgressState progress,
            out string path,
            out RunProgressSaveFailure failure)
        {
            path = DefaultPath;
            return TryLoad(
                path,
                cardDatabase,
                enchantDatabase,
                permanentRewards,
                out progress,
                out failure);
        }

        public static bool TrySave(
            RunEncounterProgressState progress,
            string path,
            out RunProgressSaveFailure failure)
        {
            if (!TryNormalizePath(path, out string normalizedPath))
            {
                failure = RunProgressSaveFailure.InvalidPath;
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
                failure = RunProgressSaveFailure.DirectoryCreationFailed;
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
                failure = RunProgressSaveFailure.WriteFailed;
                return false;
            }

            failure = RunProgressSaveFailure.None;
            return true;
        }

        public static bool TryLoad(
            string path,
            CardDatabase cardDatabase,
            EnchantDatabase enchantDatabase,
            PlayerPermanentRewardState permanentRewards,
            out RunEncounterProgressState progress,
            out RunProgressSaveFailure failure)
        {
            progress = null;
            if (!TryNormalizePath(path, out string normalizedPath))
            {
                failure = RunProgressSaveFailure.InvalidPath;
                return false;
            }

            if (!File.Exists(normalizedPath))
            {
                failure = RunProgressSaveFailure.NotFound;
                return false;
            }

            string json;
            try
            {
                json = File.ReadAllText(normalizedPath);
            }
            catch (Exception)
            {
                failure = RunProgressSaveFailure.ReadFailed;
                return false;
            }

            return TryDeserialize(
                json,
                cardDatabase,
                enchantDatabase,
                permanentRewards,
                out progress,
                out failure);
        }

        private static bool ValidateRunData(SaveData data)
        {
            if (data.MaximumHealth < 1 || data.CurrentHealth < 0 ||
                data.CurrentHealth > data.MaximumHealth || data.Gold < 0 ||
                data.CompletedEncounterCount < 0 ||
                data.ConsumableItemIds == null ||
                data.CompletedBattleInstanceIds == null ||
                data.Cards == null ||
                (data.RunEnded && data.CurrentHealth > 0))
            {
                return false;
            }

            foreach (string itemId in data.ConsumableItemIds)
            {
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    return false;
                }
            }

            return data.CompletedBattleInstanceIds.Count ==
                   data.CompletedEncounterCount;
        }

        private static bool TryRestoreBattleIds(
            SaveData data,
            out List<string> battleIds,
            out RunProgressSaveFailure failure)
        {
            battleIds = new List<string>();
            HashSet<string> unique = new(StringComparer.OrdinalIgnoreCase);
            foreach (string battleId in data.CompletedBattleInstanceIds)
            {
                if (string.IsNullOrWhiteSpace(battleId))
                {
                    failure = RunProgressSaveFailure.InvalidData;
                    return false;
                }

                string normalized = battleId.Trim();
                if (!unique.Add(normalized))
                {
                    failure =
                        RunProgressSaveFailure.DuplicateBattleInstanceId;
                    return false;
                }

                battleIds.Add(normalized);
            }

            failure = RunProgressSaveFailure.None;
            return true;
        }

        private static bool TryRestoreOwnedCards(
            SaveData data,
            CardDatabase cardDatabase,
            EnchantDatabase enchantDatabase,
            out RunOwnedCardState ownedCards,
            out RunProgressSaveFailure failure)
        {
            ownedCards = new RunOwnedCardState();
            foreach (CardSaveData savedCard in data.Cards)
            {
                if (savedCard == null ||
                    string.IsNullOrWhiteSpace(savedCard.CatalogCardId) ||
                    string.IsNullOrWhiteSpace(savedCard.OwnedCardId) ||
                    savedCard.Level < CardData.MinimumLevel ||
                    savedCard.Level > CardData.MaximumLevel ||
                    savedCard.Enchants == null)
                {
                    failure = RunProgressSaveFailure.InvalidData;
                    return false;
                }

                if (!cardDatabase.TryGetCard(
                        savedCard.CatalogCardId,
                        out CardData card))
                {
                    failure = RunProgressSaveFailure.MissingCard;
                    return false;
                }

                RunCardInstance restored = new(
                    card,
                    savedCard.OwnedCardId,
                    savedCard.Level);
                if (savedCard.EnchantSlotCount < restored.Enchants.SlotCount ||
                    savedCard.EnchantSlotCount >
                    RunCardEnchantState.MaximumSlotCount)
                {
                    failure = RunProgressSaveFailure.InvalidData;
                    return false;
                }

                while (restored.Enchants.SlotCount <
                       savedCard.EnchantSlotCount)
                {
                    if (!restored.Enchants.TryIncreaseSlotCount())
                    {
                        failure = RunProgressSaveFailure.InvalidData;
                        return false;
                    }
                }

                foreach (EnchantSaveData savedEnchant in savedCard.Enchants)
                {
                    if (savedEnchant == null ||
                        string.IsNullOrWhiteSpace(
                            savedEnchant.DefinitionId) ||
                        savedEnchant.SlotIndex < 0 ||
                        savedEnchant.SlotIndex >=
                        savedCard.EnchantSlotCount ||
                        savedEnchant.AttachmentOrder <= 0)
                    {
                        failure = RunProgressSaveFailure.InvalidData;
                        return false;
                    }

                    EnchantData enchant = enchantDatabase.Find(
                        savedEnchant.DefinitionId);
                    if (enchant == null)
                    {
                        failure = RunProgressSaveFailure.MissingEnchant;
                        return false;
                    }

                    if (!restored.Enchants.TryRestore(
                            enchant,
                            savedEnchant.SlotIndex,
                            savedEnchant.AttachmentOrder,
                            savedEnchant.Active,
                            out _))
                    {
                        failure =
                            RunProgressSaveFailure.EnchantRestoreFailed;
                        return false;
                    }
                }

                if (!ownedCards.TryAdd(
                        restored,
                        out RunDeckFailure deckFailure))
                {
                    failure = deckFailure ==
                        RunDeckFailure.DuplicateOwnedCardId
                            ? RunProgressSaveFailure.DuplicateOwnedCardId
                            : RunProgressSaveFailure.InvalidData;
                    return false;
                }
            }

            failure = RunProgressSaveFailure.None;
            return true;
        }

        private static bool TryRestoreSelectedDeck(
            SaveData data,
            RunOwnedCardState ownedCards,
            out RunDeckState runDeck,
            out RunProgressSaveFailure failure)
        {
            IEnumerable<string> selectedIds =
                data.SchemaVersion == 1 || data.DeckOwnedCardIds == null ||
                data.DeckOwnedCardIds.Count == 0
                    ? GetAllOwnedCardIds(ownedCards)
                    : data.DeckOwnedCardIds;
            if (!RunDeckSelectionService.TryCreateDeck(
                    ownedCards, selectedIds, out runDeck,
                    out RunDeckFailure deckFailure))
            {
                failure = deckFailure == RunDeckFailure.DuplicateOwnedCardId
                    ? RunProgressSaveFailure.DuplicateOwnedCardId
                    : RunProgressSaveFailure.InvalidData;
                return false;
            }
            failure = RunProgressSaveFailure.None;
            return true;
        }

        private static IEnumerable<string> GetAllOwnedCardIds(
            RunOwnedCardState ownedCards)
        {
            foreach (RunCardInstance card in ownedCards.Cards)
            {
                yield return card.OwnedCardId;
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
