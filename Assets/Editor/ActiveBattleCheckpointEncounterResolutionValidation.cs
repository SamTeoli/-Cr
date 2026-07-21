using System;
using System.Collections.Generic;
using System.IO;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class ActiveBattleCheckpointEncounterResolutionValidation
    {
        private const string CardDatabasePath =
            "Assets/GameData/CardDatabase.asset";
        private const string EnchantDatabasePath =
            "Assets/GameData/EnchantDatabase.asset";

        [MenuItem("Have a Break/Validate Checkpoint Encounter Resolution")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            Debug.Log(valid
                ? "Checkpoint encounter resolution passed."
                : "Checkpoint encounter resolution failed.");
            EditorUtility.DisplayDialog(
                "Checkpoint Encounter Resolution Validation",
                valid
                    ? "Checkpoint encounter resolution passed."
                    : "Checkpoint encounter resolution failed. Check the Console.",
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
            EncounterData duplicate =
                ScriptableObject.CreateInstance<EncounterData>();
            EncounterDatabase encounterDatabase =
                ScriptableObject.CreateInstance<EncounterDatabase>();
            string directory = Path.Combine(
                Path.GetTempPath(),
                $"HaveABreak-EncounterResolution-{Guid.NewGuid():N}");
            string path = Path.Combine(directory, "checkpoint.json");
            try
            {
                enemy.EditorInitialize(
                    "TEST-ENEMY-55",
                    "Test Resolution Enemy",
                    3,
                    18);
                InitializeEncounter(encounter, enemy);
                InitializeEncounter(duplicate, enemy);
                return ValidateResolution(
                    source,
                    encounter,
                    duplicate,
                    encounterDatabase,
                    cardDatabase,
                    enchantDatabase,
                    permanentRewards,
                    path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemy);
                UnityEngine.Object.DestroyImmediate(encounter);
                UnityEngine.Object.DestroyImmediate(duplicate);
                UnityEngine.Object.DestroyImmediate(encounterDatabase);
                TryDeleteDirectory(directory);
            }
        }

        private static bool ValidateResolution(
            RunEncounterProgressState source,
            EncounterData encounter,
            EncounterData duplicate,
            EncounterDatabase encounterDatabase,
            CardDatabase cardDatabase,
            EnchantDatabase enchantDatabase,
            PlayerPermanentRewardState permanentRewards,
            string path)
        {
            encounterDatabase.EditorSetEncounters(new[] { encounter });
            if (encounterDatabase.GetValidationErrors().Count != 0 ||
                !TryBegin(source, encounter) ||
                !ActiveBattleCheckpointService.TrySave(
                    source,
                    path,
                    out ActiveBattleCheckpointFailure saveFailure) ||
                saveFailure != ActiveBattleCheckpointFailure.None)
            {
                return false;
            }

            encounterDatabase.EditorSetEncounters(Array.Empty<EncounterData>());
            bool missingLoaded = ActiveBattleCheckpointService.TryLoad(
                path,
                cardDatabase,
                enchantDatabase,
                permanentRewards,
                encounterDatabase,
                out RunEncounterProgressState missingProgress,
                out EncounterData missingEncounter,
                out ActiveBattleCheckpointFailure missingFailure);
            if (missingLoaded || missingProgress != null ||
                missingEncounter != null ||
                missingFailure !=
                ActiveBattleCheckpointFailure.EncounterNotFound)
            {
                return false;
            }

            encounterDatabase.EditorSetEncounters(
                new[] { encounter, duplicate });
            bool duplicateLoaded = ActiveBattleCheckpointService.TryLoad(
                path,
                cardDatabase,
                enchantDatabase,
                permanentRewards,
                encounterDatabase,
                out RunEncounterProgressState duplicateProgress,
                out EncounterData duplicateEncounter,
                out ActiveBattleCheckpointFailure duplicateFailure);
            if (duplicateLoaded || duplicateProgress != null ||
                duplicateEncounter != null ||
                duplicateFailure !=
                ActiveBattleCheckpointFailure.InvalidEncounterDatabase)
            {
                return false;
            }

            encounterDatabase.EditorSetEncounters(new[] { encounter });
            bool loaded = ActiveBattleCheckpointService.TryLoad(
                path,
                cardDatabase,
                enchantDatabase,
                permanentRewards,
                encounterDatabase,
                out RunEncounterProgressState restored,
                out EncounterData resolvedEncounter,
                out ActiveBattleCheckpointFailure loadFailure);
            return loaded && restored?.ActiveEncounter != null &&
                   resolvedEncounter == encounter &&
                   loadFailure == ActiveBattleCheckpointFailure.None &&
                   restored.ActiveEncounter.Encounter == encounter &&
                   restored.ActiveEncounter.StartParameters.EncounterId ==
                   "TEST-ENCOUNTER-55" &&
                   restored.ActiveEncounter.StartParameters.BattleInstanceId ==
                   "TEST-BATTLE-55" &&
                   restored.ActiveEncounter.Runtime.Enemies.Count == 1 &&
                   restored.ActiveEncounter.Runtime.Enemies[0].Vital
                       .CurrentHealth == 18 &&
                   restored.RunDeck.Count == 12 &&
                   restored.UsedBattleInstanceIds.Count == 1;
        }

        private static void InitializeEncounter(
            EncounterData encounter,
            EnemyDefinitionData enemy)
        {
            encounter.EditorInitialize(
                "TEST-ENCOUNTER-55",
                "Test Resolution Encounter",
                BattleEncounterGrade.Normal,
                new[]
                {
                    new EncounterEnemySlot(
                        "TEST-ENEMY-55-A",
                        enemy,
                        EnemyFieldPosition.Center)
                });
        }

        private static bool TryBegin(
            RunEncounterProgressState progress,
            EncounterData encounter)
        {
            bool begun = RunEncounterProgressService.TryBegin(
                progress,
                "TEST-BATTLE-55",
                encounter,
                550,
                5,
                Array.Empty<string>(),
                55,
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
                        new RunCardInstance(card, $"OWNED-55-{cardId}"),
                        out RunDeckFailure failure) ||
                    failure != RunDeckFailure.None)
                {
                    return null;
                }
            }

            return new RunEncounterProgressState(
                new RunBattleState(30, 26, 7),
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
                    $"Could not delete resolution validation directory: {exception.Message}");
            }
        }
    }
}
