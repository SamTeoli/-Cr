using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleStartCommandFlowValidation
    {
        private sealed class FakeCheckpointWriter :
            IBattleStartCheckpointWriter
        {
            private readonly Queue<bool> outcomes;

            internal FakeCheckpointWriter(params bool[] values)
            {
                outcomes = new Queue<bool>(
                    values == null || values.Length == 0
                        ? new[] { true }
                        : values);
            }

            internal int CallCount { get; private set; }

            public bool TrySave(
                RunCampaignState campaign,
                RunEncounterProgressState progress,
                out string destination,
                out RunCampaignFailure failure)
            {
                CallCount++;
                bool succeeds = outcomes.Count == 0 || outcomes.Dequeue();
                destination = "TestCheckpoint";
                failure = succeeds
                    ? RunCampaignFailure.None
                    : (RunCampaignFailure)999;
                return succeeds;
            }
        }

        [MenuItem("Have a Break/Validate Battle Start Command Flow")]
        private static void ValidateFromMenu()
        {
            Debug.Log(Validate()
                ? "Battle start command flow passed."
                : "Battle start command flow failed.");
        }

        internal static bool Validate()
        {
            RuntimePrototypeConfig config =
                Resources.Load<RuntimePrototypeConfig>(
                    "GameData/RuntimePrototypeConfig");
            CardDatabase cards = AssetDatabase.LoadAssetAtPath<CardDatabase>(
                "Assets/GameData/CardDatabase.asset");
            if (config == null || !config.IsReady || cards == null)
            {
                Debug.LogError(
                    "Battle start validation setup failed: runtime config or cards missing.");
                return false;
            }

            bool valid = true;
            valid &= RunCase("grade mapping", ValidateGradeMapping);
            valid &= RunCase(
                "configured encounter pools",
                () => ValidateConfiguredPools(config));
            valid &= RunCase(
                "invalid state and missing configuration",
                () => ValidateInvalidState(cards, config));
            valid &= RunCase(
                "successful start and existing checkpoint retry",
                () => ValidateSuccessfulStart(cards, config));
            valid &= RunCase(
                "checkpoint failure without duplicate battle",
                () => ValidateCheckpointRetry(cards, config));
            return valid;
        }

        private static bool ValidateGradeMapping()
        {
            return BattleStartViewModel.ResolveGrade(
                       RunNodeType.Battle) == BattleEncounterGrade.Normal &&
                   BattleStartViewModel.ResolveGrade(
                       RunNodeType.EliteBattle) == BattleEncounterGrade.Elite &&
                   BattleStartViewModel.ResolveGrade(
                       RunNodeType.MidBoss) == BattleEncounterGrade.MidBoss &&
                   BattleStartViewModel.ResolveGrade(
                       RunNodeType.FinalBoss) == BattleEncounterGrade.FinalBoss;
        }

        private static bool ValidateConfiguredPools(
            RuntimePrototypeConfig config)
        {
            BattleEncounterGrade[] grades =
            {
                BattleEncounterGrade.Normal,
                BattleEncounterGrade.Elite,
                BattleEncounterGrade.MidBoss,
                BattleEncounterGrade.FinalBoss
            };
            for (int index = 0; index < grades.Length; index++)
            {
                BattleEncounterGrade grade = grades[index];
                IReadOnlyList<string> pool = config.GetEncounterPool(grade, 0);
                EncounterData encounter = null;
                string error = null;
                if (pool == null || pool.Count == 0 ||
                    !RunEncounterPoolService.TryResolve(
                        config.EncounterDatabase,
                        pool,
                        grade,
                        700 + index,
                        out encounter,
                        out error) ||
                    encounter == null || !string.IsNullOrWhiteSpace(error))
                {
                    Debug.LogError(
                        $"Configured encounter pool failed: {grade} / {error}");
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateInvalidState(
            CardDatabase cards,
            RuntimePrototypeConfig config)
        {
            FakeCheckpointWriter writer = new(true);
            BattleStartViewModel viewModel = new(writer);
            BattleStartCommandResult empty =
                viewModel.TryStart(null, null, config);
            RunCampaignState nonBattle = CampaignAtNonBattleNode();
            RunEncounterProgressState progress = CreateProgress(cards, config);
            BattleStartCommandResult wrongNode =
                viewModel.TryStart(nonBattle, progress, config);
            RuntimePrototypeConfig invalidConfig =
                ScriptableObject.CreateInstance<RuntimePrototypeConfig>();
            try
            {
                RunCampaignState battle = CampaignAtBattleNode();
                RunEncounterProgressState secondProgress =
                    CreateProgress(cards, config);
                BattleStartCommandResult missingConfig =
                    viewModel.TryStart(
                        battle,
                        secondProgress,
                        invalidConfig);
                return empty != null && !empty.Succeeded &&
                       empty.Failure ==
                           BattleStartCommandFailure.InvalidState &&
                       wrongNode != null && !wrongNode.Succeeded &&
                       wrongNode.Failure ==
                           BattleStartCommandFailure.InvalidState &&
                       missingConfig != null && !missingConfig.Succeeded &&
                       missingConfig.Failure ==
                           BattleStartCommandFailure.MissingConfiguration &&
                       progress != null && !progress.HasActiveEncounter &&
                       secondProgress != null &&
                       !secondProgress.HasActiveEncounter &&
                       writer.CallCount == 0;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(invalidConfig);
            }
        }

        private static bool ValidateSuccessfulStart(
            CardDatabase cards,
            RuntimePrototypeConfig config)
        {
            RunCampaignState campaign = CampaignAtBattleNode();
            RunEncounterProgressState progress = CreateProgress(cards, config);
            FakeCheckpointWriter writer = new(true, true);
            BattleStartViewModel viewModel = new(writer);
            if (campaign == null || progress == null)
            {
                return false;
            }

            string expectedBattleId =
                BattleStartViewModel.CreateBattleId(campaign);
            int expectedSelectionSeed =
                BattleStartViewModel.CreateSelectionSeed(campaign);
            int expectedBattleSeed =
                BattleStartViewModel.CreateBattleSeed(campaign);
            BattleEncounterGrade expectedGrade =
                BattleStartViewModel.ResolveGrade(
                    campaign.ActiveNode.NodeType);
            BattleStartCommandResult first =
                viewModel.TryStart(campaign, progress, config);
            BattleRuntimeEncounterContext active = progress.ActiveEncounter;
            BattleStartCommandResult second =
                viewModel.TryStart(campaign, progress, config);
            return first != null && first.Succeeded &&
                   first.BattleStarted && first.StartedNewBattle &&
                   first.CheckpointSaved &&
                   first.Failure == BattleStartCommandFailure.None &&
                   first.Grade == expectedGrade && first.Encounter != null &&
                   first.BattleId == expectedBattleId &&
                   first.SelectionSeed == expectedSelectionSeed &&
                   first.BattleSeed == expectedBattleSeed &&
                   first.ValidationErrors.Count == 0 &&
                   !string.IsNullOrWhiteSpace(first.Message) &&
                   progress.HasActiveEncounter && active != null &&
                   second != null && second.Succeeded &&
                   second.BattleStarted && !second.StartedNewBattle &&
                   second.CheckpointSaved &&
                   ReferenceEquals(active, progress.ActiveEncounter) &&
                   writer.CallCount == 2;
        }

        private static bool ValidateCheckpointRetry(
            CardDatabase cards,
            RuntimePrototypeConfig config)
        {
            RunCampaignState campaign = CampaignAtBattleNode();
            RunEncounterProgressState progress = CreateProgress(cards, config);
            FakeCheckpointWriter writer = new(false, true);
            BattleStartViewModel viewModel = new(writer);
            if (campaign == null || progress == null)
            {
                return false;
            }

            BattleStartCommandResult failedCheckpoint =
                viewModel.TryStart(campaign, progress, config);
            BattleRuntimeEncounterContext active = progress.ActiveEncounter;
            BattleStartCommandResult retry =
                viewModel.TryStart(campaign, progress, config);
            return failedCheckpoint != null &&
                   !failedCheckpoint.Succeeded &&
                   failedCheckpoint.BattleStarted &&
                   failedCheckpoint.StartedNewBattle &&
                   !failedCheckpoint.CheckpointSaved &&
                   failedCheckpoint.Failure ==
                       BattleStartCommandFailure.CheckpointSaveFailed &&
                   failedCheckpoint.SaveFailure != RunCampaignFailure.None &&
                   progress.HasActiveEncounter && active != null &&
                   retry != null && retry.Succeeded && retry.BattleStarted &&
                   !retry.StartedNewBattle && retry.CheckpointSaved &&
                   retry.Failure == BattleStartCommandFailure.None &&
                   ReferenceEquals(active, progress.ActiveEncounter) &&
                   writer.CallCount == 2;
        }

        private static RunEncounterProgressState CreateProgress(
            CardDatabase cards,
            RuntimePrototypeConfig config)
        {
            if (cards == null || config?.RunStartProgressionConfig == null)
            {
                return null;
            }

            RunOwnedCardState owned = new();
            RunDeckState deck = new();
            int index = 0;
            foreach (CardData card in cards.Cards.Where(card => card != null))
            {
                RunCardInstance instance = new(
                    card,
                    $"OWNED-BATTLE-START-VM-{++index:00}-{card.CatalogCardId}",
                    1);
                if (!owned.TryAdd(instance, out _) ||
                    !deck.TryAdd(instance, out RunDeckFailure failure) ||
                    failure != RunDeckFailure.None)
                {
                    return null;
                }
            }

            return new RunEncounterProgressState(
                config.RunStartProgressionConfig.CreateInitialRunState(),
                owned,
                deck,
                new PlayerPermanentRewardState(),
                Array.Empty<string>(),
                0);
        }

        private static RunCampaignState CampaignAtBattleNode()
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

        private static RunCampaignState CampaignAtNonBattleNode()
        {
            for (int seed = 0; seed < 1000; seed++)
            {
                RunCampaignState campaign = new(seed);
                RunNodeChoice choice = RunCampaignService.GetChoices(campaign)
                    .FirstOrDefault(value => !value.IsBattle);
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

        private static bool RunCase(
            string label,
            Func<bool> validation)
        {
            try
            {
                bool passed = validation();
                if (passed)
                {
                    Debug.Log($"Battle start validation passed: {label}.");
                }
                else
                {
                    Debug.LogError($"Battle start validation failed: {label}.");
                }

                return passed;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Battle start validation threw: {label}.\n{exception}");
                return false;
            }
        }
    }
}
