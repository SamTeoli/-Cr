using System;
using System.IO;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class RunProgressSaveServiceValidation
    {
        private const string CardDatabasePath =
            "Assets/GameData/CardDatabase.asset";
        private const string EnchantDatabasePath =
            "Assets/GameData/EnchantDatabase.asset";

        [MenuItem("Have a Break/Validate Run Progress Save Load")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            if (valid)
            {
                Debug.Log("Run progress save and load passed.");
            }
            else
            {
                Debug.LogError("Run progress save and load failed.");
            }

            EditorUtility.DisplayDialog(
                "Run Progress Save Load Validation",
                valid
                    ? "Run progress save and load passed."
                    : "Run progress save and load failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            CardDatabase cardDatabase =
                AssetDatabase.LoadAssetAtPath<CardDatabase>(
                    CardDatabasePath);
            EnchantDatabase enchantDatabase =
                AssetDatabase.LoadAssetAtPath<EnchantDatabase>(
                    EnchantDatabasePath);
            PlayerPermanentRewardState permanentRewards = new();
            RunEncounterProgressState source = CreateSource(
                cardDatabase,
                enchantDatabase,
                permanentRewards);
            if (source == null || string.IsNullOrWhiteSpace(
                    RunProgressSaveService.DefaultPath))
            {
                return false;
            }

            bool serialized = RunProgressSaveService.TrySerialize(
                source,
                out string json,
                out RunProgressSaveFailure serializeFailure);
            if (!serialized || string.IsNullOrWhiteSpace(json) ||
                serializeFailure != RunProgressSaveFailure.None ||
                !ValidateRejectedData(
                    json,
                    cardDatabase,
                    enchantDatabase,
                    permanentRewards))
            {
                return false;
            }

            string directory = Path.Combine(
                Path.GetTempPath(),
                $"HaveABreak-RunProgress-{Guid.NewGuid():N}");
            string path = Path.Combine(directory, "run.json");
            try
            {
                return ValidateFileRoundTrip(
                    source,
                    path,
                    cardDatabase,
                    enchantDatabase,
                    permanentRewards);
            }
            finally
            {
                TryDeleteDirectory(directory);
            }
        }

        private static RunEncounterProgressState CreateSource(
            CardDatabase cardDatabase,
            EnchantDatabase enchantDatabase,
            PlayerPermanentRewardState permanentRewards)
        {
            if (cardDatabase == null || enchantDatabase == null ||
                permanentRewards == null)
            {
                return null;
            }

            RunDeckState runDeck = new();
            for (int number = 1; number <= 12; number++)
            {
                string cardId = $"C{number:00}";
                if (!cardDatabase.TryGetCard(cardId, out CardData card) ||
                    !runDeck.TryAdd(
                        new RunCardInstance(
                            card,
                            $"OWNED-52-{cardId}",
                            ((number - 1) % CardData.MaximumLevel) + 1),
                        out RunDeckFailure failure) ||
                    failure != RunDeckFailure.None)
                {
                    return null;
                }
            }

            EnchantData e01 = enchantDatabase.Find("E01");
            RunCardInstance target = runDeck.Find("OWNED-52-C01");
            if (e01 == null || target == null ||
                !target.Enchants.HasImmediateAttachmentTarget(e01) ||
                !target.Enchants.TryIncreaseSlotCount() ||
                !target.Enchants.TryIncreaseSlotCount() ||
                !target.Enchants.TryAttach(
                    e01,
                    2,
                    false,
                    out EnchantAttachmentFailure attachmentFailure) ||
                attachmentFailure != EnchantAttachmentFailure.None)
            {
                return null;
            }

            return new RunEncounterProgressState(
                new RunBattleState(
                    40,
                    27,
                    123,
                    new[] { "ITEM-52-A", "ITEM-52-B" }),
                runDeck,
                permanentRewards,
                new[] { "BATTLE-52-A", "BATTLE-52-B" },
                2);
        }

        private static bool ValidateRejectedData(
            string json,
            CardDatabase cardDatabase,
            EnchantDatabase enchantDatabase,
            PlayerPermanentRewardState permanentRewards)
        {
            bool nullDatabaseLoaded = RunProgressSaveService.TryDeserialize(
                json,
                null,
                enchantDatabase,
                permanentRewards,
                out RunEncounterProgressState nullDatabaseProgress,
                out RunProgressSaveFailure nullDatabaseFailure);
            string unsupportedJson = json.Replace(
                "\"schemaVersion\": 1",
                "\"schemaVersion\": 2");
            bool unsupportedLoaded = RunProgressSaveService.TryDeserialize(
                unsupportedJson,
                cardDatabase,
                enchantDatabase,
                permanentRewards,
                out RunEncounterProgressState unsupportedProgress,
                out RunProgressSaveFailure unsupportedFailure);
            string missingCardJson = json.Replace(
                "\"catalogCardId\": \"C01\"",
                "\"catalogCardId\": \"C99\"");
            bool missingCardLoaded = RunProgressSaveService.TryDeserialize(
                missingCardJson,
                cardDatabase,
                enchantDatabase,
                permanentRewards,
                out RunEncounterProgressState missingCardProgress,
                out RunProgressSaveFailure missingCardFailure);
            string duplicateOwnedJson = json.Replace(
                "\"ownedCardId\": \"OWNED-52-C02\"",
                "\"ownedCardId\": \"OWNED-52-C01\"");
            bool duplicateOwnedLoaded =
                RunProgressSaveService.TryDeserialize(
                    duplicateOwnedJson,
                    cardDatabase,
                    enchantDatabase,
                    permanentRewards,
                    out RunEncounterProgressState duplicateOwnedProgress,
                    out RunProgressSaveFailure duplicateOwnedFailure);

            return !nullDatabaseLoaded && nullDatabaseProgress == null &&
                   nullDatabaseFailure ==
                   RunProgressSaveFailure.InvalidCardDatabase &&
                   unsupportedJson != json && !unsupportedLoaded &&
                   unsupportedProgress == null &&
                   unsupportedFailure ==
                   RunProgressSaveFailure.UnsupportedVersion &&
                   missingCardJson != json && !missingCardLoaded &&
                   missingCardProgress == null &&
                   missingCardFailure == RunProgressSaveFailure.MissingCard &&
                   duplicateOwnedJson != json && !duplicateOwnedLoaded &&
                   duplicateOwnedProgress == null &&
                   duplicateOwnedFailure ==
                   RunProgressSaveFailure.DuplicateOwnedCardId;
        }

        private static bool ValidateFileRoundTrip(
            RunEncounterProgressState source,
            string path,
            CardDatabase cardDatabase,
            EnchantDatabase enchantDatabase,
            PlayerPermanentRewardState permanentRewards)
        {
            bool missingLoaded = RunProgressSaveService.TryLoad(
                path,
                cardDatabase,
                enchantDatabase,
                permanentRewards,
                out RunEncounterProgressState missingProgress,
                out RunProgressSaveFailure missingFailure);
            bool saved = RunProgressSaveService.TrySave(
                source,
                path,
                out RunProgressSaveFailure saveFailure);
            bool loaded = RunProgressSaveService.TryLoad(
                path,
                cardDatabase,
                enchantDatabase,
                permanentRewards,
                out RunEncounterProgressState restored,
                out RunProgressSaveFailure loadFailure);

            return !missingLoaded && missingProgress == null &&
                   missingFailure == RunProgressSaveFailure.NotFound &&
                   saved && saveFailure == RunProgressSaveFailure.None &&
                   loaded && loadFailure == RunProgressSaveFailure.None &&
                   ValidateRestored(restored, permanentRewards) &&
                   !File.Exists(path + ".tmp") &&
                   !File.Exists(path + ".bak");
        }

        private static bool ValidateRestored(
            RunEncounterProgressState restored,
            PlayerPermanentRewardState permanentRewards)
        {
            if (restored?.RunState == null || restored.RunDeck == null ||
                restored.HasActiveEncounter ||
                restored.PermanentRewards != permanentRewards ||
                restored.RunState.MaximumHealth != 40 ||
                restored.RunState.CurrentHealth != 27 ||
                restored.RunState.Gold != 123 ||
                restored.RunState.RunEnded ||
                restored.RunState.ConsumableItemIds.Count != 2 ||
                restored.RunState.ConsumableItemIds[0] != "ITEM-52-A" ||
                restored.RunState.ConsumableItemIds[1] != "ITEM-52-B" ||
                restored.CompletedEncounterCount != 2 ||
                restored.UsedBattleInstanceIds.Count != 2 ||
                restored.UsedBattleInstanceIds[0] != "BATTLE-52-A" ||
                restored.UsedBattleInstanceIds[1] != "BATTLE-52-B" ||
                restored.RunDeck.Count != 12)
            {
                return false;
            }

            for (int number = 1; number <= 12; number++)
            {
                string cardId = $"C{number:00}";
                RunCardInstance card = restored.RunDeck.Find(
                    $"OWNED-52-{cardId}");
                if (card == null || card.CatalogCardId != cardId ||
                    card.CurrentLevel !=
                    ((number - 1) % CardData.MaximumLevel) + 1)
                {
                    return false;
                }
            }

            RunCardInstance enchanted =
                restored.RunDeck.Find("OWNED-52-C01");
            if (enchanted?.Enchants == null ||
                enchanted.Enchants.SlotCount != 3)
            {
                return false;
            }

            RunEnchantSlot slot = enchanted.Enchants.Slots[2];
            return slot != null && !slot.IsEmpty &&
                   slot.Enchant.DefinitionId == "E01" &&
                   slot.AttachmentOrder == 1 && slot.Active;
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
