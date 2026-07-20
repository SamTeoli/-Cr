using System;
using System.Collections.Generic;
using System.IO;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class ActiveBattleCheckpointServiceValidation
    {
        private const string CardDatabasePath =
            "Assets/GameData/CardDatabase.asset";
        private const string EnchantDatabasePath =
            "Assets/GameData/EnchantDatabase.asset";

        [MenuItem("Have a Break/Validate Active Battle Checkpoint")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            if (valid)
            {
                Debug.Log("Active battle start checkpoint passed.");
            }
            else
            {
                Debug.LogError("Active battle start checkpoint failed.");
            }

            EditorUtility.DisplayDialog(
                "Active Battle Checkpoint Validation",
                valid
                    ? "Active battle start checkpoint passed."
                    : "Active battle start checkpoint failed. Check the Console.",
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
            RunEncounterProgressState source = CreateProgress(
                cardDatabase,
                permanentRewards);
            if (source == null || enchantDatabase == null)
            {
                return false;
            }

            EnemyDefinitionData enemy =
                ScriptableObject.CreateInstance<EnemyDefinitionData>();
            EncounterData encounter =
                ScriptableObject.CreateInstance<EncounterData>();
            try
            {
                enemy.EditorInitialize(
                    "TEST-ENEMY-53",
                    "Test Checkpoint Enemy",
                    4,
                    20);
                encounter.EditorInitialize(
                    "TEST-ENCOUNTER-53",
                    "Test Checkpoint Encounter",
                    BattleEncounterGrade.Normal,
                    new[]
                    {
                        new EncounterEnemySlot(
                            "TEST-ENEMY-53-A",
                            enemy,
                            EnemyFieldPosition.Center)
                    });

                return ValidateCheckpoint(
                    source,
                    encounter,
                    cardDatabase,
                    enchantDatabase,
                    permanentRewards);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemy);
                UnityEngine.Object.DestroyImmediate(encounter);
            }
        }

        private static bool ValidateCheckpoint(
            RunEncounterProgressState source,
            EncounterData encounter,
            CardDatabase cardDatabase,
            EnchantDatabase enchantDatabase,
            PlayerPermanentRewardState permanentRewards)
        {
            if (!TryBegin(source, encounter) ||
                !ActiveBattleCheckpointService.CanCreate(
                    source,
                    out ActiveBattleCheckpointFailure safeFailure) ||
                safeFailure != ActiveBattleCheckpointFailure.None ||
                !ActiveBattleCheckpointService.TrySerialize(
                    source,
                    out string json,
                    out ActiveBattleCheckpointFailure serializeFailure) ||
                serializeFailure != ActiveBattleCheckpointFailure.None ||
                string.IsNullOrWhiteSpace(json) ||
                !ActiveBattleCheckpointService.TryDeserialize(
                    json,
                    cardDatabase,
                    enchantDatabase,
                    permanentRewards,
                    encounter,
                    out RunEncounterProgressState restored,
                    out ActiveBattleCheckpointFailure restoreFailure) ||
                restoreFailure != ActiveBattleCheckpointFailure.None ||
                !ValidateEquivalent(source, restored))
            {
                return false;
            }

            string directory = Path.Combine(
                Path.GetTempPath(),
                $"HaveABreak-ActiveCheckpoint-{Guid.NewGuid():N}");
            string path = Path.Combine(directory, "checkpoint.json");
            bool fileValid;
            try
            {
                fileValid = ValidateFileRoundTrip(
                    source,
                    path,
                    encounter,
                    cardDatabase,
                    enchantDatabase,
                    permanentRewards);
            }
            finally
            {
                TryDeleteDirectory(directory);
            }

            if (!fileValid ||
                !source.ActiveEncounter.Runtime.Turn.TryBeginPlayerAction(
                    out BattleTurnFailure turnFailure) ||
                turnFailure != BattleTurnFailure.None)
            {
                return false;
            }

            bool unsafeSaved = ActiveBattleCheckpointService.TrySerialize(
                source,
                out string unsafeJson,
                out ActiveBattleCheckpointFailure unsafeFailure);
            return !unsafeSaved && unsafeJson == null &&
                   unsafeFailure ==
                   ActiveBattleCheckpointFailure.UnsafeCheckpoint;
        }

        private static bool ValidateFileRoundTrip(
            RunEncounterProgressState source,
            string path,
            EncounterData encounter,
            CardDatabase cardDatabase,
            EnchantDatabase enchantDatabase,
            PlayerPermanentRewardState permanentRewards)
        {
            bool missingLoaded = ActiveBattleCheckpointService.TryLoad(
                path,
                cardDatabase,
                enchantDatabase,
                permanentRewards,
                encounter,
                out RunEncounterProgressState missing,
                out ActiveBattleCheckpointFailure missingFailure);
            bool saved = ActiveBattleCheckpointService.TrySave(
                source,
                path,
                out ActiveBattleCheckpointFailure saveFailure);
            bool loaded = ActiveBattleCheckpointService.TryLoad(
                path,
                cardDatabase,
                enchantDatabase,
                permanentRewards,
                encounter,
                out RunEncounterProgressState restored,
                out ActiveBattleCheckpointFailure loadFailure);
            return !missingLoaded && missing == null &&
                   missingFailure == ActiveBattleCheckpointFailure.NotFound &&
                   saved && saveFailure == ActiveBattleCheckpointFailure.None &&
                   loaded && loadFailure == ActiveBattleCheckpointFailure.None &&
                   ValidateEquivalent(source, restored) &&
                   !File.Exists(path + ".tmp") &&
                   !File.Exists(path + ".bak");
        }

        private static bool ValidateEquivalent(
            RunEncounterProgressState source,
            RunEncounterProgressState restored)
        {
            BattleRuntimeEncounterContext expected =
                source?.ActiveEncounter;
            BattleRuntimeEncounterContext actual =
                restored?.ActiveEncounter;
            if (expected?.StartParameters == null ||
                actual?.StartParameters == null ||
                restored.PermanentRewards != source.PermanentRewards ||
                restored.CompletedEncounterCount != 0 ||
                restored.UsedBattleInstanceIds.Count != 1 ||
                restored.UsedBattleInstanceIds[0] != "TEST-BATTLE-53" ||
                actual.StartParameters.BattleInstanceId != "TEST-BATTLE-53" ||
                actual.StartParameters.EncounterId != "TEST-ENCOUNTER-53" ||
                actual.StartParameters.ShuffleSeed != 530 ||
                actual.StartParameters.MaximumMana != 5 ||
                actual.StartParameters.RewardSeed != 53 ||
                actual.StartParameters.StartingHandRedrawIds.Count != 0 ||
                restored.RunState.MaximumHealth != 30 ||
                restored.RunState.CurrentHealth != 24 ||
                restored.RunState.Gold != 9 ||
                restored.RunDeck.Count != 12 ||
                actual.Runtime.Turn.Phase != BattleTurnPhase.PlayerAction ||
                actual.Runtime.Turn.PlayerTurnNumber != 1 ||
                actual.Session.CompletedRoundCount != 0 ||
                actual.Runtime.Player.CurrentHealth != 24 ||
                actual.Runtime.Enemies.Count != 1 ||
                actual.Runtime.Enemies[0].Vital.CurrentHealth != 20 ||
                expected.Runtime.Deck.DrawPileOrder.Count !=
                actual.Runtime.Deck.DrawPileOrder.Count ||
                expected.Runtime.Deck.Zones.Cards.Count !=
                actual.Runtime.Deck.Zones.Cards.Count)
            {
                return false;
            }

            for (int i = 0;
                 i < expected.Runtime.Deck.DrawPileOrder.Count;
                 i++)
            {
                if (expected.Runtime.Deck.DrawPileOrder[i] !=
                    actual.Runtime.Deck.DrawPileOrder[i])
                {
                    return false;
                }
            }

            foreach (BattleCardInstance expectedCard in
                     expected.Runtime.Deck.Zones.Cards)
            {
                BattleCardInstance actualCard =
                    actual.Runtime.Deck.Zones.Find(
                        expectedCard.Ids.BattleCardId);
                if (actualCard == null ||
                    actualCard.Zone != expectedCard.Zone ||
                    actualCard.CurrentLevel != expectedCard.CurrentLevel)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryBegin(
            RunEncounterProgressState progress,
            EncounterData encounter)
        {
            bool begun = RunEncounterProgressService.TryBegin(
                progress,
                "TEST-BATTLE-53",
                encounter,
                530,
                5,
                Array.Empty<string>(),
                53,
                out BattleRuntimeEncounterContext context,
                out RunEncounterProgressFailure progressFailure,
                out BattleRuntimeEncounterFlowFailure flowFailure,
                out RunDeckFailure runDeckFailure,
                out BattleRuntimeBootstrapFailure bootstrapFailure,
                out BattleRuntimeSessionFailure sessionFailure,
                out StartingHandRedrawFailure redrawFailure,
                out BattleTurnFailure turnFailure,
                out List<string> validationErrors);
            return begun && context != null &&
                   progressFailure == RunEncounterProgressFailure.None &&
                   flowFailure == BattleRuntimeEncounterFlowFailure.None &&
                   runDeckFailure == RunDeckFailure.None &&
                   bootstrapFailure == BattleRuntimeBootstrapFailure.None &&
                   sessionFailure == BattleRuntimeSessionFailure.None &&
                   redrawFailure == StartingHandRedrawFailure.None &&
                   turnFailure == BattleTurnFailure.None &&
                   validationErrors.Count == 0;
        }

        private static RunEncounterProgressState CreateProgress(
            CardDatabase cardDatabase,
            PlayerPermanentRewardState permanentRewards)
        {
            if (cardDatabase == null || permanentRewards == null)
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
                            $"OWNED-53-{cardId}"),
                        out RunDeckFailure failure) ||
                    failure != RunDeckFailure.None)
                {
                    return null;
                }
            }

            return new RunEncounterProgressState(
                new RunBattleState(30, 24, 9),
                runDeck,
                permanentRewards);
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
