using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleAutoplayViewModelValidation
    {
        [MenuItem("Have a Break/Validate Battle Autoplay ViewModel")]
        private static void ValidateFromMenu()
        {
            Debug.Log(Validate()
                ? "Battle autoplay ViewModel passed."
                : "Battle autoplay ViewModel failed.");
        }

        internal static bool Validate()
        {
            CardDatabase database = AssetDatabase.LoadAssetAtPath<CardDatabase>(
                "Assets/GameData/CardDatabase.asset");
            CardData c01 = database?.Cards.FirstOrDefault(card =>
                card != null && string.Equals(
                    card.CatalogCardId,
                    TestContentIds.C01,
                    StringComparison.OrdinalIgnoreCase));
            if (c01 == null)
            {
                Debug.LogError("Battle autoplay validation requires C01.");
                return false;
            }

            BattleAutoplayViewModel autoplay = new();
            BattleAutoplayCommandResult invalid = autoplay.TryRun(null, null);
            if (invalid == null || invalid.Succeeded ||
                invalid.Failure != BattleAutoplayFailure.InvalidState)
            {
                Debug.LogError("Battle autoplay invalid-state rejection failed.");
                return false;
            }

            bool victory = ValidateVictory(c01, autoplay);
            bool safety = ValidateSafetyLimit(c01, autoplay);
            return victory && safety;
        }

        private static bool ValidateVictory(
            CardData c01,
            BattleAutoplayViewModel autoplay)
        {
            EnemyDefinitionData enemy =
                ScriptableObject.CreateInstance<EnemyDefinitionData>();
            EncounterData encounter =
                ScriptableObject.CreateInstance<EncounterData>();
            try
            {
                enemy.EditorInitialize(
                    "TEST-ENEMY-AUTOPLAY-VICTORY",
                    "Autoplay Victory Enemy",
                    0,
                    18);
                encounter.EditorInitialize(
                    "TEST-ENCOUNTER-AUTOPLAY-VICTORY",
                    "Autoplay Victory Encounter",
                    BattleEncounterGrade.Normal,
                    new[]
                    {
                        new EncounterEnemySlot(
                            "TEST-ENEMY-AUTOPLAY-VICTORY-A",
                            enemy,
                            EnemyFieldPosition.Center)
                    });

                RunCampaignState campaign = CampaignAtBattle();
                RunEncounterProgressState progress = CreateProgress(c01, 100);
                if (campaign == null || progress == null ||
                    !TryBegin(
                        progress,
                        encounter,
                        "TEST-BATTLE-AUTOPLAY-VICTORY",
                        9201,
                        out BattleRuntimeEncounterContext context))
                {
                    Debug.LogError("Battle autoplay victory setup failed.");
                    return false;
                }

                BattleAutoplayCommandResult result = autoplay.TryRun(
                    progress,
                    campaign,
                    new BattleAutoplaySettings(
                        maximumPlayerTurns: 20,
                        maximumCardPlaysPerTurn: 16,
                        maximumAttacksPerTurn: 8,
                        maximumConsumablesPerTurn: 0,
                        maximumStalledTurns: 4,
                        useConsumables: false));
                bool passed = result != null &&
                              result.Succeeded &&
                              result.Failure == BattleAutoplayFailure.None &&
                              result.Outcome == BattleOutcome.Victory &&
                              result.CardsPlayed > 0 &&
                              result.AttacksResolved > 0 &&
                              result.LivingEnemyCount == 0 &&
                              result.FinalPlayerHealth > 0 &&
                              context.Session.IsFinished &&
                              context.Session.Outcome == BattleOutcome.Victory;
                if (!passed)
                {
                    Debug.LogError(
                        "Battle autoplay victory failed: " + Describe(result));
                    return false;
                }

                Debug.Log(
                    $"Battle autoplay validation passed: victory in " +
                    $"{result.PlayerTurnsCompleted} turns, cards " +
                    $"{result.CardsPlayed}, attacks {result.AttacksResolved}.");
                return true;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemy);
                UnityEngine.Object.DestroyImmediate(encounter);
            }
        }

        private static bool ValidateSafetyLimit(
            CardData c01,
            BattleAutoplayViewModel autoplay)
        {
            EnemyDefinitionData enemy =
                ScriptableObject.CreateInstance<EnemyDefinitionData>();
            EncounterData encounter =
                ScriptableObject.CreateInstance<EncounterData>();
            try
            {
                enemy.EditorInitialize(
                    "TEST-ENEMY-AUTOPLAY-LIMIT",
                    "Autoplay Limit Enemy",
                    0,
                    9999);
                encounter.EditorInitialize(
                    "TEST-ENCOUNTER-AUTOPLAY-LIMIT",
                    "Autoplay Limit Encounter",
                    BattleEncounterGrade.Normal,
                    new[]
                    {
                        new EncounterEnemySlot(
                            "TEST-ENEMY-AUTOPLAY-LIMIT-A",
                            enemy,
                            EnemyFieldPosition.Center)
                    });

                RunCampaignState campaign = CampaignAtBattle();
                RunEncounterProgressState progress = CreateProgress(c01, 100);
                if (campaign == null || progress == null ||
                    !TryBegin(
                        progress,
                        encounter,
                        "TEST-BATTLE-AUTOPLAY-LIMIT",
                        9202,
                        out BattleRuntimeEncounterContext context))
                {
                    Debug.LogError("Battle autoplay safety setup failed.");
                    return false;
                }

                BattleAutoplayCommandResult result = autoplay.TryRun(
                    progress,
                    campaign,
                    new BattleAutoplaySettings(
                        maximumPlayerTurns: 1,
                        maximumCardPlaysPerTurn: 16,
                        maximumAttacksPerTurn: 8,
                        maximumConsumablesPerTurn: 0,
                        maximumStalledTurns: 2,
                        useConsumables: false));
                bool passed = result != null &&
                              !result.Succeeded &&
                              result.Failure == BattleAutoplayFailure.SafetyLimit &&
                              result.Outcome == BattleOutcome.Ongoing &&
                              result.PlayerTurnsCompleted == 1 &&
                              result.CardsPlayed > 0 &&
                              result.AttacksResolved > 0 &&
                              result.LivingEnemyCount == 1 &&
                              !context.Session.IsFinished;
                if (!passed)
                {
                    Debug.LogError(
                        "Battle autoplay safety-limit failed: " +
                        Describe(result));
                    return false;
                }

                Debug.Log(
                    "Battle autoplay validation passed: safety limit without " +
                    "direct victory mutation.");
                return true;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemy);
                UnityEngine.Object.DestroyImmediate(encounter);
            }
        }

        private static RunEncounterProgressState CreateProgress(
            CardData c01,
            int playerHealth)
        {
            RunDeckState deck = new();
            for (int index = 0; index < 5; index++)
            {
                RunCardInstance card = new(
                    c01,
                    $"OWNED-AUTOPLAY-C01-{index + 1:00}",
                    1);
                if (!deck.TryAdd(card, out RunDeckFailure failure) ||
                    failure != RunDeckFailure.None)
                {
                    return null;
                }
            }

            return new RunEncounterProgressState(
                new RunBattleState(playerHealth, playerHealth, 0),
                deck);
        }

        private static RunCampaignState CampaignAtBattle()
        {
            for (int seed = 0; seed < 1000; seed++)
            {
                RunCampaignState campaign = new(seed);
                RunNodeChoice choice = RunCampaignService.GetChoices(campaign)
                    .FirstOrDefault(value => value.IsBattle);
                if (choice != null && RunCampaignService.TrySelectNode(
                        campaign,
                        choice.NodeId,
                        out RunCampaignFailure failure) &&
                    failure == RunCampaignFailure.None)
                {
                    return campaign;
                }
            }

            return null;
        }

        private static bool TryBegin(
            RunEncounterProgressState progress,
            EncounterData encounter,
            string battleId,
            int seed,
            out BattleRuntimeEncounterContext context)
        {
            bool created = RunEncounterProgressService.TryBegin(
                progress,
                battleId,
                encounter,
                seed,
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
                   validationErrors.Count == 0;
        }

        private static string Describe(BattleAutoplayCommandResult result)
        {
            return result == null
                ? "result=null"
                : $"success={result.Succeeded}, failure={result.Failure}, " +
                  $"outcome={result.Outcome}, turns=" +
                  $"{result.PlayerTurnsCompleted}, cards={result.CardsPlayed}, " +
                  $"attacks={result.AttacksResolved}, items=" +
                  $"{result.ConsumablesUsed}, health=" +
                  $"{result.FinalPlayerHealth}, enemies=" +
                  $"{result.LivingEnemyCount}, message={result.Message}";
        }
    }
}
