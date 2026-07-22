using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    // Focused persistence checks remain separate classes and menu commands,
    // but share one compilation unit for the prototype test harness.
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

    internal static class PlayerPermanentRewardSaveServiceValidation
    {
        [MenuItem("Have a Break/Validate Permanent Reward Save Load")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            if (valid)
            {
                Debug.Log("Permanent reward save and load passed.");
            }
            else
            {
                Debug.LogError("Permanent reward save and load failed.");
            }

            EditorUtility.DisplayDialog(
                "Permanent Reward Save Load Validation",
                valid
                    ? "Permanent reward save and load passed."
                    : "Permanent reward save and load failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            if (string.IsNullOrWhiteSpace(
                    PlayerPermanentRewardSaveService.DefaultPath) ||
                !ValidateSerializationFailures())
            {
                return false;
            }

            string directory = Path.Combine(
                Path.GetTempPath(),
                $"HaveABreak-PermanentReward-{Guid.NewGuid():N}");
            string path = Path.Combine(directory, "save.json");
            try
            {
                return ValidateFileRoundTrip(path);
            }
            finally
            {
                TryDeleteDirectory(directory);
            }
        }

        private static bool ValidateSerializationFailures()
        {
            bool nullSerialized =
                PlayerPermanentRewardSaveService.TrySerialize(
                    null,
                    out string nullJson,
                    out PlayerPermanentRewardSaveFailure nullFailure);
            bool corruptLoaded =
                PlayerPermanentRewardSaveService.TryDeserialize(
                    "{",
                    out PlayerPermanentRewardState corruptState,
                    out PlayerPermanentRewardSaveFailure corruptFailure);
            bool unsupportedLoaded =
                PlayerPermanentRewardSaveService.TryDeserialize(
                    "{\"schemaVersion\":2,\"rewardIds\":[]}",
                    out PlayerPermanentRewardState unsupportedState,
                    out PlayerPermanentRewardSaveFailure unsupportedFailure);
            bool duplicateLoaded =
                PlayerPermanentRewardSaveService.TryDeserialize(
                    "{\"schemaVersion\":1,\"rewardIds\":[\"R-A\",\"r-a\"]}",
                    out PlayerPermanentRewardState duplicateState,
                    out PlayerPermanentRewardSaveFailure duplicateFailure);
            bool blankLoaded =
                PlayerPermanentRewardSaveService.TryDeserialize(
                    "{\"schemaVersion\":1,\"rewardIds\":[\" \"]}",
                    out PlayerPermanentRewardState blankState,
                    out PlayerPermanentRewardSaveFailure blankFailure);

            return !nullSerialized && nullJson == null &&
                   nullFailure ==
                   PlayerPermanentRewardSaveFailure.InvalidState &&
                   !corruptLoaded && corruptState == null &&
                   corruptFailure ==
                   PlayerPermanentRewardSaveFailure.InvalidData &&
                   !unsupportedLoaded && unsupportedState == null &&
                   unsupportedFailure ==
                   PlayerPermanentRewardSaveFailure.UnsupportedVersion &&
                   !duplicateLoaded && duplicateState == null &&
                   duplicateFailure ==
                   PlayerPermanentRewardSaveFailure.DuplicateRewardId &&
                   !blankLoaded && blankState == null &&
                   blankFailure ==
                   PlayerPermanentRewardSaveFailure.InvalidData;
        }

        private static bool ValidateFileRoundTrip(string path)
        {
            bool missingLoaded = PlayerPermanentRewardSaveService.TryLoad(
                path,
                out PlayerPermanentRewardState missingState,
                out PlayerPermanentRewardSaveFailure missingFailure);
            if (missingLoaded || missingState != null ||
                missingFailure !=
                PlayerPermanentRewardSaveFailure.NotFound ||
                !TryCreateState(
                    new[] { "TEST-PERMANENT-50", "TEST-PERMANENT-51" },
                    out PlayerPermanentRewardState source))
            {
                return false;
            }

            bool saved = PlayerPermanentRewardSaveService.TrySave(
                source,
                path,
                out PlayerPermanentRewardSaveFailure saveFailure);
            bool loaded = PlayerPermanentRewardSaveService.TryLoad(
                path,
                out PlayerPermanentRewardState restored,
                out PlayerPermanentRewardSaveFailure loadFailure);
            if (!saved ||
                saveFailure != PlayerPermanentRewardSaveFailure.None ||
                !loaded || restored == null ||
                loadFailure != PlayerPermanentRewardSaveFailure.None ||
                restored.RewardIds.Count != 2 ||
                restored.RewardIds[0] != "TEST-PERMANENT-50" ||
                restored.RewardIds[1] != "TEST-PERMANENT-51" ||
                File.Exists(path + ".tmp") || File.Exists(path + ".bak"))
            {
                return false;
            }

            RunEncounterProgressState nextRun = new(
                new RunBattleState(30, 30, 0),
                new RunDeckState(),
                restored);
            if (nextRun.PermanentRewards != restored ||
                !nextRun.PermanentRewards.Contains("TEST-PERMANENT-50") ||
                !nextRun.PermanentRewards.Contains("TEST-PERMANENT-51"))
            {
                return false;
            }

            if (!TryCreateState(
                    new[] { "TEST-PERMANENT-52" },
                    out PlayerPermanentRewardState replacement) ||
                !PlayerPermanentRewardSaveService.TrySave(
                    replacement,
                    path,
                    out PlayerPermanentRewardSaveFailure replaceFailure) ||
                replaceFailure != PlayerPermanentRewardSaveFailure.None ||
                !PlayerPermanentRewardSaveService.TryLoad(
                    path,
                    out PlayerPermanentRewardState replaced,
                    out PlayerPermanentRewardSaveFailure reloadFailure))
            {
                return false;
            }

            return reloadFailure == PlayerPermanentRewardSaveFailure.None &&
                   replaced != null && replaced.RewardIds.Count == 1 &&
                   replaced.RewardIds[0] == "TEST-PERMANENT-52" &&
                   !File.Exists(path + ".tmp") &&
                   !File.Exists(path + ".bak");
        }

        private static bool TryCreateState(
            string[] rewardIds,
            out PlayerPermanentRewardState state)
        {
            string quotedIds = string.Join(
                ",",
                Array.ConvertAll(rewardIds, id => $"\"{id}\""));
            return PlayerPermanentRewardSaveService.TryDeserialize(
                $"{{\"schemaVersion\":1,\"rewardIds\":[{quotedIds}]}}",
                out state,
                out PlayerPermanentRewardSaveFailure failure) &&
                   failure == PlayerPermanentRewardSaveFailure.None;
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

    internal static class RunEncounterProgressServiceValidation
    {
        [MenuItem("Have a Break/Validate Run Encounter Progression")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            if (valid)
            {
                Debug.Log("Run encounter progression passed.");
            }
            else
            {
                Debug.LogError("Run encounter progression failed.");
            }

            EditorUtility.DisplayDialog(
                "Run Encounter Progression Validation",
                valid
                    ? "Run encounter gating, rewards, and terminal defeat passed."
                    : "Run encounter progression failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            EnchantData e01 = FindEnchant(TestContentIds.E01);
            EnchantData e03 = FindEnchant(TestContentIds.E03);
            EnchantData e06 = FindEnchant(TestContentIds.E06);
            if (e01 == null || e03 == null || e06 == null)
            {
                return false;
            }

            EnemyDefinitionData enemy =
                ScriptableObject.CreateInstance<EnemyDefinitionData>();
            EncounterData normalEncounter =
                ScriptableObject.CreateInstance<EncounterData>();
            EncounterData eliteEncounter =
                ScriptableObject.CreateInstance<EncounterData>();
            EncounterData finalBossEncounter =
                ScriptableObject.CreateInstance<EncounterData>();
            try
            {
                enemy.EditorInitialize(
                    "TEST-ENEMY-48",
                    "Test Run Progress Enemy",
                    0,
                    1);
                InitializeEncounter(
                    normalEncounter,
                    "TEST-ENCOUNTER-48-NORMAL",
                    BattleEncounterGrade.Normal,
                    enemy);
                InitializeEncounter(
                    eliteEncounter,
                    "TEST-ENCOUNTER-48-ELITE",
                    BattleEncounterGrade.Elite,
                    enemy);
                InitializeEncounter(
                    finalBossEncounter,
                    "TEST-ENCOUNTER-48-FINAL",
                    BattleEncounterGrade.FinalBoss,
                    enemy);

                return ValidateNormalProgressAndDefeat(
                           normalEncounter,
                           e01,
                           e03,
                           e06) &&
                       ValidateUnsupportedRewardGates(
                           eliteEncounter,
                           finalBossEncounter,
                           e01,
                           e03,
                           e06);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemy);
                UnityEngine.Object.DestroyImmediate(normalEncounter);
                UnityEngine.Object.DestroyImmediate(eliteEncounter);
                UnityEngine.Object.DestroyImmediate(finalBossEncounter);
            }
        }

        private static bool ValidateNormalProgressAndDefeat(
            EncounterData encounter,
            EnchantData e01,
            EnchantData e03,
            EnchantData e06)
        {
            RunEncounterProgressState progress = CreateProgress();
            if (progress == null ||
                !TryBegin(
                    progress,
                    "TEST-BATTLE-48-A",
                    encounter,
                    480,
                    out BattleRuntimeEncounterContext first))
            {
                return false;
            }

            if (!TryBeginRejected(
                    progress,
                    "TEST-BATTLE-48-B",
                    encounter,
                    RunEncounterProgressFailure.EncounterAlreadyActive) ||
                progress.ActiveEncounter != first ||
                progress.UsedBattleInstanceIds.Count != 1 ||
                !MakeVictory(first) ||
                !TrySettle(progress))
            {
                return false;
            }

            bool completedBeforeGold =
                RunEncounterProgressService.TryCompleteActive(
                    progress,
                    out RunEncounterProgressFailure beforeGoldFailure);
            bool goldClaimed = first.VictoryRewards.TryClaimGold(
                out BattleRewardFailure goldFailure);
            bool completedBeforeEnchant =
                RunEncounterProgressService.TryCompleteActive(
                    progress,
                    out RunEncounterProgressFailure beforeEnchantFailure);
            if (completedBeforeGold ||
                beforeGoldFailure !=
                RunEncounterProgressFailure.GoldRewardPending ||
                !goldClaimed || goldFailure != BattleRewardFailure.None ||
                completedBeforeEnchant ||
                beforeEnchantFailure !=
                RunEncounterProgressFailure.EnchantRewardPending ||
                !ClaimEnchantReward(
                    first,
                    progress.RunDeck,
                    e01,
                    e03,
                    e06))
            {
                return false;
            }

            bool firstCompleted =
                RunEncounterProgressService.TryCompleteActive(
                    progress,
                    out RunEncounterProgressFailure firstCompleteFailure);
            if (!firstCompleted ||
                firstCompleteFailure != RunEncounterProgressFailure.None ||
                progress.HasActiveEncounter ||
                progress.CompletedEncounterCount != 1 ||
                !TryBeginRejected(
                    progress,
                    "test-battle-48-a",
                    encounter,
                    RunEncounterProgressFailure.BattleInstanceAlreadyUsed) ||
                !TryBegin(
                    progress,
                    "TEST-BATTLE-48-B",
                    encounter,
                    481,
                    out BattleRuntimeEncounterContext second))
            {
                return false;
            }

            int currentHealth = second.Runtime.Player.CurrentHealth;
            if (currentHealth <= 0 ||
                second.Runtime.Player.ApplyDamage(currentHealth) !=
                currentHealth ||
                !TrySettle(progress))
            {
                return false;
            }

            bool defeatCompleted =
                RunEncounterProgressService.TryCompleteActive(
                    progress,
                    out RunEncounterProgressFailure defeatFailure);
            return defeatCompleted &&
                   defeatFailure == RunEncounterProgressFailure.None &&
                   progress.RunState.RunEnded &&
                   !progress.HasActiveEncounter &&
                   progress.CompletedEncounterCount == 2 &&
                   progress.UsedBattleInstanceIds.Count == 2 &&
                   TryBeginRejected(
                       progress,
                       "TEST-BATTLE-48-C",
                       encounter,
                       RunEncounterProgressFailure.RunEnded);
        }

        private static bool ValidateUnsupportedRewardGates(
            EncounterData eliteEncounter,
            EncounterData finalBossEncounter,
            EnchantData e01,
            EnchantData e03,
            EnchantData e06)
        {
            RunEncounterProgressState elite = CreateProgress();
            if (elite == null ||
                !TryBegin(
                    elite,
                    "TEST-BATTLE-48-ELITE",
                    eliteEncounter,
                    482,
                    out BattleRuntimeEncounterContext eliteContext) ||
                !MakeVictory(eliteContext) ||
                !TrySettle(elite) ||
                !eliteContext.VictoryRewards.TryClaimGold(out _) ||
                !ClaimEnchantReward(
                    eliteContext,
                    elite.RunDeck,
                    e01,
                    e03,
                    e06))
            {
                return false;
            }

            bool eliteCompleted =
                RunEncounterProgressService.TryCompleteActive(
                    elite,
                    out RunEncounterProgressFailure eliteFailure);
            if (eliteCompleted ||
                eliteFailure !=
                RunEncounterProgressFailure.ConsumableRewardPending ||
                !elite.HasActiveEncounter ||
                elite.CompletedEncounterCount != 0)
            {
                return false;
            }

            RunEncounterProgressState finalBoss = CreateProgress();
            if (finalBoss == null ||
                !TryBegin(
                    finalBoss,
                    "TEST-BATTLE-48-FINAL",
                    finalBossEncounter,
                    483,
                    out BattleRuntimeEncounterContext finalContext) ||
                !MakeVictory(finalContext) ||
                !TrySettle(finalBoss) ||
                finalContext.VictoryRewards.EnchantChoiceCount != 0 ||
                !finalContext.VictoryRewards.TryClaimGold(out _))
            {
                return false;
            }

            bool finalCompleted =
                RunEncounterProgressService.TryCompleteActive(
                    finalBoss,
                    out RunEncounterProgressFailure finalFailure);
            return !finalCompleted &&
                   finalFailure ==
                   RunEncounterProgressFailure.PermanentRewardPending &&
                   finalBoss.HasActiveEncounter &&
                   finalBoss.CompletedEncounterCount == 0;
        }

        private static bool TryBegin(
            RunEncounterProgressState progress,
            string battleInstanceId,
            EncounterData encounter,
            int shuffleSeed,
            out BattleRuntimeEncounterContext context)
        {
            bool created = RunEncounterProgressService.TryBegin(
                progress,
                battleInstanceId,
                encounter,
                shuffleSeed,
                5,
                Array.Empty<string>(),
                0,
                out context,
                out RunEncounterProgressFailure progressFailure,
                out BattleRuntimeEncounterFlowFailure flowFailure,
                out RunDeckFailure runDeckFailure,
                out BattleRuntimeBootstrapFailure bootstrapFailure,
                out BattleRuntimeSessionFailure sessionFailure,
                out StartingHandRedrawFailure redrawFailure,
                out BattleTurnFailure turnFailure,
                out List<string> validationErrors);
            return created && context != null &&
                   progressFailure == RunEncounterProgressFailure.None &&
                   flowFailure == BattleRuntimeEncounterFlowFailure.None &&
                   runDeckFailure == RunDeckFailure.None &&
                   bootstrapFailure == BattleRuntimeBootstrapFailure.None &&
                   sessionFailure == BattleRuntimeSessionFailure.None &&
                   redrawFailure == StartingHandRedrawFailure.None &&
                   turnFailure == BattleTurnFailure.None &&
                   validationErrors.Count == 0 &&
                   progress.ActiveEncounter == context;
        }

        private static bool TryBeginRejected(
            RunEncounterProgressState progress,
            string battleInstanceId,
            EncounterData encounter,
            RunEncounterProgressFailure expectedFailure)
        {
            bool created = RunEncounterProgressService.TryBegin(
                progress,
                battleInstanceId,
                encounter,
                489,
                5,
                Array.Empty<string>(),
                0,
                out BattleRuntimeEncounterContext context,
                out RunEncounterProgressFailure progressFailure,
                out BattleRuntimeEncounterFlowFailure flowFailure,
                out RunDeckFailure runDeckFailure,
                out BattleRuntimeBootstrapFailure bootstrapFailure,
                out BattleRuntimeSessionFailure sessionFailure,
                out StartingHandRedrawFailure redrawFailure,
                out BattleTurnFailure turnFailure,
                out List<string> validationErrors);
            return !created && context == null &&
                   progressFailure == expectedFailure &&
                   flowFailure == BattleRuntimeEncounterFlowFailure.None &&
                   runDeckFailure == RunDeckFailure.None &&
                   bootstrapFailure == BattleRuntimeBootstrapFailure.None &&
                   sessionFailure == BattleRuntimeSessionFailure.None &&
                   redrawFailure == StartingHandRedrawFailure.NotAvailable &&
                   turnFailure == BattleTurnFailure.None &&
                   validationErrors.Count == 0;
        }

        private static bool TrySettle(
            RunEncounterProgressState progress)
        {
            bool settled = RunEncounterProgressService.TrySettleActive(
                progress,
                out RunEncounterProgressFailure progressFailure,
                out BattleRuntimeEncounterFlowFailure flowFailure,
                out BattleRuntimeSessionFailure sessionFailure,
                out BattleSettlementFailure settlementFailure);
            return settled &&
                   progressFailure == RunEncounterProgressFailure.None &&
                   flowFailure == BattleRuntimeEncounterFlowFailure.None &&
                   sessionFailure == BattleRuntimeSessionFailure.None &&
                   settlementFailure == BattleSettlementFailure.None;
        }

        private static bool MakeVictory(
            BattleRuntimeEncounterContext context)
        {
            BattleEnemyRuntimeState enemy =
                context?.Runtime.FindEnemy("TEST-ENEMY-48-A");
            return enemy != null &&
                   enemy.Vital.ApplyDamage(enemy.Vital.CurrentHealth) == 1 &&
                   context.Runtime.LivingEnemies.TryRemove(enemy.EnemyId);
        }

        private static bool ClaimEnchantReward(
            BattleRuntimeEncounterContext context,
            RunDeckState runDeck,
            EnchantData e01,
            EnchantData e03,
            EnchantData e06)
        {
            bool created = BattleVictoryEnchantRewardService.TryCreate(
                context,
                runDeck,
                new[] { e01, e03, e06 },
                out BattleVictoryEnchantRewardService reward,
                out BattleVictoryEnchantRewardFailure createFailure);
            RunCardInstance target = runDeck.Cards.FirstOrDefault(
                card => card?.Enchants != null &&
                        card.Enchants.HasImmediateAttachmentTarget(e01));
            if (!created || reward == null || target == null ||
                createFailure != BattleVictoryEnchantRewardFailure.None)
            {
                return false;
            }

            bool claimed = reward.TryClaim(
                e01.DefinitionId,
                target.OwnedCardId,
                0,
                out EnchantAttachmentFailure attachmentFailure,
                out BattleVictoryEnchantRewardFailure claimFailure);
            return claimed &&
                   attachmentFailure == EnchantAttachmentFailure.None &&
                   claimFailure == BattleVictoryEnchantRewardFailure.None;
        }

        private static RunEncounterProgressState CreateProgress()
        {
            RunDeckState runDeck = new();
            for (int number = 1; number <= 12; number++)
            {
                string catalogCardId = $"C{number:00}";
                CardData card = FindCard(catalogCardId);
                if (card == null)
                {
                    return null;
                }

                bool added = runDeck.TryAdd(
                    new RunCardInstance(
                        card,
                        $"OWNED-48-{catalogCardId}"),
                    out RunDeckFailure failure);
                if (!added || failure != RunDeckFailure.None)
                {
                    return null;
                }
            }

            return new RunEncounterProgressState(
                new RunBattleState(30, 24, 5),
                runDeck);
        }

        private static void InitializeEncounter(
            EncounterData encounter,
            string encounterId,
            BattleEncounterGrade grade,
            EnemyDefinitionData enemy)
        {
            encounter.EditorInitialize(
                encounterId,
                encounterId,
                grade,
                new[]
                {
                    new EncounterEnemySlot(
                        "TEST-ENEMY-48-A",
                        enemy,
                        EnemyFieldPosition.Center)
                });
        }

        private static CardData FindCard(string catalogCardId)
        {
            return AssetDatabase.FindAssets("t:CardData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<CardData>)
                .FirstOrDefault(card => card != null && string.Equals(
                    card.CatalogCardId,
                    catalogCardId,
                    StringComparison.OrdinalIgnoreCase));
        }

        private static EnchantData FindEnchant(string definitionId)
        {
            return AssetDatabase.FindAssets("t:EnchantData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<EnchantData>)
                .FirstOrDefault(enchant => enchant != null && string.Equals(
                    enchant.DefinitionId,
                    definitionId,
                    StringComparison.OrdinalIgnoreCase));
        }
    }

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

            EnchantData e01 = enchantDatabase.Find(TestContentIds.E01);
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
                   slot.Enchant.DefinitionId == TestContentIds.E01 &&
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

    internal static class RunActionConfirmationPolicyValidation
    {
        internal static bool Validate()
        {
            return !RunActionConfirmationPolicy.ShouldConfirmNewRun(
                       false,
                       true,
                       RunSaveSlotState.Empty) &&
                   RunActionConfirmationPolicy.ShouldConfirmNewRun(
                       true,
                       true,
                       RunSaveSlotState.Empty) &&
                   RunActionConfirmationPolicy.ShouldConfirmNewRun(
                       false,
                       false,
                       RunSaveSlotState.Empty) &&
                   RunActionConfirmationPolicy.ShouldConfirmNewRun(
                       false,
                       true,
                       RunSaveSlotState.RunProgress) &&
                   RunActionConfirmationPolicy.ShouldConfirmNewRun(
                       false,
                       true,
                       RunSaveSlotState.ActiveBattleCheckpoint) &&
                   RunActionConfirmationPolicy.ShouldConfirmNewRun(
                       false,
                       true,
                       RunSaveSlotState.InvalidRunProgress) &&
                   RunActionConfirmationPolicy.ShouldConfirmContinue(
                       true,
                       RunCampaignPhase.Battle) &&
                   !RunActionConfirmationPolicy.ShouldConfirmContinue(
                       false,
                       RunCampaignPhase.Battle) &&
                   !RunActionConfirmationPolicy.ShouldConfirmContinue(
                       true,
                       RunCampaignPhase.NodeSelection);
        }
    }
}
