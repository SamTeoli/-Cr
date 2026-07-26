using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    public enum BattleStartCommandFailure
    {
        None,
        InvalidState,
        MissingConfiguration,
        EncounterResolutionFailed,
        BattleBeginFailed,
        CheckpointSaveFailed
    }

    public interface IBattleStartCheckpointWriter
    {
        bool TrySave(
            RunCampaignState campaign,
            RunEncounterProgressState progress,
            out RunSaveDestination destination,
            out RunCampaignFailure failure);
    }

    public sealed class BattleStartCommandResult
    {
        internal BattleStartCommandResult(
            bool succeeded,
            bool battleStarted,
            bool startedNewBattle,
            bool checkpointSaved,
            BattleStartCommandFailure failure,
            BattleEncounterGrade grade,
            EncounterData encounter,
            string battleId,
            int selectionSeed,
            int battleSeed,
            RunSaveDestination saveDestination,
            RunCampaignFailure saveFailure,
            RunEncounterProgressFailure progressFailure,
            BattleRuntimeEncounterFlowFailure flowFailure,
            RunDeckFailure deckFailure,
            BattleRuntimeBootstrapFailure bootstrapFailure,
            BattleRuntimeSessionFailure sessionFailure,
            StartingHandRedrawFailure redrawFailure,
            BattleTurnFailure turnFailure,
            IReadOnlyList<string> validationErrors,
            string message)
        {
            Succeeded = succeeded;
            BattleStarted = battleStarted;
            StartedNewBattle = startedNewBattle;
            CheckpointSaved = checkpointSaved;
            Failure = failure;
            Grade = grade;
            Encounter = encounter;
            BattleId = battleId;
            SelectionSeed = selectionSeed;
            BattleSeed = battleSeed;
            SaveDestination = saveDestination;
            SaveFailure = saveFailure;
            ProgressFailure = progressFailure;
            FlowFailure = flowFailure;
            DeckFailure = deckFailure;
            BootstrapFailure = bootstrapFailure;
            SessionFailure = sessionFailure;
            RedrawFailure = redrawFailure;
            TurnFailure = turnFailure;
            ValidationErrors = validationErrors ?? Array.Empty<string>();
            Message = message;
        }

        public bool Succeeded { get; }
        public bool BattleStarted { get; }
        public bool StartedNewBattle { get; }
        public bool CheckpointSaved { get; }
        public BattleStartCommandFailure Failure { get; }
        public BattleEncounterGrade Grade { get; }
        public EncounterData Encounter { get; }
        public string BattleId { get; }
        public int SelectionSeed { get; }
        public int BattleSeed { get; }
        public RunSaveDestination SaveDestination { get; }
        public RunCampaignFailure SaveFailure { get; }
        public RunEncounterProgressFailure ProgressFailure { get; }
        public BattleRuntimeEncounterFlowFailure FlowFailure { get; }
        public RunDeckFailure DeckFailure { get; }
        public BattleRuntimeBootstrapFailure BootstrapFailure { get; }
        public BattleRuntimeSessionFailure SessionFailure { get; }
        public StartingHandRedrawFailure RedrawFailure { get; }
        public BattleTurnFailure TurnFailure { get; }
        public IReadOnlyList<string> ValidationErrors { get; }
        public string Message { get; }
    }

    public sealed class BattleStartViewModel
    {
        private sealed class IntegratedCheckpointWriter :
            IBattleStartCheckpointWriter
        {
            public bool TrySave(
                RunCampaignState campaign,
                RunEncounterProgressState progress,
                out RunSaveDestination destination,
                out RunCampaignFailure failure)
            {
                return IntegratedRunSaveService.TrySave(
                    campaign,
                    progress,
                    out destination,
                    out failure);
            }
        }

        private readonly IBattleStartCheckpointWriter checkpointWriter;

        public BattleStartViewModel(
            IBattleStartCheckpointWriter checkpointWriter = null)
        {
            this.checkpointWriter = checkpointWriter ??
                                    new IntegratedCheckpointWriter();
        }

        public static BattleEncounterGrade ResolveGrade(
            RunNodeType nodeType)
        {
            return nodeType switch
            {
                RunNodeType.EliteBattle => BattleEncounterGrade.Elite,
                RunNodeType.MidBoss => BattleEncounterGrade.MidBoss,
                RunNodeType.FinalBoss => BattleEncounterGrade.FinalBoss,
                _ => BattleEncounterGrade.Normal
            };
        }

        public static int CreateSelectionSeed(
            RunCampaignState campaign)
        {
            return campaign == null
                ? 0
                : campaign.Seed + campaign.CompletedNodeCount * 1009;
        }

        public static int CreateBattleSeed(
            RunCampaignState campaign)
        {
            return campaign == null
                ? 0
                : campaign.Seed + campaign.CompletedNodeCount * 101;
        }

        public static string CreateBattleId(
            RunCampaignState campaign)
        {
            return campaign == null
                ? null
                : $"RUN-{campaign.Seed}-NODE-" +
                  $"{campaign.CompletedNodeCount + 1:00}";
        }

        public BattleStartCommandResult TryStart(
            RunCampaignState campaign,
            RunEncounterProgressState progress,
            RuntimePrototypeConfig config)
        {
            if (campaign == null || progress == null)
            {
                return Invalid(
                    "전투 시작 실패: 런 상태를 찾을 수 없습니다.");
            }

            if (progress.HasActiveEncounter)
            {
                return TrySaveExistingCheckpoint(campaign, progress);
            }

            if (config == null || !config.IsReady ||
                campaign.Phase != RunCampaignPhase.Battle ||
                campaign.ActiveNode == null ||
                !campaign.ActiveNode.IsBattle)
            {
                return Invalid(
                    config == null || !config.IsReady
                        ? "전투 시작 실패: 프로토타입 설정이 준비되지 않았습니다."
                        : "전투 시작 실패: 선택된 전투 노드가 없습니다.",
                    config == null || !config.IsReady
                        ? BattleStartCommandFailure.MissingConfiguration
                        : BattleStartCommandFailure.InvalidState);
            }

            BattleEncounterGrade grade = ResolveGrade(
                campaign.ActiveNode.NodeType);
            int selectionSeed = CreateSelectionSeed(campaign);
            if (!RunEncounterPoolService.TryResolve(
                    config.EncounterDatabase,
                    config.GetEncounterPool(
                        grade,
                        campaign.CompletedNodeCount),
                    grade,
                    selectionSeed,
                    out EncounterData encounter,
                    out string poolError))
            {
                return new BattleStartCommandResult(
                    false,
                    false,
                    false,
                    false,
                    BattleStartCommandFailure.EncounterResolutionFailed,
                    grade,
                    null,
                    CreateBattleId(campaign),
                    selectionSeed,
                    CreateBattleSeed(campaign),
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    Array.Empty<string>(),
                    $"조우 선택 실패: {poolError}");
            }

            string battleId = CreateBattleId(campaign);
            int battleSeed = CreateBattleSeed(campaign);
            if (!RunEncounterProgressService.TryBegin(
                    progress,
                    battleId,
                    encounter,
                    battleSeed,
                    config.RunStartProgressionConfig.BattleMaximumMana,
                    Array.Empty<string>(),
                    (uint)Mathf.Abs(battleSeed),
                    config.BattleRewardConfig,
                    out BattleRuntimeEncounterContext context,
                    out RunEncounterProgressFailure progressFailure,
                    out BattleRuntimeEncounterFlowFailure flowFailure,
                    out RunDeckFailure deckFailure,
                    out BattleRuntimeBootstrapFailure bootstrapFailure,
                    out BattleRuntimeSessionFailure sessionFailure,
                    out StartingHandRedrawFailure redrawFailure,
                    out BattleTurnFailure turnFailure,
                    out List<string> validationErrors))
            {
                string validationText = validationErrors == null ||
                                        validationErrors.Count == 0
                    ? string.Empty
                    : $"\n{string.Join("\n", validationErrors)}";
                return new BattleStartCommandResult(
                    false,
                    false,
                    false,
                    false,
                    BattleStartCommandFailure.BattleBeginFailed,
                    grade,
                    encounter,
                    battleId,
                    selectionSeed,
                    battleSeed,
                    default,
                    default,
                    progressFailure,
                    flowFailure,
                    deckFailure,
                    bootstrapFailure,
                    sessionFailure,
                    redrawFailure,
                    turnFailure,
                    validationErrors,
                    $"전투 시작 실패: {progressFailure} / {flowFailure} / " +
                    $"{deckFailure} / {bootstrapFailure} / {sessionFailure} / " +
                    $"{redrawFailure} / {turnFailure}{validationText}");
            }

            BattleStartCommandResult checkpoint = TryWriteCheckpoint(
                campaign,
                progress,
                grade,
                encounter,
                battleId,
                selectionSeed,
                battleSeed,
                true);
            return checkpoint.Succeeded
                ? new BattleStartCommandResult(
                    true,
                    true,
                    true,
                    true,
                    BattleStartCommandFailure.None,
                    grade,
                    encounter,
                    battleId,
                    selectionSeed,
                    battleSeed,
                    checkpoint.SaveDestination,
                    checkpoint.SaveFailure,
                    progressFailure,
                    flowFailure,
                    deckFailure,
                    bootstrapFailure,
                    sessionFailure,
                    redrawFailure,
                    turnFailure,
                    validationErrors,
                    $"{campaign.ActiveNode.DisplayName} 전투 시작 · " +
                    $"체크포인트 저장 완료")
                : checkpoint;
        }

        private BattleStartCommandResult TrySaveExistingCheckpoint(
            RunCampaignState campaign,
            RunEncounterProgressState progress)
        {
            if (campaign.Phase != RunCampaignPhase.Battle ||
                progress.ActiveEncounter?.Encounter == null)
            {
                return Invalid(
                    "전투 체크포인트 저장 실패: 활성 전투 상태가 올바르지 않습니다.");
            }

            BattleEncounterGrade grade = campaign.ActiveNode == null
                ? BattleEncounterGrade.Normal
                : ResolveGrade(campaign.ActiveNode.NodeType);
            return TryWriteCheckpoint(
                campaign,
                progress,
                grade,
                progress.ActiveEncounter.Encounter,
                CreateBattleId(campaign),
                CreateSelectionSeed(campaign),
                CreateBattleSeed(campaign),
                false);
        }

        private BattleStartCommandResult TryWriteCheckpoint(
            RunCampaignState campaign,
            RunEncounterProgressState progress,
            BattleEncounterGrade grade,
            EncounterData encounter,
            string battleId,
            int selectionSeed,
            int battleSeed,
            bool startedNewBattle)
        {
            if (!checkpointWriter.TrySave(
                    campaign,
                    progress,
                    out RunSaveDestination destination,
                    out RunCampaignFailure saveFailure))
            {
                return new BattleStartCommandResult(
                    false,
                    true,
                    startedNewBattle,
                    false,
                    BattleStartCommandFailure.CheckpointSaveFailed,
                    grade,
                    encounter,
                    battleId,
                    selectionSeed,
                    battleSeed,
                    destination,
                    saveFailure,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    Array.Empty<string>(),
                    "전투는 시작됐지만 시작 체크포인트 저장에 실패했습니다: " +
                    saveFailure);
            }

            return new BattleStartCommandResult(
                true,
                true,
                startedNewBattle,
                true,
                BattleStartCommandFailure.None,
                grade,
                encounter,
                battleId,
                selectionSeed,
                battleSeed,
                destination,
                saveFailure,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                Array.Empty<string>(),
                startedNewBattle
                    ? "전투 시작 체크포인트 저장 완료."
                    : "기존 전투 시작 체크포인트 저장 완료.");
        }

        private static BattleStartCommandResult Invalid(
            string message,
            BattleStartCommandFailure failure =
                BattleStartCommandFailure.InvalidState)
        {
            return new BattleStartCommandResult(
                false,
                false,
                false,
                false,
                failure,
                BattleEncounterGrade.Normal,
                null,
                null,
                0,
                0,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                Array.Empty<string>(),
                message);
        }
    }
}
