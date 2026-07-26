using System;
using System.Linq;

namespace HaveABreak.Cards
{
    public enum RunLifecycleRequestKind
    {
        None,
        StartNewRun,
        ContinueRun
    }

    public sealed class RunLifecycleRequest
    {
        internal RunLifecycleRequest(
            RunLifecycleRequestKind kind,
            bool canProceed,
            bool confirmationRequired,
            string title,
            string body,
            string confirmLabel,
            string message)
        {
            Kind = kind;
            CanProceed = canProceed;
            ConfirmationRequired = confirmationRequired;
            Title = title;
            Body = body;
            ConfirmLabel = confirmLabel;
            Message = message;
        }

        public RunLifecycleRequestKind Kind { get; }
        public bool CanProceed { get; }
        public bool ConfirmationRequired { get; }
        public string Title { get; }
        public string Body { get; }
        public string ConfirmLabel { get; }
        public string Message { get; }
    }

    public sealed class RunPreparationCommandResult
    {
        internal RunPreparationCommandResult(
            bool succeeded,
            RunOwnedCardState ownedCards,
            string message)
        {
            Succeeded = succeeded;
            OwnedCards = ownedCards;
            Message = message;
        }

        public bool Succeeded { get; }
        public RunOwnedCardState OwnedCards { get; }
        public string Message { get; }
    }

    public sealed class RunCreationCommandResult
    {
        internal RunCreationCommandResult(
            bool succeeded,
            bool saved,
            RunCampaignState campaign,
            RunEncounterProgressState progress,
            string selectedOwnedCardId,
            RunDeckFailure deckFailure,
            RunCampaignFailure saveFailure,
            string saveDestination,
            string message)
        {
            Succeeded = succeeded;
            Saved = saved;
            Campaign = campaign;
            Progress = progress;
            SelectedOwnedCardId = selectedOwnedCardId;
            DeckFailure = deckFailure;
            SaveFailure = saveFailure;
            SaveDestination = saveDestination;
            Message = message;
        }

        public bool Succeeded { get; }
        public bool Saved { get; }
        public RunCampaignState Campaign { get; }
        public RunEncounterProgressState Progress { get; }
        public string SelectedOwnedCardId { get; }
        public RunDeckFailure DeckFailure { get; }
        public RunCampaignFailure SaveFailure { get; }
        public string SaveDestination { get; }
        public string Message { get; }
    }

    public sealed class RunContinueCommandResult
    {
        internal RunContinueCommandResult(
            bool succeeded,
            RunCampaignState campaign,
            RunEncounterProgressState progress,
            RunResumeSource source,
            RunCampaignFailure failure,
            string selectedOwnedCardId,
            string message)
        {
            Succeeded = succeeded;
            Campaign = campaign;
            Progress = progress;
            Source = source;
            Failure = failure;
            SelectedOwnedCardId = selectedOwnedCardId;
            Message = message;
        }

        public bool Succeeded { get; }
        public RunCampaignState Campaign { get; }
        public RunEncounterProgressState Progress { get; }
        public RunResumeSource Source { get; }
        public RunCampaignFailure Failure { get; }
        public string SelectedOwnedCardId { get; }
        public string Message { get; }
    }

    public sealed class RunSaveCommandResult
    {
        internal RunSaveCommandResult(
            bool succeeded,
            bool skipped,
            bool checkpointRetried,
            string destination,
            RunCampaignFailure failure,
            string message)
        {
            Succeeded = succeeded;
            Skipped = skipped;
            CheckpointRetried = checkpointRetried;
            Destination = destination;
            Failure = failure;
            Message = message;
        }

        public bool Succeeded { get; }
        public bool Skipped { get; }
        public bool CheckpointRetried { get; }
        public string Destination { get; }
        public RunCampaignFailure Failure { get; }
        public string Message { get; }
    }

    public interface IRunLifecyclePersistence
    {
        bool TryInspect(
            CardDatabase cards,
            EnchantDatabase enchants,
            EncounterDatabase encounters,
            PlayerPermanentRewardState permanentRewards,
            out RunSaveSlotState state);

        bool TryLoad(
            CardDatabase cards,
            EnchantDatabase enchants,
            EncounterDatabase encounters,
            PlayerPermanentRewardState permanentRewards,
            out RunCampaignState campaign,
            out RunEncounterProgressState progress,
            out RunResumeSource source,
            out RunCampaignFailure failure);

        bool TrySave(
            RunCampaignState campaign,
            RunEncounterProgressState progress,
            out string destination,
            out RunCampaignFailure failure);

        bool TryLoadPermanentRewards(
            out PlayerPermanentRewardState state);
    }

