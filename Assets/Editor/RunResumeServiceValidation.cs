using System;
using System.Collections.Generic;
using System.IO;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class RunResumeServiceValidation
    {
        private const string CardDatabasePath =
            "Assets/GameData/CardDatabase.asset";
        private const string EnchantDatabasePath =
            "Assets/GameData/EnchantDatabase.asset";

        [MenuItem("Have a Break/Validate Unified Run Resume")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            Debug.Log(valid
                ? "Unified run resume routing passed."
                : "Unified run resume routing failed.");
            EditorUtility.DisplayDialog(
                "Unified Run Resume Validation",
                valid
                    ? "Unified run resume routing passed."
                    : "Unified run resume routing failed. Check the Console.",
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
            EncounterDatabase encounterDatabase =
                ScriptableObject.CreateInstance<EncounterDatabase>();
            string directory = Path.Combine(
                Path.GetTempPath(),
                $"HaveABreak-UnifiedResume-{Guid.NewGuid():N}");
            string checkpointPath = Path.Combine(
                directory,
                "active-checkpoint.json");
            string runPath = Path.Combine(directory, "run-progress.json");
            try
            {
                enemy.EditorInitialize(
                    "TEST-ENEMY-56",
                    "Test Resume Enemy",
                    2,
                    16);
                encounter.EditorInitialize(
                    "TEST-ENCOUNTER-56",
                    "Test Resume Encounter",
                    BattleEncounterGrade.Normal,
                    new[]
                    {
                        new EncounterEnemySlot(
                            "TEST-ENEMY-56-A",
                            enemy,
                            EnemyFieldPosition.Center)
                    });
                encounterDatabase.EditorSetEncounters(
                    new[] { encounter });
                return ValidateRouting(
                    source,
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
            RunEncounterProgressState source,
            EncounterData encounter,
            EncounterDatabase encounterDatabase,
            CardDatabase cardDatabase,
            EnchantDatabase enchantDatabase,
            PlayerPermanentRewardState permanentRewards,
            string checkpointPath,
            string runPath)
        {
            if (!RunProgressSaveService.TrySave(
                    source,
                    runPath,
                    out RunProgressSaveFailure saveFailure) ||
                saveFailure != RunProgressSaveFailure.None ||
                !TryLoadRunProgress(
                    checkpointPath,
                    runPath,
                    cardDatabase,
                    enchantDatabase,
                    encounterDatabase,
                    permanentRewards) ||
                !TryBegin(source, encounter) ||
                !ActiveBattleCheckpointService.TrySave(
                    source,
                    checkpointPath,
                    out ActiveBattleCheckpointFailure checkpointSaveFailure) ||
                checkpointSaveFailure !=
                ActiveBattleCheckpointFailure.None ||
                !TryLoadActiveCheckpoint(
                    checkpointPath,
                    runPath,
                    encounter,
                    cardDatabase,
                    enchantDatabase,
                    encounterDatabase,
                    permanentRewards))
            {
                return false;
            }

            File.WriteAllText(checkpointPath, "{corrupt-checkpoint");
            bool corruptLoaded = RunResumeService.TryLoad(
                checkpointPath,
                runPath,
                cardDatabase,
                enchantDatabase,
                encounterDatabase,
                permanentRewards,
                out RunEncounterProgressState corruptProgress,
                out RunResumeSource corruptSource,
                out EncounterData corruptEncounter,
                out RunResumeFailure corruptFailure,
                out ActiveBattleCheckpointFailure checkpointFailure,
                out RunProgressSaveFailure corruptRunFailure);
            if (corruptLoaded || corruptProgress != null ||
                corruptSource != RunResumeSource.None ||
                corruptEncounter != null ||
                corruptFailure !=
                RunResumeFailure.ActiveCheckpointLoadFailed ||
                checkpointFailure !=
                ActiveBattleCheckpointFailure.InvalidData ||
                corruptRunFailure != RunProgressSaveFailure.None)
            {
                return false;
            }

            if (!ActiveBattleCheckpointService.TryClear(
                    checkpointPath,
                    out ActiveBattleCheckpointFailure clearFailure) ||
                clearFailure != ActiveBattleCheckpointFailure.None ||
                !TryLoadRunProgress(
                    checkpointPath,
                    runPath,
                    cardDatabase,
                    enchantDatabase,
                    encounterDatabase,
                    permanentRewards))
            {
                return false;
            }

            File.Delete(runPath);
            bool missingLoaded = RunResumeService.TryLoad(
                checkpointPath,
                runPath,
                cardDatabase,
                enchantDatabase,
                encounterDatabase,
                permanentRewards,
                out RunEncounterProgressState missingProgress,
                out RunResumeSource missingSource,
                out EncounterData missingEncounter,
                out RunResumeFailure missingFailure,
                out ActiveBattleCheckpointFailure missingCheckpointFailure,
                out RunProgressSaveFailure missingRunFailure);
            return !missingLoaded && missingProgress == null &&
                   missingSource == RunResumeSource.None &&
                   missingEncounter == null &&
                   missingFailure == RunResumeFailure.NotFound &&
                   missingCheckpointFailure ==
                   ActiveBattleCheckpointFailure.None &&
                   missingRunFailure == RunProgressSaveFailure.None;
        }

        private static bool TryLoadRunProgress(
            string checkpointPath,
            string runPath,
            CardDatabase cardDatabase,
            EnchantDatabase enchantDatabase,
            EncounterDatabase encounterDatabase,
            PlayerPermanentRewardState permanentRewards)
        {
            bool loaded = RunResumeService.TryLoad(
                checkpointPath,
                runPath,
                cardDatabase,
                enchantDatabase,
                encounterDatabase,
                permanentRewards,
                out RunEncounterProgressState restored,
                out RunResumeSource source,
                out EncounterData encounter,
                out RunResumeFailure failure,
                out ActiveBattleCheckpointFailure checkpointFailure,
                out RunProgressSaveFailure runFailure);
            return loaded && restored != null &&
                   !restored.HasActiveEncounter &&
                   restored.RunState.CurrentHealth == 27 &&
                   restored.RunState.Gold == 8 &&
                   restored.RunDeck.Count == 12 &&
                   source == RunResumeSource.RunProgress &&
                   encounter == null &&
                   failure == RunResumeFailure.None &&
                   checkpointFailure ==
                   ActiveBattleCheckpointFailure.None &&
                   runFailure == RunProgressSaveFailure.None;
        }

        private static bool TryLoadActiveCheckpoint(
            string checkpointPath,
            string runPath,
            EncounterData expectedEncounter,
            CardDatabase cardDatabase,
            EnchantDatabase enchantDatabase,
            EncounterDatabase encounterDatabase,
            PlayerPermanentRewardState permanentRewards)
        {
            bool loaded = RunResumeService.TryLoad(
                checkpointPath,
                runPath,
                cardDatabase,
                enchantDatabase,
                encounterDatabase,
                permanentRewards,
                out RunEncounterProgressState restored,
                out RunResumeSource source,
                out EncounterData encounter,
                out RunResumeFailure failure,
                out ActiveBattleCheckpointFailure checkpointFailure,
                out RunProgressSaveFailure runFailure);
            return loaded && restored?.ActiveEncounter != null &&
                   source == RunResumeSource.ActiveBattleCheckpoint &&
                   encounter == expectedEncounter &&
                   restored.ActiveEncounter.Encounter == expectedEncounter &&
                   restored.ActiveEncounter.StartParameters.BattleInstanceId ==
                   "TEST-BATTLE-56" &&
                   failure == RunResumeFailure.None &&
                   checkpointFailure ==
                   ActiveBattleCheckpointFailure.None &&
                   runFailure == RunProgressSaveFailure.None;
        }

        private static bool TryBegin(
            RunEncounterProgressState progress,
            EncounterData encounter)
        {
            bool begun = RunEncounterProgressService.TryBegin(
                progress,
                "TEST-BATTLE-56",
                encounter,
                560,
                5,
                Array.Empty<string>(),
                56,
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
                        new RunCardInstance(card, $"OWNED-56-{cardId}"),
                        out RunDeckFailure deckFailure) ||
                    deckFailure != RunDeckFailure.None)
                {
                    return null;
                }
            }

            return new RunEncounterProgressState(
                new RunBattleState(30, 27, 8),
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
                    $"Could not delete resume validation directory: {exception.Message}");
            }
        }
    }
}
