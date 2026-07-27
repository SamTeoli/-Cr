using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    public sealed partial class RuntimePrototypeScreen : MonoBehaviour
    {
        private RuntimeGameScreen? finalCampaignScreen;

        private void InitializeFinalUi()
        {
            if (FinalUiRoot != null)
            {
                return;
            }

            FinalUiRoot = gameObject.AddComponent<RuntimeGameUiRoot>();
            FinalUiRoot.NewRunRequested += RequestStartNewRun;
            FinalUiRoot.ContinueRequested += RequestContinueRun;
            FinalUiRoot.RunPreparationCardToggleRequested +=
                ToggleFinalRunPreparationCard;
            FinalUiRoot.RunPreparationCancelled += CancelRunPreparation;
            FinalUiRoot.RunPreparationConfirmed += ConfirmRunPreparation;
            FinalUiRoot.NodeSelectionRequested += SelectFinalNode;
            FinalUiRoot.NodeResolutionCommandRequested +=
                ExecuteFinalNodeCommand;
            FinalUiRoot.Initialize();
            RefreshFinalUiVisibility();
        }

        private void OnDestroy()
        {
            if (FinalUiRoot == null)
            {
                return;
            }

            FinalUiRoot.NewRunRequested -= RequestStartNewRun;
            FinalUiRoot.ContinueRequested -= RequestContinueRun;
            FinalUiRoot.RunPreparationCardToggleRequested -=
                ToggleFinalRunPreparationCard;
            FinalUiRoot.RunPreparationCancelled -= CancelRunPreparation;
            FinalUiRoot.RunPreparationConfirmed -= ConfirmRunPreparation;
            FinalUiRoot.NodeSelectionRequested -= SelectFinalNode;
            FinalUiRoot.NodeResolutionCommandRequested -=
                ExecuteFinalNodeCommand;
        }

        private bool TryShowFinalUi()
        {
            if (FinalUiRoot == null)
            {
                return false;
            }

            if (pendingRunRequest?.ConfirmationRequired == true)
            {
                SetFinalUiActive(false);
                return false;
            }

            if (runPreparationCards != null && deckSelection.IsOpen)
            {
                SetFinalUiActive(true);
                FinalUiRoot.ShowScreen(RuntimeGameScreen.RunPreparation);
                return true;
            }

            if (campaign == null || progress == null)
            {
                SetFinalUiActive(true);
                FinalUiRoot.ShowScreen(RuntimeGameScreen.Start);
                return true;
            }

            if (campaign.Phase == RunCampaignPhase.NodeSelection)
            {
                if (finalCampaignScreen != RuntimeGameScreen.NodeSelection)
                {
                    RefreshFinalNodeSelection();
                }
                SetFinalUiActive(true);
                FinalUiRoot.ShowScreen(RuntimeGameScreen.NodeSelection);
                return true;
            }

            if (campaign.Phase == RunCampaignPhase.NodeResolution &&
                campaign.ActiveNode != null &&
                !campaign.ActiveNode.IsBattle)
            {
                if (finalCampaignScreen != RuntimeGameScreen.NodeResolution)
                {
                    RefreshFinalNodeResolution();
                }
                SetFinalUiActive(true);
                FinalUiRoot.ShowScreen(RuntimeGameScreen.NodeResolution);
                return true;
            }

            finalCampaignScreen = null;
            SetFinalUiActive(false);
            return false;
        }

        private void ToggleFinalRunPreparationCard(string ownedCardId)
        {
            if (runPreparationCards == null ||
                !deckSelection.Toggle(ownedCardId))
            {
                return;
            }

            RefreshFinalRunPreparation();
        }

        private void RefreshFinalRunPreparation()
        {
            if (FinalUiRoot == null || runPreparationCards == null ||
                !deckSelection.IsOpen)
            {
                RefreshFinalUiVisibility();
                return;
            }

            RunDeckSelectionOption[] options =
                deckSelection.CreateOptions(runPreparationCards);
            FinalUiRoot.BindRunPreparation(
                options,
                deckSelection.SelectedCount,
                message,
                deckSelection.SelectedCount > 0);
            SetFinalUiActive(true);
            FinalUiRoot.ShowScreen(RuntimeGameScreen.RunPreparation);
            finalCampaignScreen = null;
        }

        private void RefreshFinalUiVisibility()
        {
            if (FinalUiRoot == null)
            {
                return;
            }

            if (runPreparationCards != null && deckSelection.IsOpen)
            {
                RefreshFinalRunPreparation();
                return;
            }

            bool showStart = pendingRunRequest == null &&
                             (campaign == null || progress == null);
            SetFinalUiActive(showStart);
            if (showStart)
            {
                FinalUiRoot.ShowScreen(RuntimeGameScreen.Start);
            }
            finalCampaignScreen = null;
        }

        private void RefreshFinalNodeSelection()
        {
            if (FinalUiRoot == null || campaign == null || progress == null)
            {
                return;
            }

            RunNodeSelectionOption[] source =
                nodeSelection.CreateOptions(campaign);
            List<RuntimeGameCommandOption> options = new(source.Length);
            foreach (RunNodeSelectionOption option in source)
            {
                options.Add(new RuntimeGameCommandOption(
                    option.NodeId,
                    option.InlineLabel));
            }

            FinalUiRoot.BindNodeSelection(
                options,
                CreateFinalRunSummary(),
                message);
            finalCampaignScreen = RuntimeGameScreen.NodeSelection;
        }

        private void SelectFinalNode(string nodeId)
        {
            if (!nodeSelection.TrySelect(
                    campaign,
                    nodeId,
                    out RunNodeSelectionOption selected,
                    out RunCampaignFailure failure))
            {
                message = $"노드 선택 실패: {failure}";
                RefreshFinalNodeSelection();
                return;
            }

            finalCampaignScreen = null;
            if (selected.IsBattle)
            {
                BeginSelectedBattle();
            }
            else
            {
                message = $"{selected.DisplayName} 노드에 들어왔습니다.";
                SaveRun(null);
            }
            RefreshFinalUiVisibility();
        }

        private void RefreshFinalNodeResolution()
        {
            if (FinalUiRoot == null || campaign?.ActiveNode == null ||
                progress == null)
            {
                return;
            }

            List<RuntimeGameCommandOption> options = new();
            string title;
            switch (campaign.ActiveNode.NodeType)
            {
                case RunNodeType.SituationEvent:
                    title = "상황 이벤트";
                    foreach (RunSituationEventOption option in
                             situationEvent.CreateOptions(campaign))
                    {
                        options.Add(new RuntimeGameCommandOption(
                            $"event:{option.ChoiceId}",
                            option.DisplayText));
                    }
                    break;
                case RunNodeType.RestOrUpgrade:
                    title = "회복 · 강화";
                    RestUpgradeConfig restRules = config.RestUpgradeConfig;
                    RunRestUpgradeCardOption selected = restUpgrade.SelectedCard(
                        campaign,
                        progress,
                        selectedUpgradeCardId);
                    selectedUpgradeCardId = selected?.OwnedCardId;
                    options.Add(new RuntimeGameCommandOption(
                        "rest",
                        restUpgrade.RestButtonLabel(restRules)));
                    options.Add(new RuntimeGameCommandOption(
                        "cycle",
                        selected == null
                            ? "강화할 카드 없음"
                            : $"강화 대상 변경 · {selected.DisplayName} " +
                              $"Lv.{selected.CurrentLevel}",
                        selected != null));
                    options.Add(new RuntimeGameCommandOption(
                        "upgrade",
                        restUpgrade.UpgradeButtonLabel(restRules),
                        selected != null));
                    break;
                case RunNodeType.Shop:
                    title = "상점";
                    foreach (RunShopProductOption option in shop.CreateOptions(
                                 campaign,
                                 progress,
                                 config.EnchantDatabase,
                                 config.ShopEconomyConfig))
                    {
                        string detail = string.IsNullOrWhiteSpace(
                            option.BlockReason)
                            ? option.DisplayText
                            : $"{option.DisplayText} · {option.BlockReason}";
                        options.Add(new RuntimeGameCommandOption(
                            $"buy:{option.SlotId}",
                            detail,
                            option.CanPurchase));
                    }
                    int rerollCost = shop.GetRerollCost(
                        campaign,
                        config.ShopEconomyConfig);
                    options.Add(new RuntimeGameCommandOption(
                        "reroll",
                        $"전체 리롤 · {rerollCost}G"));
                    options.Add(new RuntimeGameCommandOption(
                        "leave",
                        "상점 나가기"));
                    break;
                default:
                    title = "노드 진행";
                    break;
            }

            FinalUiRoot.BindNodeResolution(
                title,
                options,
                CreateFinalRunSummary(),
                message);
            finalCampaignScreen = RuntimeGameScreen.NodeResolution;
        }

        private void ExecuteFinalNodeCommand(string commandId)
        {
            if (campaign?.ActiveNode == null || progress == null)
            {
                return;
            }

            switch (campaign.ActiveNode.NodeType)
            {
                case RunNodeType.SituationEvent:
                    ResolveFinalSituationEvent(commandId);
                    break;
                case RunNodeType.RestOrUpgrade:
                    ResolveFinalRestCommand(commandId);
                    break;
                case RunNodeType.Shop:
                    ResolveFinalShopCommand(commandId);
                    break;
            }

            finalCampaignScreen = null;
            RefreshFinalUiVisibility();
        }

        private void ResolveFinalSituationEvent(string commandId)
        {
            const string prefix = "event:";
            if (commandId == null || !commandId.StartsWith(prefix))
            {
                return;
            }

            if (situationEvent.TryResolve(
                    campaign,
                    progress.RunState,
                    commandId.Substring(prefix.Length),
                    out _,
                    out string result,
                    out RunCampaignFailure failure))
            {
                message = result;
                SaveRun(null);
            }
            else
            {
                message = $"이벤트 처리 실패: {failure}";
            }
        }

        private void ResolveFinalRestCommand(string commandId)
        {
            RestUpgradeConfig rules = config.RestUpgradeConfig;
            if (commandId == "rest")
            {
                if (restUpgrade.TryRest(
                        campaign,
                        progress.RunState,
                        rules,
                        out _,
                        out string result,
                        out RunCampaignFailure failure))
                {
                    message = result;
                    SaveRun(null);
                }
                else
                {
                    message = $"회복 실패: {failure}";
                }
                return;
            }

            if (commandId == "cycle")
            {
                RunRestUpgradeCardOption selected = restUpgrade.CycleCard(
                    campaign,
                    progress,
                    selectedUpgradeCardId);
                selectedUpgradeCardId = selected?.OwnedCardId;
                message = selected == null
                    ? "강화할 카드가 없습니다."
                    : $"강화 대상: {selected.DisplayName} " +
                      $"Lv.{selected.CurrentLevel}";
                return;
            }

            if (commandId != "upgrade")
            {
                return;
            }

            if (restUpgrade.TryUpgrade(
                    campaign,
                    progress,
                    rules,
                    selectedUpgradeCardId,
                    out RunRestUpgradeCardOption upgraded,
                    out string upgradeResult,
                    out RunCampaignFailure upgradeFailure))
            {
                selectedUpgradeCardId = upgraded.OwnedCardId;
                message = upgradeResult;
                SaveRun(null);
            }
            else
            {
                message = $"강화 실패: {upgradeFailure}";
            }
        }

        private void ResolveFinalShopCommand(string commandId)
        {
            if (commandId == "reroll")
            {
                if (shop.TryReroll(
                        campaign,
                        progress.RunState,
                        config.ShopEconomyConfig,
                        out _,
                        out string result,
                        out RunCampaignFailure failure))
                {
                    message = result;
                    SaveRun(null);
                }
                else
                {
                    message = $"리롤 실패: {failure}";
                }
                return;
            }

            if (commandId == "leave")
            {
                if (shop.TryLeave(
                        campaign,
                        progress.RunState,
                        out string result,
                        out RunCampaignFailure failure))
                {
                    message = result;
                    SaveRun(null);
                }
                else
                {
                    message = $"상점 종료 실패: {failure}";
                }
                return;
            }

            const string prefix = "buy:";
            if (commandId == null || !commandId.StartsWith(prefix))
            {
                return;
            }

            string slotId = commandId.Substring(prefix.Length);
            if (shop.TryBuy(
                    campaign,
                    progress,
                    config.EnchantDatabase,
                    config.ShopEconomyConfig,
                    slotId,
                    out _,
                    out string purchaseResult,
                    out EnchantAttachmentFailure attachmentFailure,
                    out RunCampaignFailure purchaseFailure))
            {
                message = purchaseResult;
                SaveRun(null);
            }
            else
            {
                message =
                    $"구매 실패: {purchaseFailure} / {attachmentFailure}";
            }
        }

        private string CreateFinalRunSummary()
        {
            RunBattleState run = progress.RunState;
            return $"완료 {campaign.CompletedNodeCount}/" +
                   $"{config.RunStartProgressionConfig.TotalNodeCount} · " +
                   $"HP {run.CurrentHealth}/{run.MaximumHealth} · " +
                   $"골드 {run.Gold}";
        }

        private void SetFinalUiActive(bool active)
        {
            GameObject canvasObject =
                FinalUiRoot?.RootCanvas?.gameObject;
            if (canvasObject != null && canvasObject.activeSelf != active)
            {
                canvasObject.SetActive(active);
            }
        }
    }
}