    public sealed class DefaultRunLifecyclePersistence :
        IRunLifecyclePersistence
    {
        public bool TryInspect(
            CardDatabase cards,
            EnchantDatabase enchants,
            EncounterDatabase encounters,
            PlayerPermanentRewardState permanentRewards,
            out RunSaveSlotState state)
        {
            bool inspected = RunSaveSlotService.TryInspectDefault(
                cards,
                enchants,
                encounters,
                permanentRewards,
                out RunSaveSlotInfo slot,
                out _);
            state = slot?.State ?? RunSaveSlotState.Empty;
            return inspected;
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
            return IntegratedRunSaveService.TryLoad(
                cards,
                enchants,
                encounters,
                permanentRewards,
                out campaign,
                out progress,
                out _,
                out source,
                out failure);
        }

        public bool TrySave(
            RunCampaignState campaign,
            RunEncounterProgressState progress,
            out string destination,
            out RunCampaignFailure failure)
        {
            bool saved = IntegratedRunSaveService.TrySave(
                campaign,
                progress,
                out RunSaveDestination resolved,
                out failure);
            destination = resolved.ToString();
            return saved;
        }

        public bool TryLoadPermanentRewards(
            out PlayerPermanentRewardState state)
        {
            return PlayerPermanentRewardSaveService.TryLoadDefault(
                out state,
                out _,
                out _);
        }
    }

    public sealed class RunLifecycleViewModel
    {
        private readonly IRunLifecyclePersistence persistence;
        private readonly BattleStartViewModel battleStart;

        public RunLifecycleViewModel()
            : this(
                new DefaultRunLifecyclePersistence(),
                new BattleStartViewModel())
        {
        }

        public RunLifecycleViewModel(
            IRunLifecyclePersistence persistence,
            BattleStartViewModel battleStart)
        {
            this.persistence = persistence ??
                               throw new ArgumentNullException(nameof(persistence));
            this.battleStart = battleStart ??
                               throw new ArgumentNullException(nameof(battleStart));
        }

        public PlayerPermanentRewardState LoadPermanentRewards(
            PlayerPermanentRewardState fallback = null)
        {
            if (persistence.TryLoadPermanentRewards(
                    out PlayerPermanentRewardState loaded) &&
                loaded != null)
            {
                return loaded;
            }

            return fallback ?? new PlayerPermanentRewardState();
        }

        public RunLifecycleRequest CreateNewRunRequest(
            bool hasCurrentRun,
            RuntimePrototypeConfig config,
            PlayerPermanentRewardState permanentRewards)
        {
            if (config == null || !config.IsReady)
            {
                return new RunLifecycleRequest(
                    RunLifecycleRequestKind.StartNewRun,
                    false,
                    false,
                    null,
                    null,
                    null,
                    "게임 데이터베이스를 불러올 수 없습니다.");
            }

            bool inspected = persistence.TryInspect(
                config.CardDatabase,
                config.EnchantDatabase,
                config.EncounterDatabase,
                permanentRewards,
                out RunSaveSlotState slotState);
            bool confirm = RunActionConfirmationPolicy.ShouldConfirmNewRun(
                hasCurrentRun,
                inspected,
                slotState);
            return new RunLifecycleRequest(
                RunLifecycleRequestKind.StartNewRun,
                true,
                confirm,
                "새 런을 시작할까요?",
                "현재 진행과 저장된 런이 새 런으로 교체됩니다. " +
                "이 작업은 되돌릴 수 없습니다.",
                "새 런 시작",
                null);
        }

        public RunLifecycleRequest CreateContinueRequest(
            RunCampaignState campaign,
            RunEncounterProgressState progress)
        {
            bool hasCurrentRun = campaign != null && progress != null;
            RunCampaignPhase phase = campaign?.Phase ??
                                     RunCampaignPhase.NodeSelection;
            bool confirm = RunActionConfirmationPolicy.ShouldConfirmContinue(
                hasCurrentRun,
                phase);
            return new RunLifecycleRequest(
                RunLifecycleRequestKind.ContinueRun,
                true,
                confirm,
                "전투를 처음부터 다시 시작할까요?",
                "이어하기를 선택하면 현재 전투 진행을 버리고 " +
                "전투 시작 체크포인트에서 다시 시작합니다.",
                "전투 다시 시작",
                null);
        }

        public RunPreparationCommandResult BeginPreparation(
            CardDatabase cards)
        {
            if (cards == null)
            {
                return new RunPreparationCommandResult(
                    false,
                    null,
                    "카드 데이터베이스를 불러올 수 없습니다.");
            }

            RunOwnedCardState ownedCards = new();
            int index = 0;
            foreach (CardData card in cards.Cards.Where(card => card != null))
            {
                RunCardInstance ownedCard = new(
                    card,
                    $"OWNED-RUN-{++index:00}-{card.CatalogCardId}",
                    1);
                if (!ownedCards.TryAdd(ownedCard, out _))
                {
                    return new RunPreparationCommandResult(
                        false,
                        null,
                        $"보유카드 준비 실패: {card.CatalogCardId}");
                }
            }

            return new RunPreparationCommandResult(
                true,
                ownedCards,
                "런에 사용할 덱을 선택한 뒤 확정하세요.");
        }

