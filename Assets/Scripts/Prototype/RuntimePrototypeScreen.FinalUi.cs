using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HaveABreak.Cards
{
    public sealed partial class RuntimePrototypeScreen : MonoBehaviour
    {
        private RuntimeGameScreen? finalCampaignScreen;
        private bool finalStartScreenRequested;

        private void InitializeFinalUi()
        {
            if (FinalUiRoot != null)
            {
                return;
            }

            FinalUiRoot = gameObject.AddComponent<RuntimeGameUiRoot>();
            FinalUiRoot.NewRunRequested += RequestFinalStartNewRun;
            FinalUiRoot.ContinueRequested += RequestFinalContinueRun;
            FinalUiRoot.ConfirmationCancelled += CancelFinalConfirmation;
            FinalUiRoot.ConfirmationAccepted += AcceptFinalConfirmation;
            FinalUiRoot.RunPreparationCardToggleRequested +=
                ToggleFinalRunPreparationCard;
            FinalUiRoot.RunPreparationCancelled += CancelRunPreparation;
            FinalUiRoot.RunPreparationConfirmed += ConfirmRunPreparation;
            FinalUiRoot.NodeSelectionRequested += SelectFinalNode;
            FinalUiRoot.NodeResolutionCommandRequested +=
                ExecuteFinalNodeCommand;
            FinalUiRoot.BattleCommandRequested += ExecuteFinalBattleCommand;
            FinalUiRoot.BattleCardDropped += ExecuteFinalBattleCardDrop;
            FinalUiRoot.RewardCommandRequested += ExecuteFinalRewardCommand;
            FinalUiRoot.RunResultNewRunRequested += RequestFinalStartNewRun;
            FinalUiRoot.ReturnToStartRequested += ShowFinalStartScreen;
            FinalUiRoot.Initialize();
            RefreshFinalUiVisibility();
        }

        private void OnDestroy()
        {
            if (FinalUiRoot == null)
            {
                return;
            }

            FinalUiRoot.NewRunRequested -= RequestFinalStartNewRun;
            FinalUiRoot.ContinueRequested -= RequestFinalContinueRun;
            FinalUiRoot.ConfirmationCancelled -= CancelFinalConfirmation;
            FinalUiRoot.ConfirmationAccepted -= AcceptFinalConfirmation;
            FinalUiRoot.RunPreparationCardToggleRequested -=
                ToggleFinalRunPreparationCard;
            FinalUiRoot.RunPreparationCancelled -= CancelRunPreparation;
            FinalUiRoot.RunPreparationConfirmed -= ConfirmRunPreparation;
            FinalUiRoot.NodeSelectionRequested -= SelectFinalNode;
            FinalUiRoot.NodeResolutionCommandRequested -=
                ExecuteFinalNodeCommand;
            FinalUiRoot.BattleCommandRequested -= ExecuteFinalBattleCommand;
            FinalUiRoot.BattleCardDropped -= ExecuteFinalBattleCardDrop;
            FinalUiRoot.RewardCommandRequested -= ExecuteFinalRewardCommand;
            FinalUiRoot.RunResultNewRunRequested -= RequestFinalStartNewRun;
            FinalUiRoot.ReturnToStartRequested -= ShowFinalStartScreen;
        }

        private bool TryShowFinalUi()
        {
            if (FinalUiRoot == null)
            {
                return false;
            }

            if (pendingRunRequest?.ConfirmationRequired == true)
            {
                FinalUiRoot.BindConfirmation(
                    pendingRunRequest.Title,
                    pendingRunRequest.Body,
                    pendingRunRequest.ConfirmLabel);
                SetFinalUiActive(true);
                FinalUiRoot.ShowScreen(RuntimeGameScreen.Confirmation);
                return true;
            }

            if (runPreparationCards != null && deckSelection.IsOpen)
            {
                SetFinalUiActive(true);
                FinalUiRoot.ShowScreen(RuntimeGameScreen.RunPreparation);
                return true;
            }

            if (finalStartScreenRequested)
            {
                SetFinalUiActive(true);
                FinalUiRoot.ShowScreen(RuntimeGameScreen.Start);
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

            if (campaign.Phase == RunCampaignPhase.Battle)
            {
                if (finalCampaignScreen != RuntimeGameScreen.Battle)
                {
                    RefreshFinalBattle();
                }
                SetFinalUiActive(true);
                FinalUiRoot.ShowScreen(RuntimeGameScreen.Battle);
                return true;
            }

            if (campaign.Phase == RunCampaignPhase.Reward)
            {
                if (finalCampaignScreen != RuntimeGameScreen.Reward)
                {
                    RefreshFinalReward();
                }
                SetFinalUiActive(true);
                FinalUiRoot.ShowScreen(RuntimeGameScreen.Reward);
                return true;
            }

            if (campaign.Phase == RunCampaignPhase.Completed ||
                campaign.Phase == RunCampaignPhase.Defeated)
            {
                RuntimeGameScreen screen =
                    campaign.Phase == RunCampaignPhase.Completed
                        ? RuntimeGameScreen.Completed
                        : RuntimeGameScreen.Defeated;
                if (finalCampaignScreen != screen)
                {
                    RefreshFinalRunResult(screen);
                }
                SetFinalUiActive(true);
                FinalUiRoot.ShowScreen(screen);
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

        private void RequestFinalStartNewRun()
        {
            RequestStartNewRun();
            if (pendingRunRequest == null)
            {
                finalStartScreenRequested = false;
            }
            TryShowFinalUi();
        }

        private void RequestFinalContinueRun()
        {
            RequestContinueRun();
            if (pendingRunRequest == null)
            {
                finalStartScreenRequested = false;
            }
            TryShowFinalUi();
        }

        private void CancelFinalConfirmation()
        {
            pendingRunRequest = null;
            TryShowFinalUi();
        }

        private void AcceptFinalConfirmation()
        {
            RunLifecycleRequestKind kind =
                pendingRunRequest?.Kind ?? RunLifecycleRequestKind.None;
            pendingRunRequest = null;
            if (kind == RunLifecycleRequestKind.StartNewRun)
            {
                finalStartScreenRequested = false;
                BeginRunPreparation();
            }
            else if (kind == RunLifecycleRequestKind.ContinueRun)
            {
                finalStartScreenRequested = false;
                ContinueRun();
            }
            TryShowFinalUi();
        }

        private void ShowFinalStartScreen()
        {
            finalStartScreenRequested = true;
            finalCampaignScreen = null;
            SetFinalUiActive(true);
            FinalUiRoot.ShowScreen(RuntimeGameScreen.Start);
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

        private void RefreshFinalRunResult(RuntimeGameScreen screen)
        {
            if (FinalUiRoot == null || campaign == null || progress == null)
            {
                return;
            }

            bool completed = screen == RuntimeGameScreen.Completed;
            string title = completed ? "런 완료" : "런 패배";
            string result = completed
                ? "보스를 쓰러뜨리고 런을 완료했습니다."
                : "플레이어 HP가 0이 되어 런이 종료되었습니다.";
            string summary =
                $"{CreateFinalRunSummary()}\n" +
                $"완료 전투 {progress.CompletedEncounterCount}";
            FinalUiRoot.BindRunResult(title, summary, result);
            finalCampaignScreen = screen;
        }

        private void RefreshFinalBattle()
        {
            if (FinalUiRoot == null || campaign == null || progress == null)
            {
                return;
            }

            BattleScreenSnapshot snapshot =
                battleScreen.CreateSnapshot(progress, campaign);
            List<RuntimeGameCommandOption> options = new();
            if (!snapshot.Available)
            {
                options.Add(new RuntimeGameCommandOption(
                    "restart",
                    "전투 다시 시작"));
                FinalUiRoot.BindBattle(
                    "전투",
                    options,
                    snapshot.ErrorText ?? "활성 전투를 찾을 수 없습니다.",
                    message);
                FinalUiRoot.BindBattleHud(
                    progress.RunState.CurrentHealth,
                    progress.RunState.MaximumHealth,
                    progress.RunState.Gold,
                    campaign.CompletedNodeCount + 1,
                    0,
                    0,
                    "활성 전투가 없습니다.",
                    $"덱 {progress.RunDeck.Count}장");
                FinalUiRoot.BindBattleHand(
                    System.Array.Empty<RuntimeCardPresentation>());
                FinalUiRoot.BindBattleConsumables(
                    System.Array.Empty<BattleConsumableActionOption>());
                finalCampaignScreen = RuntimeGameScreen.Battle;
                return;
            }

            List<RuntimeCardPresentation> handCards =
                new(snapshot.Hand.Length);
            foreach (BattleEnemyDisplayOption enemy in snapshot.Enemies)
            {
                if (!enemy.IsOccupied)
                {
                    continue;
                }
                string status = string.IsNullOrWhiteSpace(enemy.StatusText)
                    ? string.Empty
                    : $"\n{enemy.StatusText}";
                options.Add(new RuntimeGameCommandOption(
                    $"enemy:{enemy.EnemyId}",
                    $"[적 대상] {enemy.DisplayText}{status}",
                    enemy.CanSelect));
            }

            foreach (BattleHandCardActionOption card in snapshot.Hand)
            {
                handCards.Add(
                    RuntimeCardPresentation.FromBattleHand(card));
                if (card.BanishTargets.Length > 0)
                {
                    string target =
                        card.SelectedBanishTarget?.DisplayLabel ??
                        "소멸 대상 선택";
                    options.Add(new RuntimeGameCommandOption(
                        $"banish:{card.BattleCardId}",
                        $"[소멸 대상 변경] {target}",
                        !snapshot.SessionFinished));
                }
            }

            foreach (BattleMonsterDisplayOption monster in snapshot.Monsters)
            {
                if (!monster.IsOccupied)
                {
                    continue;
                }
                string block = string.IsNullOrWhiteSpace(monster.BlockReason)
                    ? string.Empty
                    : $"\n{monster.BlockReason}";
                options.Add(new RuntimeGameCommandOption(
                    $"attack:{monster.BattleCardId}",
                    $"[공격] {monster.DisplayText}{block}",
                    monster.CanAttack));
            }

            options.Add(new RuntimeGameCommandOption(
                "end-turn",
                "턴 종료",
                snapshot.CanEndTurn));
            if (snapshot.Chain.CanPlayerPass)
            {
                options.Add(new RuntimeGameCommandOption(
                    "chain-pass",
                    "체인 해결",
                    true));
            }
            options.Add(new RuntimeGameCommandOption(
                "settle",
                snapshot.FinishedText ?? "전투 정산",
                snapshot.CanSettle));

            List<string> summaryParts = new()
            {
                snapshot.PlayerSummaryText,
                snapshot.ZoneSummaryText
            };
            if (!string.IsNullOrWhiteSpace(snapshot.PlayerStatusText))
            {
                summaryParts.Add(snapshot.PlayerStatusText);
            }
            if (snapshot.InstalledCards.Length > 0)
            {
                summaryParts.Add(
                    "설치: " + string.Join(
                        ", ",
                        snapshot.InstalledCards.Select(
                            option => option.DisplayName)));
            }
            if (snapshot.RecentEvents.Length > 0)
            {
                summaryParts.Add(
                    "최근: " + string.Join(
                        "\n",
                        snapshot.RecentEvents
                            .TakeLast(3)
                            .Select(option => option.DisplayText)));
            }
            if (snapshot.Chain.IsActive)
            {
                summaryParts.Add(snapshot.Chain.DisplayText);
            }

            FinalUiRoot.BindBattle(
                snapshot.TitleText,
                options,
                string.Join("\n", summaryParts),
                message);
            BattleRuntimeState runtime =
                progress.ActiveEncounter?.Session?.Runtime;
            string path = campaign.SelectedNodePath.Count == 0
                ? "아직 이동한 노드가 없습니다."
                : string.Join(
                    "\n",
                    campaign.SelectedNodePath.Select(
                        (nodeId, index) => $"{index + 1}. {nodeId}"));
            string currentNode = campaign.ActiveNode == null
                ? string.Empty
                : $"\n\n현재: {campaign.ActiveNode.DisplayName}";
            string deckDetails = string.Join(
                "\n",
                progress.RunDeck.Cards.Select(
                    (card, index) =>
                        $"{index + 1}. " +
                        $"{card?.Card?.DisplayName ?? "알 수 없는 카드"}"));
            FinalUiRoot.BindBattleHud(
                runtime?.Player.CurrentHealth ??
                    progress.RunState.CurrentHealth,
                runtime?.Player.MaximumHealth ??
                    progress.RunState.MaximumHealth,
                progress.RunState.Gold,
                campaign.CompletedNodeCount + 1,
                runtime?.CardPlay.Mana.CurrentMana ?? 0,
                runtime?.CardPlay.Mana.MaximumMana ?? 0,
                path + currentNode,
                string.IsNullOrWhiteSpace(deckDetails)
                    ? "덱이 비어 있습니다."
                    : deckDetails);
            FinalUiRoot.BindBattleHand(handCards);
            FinalUiRoot.BindBattleConsumables(snapshot.Consumables);
            finalCampaignScreen = RuntimeGameScreen.Battle;
        }

        private void ExecuteFinalBattleCardDrop(
            string cardCommandId,
            string targetCommandId)
        {
    if (TryExecuteFinalFieldCardDrop(
            cardCommandId,
            targetCommandId))
    {
        return;
    }

            if (!string.IsNullOrWhiteSpace(targetCommandId) &&
                TryReadCommandValue(
                    targetCommandId,
                    "enemy:",
                    out string enemyId))
            {
                battleScreen.SelectEnemy(progress, enemyId);
            }

            ExecuteFinalBattleCommand(cardCommandId);
        }

        private void ExecuteFinalBattleCommand(string commandId)
        {
            if (campaign == null || progress == null ||
                string.IsNullOrWhiteSpace(commandId))
            {
                return;
            }

            if (commandId == "restart")
            {
                BeginSelectedBattle();
            }
            else if (commandId == "end-turn")
            {
                BattleEndTurnCommandResult command =
                    battleScreen.TryEndPlayerTurn(progress, campaign);
                message = command.Message;
                if (command.Succeeded)
                {
                    SaveRun(null);
                }
            }
            else if (commandId == "chain-pass")
            {
                BattleChainCommandResult command =
                    battleScreen.TryPassAndResolveChain(progress);
                message = command.Message;
                if (command.Succeeded)
                {
                    SaveRun(null);
                }
            }
            else if (commandId == "settle")
            {
                SettleBattle();
            }
            else if (TryReadCommandValue(
                         commandId,
                         "enemy:",
                         out string enemyId))
            {
                string pendingCardId =
                    battleScreen.PendingTargetedCardId;
                string pendingAttackerId =
                    battleScreen.PendingAttackerId;
                if (battleScreen.SelectEnemy(progress, enemyId))
                {
                    if (!string.IsNullOrWhiteSpace(pendingCardId))
                    {
                        BattleCardPlayCommandResult command =
                            battleScreen.TryPlayCard(
                                progress,
                                pendingCardId);
                        message = command.Message;
                        if (command.Succeeded)
                        {
                            SaveRun(null);
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(
                                 pendingAttackerId))
                    {
                        BattleMonsterAttackCommandResult command =
                            battleScreen.TryAttack(
                                progress,
                                pendingAttackerId);
                        message = command.Message;
                    }
                }
            }
            else if (TryReadCommandValue(
                         commandId,
                         "banish:",
                         out string banishSourceId))
            {
                battleScreen.CycleBanishTarget(progress, banishSourceId);
            }
            else if (TryReadCommandValue(
                         commandId,
                         "play:",
                         out string cardId))
            {
                BattleCardInstance card = progress.ActiveEncounter?
                    .Session?.Runtime?.Deck.Zones.Find(cardId);
                bool requiresTarget = card != null &&
                    CardEffectRegistrationCatalog.TryFind(
                        card.SourceCard.CatalogCardId,
                        out CardEffectRegistration registration) &&
                    registration.Route == CardEffectRoute.TargetedSkill;
                if (requiresTarget &&
                    string.IsNullOrWhiteSpace(
                        battleScreen.SelectedEnemyId))
                {
                    battleScreen.TryBeginCardTargeting(
                        progress,
                        cardId,
                        out message);
                }
                else
                {
                    BattleCardPlayCommandResult command =
                        battleScreen.TryPlayCard(progress, cardId);
                    message = command.Message;
                    if (command.Succeeded)
                    {
                        SaveRun(null);
                    }
                }
            }
            else if (TryReadCommandValue(
                         commandId,
                         "attack:",
                         out string monsterId))
            {
                battleScreen.TryBeginAttackTargeting(
                    progress,
                    monsterId,
                    out message);
            }
            else if (TryReadCommandValue(
                         commandId,
                         "consumable:",
                         out string itemId))
            {
                BattleConsumableCommandResult command =
                    battleScreen.TryUseConsumable(progress, itemId);
                message = command.Message;
                if (command.Succeeded)
                {
                    SaveRun(null);
                }
            }

            finalCampaignScreen = null;
            if (campaign.Phase == RunCampaignPhase.Battle)
            {
                RefreshFinalBattle();
                SetFinalUiActive(true);
                FinalUiRoot.ShowScreen(RuntimeGameScreen.Battle);
            }
            else
            {
                RefreshFinalUiVisibility();
            }
        }

        private void RefreshFinalReward()
        {
            if (FinalUiRoot == null || campaign == null || progress == null)
            {
                return;
            }

            RunBattleRewardSnapshot snapshot = battleReward.CreateSnapshot(
                campaign,
                progress,
                config.EnchantDatabase);
            List<RuntimeGameCommandOption> options = new();
            foreach (RunBattleEnchantRewardOption option in
                     snapshot.EnchantOptions)
            {
                string target = string.IsNullOrWhiteSpace(option.TargetLabel)
                    ? string.Empty
                    : $"\n대상: {option.TargetLabel}";
                string block = string.IsNullOrWhiteSpace(option.BlockReason)
                    ? string.Empty
                    : $"\n{option.BlockReason}";
                options.Add(new RuntimeGameCommandOption(
                    $"enchant:{option.DefinitionId}",
                    $"[인첸트] {option.DisplayText}{target}{block}",
                    option.CanClaim));
            }

            foreach (RunBattleConsumableRewardOption option in
                     snapshot.ConsumableOptions)
            {
                string block = string.IsNullOrWhiteSpace(option.BlockReason)
                    ? string.Empty
                    : $"\n{option.BlockReason}";
                options.Add(new RuntimeGameCommandOption(
                    $"reward-consumable:{option.ItemId}",
                    $"[소모아이템] {option.DisplayText}{block}",
                    option.CanClaim));
            }

            string completionBlock = snapshot.CanComplete ||
                                     !string.IsNullOrWhiteSpace(
                                         snapshot.ErrorText)
                ? string.Empty
                : "\n필수 보상을 모두 선택하세요.";
            options.Add(new RuntimeGameCommandOption(
                "complete",
                $"보상 완료 · 다음 노드{completionBlock}",
                snapshot.CanComplete));
            List<string> summaryParts = new()
            {
                snapshot.GoldLabel,
                CreateFinalRunSummary()
            };
            if (progress.ActiveEncounter?.VictoryRewards
                    ?.GrantsFinalBossPermanentReward == true)
            {
                summaryParts.Add("영구 카드 보상 · 전투 정산 시 적용 완료");
            }
            FinalUiRoot.BindReward(
                options,
                string.Join("\n", summaryParts),
                snapshot.ErrorText ?? message);
            finalCampaignScreen = RuntimeGameScreen.Reward;
        }

        private void ExecuteFinalRewardCommand(string commandId)
        {
            if (campaign == null || progress == null ||
                string.IsNullOrWhiteSpace(commandId))
            {
                return;
            }

            bool changed = false;
            if (commandId == "complete")
            {
                changed = battleReward.TryComplete(
                    campaign,
                    progress,
                    out string result,
                    out RunEncounterProgressFailure failure);
                message = changed ? result : $"보상 미완료: {failure}";
            }
            else if (TryReadCommandValue(
                         commandId,
                         "enchant:",
                         out string definitionId))
            {
                changed = battleReward.TryClaimEnchant(
                    campaign,
                    progress,
                    config.EnchantDatabase,
                    definitionId,
                    out _,
                    out string result,
                    out EnchantAttachmentFailure attachmentFailure,
                    out BattleVictoryEnchantRewardFailure failure);
                message = changed
                    ? result
                    : $"인첸트 보상 실패: {failure} / {attachmentFailure}";
            }
            else if (TryReadCommandValue(
                         commandId,
                         "reward-consumable:",
                         out string itemId))
            {
                changed = battleReward.TryClaimConsumable(
                    campaign,
                    progress,
                    itemId,
                    out _,
                    out string result,
                    out BattleVictoryConsumableRewardFailure failure);
                message = changed
                    ? result
                    : $"소모아이템 보상 실패: {failure}";
            }

            if (changed)
            {
                SaveRun(null);
            }

            finalCampaignScreen = null;
            if (campaign.Phase == RunCampaignPhase.Reward)
            {
                RefreshFinalReward();
                SetFinalUiActive(true);
                FinalUiRoot.ShowScreen(RuntimeGameScreen.Reward);
            }
            else
            {
                TryShowFinalUi();
            }
        }

        private static bool TryReadCommandValue(
            string commandId,
            string prefix,
            out string value)
        {
            if (commandId.StartsWith(prefix) &&
                commandId.Length > prefix.Length)
            {
                value = commandId.Substring(prefix.Length);
                return true;
            }

            value = null;
            return false;
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
