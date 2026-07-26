using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class RunLifecycleViewModelValidation
    {
        private sealed class FakePersistence : IRunLifecyclePersistence
        {
            private readonly Queue<bool> saveOutcomes = new();

            internal bool InspectResult { get; set; } = true;
            internal RunSaveSlotState InspectState { get; set; } =
                RunSaveSlotState.Empty;
            internal bool LoadResult { get; set; }
            internal RunCampaignState LoadedCampaign { get; set; }
            internal RunEncounterProgressState LoadedProgress { get; set; }
            internal RunResumeSource LoadedSource { get; set; }
            internal RunCampaignFailure LoadFailure { get; set; }
            internal PlayerPermanentRewardState PermanentRewards { get; set; }
            internal int InspectCalls { get; private set; }
            internal int LoadCalls { get; private set; }
            internal int SaveCalls { get; private set; }
            internal int PermanentRewardCalls { get; private set; }

            internal void SetSaveOutcomes(params bool[] outcomes)
            {
                saveOutcomes.Clear();
                foreach (bool outcome in outcomes ?? Array.Empty<bool>())
                {
                    saveOutcomes.Enqueue(outcome);
                }
            }

            public bool TryInspect(
                CardDatabase cards,
                EnchantDatabase enchants,
                EncounterDatabase encounters,
                PlayerPermanentRewardState permanentRewards,
                out RunSaveSlotState state)
            {
                InspectCalls++;
                state = InspectState;
                return InspectResult;
            }

            public bool TryLoad(
                CardDatabase cards,
                EnchantDatabase enchants,
                EncounterDatabase encounters,
                PlayerPermanentRewardState permanentRewards,
                out RunCampaignState campaign,
                out RunEncounterProgressState progress,
                out RunResumeSource source,
                out RunCampaignFailure failure)
            {
                LoadCalls++;
                campaign = LoadedCampaign;
                progress = LoadedProgress;
                source = LoadedSource;
                failure = LoadFailure;
                return LoadResult;
            }

            public bool TrySave(
                RunCampaignState campaign,
                RunEncounterProgressState progress,
                out string destination,
                out RunCampaignFailure failure)
            {
                SaveCalls++;
                bool succeeds = saveOutcomes.Count == 0 ||
                                saveOutcomes.Dequeue();
                destination = "FakeRunSlot";
                failure = succeeds
                    ? RunCampaignFailure.None
                    : (RunCampaignFailure)991;
                return succeeds;
            }

            public bool TryLoadPermanentRewards(
                out PlayerPermanentRewardState state)
            {
                PermanentRewardCalls++;
                state = PermanentRewards;
                return state != null;
            }
        }

        private sealed class FakeCheckpointWriter :
            IBattleStartCheckpointWriter
        {
            internal int CallCount { get; private set; }

            public bool TrySave(
                RunCampaignState campaign,
                RunEncounterProgressState progress,
                out string destination,
                out RunCampaignFailure failure)
            {
                CallCount++;
                destination = "FakeBattleCheckpoint";
                failure = RunCampaignFailure.None;
                return true;
            }
        }

        [MenuItem("Have a Break/Validate Run Lifecycle ViewModel")]
        private static void ValidateFromMenu()
        {
            Debug.Log(Validate()
                ? "Run lifecycle ViewModel passed."
                : "Run lifecycle ViewModel failed.");
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
                    "Run lifecycle validation setup failed: config or cards missing.");
                return false;
            }

            bool valid = true;
            valid &= RunCase(
                "request confirmation and permanent rewards",
                () => ValidateRequests(config));
            valid &= RunCase(
                "preparation and new run creation",
                () => ValidateCreation(config, cards));
            valid &= RunCase(
                "continue and normal save routing",
                () => ValidateContinueAndSave(config, cards));
            valid &= RunCase(
                "active battle checkpoint routing",
                () => ValidateCheckpointRouting(config, cards));
            return valid;
        }

        private static bool ValidateRequests(RuntimePrototypeConfig config)
        {
            PlayerPermanentRewardState rewards = new();
            FakePersistence persistence = new()
            {
                PermanentRewards = rewards
            };
            RunLifecycleViewModel viewModel = new(
                persistence,
                new BattleStartViewModel());
            PlayerPermanentRewardState loaded =
                viewModel.LoadPermanentRewards();
            RunLifecycleRequest fresh = viewModel.CreateNewRunRequest(
                false,
                config,
                loaded);
            RunLifecycleRequest replacing = viewModel.CreateNewRunRequest(
                true,
                config,
                loaded);
            RunLifecycleRequest invalid = viewModel.CreateNewRunRequest(
                false,
                null,
                loaded);
            RunLifecycleRequest continueEmpty =
                viewModel.CreateContinueRequest(null, null);

            RunCampaignState battleCampaign = CampaignAtBattleNode();
            RunEncounterProgressState dummyProgress = battleCampaign == null
                ? null
                : new RunEncounterProgressState(
                    config.RunStartProgressionConfig.CreateInitialRunState(),
                    new RunDeckState());
            RunLifecycleRequest continueBattle =
                viewModel.CreateContinueRequest(
                    battleCampaign,
                    dummyProgress);
            return ReferenceEquals(loaded, rewards) &&
                   persistence.PermanentRewardCalls == 1 &&
                   fresh.CanProceed && !fresh.ConfirmationRequired &&
                   fresh.Kind == RunLifecycleRequestKind.StartNewRun &&
                   replacing.CanProceed && replacing.ConfirmationRequired &&
                   !string.IsNullOrWhiteSpace(replacing.Title) &&
                   !string.IsNullOrWhiteSpace(replacing.Body) &&
                   !string.IsNullOrWhiteSpace(replacing.ConfirmLabel) &&
                   !invalid.CanProceed &&
                   !string.IsNullOrWhiteSpace(invalid.Message) &&
                   continueEmpty.CanProceed &&
                   continueEmpty.Kind == RunLifecycleRequestKind.ContinueRun &&
                   continueBattle.CanProceed &&
                   continueBattle.ConfirmationRequired &&
                   persistence.InspectCalls == 2;
        }

        private static bool ValidateCreation(
            RuntimePrototypeConfig config,
            CardDatabase cards)
        {
            FakePersistence successfulPersistence = new();
            successfulPersistence.SetSaveOutcomes(true);
            RunLifecycleViewModel successful = new(
                successfulPersistence,
                new BattleStartViewModel());
            RunPreparationCommandResult preparation =
                successful.BeginPreparation(cards);
            RunDeckSelectionViewModel selection = new();
            if (preparation?.Succeeded != true ||
                preparation.OwnedCards == null ||
                preparation.OwnedCards.Count !=
                    cards.Cards.Count(card => card != null))
            {
                return false;
            }

            selection.OpenWithAllOwnedCards(preparation.OwnedCards);
            RunCreationCommandResult created =
                successful.TryConfirmPreparation(
                    config,
                    selection,
                    preparation.OwnedCards,
                    new PlayerPermanentRewardState(),
                    7301);
            if (created == null || !created.Succeeded || !created.Saved ||
                created.Campaign == null || created.Progress == null ||
                created.Campaign.Seed != 7301 ||
                created.Progress.RunDeck.Count != preparation.OwnedCards.Count ||
                string.IsNullOrWhiteSpace(created.SelectedOwnedCardId) ||
                created.DeckFailure != RunDeckFailure.None ||
                created.SaveFailure != RunCampaignFailure.None ||
                created.SaveDestination != "FakeRunSlot" ||
                string.IsNullOrWhiteSpace(created.Message) ||
                successfulPersistence.SaveCalls != 1)
            {
                return false;
            }

            FakePersistence failingPersistence = new();
            failingPersistence.SetSaveOutcomes(false);
            RunLifecycleViewModel failing = new(
                failingPersistence,
                new BattleStartViewModel());
            RunDeckSelectionViewModel secondSelection = new();
            secondSelection.OpenWithAllOwnedCards(preparation.OwnedCards);
            RunCreationCommandResult partial =
                failing.TryConfirmPreparation(
                    config,
                    secondSelection,
                    preparation.OwnedCards,
                    new PlayerPermanentRewardState(),
                    7302);
            return partial != null && partial.Succeeded && !partial.Saved &&
                   partial.Campaign != null && partial.Progress != null &&
                   partial.SaveFailure != RunCampaignFailure.None &&
                   partial.Message.Contains("자동 저장 실패") &&
                   failingPersistence.SaveCalls == 1 &&
                   !failing.TryConfirmPreparation(
                       null,
                       secondSelection,
                       preparation.OwnedCards,
                       new PlayerPermanentRewardState(),
                       7303).Succeeded;
        }

        private static bool ValidateContinueAndSave(
            RuntimePrototypeConfig config,
            CardDatabase cards)
        {
            RunPreparationCommandResult preparation =
                new RunLifecycleViewModel(
                        new FakePersistence(),
                        new BattleStartViewModel())
                    .BeginPreparation(cards);
            RunDeckSelectionViewModel selection = new();
            selection.OpenWithAllOwnedCards(preparation.OwnedCards);
            FakePersistence creationPersistence = new();
            creationPersistence.SetSaveOutcomes(true);
            RunCreationCommandResult created = new RunLifecycleViewModel(
                    creationPersistence,
                    new BattleStartViewModel())
                .TryConfirmPreparation(
                    config,
                    selection,
                    preparation.OwnedCards,
                    new PlayerPermanentRewardState(),
                    7401);
            if (created?.Succeeded != true)
            {
                return false;
            }

            FakePersistence persistence = new()
            {
                LoadResult = true,
                LoadedCampaign = created.Campaign,
                LoadedProgress = created.Progress,
                LoadedSource = default
            };
            persistence.SetSaveOutcomes(true, false);
            RunLifecycleViewModel viewModel = new(
                persistence,
                new BattleStartViewModel());
            RunContinueCommandResult continued = viewModel.TryContinue(
                config.CardDatabase,
                config.EnchantDatabase,
                config.EncounterDatabase,
                new PlayerPermanentRewardState());
            RunSaveCommandResult manual = viewModel.Save(
                continued.Campaign,
                continued.Progress,
                config,
                "수동 저장 완료");
            RunSaveCommandResult automaticFailure = viewModel.Save(
                continued.Campaign,
                continued.Progress,
                config,
                null);

            persistence.LoadResult = false;
            persistence.LoadFailure = (RunCampaignFailure)992;
            RunContinueCommandResult failed = viewModel.TryContinue(
                config.CardDatabase,
                config.EnchantDatabase,
                config.EncounterDatabase,
                new PlayerPermanentRewardState());
            return continued.Succeeded &&
                   ReferenceEquals(continued.Campaign, created.Campaign) &&
                   ReferenceEquals(continued.Progress, created.Progress) &&
                   !string.IsNullOrWhiteSpace(continued.SelectedOwnedCardId) &&
                   manual.Succeeded && !manual.Skipped &&
                   !manual.CheckpointRetried &&
                   manual.Destination == "FakeRunSlot" &&
                   manual.Message.Contains("수동 저장 완료") &&
                   !automaticFailure.Succeeded &&
                   automaticFailure.Message.Contains("자동 저장 실패") &&
                   !failed.Succeeded && failed.Campaign == null &&
                   failed.Progress == null &&
                   failed.Failure != RunCampaignFailure.None &&
                   persistence.LoadCalls == 2 &&
                   persistence.SaveCalls == 2;
        }

        private static bool ValidateCheckpointRouting(
            RuntimePrototypeConfig config,
            CardDatabase cards)
        {
            RunCampaignState campaign = CampaignAtBattleNode();
            RunEncounterProgressState progress = CreateProgress(cards, config);
            FakeCheckpointWriter checkpointWriter = new();
            BattleStartViewModel battleStart = new(checkpointWriter);
            if (campaign == null || progress == null ||
                !battleStart.TryStart(
                    campaign,
                    progress,
                    config).Succeeded)
            {
                return false;
            }

            FakePersistence persistence = new();
            RunLifecycleViewModel viewModel = new(
                persistence,
                battleStart);
            RunSaveCommandResult automatic = viewModel.Save(
                campaign,
                progress,
                config,
                null);
            RunSaveCommandResult manual = viewModel.Save(
                campaign,
                progress,
                config,
                "수동 저장 완료");
            return automatic.Succeeded && automatic.Skipped &&
                   !automatic.CheckpointRetried && automatic.Message == null &&
                   manual.Succeeded && !manual.Skipped &&
                   manual.CheckpointRetried &&
                   manual.Destination == "FakeBattleCheckpoint" &&
                   manual.Message.Contains("수동 저장 완료") &&
                   checkpointWriter.CallCount == 2 &&
                   persistence.SaveCalls == 0;
        }

        private static RunEncounterProgressState CreateProgress(
            CardDatabase cards,
            RuntimePrototypeConfig config)
        {
            RunOwnedCardState owned = new();
            RunDeckState deck = new();
            int index = 0;
            foreach (CardData card in cards.Cards.Where(card => card != null))
            {
                RunCardInstance instance = new(
                    card,
                    $"OWNED-RUN-LIFECYCLE-{++index:00}-{card.CatalogCardId}",
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

        private static bool RunCase(
            string label,
            Func<bool> validation)
        {
            try
            {
                bool passed = validation();
                if (passed)
                {
                    Debug.Log($"Run lifecycle validation passed: {label}.");
                }
                else
                {
                    Debug.LogError($"Run lifecycle validation failed: {label}.");
                }

                return passed;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Run lifecycle validation threw: {label}.\n{exception}");
                return false;
            }
        }
    }
}
