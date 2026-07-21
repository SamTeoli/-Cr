using System;
using System.Collections.Generic;
using System.IO;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class RunSaveSlotServiceValidation
    {
        private const string CardDatabasePath =
            "Assets/GameData/CardDatabase.asset";
        private const string EnchantDatabasePath =
            "Assets/GameData/EnchantDatabase.asset";

        [MenuItem("Have a Break/Validate Run Save Slot Inspection")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            Debug.Log(valid
                ? "Run save slot inspection passed."
                : "Run save slot inspection failed.");
            EditorUtility.DisplayDialog(
                "Run Save Slot Inspection Validation",
                valid
                    ? "Run save slot inspection passed."
                    : "Run save slot inspection failed. Check the Console.",
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
                $"HaveABreak-SaveSlot-{Guid.NewGuid():N}");
            string checkpointPath = Path.Combine(
                directory,
                "active-checkpoint.json");
            string runPath = Path.Combine(directory, "run-progress.json");
            try
            {
                enemy.EditorInitialize(
                    "TEST-ENEMY-58",
                    "Test Slot Enemy",
                    3,
                    19);
                encounter.EditorInitialize(
                    "TEST-ENCOUNTER-58",
                    "Test Slot Encounter",
                    BattleEncounterGrade.Normal,
                    new[]
                    {
                        new EncounterEnemySlot(
                            "TEST-ENEMY-58-A",
                            enemy,
                            EnemyFieldPosition.Center)
                    });
                encounterDatabase.EditorSetEncounters(
                    new[] { encounter });
                return ValidateStates(
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

        private static bool ValidateStates(
            RunEncounterProgressState progress,
            EncounterData encounter,
            EncounterDatabase encounterDatabase,
            CardDatabase cardDatabase,
            EnchantDatabase enchantDatabase,
            PlayerPermanentRewardState permanentRewards,
            string checkpointPath,
            string runPath)
        {
            if (!Inspect(
                    checkpointPath,
                    runPath,
                    cardDatabase,
                    enchantDatabase,
                    encounterDatabase,
                    permanentRewards,
                    out RunSaveSlotInfo empty) ||
                empty.State != RunSaveSlotState.Empty ||
                empty.CanResume ||
                empty.Source != RunResumeSource.None ||
                !RunSaveService.TrySave(
                    progress,
                    checkpointPath,
                    runPath,
                    out RunSaveDestination destination,
                    out RunSaveFailure saveFailure,
                    out ActiveBattleCheckpointFailure checkpointFailure,
                    out RunProgressSaveFailure runFailure) ||
                destination != RunSaveDestination.RunProgress ||
                saveFailure != RunSaveFailure.None ||
                checkpointFailure != ActiveBattleCheckpointFailure.None ||
                runFailure != RunProgressSaveFailure.None ||
                !Inspect(
                    checkpointPath,
                    runPath,
                    cardDatabase,
                    enchantDatabase,
                    encounterDatabase,
                    permanentRewards,
                    out RunSaveSlotInfo runInfo) ||
                !ValidateRunInfo(runInfo) ||
                !TryBegin(progress, encounter) ||
                !RunSaveService.TrySave(
                    progress,
                    checkpointPath,
                    runPath,
                    out destination,
                    out saveFailure,
                    out checkpointFailure,
                    out runFailure) ||
                destination !=
                RunSaveDestination.ActiveBattleCheckpoint ||
                saveFailure != RunSaveFailure.None ||
                checkpointFailure != ActiveBattleCheckpointFailure.None ||
                runFailure != RunProgressSaveFailure.None)
            {
                return false;
            }

            encounterDatabase.EditorSetEncounters(Array.Empty<EncounterData>());
            if (!Inspect(
                    checkpointPath,
                    runPath,
                    cardDatabase,
                    enchantDatabase,
                    encounterDatabase,
                    permanentRewards,
                    out RunSaveSlotInfo missingEncounterInfo) ||
                missingEncounterInfo.State !=
                RunSaveSlotState.InvalidActiveBattleCheckpoint ||
                missingEncounterInfo.CanResume ||
                missingEncounterInfo.BattleInstanceId != "TEST-BATTLE-58" ||
                missingEncounterInfo.EncounterId != "TEST-ENCOUNTER-58" ||
                missingEncounterInfo.ResumeFailure !=
                RunResumeFailure.ActiveCheckpointLoadFailed ||
                missingEncounterInfo.CheckpointFailure !=
                ActiveBattleCheckpointFailure.EncounterNotFound)
            {
                return false;
            }

            encounterDatabase.EditorSetEncounters(new[] { encounter });
            if (!Inspect(
                    checkpointPath,
                    runPath,
                    cardDatabase,
                    enchantDatabase,
                    encounterDatabase,
                    permanentRewards,
                    out RunSaveSlotInfo activeInfo) ||
                !ValidateActiveInfo(activeInfo))
            {
                return false;
            }

            File.WriteAllText(checkpointPath, "{invalid-checkpoint");
            if (!Inspect(
                    checkpointPath,
                    runPath,
                    cardDatabase,
                    enchantDatabase,
                    encounterDatabase,
                    permanentRewards,
                    out RunSaveSlotInfo invalidCheckpoint) ||
                invalidCheckpoint.State !=
                RunSaveSlotState.InvalidActiveBattleCheckpoint ||
                invalidCheckpoint.CanResume ||
                invalidCheckpoint.ResumeFailure !=
                RunResumeFailure.ActiveCheckpointLoadFailed ||
                invalidCheckpoint.CheckpointFailure !=
                ActiveBattleCheckpointFailure.InvalidData ||
                !ActiveBattleCheckpointService.TryClear(
                    checkpointPath,
                    out ActiveBattleCheckpointFailure clearFailure) ||
                clearFailure != ActiveBattleCheckpointFailure.None)
            {
                return false;
            }

            File.WriteAllText(runPath, "{invalid-run-progress");
            if (!Inspect(
                    checkpointPath,
                    runPath,
                    cardDatabase,
                    enchantDatabase,
                    encounterDatabase,
                    permanentRewards,
                    out RunSaveSlotInfo invalidRun) ||
                invalidRun.State != RunSaveSlotState.InvalidRunProgress ||
                invalidRun.CanResume ||
                invalidRun.ResumeFailure !=
                RunResumeFailure.RunProgressLoadFailed ||
                invalidRun.RunProgressFailure !=
                RunProgressSaveFailure.InvalidData)
            {
                return false;
            }

            File.Delete(runPath);
            return Inspect(
                       checkpointPath,
                       runPath,
                       cardDatabase,
                       enchantDatabase,
                       encounterDatabase,
                       permanentRewards,
                       out RunSaveSlotInfo finalEmpty) &&
                   finalEmpty.State == RunSaveSlotState.Empty &&
                   !finalEmpty.CanResume;
        }

        private static bool Inspect(
            string checkpointPath,
            string runPath,
            CardDatabase cardDatabase,
            EnchantDatabase enchantDatabase,
            EncounterDatabase encounterDatabase,
            PlayerPermanentRewardState permanentRewards,
            out RunSaveSlotInfo info)
        {
            return RunSaveSlotService.TryInspect(
                       checkpointPath,
                       runPath,
                       cardDatabase,
                       enchantDatabase,
                       encounterDatabase,
                       permanentRewards,
                       out info,
                       out RunSaveSlotInspectionFailure failure) &&
                   failure == RunSaveSlotInspectionFailure.None;
        }

        private static bool ValidateRunInfo(RunSaveSlotInfo info)
        {
            return info.State == RunSaveSlotState.RunProgress &&
                   info.CanResume &&
                   info.Source == RunResumeSource.RunProgress &&
                   info.MaximumHealth == 30 &&
                   info.CurrentHealth == 25 &&
                   info.Gold == 11 &&
                   info.CompletedEncounterCount == 0 &&
                   info.BattleInstanceId == null &&
                   info.EncounterId == null &&
                   info.ResumeFailure == RunResumeFailure.None &&
                   info.CheckpointFailure ==
                   ActiveBattleCheckpointFailure.None &&
                   info.RunProgressFailure == RunProgressSaveFailure.None;
        }

        private static bool ValidateActiveInfo(RunSaveSlotInfo info)
        {
            return info.State ==
                   RunSaveSlotState.ActiveBattleCheckpoint &&
                   info.CanResume &&
                   info.Source ==
                   RunResumeSource.ActiveBattleCheckpoint &&
                   info.MaximumHealth == 30 &&
                   info.CurrentHealth == 25 &&
                   info.Gold == 11 &&
                   info.BattleInstanceId == "TEST-BATTLE-58" &&
                   info.EncounterId == "TEST-ENCOUNTER-58" &&
                   info.ResumeFailure == RunResumeFailure.None &&
                   info.CheckpointFailure ==
                   ActiveBattleCheckpointFailure.None &&
                   info.RunProgressFailure == RunProgressSaveFailure.None;
        }

        private static bool TryBegin(
            RunEncounterProgressState progress,
            EncounterData encounter)
        {
            bool begun = RunEncounterProgressService.TryBegin(
                progress,
                "TEST-BATTLE-58",
                encounter,
                580,
                5,
                Array.Empty<string>(),
                58,
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
                        new RunCardInstance(card, $"OWNED-58-{cardId}"),
                        out RunDeckFailure deckFailure) ||
                    deckFailure != RunDeckFailure.None)
                {
                    return null;
                }
            }

            return new RunEncounterProgressState(
                new RunBattleState(30, 25, 11),
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
                    $"Could not delete slot validation directory: {exception.Message}");
            }
        }
    }
}
