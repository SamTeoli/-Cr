using System;
using System.Collections.Generic;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HaveABreak.Editor
{
    internal static class RuntimeGameUiRootValidation
    {
        [MenuItem("Have a Break/Tests/Validate Final UGUI Start Screen")]
        private static void RunFromMenu()
        {
            Validate();
        }

        internal static bool Validate()
        {
            EventSystem previousEventSystem = EventSystem.current;
            GameObject host = new("Final UI Root Validation");
            bool newRunRequested = false;
            bool continueRequested = false;
            string toggledCardId = null;
            bool preparationCancelled = false;
            bool preparationConfirmed = false;
            string selectedNodeId = null;
            string resolutionCommandId = null;
            string battleCommandId = null;
            string rewardCommandId = null;
            bool confirmationCancelled = false;
            bool confirmationAccepted = false;
            bool resultNewRunRequested = false;
            bool returnToStartRequested = false;
            CardData validationCard = null;

            try
            {
                RuntimeGameUiRoot root =
                    host.AddComponent<RuntimeGameUiRoot>();
                root.NewRunRequested += () => newRunRequested = true;
                root.ContinueRequested += () => continueRequested = true;
                root.RunPreparationCardToggleRequested +=
                    cardId => toggledCardId = cardId;
                root.RunPreparationCancelled +=
                    () => preparationCancelled = true;
                root.RunPreparationConfirmed +=
                    () => preparationConfirmed = true;
                root.NodeSelectionRequested +=
                    nodeId => selectedNodeId = nodeId;
                root.NodeResolutionCommandRequested +=
                    commandId => resolutionCommandId = commandId;
                root.BattleCommandRequested +=
                    commandId => battleCommandId = commandId;
                root.RewardCommandRequested +=
                    commandId => rewardCommandId = commandId;
                root.ConfirmationCancelled +=
                    () => confirmationCancelled = true;
                root.ConfirmationAccepted +=
                    () => confirmationAccepted = true;
                root.RunResultNewRunRequested +=
                    () => resultNewRunRequested = true;
                root.ReturnToStartRequested +=
                    () => returnToStartRequested = true;
                root.Initialize();

                CanvasScaler scaler =
                    root.RootCanvas.GetComponent<CanvasScaler>();
                bool structure = root.CurrentScreen ==
                                     RuntimeGameScreen.Start &&
                                 root.RootCanvas.renderMode ==
                                     RenderMode.ScreenSpaceOverlay &&
                                 scaler.uiScaleMode ==
                                     CanvasScaler.ScaleMode.ScaleWithScreenSize &&
                                 scaler.referenceResolution ==
                                     new Vector2(1920f, 1080f) &&
                                 root.NewRunButton != null &&
                                 root.ContinueButton != null &&
                                 root.GetComponentInChildren<
                                     InputSystemUIInputModule>(true) != null &&
                                 root.GetComponentInChildren<
                                     StandaloneInputModule>(true) == null;

                root.NewRunButton.onClick.Invoke();
                root.ContinueButton.onClick.Invoke();
                bool commands = newRunRequested && continueRequested;

                root.BindConfirmation(
                    "새 런 확인",
                    "현재 진행이 교체됩니다.",
                    "새 런 시작");
                root.ShowScreen(RuntimeGameScreen.Confirmation);
                root.CancelConfirmationButton.onClick.Invoke();
                root.ConfirmActionButton.onClick.Invoke();
                bool confirmation =
                    root.CurrentScreen == RuntimeGameScreen.Confirmation &&
                    root.ConfirmationTitleText.text == "새 런 확인" &&
                    root.ConfirmationBodyText.text ==
                        "현재 진행이 교체됩니다." &&
                    root.ConfirmActionButton
                        .GetComponentInChildren<Text>().text == "새 런 시작" &&
                    confirmationCancelled && confirmationAccepted;

                RunDeckSelectionOption[] options =
                    CreatePreparationOptions(out validationCard);
                root.BindRunPreparation(
                    options,
                    1,
                    "덱 준비 검증",
                    true);
                root.ShowScreen(RuntimeGameScreen.RunPreparation);
                Button cardButton =
                    root.RunPreparationCardList.GetChild(0)
                        .GetComponent<Button>();
                RuntimeCardView preparationCard =
                    cardButton.GetComponent<RuntimeCardView>();
                cardButton.onClick.Invoke();
                root.CancelRunPreparationButton.onClick.Invoke();
                root.ConfirmRunPreparationButton.onClick.Invoke();
                bool preparation =
                    root.CurrentScreen == RuntimeGameScreen.RunPreparation &&
                    root.RunPreparationSelectedCountText.text ==
                        "선택 1장 / 보유 1장" &&
                    root.RunPreparationMessageText.text == "덱 준비 검증" &&
                    root.RunPreparationCardList.childCount == 1 &&
                    preparationCard != null &&
                    preparationCard.NameText != null &&
                    preparationCard.ArtPlaceholderText.text == "일러스트" &&
                    preparationCard.MetadataText.text == "스킬" &&
                    preparationCard.Presentation.Rarity == CardRarity.Common &&
                    preparationCard.SelectionText.gameObject.activeSelf &&
                    preparationCard.SelectionText.text == "선택 1" &&
                    preparationCard.AccessibilityText.text.Contains("마력") &&
                    preparationCard.FrameOverlayImage != null &&
                    (preparationCard.FrameOverlayImage.sprite != null ||
                     preparationCard.FrameImage.color != Color.clear) &&
                    root.ConfirmRunPreparationButton.interactable &&
                    toggledCardId == options[0].OwnedCardId &&
                    preparationCancelled &&
                    preparationConfirmed &&
                    !root.NewRunButton.gameObject
                        .transform.parent.parent.gameObject.activeSelf;

                RuntimeGameCommandOption[] nodeOptions =
                {
                    new("NODE-VALIDATION", "검증 노드")
                };
                root.BindNodeSelection(
                    nodeOptions,
                    "노드 선택 요약",
                    "노드 선택 메시지");
                root.ShowScreen(RuntimeGameScreen.NodeSelection);
                root.NodeSelectionCommandList.GetChild(0)
                    .GetComponent<Button>().onClick.Invoke();
                bool nodeSelection =
                    root.CurrentScreen == RuntimeGameScreen.NodeSelection &&
                    root.NodeSelectionSummaryText.text == "노드 선택 요약" &&
                    root.NodeSelectionMessageText.text == "노드 선택 메시지" &&
                    selectedNodeId == "NODE-VALIDATION";

                RuntimeGameCommandOption[] resolutionOptions =
                {
                    new("leave", "노드 나가기")
                };
                root.BindNodeResolution(
                    "검증 노드",
                    resolutionOptions,
                    "노드 진행 요약",
                    "노드 진행 메시지");
                root.ShowScreen(RuntimeGameScreen.NodeResolution);
                root.NodeResolutionCommandList.GetChild(0)
                    .GetComponent<Button>().onClick.Invoke();
                bool nodeResolution =
                    root.CurrentScreen == RuntimeGameScreen.NodeResolution &&
                    root.NodeResolutionTitleText.text == "검증 노드" &&
                    root.NodeResolutionSummaryText.text == "노드 진행 요약" &&
                    root.NodeResolutionMessageText.text == "노드 진행 메시지" &&
                    resolutionCommandId == "leave";

                RuntimeGameCommandOption[] battleOptions =
                {
                    new("end-turn", "턴 종료")
                };
                root.BindBattle(
                    "검증 전투",
                    battleOptions,
                    "전투 상태 요약",
                    "전투 명령 메시지");
                root.ShowScreen(RuntimeGameScreen.Battle);
                root.BattleEndTurnButton.onClick.Invoke();
                bool genericBattleCommand = battleCommandId == "end-turn";
                RuntimeCardPresentation[] battleCards =
                {
                    new(
                        "play:ACTIVE",
                        "검증 스킬",
                        "C-VALIDATION",
                        CardType.Skill,
                        CardRarity.Legendary,
                        2,
                        null,
                        null,
                        "카드 효과 검증",
                        false,
                        0,
                        true,
                        null,
                        "검증 스킬 접근성 설명"),
                    new(
                        "play:BLOCKED",
                        "검증 몬스터",
                        "M-VALIDATION",
                        CardType.Monster,
                        CardRarity.Rare,
                        3,
                        4,
                        5,
                        "몬스터 효과 검증",
                        false,
                        0,
                        false,
                        "마력이 부족합니다.",
                        "검증 몬스터 사용 불가")
                };
                root.BindBattleHand(battleCards);
                RuntimeCardView activeCard =
                    root.BattleHandCardList.GetChild(0)
                        .GetComponent<RuntimeCardView>();
                RuntimeCardView blockedCard =
                    root.BattleHandCardList.GetChild(1)
                        .GetComponent<RuntimeCardView>();
                RuntimeBattleHandCardHover handHover =
                    activeCard.GetComponent<RuntimeBattleHandCardHover>();
                handHover.OnPointerEnter(null);
                bool hoverRaised =
                    activeCard.transform.localScale.x > 1f &&
                    activeCard.GetComponent<Canvas>() == null;
                handHover.OnPointerExit(null);
                activeCard.ClickButton.onClick.Invoke();
                bool detailOpened =
                    root.BattleDetailPanel.activeSelf &&
                    root.BattleDetailTitleText.text == "검증 스킬" &&
                    root.BattleDetailBodyText.text.Contains("카드 효과 검증");
                root.BattleDetailActionButton.onClick.Invoke();
                bool cardFoundation =
                    root.BattleHandCardList.childCount == 2 &&
                    activeCard.ArtPlaceholderText.text == "일러스트" &&
                    activeCard.MetadataText.text == "스킬" &&
                    activeCard.Presentation.Rarity == CardRarity.Legendary &&
                    activeCard.GetComponent<RuntimeBattleHandCardHover>() !=
                    null &&
                    activeCard.GetComponent<Canvas>() == null &&
                    hoverRaised &&
                    activeCard.FrameOverlayImage != null &&
                    (activeCard.FrameOverlayImage.sprite != null ||
                     activeCard.FrameImage.color != Color.clear) &&
                    !activeCard.RarityAccentImage.gameObject.activeSelf &&
                    blockedCard.StatsText.text == "공격 4, 생명력 5" &&
                    blockedCard.MetadataText.text == "몬스터" &&
                    blockedCard.Presentation.Rarity == CardRarity.Rare &&
                    blockedCard.ClickButton.interactable &&
                    blockedCard.BlockReasonText.gameObject.activeSelf &&
                    blockedCard.BlockReasonText.text.Contains(
                        "마력이 부족합니다.") &&
                    blockedCard.FrameOverlayImage != null &&
                    (blockedCard.FrameOverlayImage.sprite != null ||
                     blockedCard.FrameImage.color != Color.clear) &&
                    detailOpened &&
                    battleCommandId == "play:ACTIVE";
                bool battle =
                    root.CurrentScreen == RuntimeGameScreen.Battle &&
                    root.BattleTitleText.text == "검증 전투" &&
                    root.BattleSummaryText.text == "전투 상태 요약" &&
                    root.BattleMessageText.text.StartsWith(
                        "전투 명령 메시지",
                        StringComparison.Ordinal) &&
                    genericBattleCommand &&
                    cardFoundation;

                RuntimeGameCommandOption[] rewardOptions =
                {
                    new("complete", "보상 완료")
                };
                root.BindReward(
                    rewardOptions,
                    "골드 10 수령 완료",
                    "보상 검증 메시지");
                root.ShowScreen(RuntimeGameScreen.Reward);
                root.RewardCommandList.GetChild(0)
                    .GetComponent<Button>().onClick.Invoke();
                bool reward =
                    root.CurrentScreen == RuntimeGameScreen.Reward &&
                    root.RewardSummaryText.text == "골드 10 수령 완료" &&
                    root.RewardMessageText.text == "보상 검증 메시지" &&
                    rewardCommandId == "complete";

                root.BindRunResult(
                    "런 완료",
                    "완료 12/12",
                    "보스를 쓰러뜨렸습니다.");
                root.ShowScreen(RuntimeGameScreen.Completed);
                root.RunResultNewRunButton.onClick.Invoke();
                root.ReturnToStartButton.onClick.Invoke();
                bool completed =
                    root.CurrentScreen == RuntimeGameScreen.Completed &&
                    root.RunResultTitleText.text == "런 완료" &&
                    root.RunResultSummaryText.text == "완료 12/12" &&
                    root.RunResultMessageText.text ==
                        "보스를 쓰러뜨렸습니다." &&
                    resultNewRunRequested && returnToStartRequested;
                root.BindRunResult(
                    "런 패배",
                    "완료 5/12",
                    "플레이어 HP가 0입니다.");
                root.ShowScreen(RuntimeGameScreen.Defeated);
                bool defeated =
                    root.CurrentScreen == RuntimeGameScreen.Defeated &&
                    root.RunResultTitleText.text == "런 패배" &&
                    root.RunResultSummaryText.text == "완료 5/12" &&
                    root.RunResultMessageText.text ==
                        "플레이어 HP가 0입니다.";

                root.ShowScreen(RuntimeGameScreen.Defeated);
                bool routing = root.CurrentScreen ==
                               RuntimeGameScreen.Defeated &&
                               !root.RunPreparationCardList.gameObject
                                   .activeInHierarchy &&
                               !root.NewRunButton.gameObject
                                    .transform.parent.parent.gameObject.activeSelf;

                bool valid = structure && commands && confirmation &&
                             preparation &&
                             nodeSelection && nodeResolution && battle &&
                             reward && completed && defeated && routing;
                if (valid)
                {
                    Debug.Log(
                        "Final UGUI start screen validation passed: " +
                        "canvas scaling, Input System module, start layout, " +
                        "button commands, run preparation binding, " +
                        "confirmation commands, " +
                        "node selection and resolution commands, " +
                        "battle state and commands, reusable card structure, " +
                        "type labels and rarity frames, selection and disabled states, " +
                        "reward state and commands, " +
                        "completed and defeated run results, " +
                        "and screen visibility.");
                }
                else
                {
                    Debug.LogError(
                        "Final UGUI start screen validation failed. " +
                        $"structure={structure}, commands={commands}, " +
                        $"confirmation={confirmation}, " +
                        $"preparation={preparation}, " +
                        $"nodeSelection={nodeSelection}, " +
                        $"nodeResolution={nodeResolution}, battle={battle}, " +
                        $"reward={reward}, " +
                        $"completed={completed}, defeated={defeated}, " +
                        $"routing={routing}");
                }

                return valid;
            }
            finally
            {
                Object.DestroyImmediate(host);
                if (validationCard != null)
                {
                    Object.DestroyImmediate(validationCard);
                }
                if (previousEventSystem != null)
                {
                    EventSystem.current = previousEventSystem;
                }
            }
        }

        private static RunDeckSelectionOption[] CreatePreparationOptions(
            out CardData card)
        {
            card = ScriptableObject.CreateInstance<SkillCardData>();
            RunCardInstance ownedCard = new(
                card,
                "OWNED-UI-VALIDATION",
                1);
            RunOwnedCardState ownedCards = new();
            if (!ownedCards.TryAdd(ownedCard, out _))
            {
                return Array.Empty<RunDeckSelectionOption>();
            }

            RunDeckSelectionViewModel selection = new();
            selection.OpenWithAllOwnedCards(ownedCards);
            return selection.CreateOptions(ownedCards);
        }
    }
}
