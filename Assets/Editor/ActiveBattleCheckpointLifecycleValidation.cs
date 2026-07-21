using System;
using System.Collections.Generic;
using System.IO;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class ActiveBattleCheckpointLifecycleValidation
    {
        private const string CardDatabasePath =
            "Assets/GameData/CardDatabase.asset";

        [MenuItem("Have a Break/Validate Active Battle Checkpoint Lifecycle")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            Debug.Log(valid
                ? "Active battle checkpoint lifecycle passed."
                : "Active battle checkpoint lifecycle failed.");
            EditorUtility.DisplayDialog(
                "Active Battle Checkpoint Lifecycle Validation",
                valid
                    ? "Active battle checkpoint lifecycle passed."
                    : "Active battle checkpoint lifecycle failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            CardDatabase database =
                AssetDatabase.LoadAssetAtPath<CardDatabase>(
                    CardDatabasePath);
            RunEncounterProgressState progress = CreateProgress(database);
            if (progress == null)
            {
                return false;
            }

            EnemyDefinitionData enemy =
                ScriptableObject.CreateInstance<EnemyDefinitionData>();
            EncounterData encounter =
                ScriptableObject.CreateInstance<EncounterData>();
            string directory = Path.Combine(
                Path.GetTempPath(),
                $"HaveABreak-CheckpointLifecycle-{Guid.NewGuid():N}");
            string path = Path.Combine(directory, "checkpoint.json");
            try
            {
                enemy.EditorInitialize(
                    "TEST-ENEMY-54",
                    "Test Lifecycle Enemy",
                    0,
                    10);
                encounter.EditorInitialize(
                    "TEST-ENCOUNTER-54",
                    "Test Lifecycle Encounter",
                    BattleEncounterGrade.Normal,
                    new[]
                    {
                        new EncounterEnemySlot(
                            "TEST-ENEMY-54-A",
                            enemy,
                            EnemyFieldPosition.Center)
                    });

                return ValidateLifecycle(progress, encounter, path) &&
                       ValidateCorruptCleanup(path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemy);
                UnityEngine.Object.DestroyImmediate(encounter);
                TryDeleteDirectory(directory);
            }
        }

        private static bool ValidateLifecycle(
            RunEncounterProgressState progress,
            EncounterData encounter,
            string path)
        {
            if (!TryBegin(progress, encounter) ||
                !ActiveBattleCheckpointService.TrySave(
                    progress,
                    path,
                    out ActiveBattleCheckpointFailure saveFailure) ||
                saveFailure != ActiveBattleCheckpointFailure.None ||
                !ActiveBattleCheckpointService.Exists(path) ||
                !ActiveBattleCheckpointService.TryReadInfo(
                    path,
                    out ActiveBattleCheckpointInfo info,
                    out ActiveBattleCheckpointFailure infoFailure) ||
                infoFailure != ActiveBattleCheckpointFailure.None ||
                info.BattleInstanceId != "TEST-BATTLE-54" ||
                info.EncounterId != "TEST-ENCOUNTER-54" ||
                info.ShuffleSeed != 540 ||
                info.MaximumMana != 5 ||
                info.RewardSeed != 54)
            {
                return false;
            }

            string validJson = File.ReadAllText(path);
            File.WriteAllText(
                path,
                validJson.Replace(
                    "TEST-BATTLE-54",
                    "OTHER-BATTLE-54"));
            bool mismatched =
                ActiveBattleCheckpointLifecycleService.TryCompleteAndClear(
                    progress,
                    path,
                    out RunEncounterProgressFailure mismatchProgressFailure,
                    out ActiveBattleCheckpointFailure mismatchFailure);
            if (mismatched ||
                mismatchProgressFailure !=
                RunEncounterProgressFailure.None ||
                mismatchFailure !=
                ActiveBattleCheckpointFailure.CheckpointMismatch ||
                !progress.HasActiveEncounter)
            {
                return false;
            }

            File.WriteAllText(path, validJson);

            bool earlyCompleted =
                ActiveBattleCheckpointLifecycleService.TryCompleteAndClear(
                    progress,
                    path,
                    out RunEncounterProgressFailure earlyProgressFailure,
                    out ActiveBattleCheckpointFailure earlyCheckpointFailure);
            if (earlyCompleted ||
                earlyProgressFailure !=
                RunEncounterProgressFailure.SettlementNotComplete ||
                earlyCheckpointFailure !=
                ActiveBattleCheckpointFailure.None ||
                !ActiveBattleCheckpointService.Exists(path))
            {
                return false;
            }

            int health = progress.ActiveEncounter.Runtime.Player.CurrentHealth;
            if (health <= 0 ||
                progress.ActiveEncounter.Runtime.Player.ApplyDamage(health) !=
                health ||
                !RunEncounterProgressService.TrySettleActive(
                    progress,
                    out RunEncounterProgressFailure settleProgressFailure,
                    out BattleRuntimeEncounterFlowFailure flowFailure,
                    out BattleRuntimeSessionFailure sessionFailure,
                    out BattleSettlementFailure settlementFailure) ||
                settleProgressFailure != RunEncounterProgressFailure.None ||
                flowFailure != BattleRuntimeEncounterFlowFailure.None ||
                sessionFailure != BattleRuntimeSessionFailure.None ||
                settlementFailure != BattleSettlementFailure.None)
            {
                return false;
            }

            bool completed =
                ActiveBattleCheckpointLifecycleService.TryCompleteAndClear(
                    progress,
                    path,
                    out RunEncounterProgressFailure completeFailure,
                    out ActiveBattleCheckpointFailure clearFailure);
            return completed &&
                   completeFailure == RunEncounterProgressFailure.None &&
                   clearFailure == ActiveBattleCheckpointFailure.None &&
                   progress.RunState.RunEnded &&
                   !progress.HasActiveEncounter &&
                   progress.CompletedEncounterCount == 1 &&
                   !ActiveBattleCheckpointService.Exists(path) &&
                   ActiveBattleCheckpointService.TryClear(
                       path,
                       out ActiveBattleCheckpointFailure secondClearFailure) &&
                   secondClearFailure == ActiveBattleCheckpointFailure.None;
        }

        private static bool ValidateCorruptCleanup(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, "{not-valid-json");
            File.WriteAllText(path + ".tmp", "temporary");
            File.WriteAllText(path + ".bak", "backup");
            bool read = ActiveBattleCheckpointService.TryReadInfo(
                path,
                out ActiveBattleCheckpointInfo info,
                out ActiveBattleCheckpointFailure readFailure);
            bool cleared = ActiveBattleCheckpointService.TryClear(
                path,
                out ActiveBattleCheckpointFailure clearFailure);
            return !read && info == null &&
                   readFailure == ActiveBattleCheckpointFailure.InvalidData &&
                   cleared &&
                   clearFailure == ActiveBattleCheckpointFailure.None &&
                   !File.Exists(path) &&
                   !File.Exists(path + ".tmp") &&
                   !File.Exists(path + ".bak");
        }

        private static bool TryBegin(
            RunEncounterProgressState progress,
            EncounterData encounter)
        {
            bool begun = RunEncounterProgressService.TryBegin(
                progress,
                "TEST-BATTLE-54",
                encounter,
                540,
                5,
                Array.Empty<string>(),
                54,
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
            CardDatabase database)
        {
            if (database == null)
            {
                return null;
            }

            RunDeckState deck = new();
            for (int number = 1; number <= 12; number++)
            {
                string cardId = $"C{number:00}";
                if (!database.TryGetCard(cardId, out CardData card) ||
                    !deck.TryAdd(
                        new RunCardInstance(card, $"OWNED-54-{cardId}"),
                        out RunDeckFailure failure) ||
                    failure != RunDeckFailure.None)
                {
                    return null;
                }
            }

            return new RunEncounterProgressState(
                new RunBattleState(30, 30, 0),
                deck,
                new PlayerPermanentRewardState());
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
                    $"Could not delete lifecycle validation directory: {exception.Message}");
            }
        }
    }
}
