using System;
using System.Collections.Generic;
using System.IO;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class RunSaveServiceValidation
    {
        private const string CardDatabasePath =
            "Assets/GameData/CardDatabase.asset";
        private const string EnchantDatabasePath =
            "Assets/GameData/EnchantDatabase.asset";

        [MenuItem("Have a Break/Validate Unified Run Save")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            Debug.Log(valid
                ? "Unified run save routing passed."
                : "Unified run save routing failed.");
            EditorUtility.DisplayDialog(
                "Unified Run Save Validation",
                valid
                    ? "Unified run save routing passed."
                    : "Unified run save routing failed. Check the Console.",
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
            RunEncounterProgressState progress = CreateProgress(
                cardDatabase,
                permanentRewards);
            if (progress == null || enchantDatabase == null)
            {
                return false;
            }

            EnemyDefinitionData enemy =
                ScriptableObject.CreateInstance<EnemyDefinitionData>();
            EncounterData encounter =
                ScriptableObject.CreateInstance<EncounterData>();
            EncounterDatabase encounterDatabase =
                ScriptableObject.CreateInstance<EncounterDatabase>();
            string directory = Path.Combine(
                Path.GetTempPath(),
                $"HaveABreak-UnifiedSave-{Guid.NewGuid():N}");
            string checkpointPath = Path.Combine(
                directory,
                "active-checkpoint.json");
            string runPath = Path.Combine(directory, "run-progress.json");
            try
            {
                enemy.EditorInitialize(
                    "TEST-ENEMY-57",
                    "Test Save Enemy",
                    2,
                    17);
                encounter.EditorInitialize(
                    "TEST-ENCOUNTER-57",
                    "Test Save Encounter",
                    BattleEncounterGrade.Normal,
                    new[]
                    {
                        new EncounterEnemySlot(
                            "TEST-ENEMY-57-A",
                            enemy,
                            EnemyFieldPosition.Center)
                    });
                encounterDatabase.EditorSetEncounters(
                    new[] { encounter });
                return ValidateRouting(
                    progress,
                    encounter,
                    encounterDatabase,
                    cardDatabase,
                    enchantDatabase,
                    permanentRewards,
                    checkpointPath,
                    runPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemy);
                UnityEngine.Object.DestroyImmediate(encounter);
                UnityEngine.Object.DestroyImmediate(encounterDatabase);
                TryDeleteDirectory(directory);
            }
        }

        private static bool ValidateRouting(
            RunEncounterProgressState progress,
            EncounterData encounter,
            EncounterDatabase encounterDatabase,
            CardDatabase cardDatabase,
            EnchantDatabase enchantDatabase,
            PlayerPermanentRewardState permanentRewards,
            string checkpointPath,
            string runPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(checkpointPath));
            File.WriteAllText(checkpointPath, "stale-checkpoint");
            File.WriteAllText(checkpointPath + ".tmp", "stale-temporary");
            File.WriteAllText(checkpointPath + ".bak", "stale-backup");
            if (!RunSaveService.TrySave(
                    progress,
                    checkpointPath,
                    runPath,
                    out RunSaveDestination runDestination,
                    out RunSaveFailure runSaveFailure,
                    out ActiveBattleCheckpointFailure runCheckpointFailure,
                    out RunProgressSaveFailure runProgressFailure) ||
                runDestination != RunSaveDestination.RunProgress ||
                runSaveFailure != RunSaveFailure.None ||
                runCheckpointFailure !=
                ActiveBattleCheckpointFailure.None ||
                runProgressFailure != RunProgressSaveFailure.None ||
                !File.Exists(runPath) ||
                File.Exists(checkpointPath) ||
                File.Exists(checkpointPath + ".tmp") ||
                File.Exists(checkpointPath + ".bak") ||
                !TryBegin(progress, encounter))
            {
                return false;
            }

            if (!RunSaveService.TrySave(
                    progress,
                    checkpointPath,
                    runPath,
                    out RunSaveDestination checkpointDestination,
                    out RunSaveFailure checkpointSaveFailure,
                    out ActiveBattleCheckpointFailure checkpointFailure,
                    out RunProgressSaveFailure checkpointRunFailure) ||
                checkpointDestination !=
                RunSaveDestination.ActiveBattleCheckpoint ||
                checkpointSaveFailure != RunSaveFailure.None ||
                checkpointFailure != ActiveBattleCheckpointFailure.None ||
                checkpointRunFailure != RunProgressSaveFailure.None ||
                !File.Exists(checkpointPath) ||
                !File.Exists(runPath))
            {
                return false;
            }

            string safeCheckpoint = File.ReadAllText(checkpointPath);
            if (!progress.ActiveEncounter.Runtime.Turn.TryBeginPlayerAction(
                    out BattleTurnFailure turnFailure) ||
                turnFailure != BattleTurnFailure.None)
            {
                return false;
            }

            bool unsafeSaved = RunSaveService.TrySave(
                progress,
                checkpointPath,
                runPath,
                out RunSaveDestination unsafeDestination,
                out RunSaveFailure unsafeFailure,
                out ActiveBattleCheckpointFailure unsafeCheckpointFailure,
                out RunProgressSaveFailure unsafeRunFailure);
            if (unsafeSaved ||
                unsafeDestination != RunSaveDestination.None ||
                unsafeFailure !=
                RunSaveFailure.ActiveCheckpointSaveFailed ||
                unsafeCheckpointFailure !=
                ActiveBattleCheckpointFailure.UnsafeCheckpoint ||
                unsafeRunFailure != RunProgressSaveFailure.None ||
                File.ReadAllText(checkpointPath) != safeCheckpoint)
            {
                return false;
            }

            bool resumed = RunResumeService.TryLoad(
                checkpointPath,
                runPath,
                cardDatabase,
                enchantDatabase,
                encounterDatabase,
                permanentRewards,
                out RunEncounterProgressState restored,
                out RunResumeSource resumeSource,
                out EncounterData restoredEncounter,
                out RunResumeFailure resumeFailure,
                out ActiveBattleCheckpointFailure resumeCheckpointFailure,
                out RunProgressSaveFailure resumeRunFailure);
            if (!resumed || restored?.ActiveEncounter == null ||
                resumeSource != RunResumeSource.ActiveBattleCheckpoint ||
                restoredEncounter != encounter ||
                resumeFailure != RunResumeFailure.None ||
                resumeCheckpointFailure !=
                ActiveBattleCheckpointFailure.None ||
                resumeRunFailure != RunProgressSaveFailure.None)
            {
                return false;
            }

            bool samePathSaved = RunSaveService.TrySave(
                restored,
                runPath,
                runPath,
                out RunSaveDestination samePathDestination,
                out RunSaveFailure samePathFailure,
                out ActiveBattleCheckpointFailure samePathCheckpointFailure,
                out RunProgressSaveFailure samePathRunFailure);
            return !samePathSaved &&
                   samePathDestination == RunSaveDestination.None &&
                   samePathFailure == RunSaveFailure.InvalidPath &&
                   samePathCheckpointFailure ==
                   ActiveBattleCheckpointFailure.None &&
                   samePathRunFailure == RunProgressSaveFailure.None;
        }

        private static bool TryBegin(
            RunEncounterProgressState progress,
            EncounterData encounter)
        {
            bool begun = RunEncounterProgressService.TryBegin(
                progress,
                "TEST-BATTLE-57",
                encounter,
                570,
                5,
                Array.Empty<string>(),
                57,
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
            CardDatabase database,
            PlayerPermanentRewardState permanentRewards)
        {
            if (database == null || permanentRewards == null)
            {
                return null;
            }

            RunDeckState deck = new();
            for (int number = 1; number <= 12; number++)
            {
                string cardId = $"C{number:00}";
                if (!database.TryGetCard(cardId, out CardData card) ||
                    !deck.TryAdd(
                        new RunCardInstance(card, $"OWNED-57-{cardId}"),
                        out RunDeckFailure deckFailure) ||
                    deckFailure != RunDeckFailure.None)
                {
                    return null;
                }
            }

            return new RunEncounterProgressState(
                new RunBattleState(30, 28, 6),
                deck,
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
                    $"Could not delete save validation directory: {exception.Message}");
            }
        }
    }
}