        public RunCreationCommandResult TryConfirmPreparation(
            RuntimePrototypeConfig config,
            RunDeckSelectionViewModel deckSelection,
            RunOwnedCardState preparationCards,
            PlayerPermanentRewardState permanentRewards,
            int seed)
        {
            if (config == null || !config.IsReady ||
                deckSelection == null || preparationCards == null)
            {
                return new RunCreationCommandResult(
                    false,
                    false,
                    null,
                    null,
                    null,
                    RunDeckFailure.InvalidDeck,
                    default,
                    null,
                    "새 런 준비 상태가 올바르지 않습니다.");
            }

            if (!deckSelection.TryCreateDeck(
                    preparationCards,
                    out RunDeckState deck,
                    out RunDeckFailure deckFailure))
            {
                return new RunCreationCommandResult(
                    false,
                    false,
                    null,
                    null,
                    null,
                    deckFailure,
                    default,
                    null,
                    $"새 런 덱 확정 실패: {deckFailure}");
            }

            RunBattleState run =
                config.RunStartProgressionConfig.CreateInitialRunState();
            RunEncounterProgressState progress = new(
                run,
                preparationCards,
                deck,
                permanentRewards ?? new PlayerPermanentRewardState(),
                Array.Empty<string>(),
                0);
            RunCampaignState campaign = new(seed);
            bool saved = persistence.TrySave(
                campaign,
                progress,
                out string destination,
                out RunCampaignFailure saveFailure);
            string message = saved
                ? "새 런을 시작했습니다."
                : "새 런을 시작했습니다.\n" +
                  $"자동 저장 실패: {saveFailure}";
            return new RunCreationCommandResult(
                true,
                saved,
                campaign,
                progress,
                deck.Cards.FirstOrDefault()?.OwnedCardId,
                deckFailure,
                saveFailure,
                destination,
                message);
        }

        public RunContinueCommandResult TryContinue(
            CardDatabase cards,
            EnchantDatabase enchants,
            EncounterDatabase encounters,
            PlayerPermanentRewardState permanentRewards)
        {
            if (!persistence.TryLoad(
                    cards,
                    enchants,
                    encounters,
                    permanentRewards,
                    out RunCampaignState campaign,
                    out RunEncounterProgressState progress,
                    out RunResumeSource source,
                    out RunCampaignFailure failure))
            {
                return new RunContinueCommandResult(
                    false,
                    null,
                    null,
                    source,
                    failure,
                    null,
                    $"이어하기 실패: {failure}");
            }

            return new RunContinueCommandResult(
                true,
                campaign,
                progress,
                source,
                failure,
                progress?.OwnedCards?.Cards.FirstOrDefault()?.OwnedCardId,
                $"이어하기 완료: {source}");
        }

        public RunSaveCommandResult Save(
            RunCampaignState campaign,
            RunEncounterProgressState progress,
            RuntimePrototypeConfig config,
            string successMessage)
        {
            if (campaign == null || progress == null)
            {
                return new RunSaveCommandResult(
                    false,
                    true,
                    false,
                    null,
                    default,
                    null);
            }

            if (progress.HasActiveEncounter)
            {
                if (campaign.Phase == RunCampaignPhase.Battle &&
                    !string.IsNullOrWhiteSpace(successMessage))
                {
                    BattleStartCommandResult checkpoint = battleStart.TryStart(
                        campaign,
                        progress,
                        config);
                    return new RunSaveCommandResult(
                        checkpoint.Succeeded,
                        false,
                        true,
                        checkpoint.SaveDestination,
                        checkpoint.SaveFailure,
                        checkpoint.Succeeded
                            ? $"{successMessage} · {checkpoint.SaveDestination}"
                            : checkpoint.Message);
                }

                return new RunSaveCommandResult(
                    true,
                    true,
                    false,
                    null,
                    default,
                    string.IsNullOrWhiteSpace(successMessage)
                        ? null
                        : "활성 조우가 완료되기 전에는 현재 진행을 " +
                          "별도 저장하지 않습니다.");
            }

            if (persistence.TrySave(
                    campaign,
                    progress,
                    out string destination,
                    out RunCampaignFailure failure))
            {
                return new RunSaveCommandResult(
                    true,
                    false,
                    false,
                    destination,
                    failure,
                    string.IsNullOrWhiteSpace(successMessage)
                        ? null
                        : $"{successMessage} · {destination}");
            }

            return new RunSaveCommandResult(
                false,
                false,
                false,
                destination,
                failure,
                string.IsNullOrWhiteSpace(successMessage)
                    ? $"자동 저장 실패: {failure}"
                    : $"저장 실패: {failure}");
        }
    }
}
