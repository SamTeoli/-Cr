using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class EnemyEncounterAndBootstrapValidation
    {
        [MenuItem("Have a Break/Validate Enemy And Encounter Data")]
        private static void ValidateAssetsFromMenu()
        {
            int enemyCount = 0;
            int encounterCount = 0;
            List<string> errors = new();
            foreach (EnemyDefinitionData enemy in FindAssets<EnemyDefinitionData>())
            {
                enemyCount++;
                errors.AddRange(
                    EncounterDataValidationService.ValidateEnemy(enemy));
            }

            foreach (EncounterData encounter in FindAssets<EncounterData>())
            {
                encounterCount++;
                errors.AddRange(
                    EncounterDataValidationService.ValidateEncounter(encounter));
            }

            bool valid = errors.Count == 0;
            foreach (string error in errors)
            {
                Debug.LogError(error);
            }

            EditorUtility.DisplayDialog(
                "Enemy And Encounter Data Validation",
                valid
                    ? $"Enemy and encounter data passed ({enemyCount} enemy(s), {encounterCount} encounter(s))."
                    : $"Enemy and encounter data failed with {errors.Count} error(s). Check the Console.",
                "OK");
        }

        [MenuItem("Have a Break/Validate Battle Runtime Bootstrap")]
        private static void ValidateBootstrapFromMenu()
        {
            bool valid = Validate();
            if (valid)
            {
                Debug.Log("Battle runtime bootstrap passed.");
            }
            else
            {
                Debug.LogError("Battle runtime bootstrap failed.");
            }

            EditorUtility.DisplayDialog(
                "Battle Runtime Bootstrap Validation",
                valid
                    ? "Battle runtime bootstrap passed."
                    : "Battle runtime bootstrap failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            CardData c01 = FindCard("C01");
            if (c01 == null)
            {
                return false;
            }

            EnemyDefinitionData validEnemy =
                ScriptableObject.CreateInstance<EnemyDefinitionData>();
            EnemyDefinitionData invalidEnemy =
                ScriptableObject.CreateInstance<EnemyDefinitionData>();
            EncounterData validEncounter =
                ScriptableObject.CreateInstance<EncounterData>();
            EncounterData invalidEncounter =
                ScriptableObject.CreateInstance<EncounterData>();
            try
            {
                validEnemy.EditorInitialize(
                    "TEST-ENEMY-43",
                    "Test Enemy",
                    2,
                    7);
                invalidEnemy.EditorInitialize(null, null, -1, 0);
                validEncounter.EditorInitialize(
                    "TEST-ENCOUNTER-43",
                    "Test Encounter",
                    new[]
                    {
                        new EncounterEnemySlot(
                            "TEST-ENEMY-43-A",
                            validEnemy,
                            EnemyFieldPosition.Center)
                    });
                invalidEncounter.EditorInitialize(
                    "TEST-ENCOUNTER-43-INVALID",
                    "Invalid Test Encounter",
                    new[]
                    {
                        new EncounterEnemySlot(
                            "DUPLICATE-43",
                            validEnemy,
                            EnemyFieldPosition.Left),
                        new EncounterEnemySlot(
                            "DUPLICATE-43",
                            validEnemy,
                            EnemyFieldPosition.Left)
                    });

                return ValidateDefinitions(
                           validEnemy,
                           invalidEnemy,
                           validEncounter,
                           invalidEncounter) &&
                       ValidateBootstrap(c01, validEnemy, validEncounter) &&
                       ValidateRejectedBootstrap(c01, invalidEncounter);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(validEnemy);
                UnityEngine.Object.DestroyImmediate(invalidEnemy);
                UnityEngine.Object.DestroyImmediate(validEncounter);
                UnityEngine.Object.DestroyImmediate(invalidEncounter);
            }
        }

        private static bool ValidateDefinitions(
            EnemyDefinitionData validEnemy,
            EnemyDefinitionData invalidEnemy,
            EncounterData validEncounter,
            EncounterData invalidEncounter)
        {
            return EncounterDataValidationService
                       .ValidateEnemy(validEnemy).Count == 0 &&
                   EncounterDataValidationService
                       .ValidateEncounter(validEncounter).Count == 0 &&
                   EncounterDataValidationService
                       .ValidateEnemy(invalidEnemy).Count >= 4 &&
                   EncounterDataValidationService
                       .ValidateEncounter(invalidEncounter).Count >= 2;
        }

        private static bool ValidateBootstrap(
            CardData card,
            EnemyDefinitionData enemy,
            EncounterData encounter)
        {
            BattleCardInstance cardInstance = Instance(card, "VALID");
            RunBattleState run = new(30, 23, 0);
            bool created = BattleRuntimeBootstrapService.TryCreate(
                new[] { cardInstance },
                run,
                encounter,
                431,
                5,
                out BattleRuntimeBootstrapResult result,
                out BattleRuntimeBootstrapFailure failure,
                out List<string> errors);
            BattleEnemyRuntimeState runtimeEnemy =
                result?.Runtime?.FindEnemy("TEST-ENEMY-43-A");

            return created &&
                   failure == BattleRuntimeBootstrapFailure.None &&
                   errors.Count == 0 &&
                   result != null &&
                   result.Encounter == encounter &&
                   result.Session != null &&
                   result.Session.Runtime == result.Runtime &&
                   !result.Session.Started &&
                   result.Runtime.Player.MaximumHealth == 30 &&
                   result.Runtime.Player.CurrentHealth == 23 &&
                   result.Runtime.Deck.Zones.Find(
                       cardInstance.Ids.BattleCardId) == cardInstance &&
                   runtimeEnemy != null &&
                   runtimeEnemy.Attack == enemy.Attack &&
                   runtimeEnemy.Vital.CurrentHealth == enemy.MaximumHealth &&
                   result.Runtime.EnemyPositions.FindPosition(
                       runtimeEnemy.EnemyId) == EnemyFieldPosition.Center;
        }

        private static bool ValidateRejectedBootstrap(
            CardData card,
            EncounterData invalidEncounter)
        {
            RunBattleState validRun = new(30, 30, 0);
            bool emptyDeckRejected =
                !BattleRuntimeBootstrapService.TryCreate(
                    Array.Empty<BattleCardInstance>(),
                    validRun,
                    invalidEncounter,
                    432,
                    5,
                    out _,
                    out BattleRuntimeBootstrapFailure emptyDeckFailure,
                    out List<string> emptyDeckErrors) &&
                emptyDeckFailure ==
                BattleRuntimeBootstrapFailure.InvalidDeck &&
                emptyDeckErrors.Count > 0;

            bool runRejected =
                !BattleRuntimeBootstrapService.TryCreate(
                    new[] { Instance(card, "RUN-INVALID") },
                    new RunBattleState(30, 0, 0),
                    invalidEncounter,
                    433,
                    5,
                    out _,
                    out BattleRuntimeBootstrapFailure runFailure,
                    out _) &&
                runFailure ==
                BattleRuntimeBootstrapFailure.InvalidRunState;

            bool encounterRejected =
                !BattleRuntimeBootstrapService.TryCreate(
                    new[] { Instance(card, "ENCOUNTER-INVALID") },
                    validRun,
                    invalidEncounter,
                    434,
                    5,
                    out _,
                    out BattleRuntimeBootstrapFailure encounterFailure,
                    out List<string> encounterErrors) &&
                encounterFailure ==
                BattleRuntimeBootstrapFailure.InvalidEncounter &&
                encounterErrors.Count >= 2;

            return emptyDeckRejected && runRejected && encounterRejected;
        }

        private static BattleCardInstance Instance(
            CardData card,
            string suffix)
        {
            return new BattleCardInstance(
                card,
                new CardInstanceIds(
                    card.CatalogCardId,
                    $"OWNED-RUNTIME-43-{suffix}",
                    $"BATTLE-RUNTIME-43-{suffix}"),
                1,
                CardZone.DrawPile);
        }

        private static IEnumerable<T> FindAssets<T>()
            where T : UnityEngine.Object
        {
            return AssetDatabase.FindAssets($"t:{typeof(T).Name}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(asset => asset != null);
        }

        private static CardData FindCard(string catalogCardId)
        {
            return FindAssets<CardData>()
                .FirstOrDefault(card => string.Equals(
                    card.CatalogCardId,
                    catalogCardId,
                    StringComparison.OrdinalIgnoreCase));
        }
    }
}
