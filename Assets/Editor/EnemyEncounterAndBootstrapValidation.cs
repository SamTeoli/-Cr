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

        [MenuItem("Have a Break/Validate Enemy Action Patterns")]
        private static void ValidateActionPatternsFromMenu()
        {
            bool valid = Validate();
            EditorUtility.DisplayDialog(
                "Enemy Action Pattern Validation",
                valid
                    ? "Enemy action pattern flow passed."
                    : "Enemy action pattern flow failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            CardData c01 = FindCard(TestContentIds.C01);
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
                validEnemy.EditorSetActionPattern(
                    new EnemyActionPatternData(
                        new[]
                        {
                            new EnemyTurnPatternStep(
                                true,
                                EnemyMoveDirection.Right,
                                1,
                                2,
                                new[]
                                {
                                    new EnemyPatternAbilityData(
                                        "TEST-ABILITY-43",
                                        true,
                                        false)
                                }),
                            new EnemyTurnPatternStep(
                                false,
                                EnemyMoveDirection.Left,
                                1,
                                1)
                        }));
                invalidEnemy.EditorInitialize(null, null, -1, 0);
                invalidEnemy.EditorSetActionPattern(
                    new EnemyActionPatternData(
                        new[]
                        {
                            new EnemyTurnPatternStep(
                                true,
                                (EnemyMoveDirection)99,
                                0,
                                -1,
                                new[]
                                {
                                    new EnemyPatternAbilityData(
                                        null,
                                        true,
                                        false)
                                }),
                            new EnemyTurnPatternStep(
                                false,
                                EnemyMoveDirection.Left,
                                1,
                                0)
                        }));
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
                       ValidateActionPatternRound(c01, validEncounter) &&
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
            bool firstTurnFound = validEnemy.ActionPattern.TryGetTurn(
                1,
                out EnemyTurnPatternStep firstTurn);
            bool repeatedTurnFound = validEnemy.ActionPattern.TryGetTurn(
                3,
                out EnemyTurnPatternStep repeatedTurn);
            return firstTurnFound &&
                   repeatedTurnFound &&
                   firstTurn == repeatedTurn &&
                   firstTurn.Moves &&
                   firstTurn.MoveDirection == EnemyMoveDirection.Right &&
                   firstTurn.MoveSteps == 1 &&
                   firstTurn.AttackCount == 2 &&
                   firstTurn.Abilities.Count == 1 &&
                   firstTurn.Abilities[0].AbilityId == "TEST-ABILITY-43" &&
                   EncounterDataValidationService
                       .ValidateEnemy(validEnemy).Count == 0 &&
                   EncounterDataValidationService
                       .ValidateEncounter(validEncounter).Count == 0 &&
                   EncounterDataValidationService
                       .ValidateEnemy(invalidEnemy).Count >= 8 &&
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

        private static bool ValidateActionPatternRound(
            CardData card,
            EncounterData encounter)
        {
            BattleRuntimeBootstrapService.TryCreate(
                new[] { Instance(card, "PATTERN") },
                new RunBattleState(30, 23, 0),
                encounter,
                435,
                5,
                out BattleRuntimeBootstrapResult bootstrap,
                out BattleRuntimeBootstrapFailure bootstrapFailure,
                out List<string> errors);
            BattleRuntimeSessionState session = bootstrap?.Session;
            if (bootstrapFailure != BattleRuntimeBootstrapFailure.None ||
                errors.Count != 0 || session == null ||
                !BattleRuntimeSessionService.TryBegin(
                    session,
                    Array.Empty<string>(),
                    out _,
                    out BattleRuntimeSessionFailure beginFailure,
                    out StartingHandRedrawFailure redrawFailure,
                    out BattleTurnFailure beginTurnFailure) ||
                beginFailure != BattleRuntimeSessionFailure.None ||
                redrawFailure != StartingHandRedrawFailure.None ||
                beginTurnFailure != BattleTurnFailure.None ||
                !BattleRuntimeEnemyPatternService.TryCreateCommands(
                    session,
                    encounter,
                    700,
                    out List<BattleRuntimeEnemyTurnCommand> commands,
                    out BattleRuntimeEnemyPatternFailure commandFailure) ||
                commandFailure != BattleRuntimeEnemyPatternFailure.None ||
                commands.Count != 3 ||
                commands[0].ActionType !=
                BattleRuntimeEnemyTurnActionType.Move ||
                commands[1].ActionType !=
                BattleRuntimeEnemyTurnActionType.Attack ||
                commands[1].AutomaticAttackCount != 2 ||
                commands[1].AttackTieBreakerValues.Count != 2 ||
                commands[1].AttackTieBreakerValues[0] != 700 ||
                commands[1].AttackTieBreakerValues[1] != 701 ||
                commands[2].ActionType !=
                BattleRuntimeEnemyTurnActionType.Ability ||
                commands[2].Ability.AbilityId != "TEST-ABILITY-43")
            {
                return false;
            }

            if (!BattleRuntimeEnemyPatternService.TryEndPlayerTurn(
                    session,
                    encounter,
                    700,
                    out BattleRuntimeSessionRoundResult round,
                    out BattleRuntimeEnemyPatternFailure patternFailure,
                    out BattleRuntimeSessionFailure sessionFailure,
                    out BattleRuntimeRoundFailure roundFailure,
                    out BattleTurnFailure playerTurnEndFailure,
                    out BattleRuntimeEnemyTurnPipelineFailure pipelineFailure,
                    out BattleRuntimeEnemyTurnPlanFailure planFailure,
                    out BattleRuntimeEnemyTurnFailure enemyTurnFailure,
                    out int failedActionIndex))
            {
                return false;
            }

            if (patternFailure != BattleRuntimeEnemyPatternFailure.None ||
                sessionFailure != BattleRuntimeSessionFailure.None ||
                roundFailure != BattleRuntimeRoundFailure.None ||
                playerTurnEndFailure != BattleTurnFailure.None ||
                pipelineFailure !=
                BattleRuntimeEnemyTurnPipelineFailure.None ||
                planFailure != BattleRuntimeEnemyTurnPlanFailure.None ||
                enemyTurnFailure != BattleRuntimeEnemyTurnFailure.None ||
                failedActionIndex != -1 || round == null ||
                round.ProcessedEnemyActionCount != 3 ||
                bootstrap.Runtime.Player.CurrentHealth != 19 ||
                bootstrap.Runtime.EnemyPositions.FindPosition(
                    "TEST-ENEMY-43-A") != EnemyFieldPosition.Right ||
                bootstrap.Runtime.Turn.PlayerTurnNumber != 2)
            {
                return false;
            }

            return BattleRuntimeEnemyPatternService.TryCreateCommands(
                       session,
                       encounter,
                       800,
                       out List<BattleRuntimeEnemyTurnCommand> secondTurn,
                       out patternFailure) &&
                   patternFailure ==
                   BattleRuntimeEnemyPatternFailure.None &&
                   secondTurn.Count == 1 &&
                   secondTurn[0].ActionType ==
                   BattleRuntimeEnemyTurnActionType.Attack &&
                   secondTurn[0].AutomaticAttackCount == 1 &&
                   secondTurn[0].AttackTieBreakerValues[0] == 800;
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
