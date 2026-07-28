using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HaveABreak.Cards
{
    public sealed class RuntimeGameCommandOption
    {
        public RuntimeGameCommandOption(
            string commandId,
            string label,
            bool interactable = true)
        {
            CommandId = commandId;
            Label = label;
            Interactable = interactable;
        }

        public string CommandId { get; }
        public string Label { get; }
        public bool Interactable { get; }
    }

    public sealed class RuntimeGameUiRoot : MonoBehaviour
    {
        private static readonly Color BackgroundColor =
            new(0.035f, 0.055f, 0.09f, 1f);
        private static readonly Color PanelColor =
            new(0.075f, 0.11f, 0.18f, 0.98f);
        private static readonly Color PrimaryColor =
            new(0.2f, 0.58f, 0.82f, 1f);
        private static readonly Color SecondaryColor =
            new(0.15f, 0.2f, 0.29f, 1f);

        private GameObject startScreen;
        private GameObject confirmationScreen;
        private GameObject runPreparationScreen;
        private GameObject nodeSelectionScreen;
        private GameObject nodeResolutionScreen;
        private GameObject battleScreen;
        private GameObject rewardScreen;
        private GameObject runResultScreen;
        public Canvas RootCanvas { get; private set; }
        public Button NewRunButton { get; private set; }
        public Button ContinueButton { get; private set; }
        public Text ConfirmationTitleText { get; private set; }
        public Text ConfirmationBodyText { get; private set; }
        public Button CancelConfirmationButton { get; private set; }
        public Button ConfirmActionButton { get; private set; }
        public Text RunPreparationSelectedCountText { get; private set; }
        public Text RunPreparationMessageText { get; private set; }
        public RectTransform RunPreparationCardList { get; private set; }
        public Button CancelRunPreparationButton { get; private set; }
        public Button ConfirmRunPreparationButton { get; private set; }
        public Text NodeSelectionSummaryText { get; private set; }
        public Text NodeSelectionMessageText { get; private set; }
        public RectTransform NodeSelectionCommandList { get; private set; }
        public Text NodeResolutionTitleText { get; private set; }
        public Text NodeResolutionSummaryText { get; private set; }
        public Text NodeResolutionMessageText { get; private set; }
        public RectTransform NodeResolutionCommandList { get; private set; }
        public Text BattleTitleText { get; private set; }
        public Text BattleSummaryText { get; private set; }
        public Text BattleMessageText { get; private set; }
        public RectTransform BattleConsumableBar { get; private set; }
        public RectTransform BattleConsumableIconList { get; private set; }
        public Text BattleConsumableTooltipText { get; private set; }
        public RectTransform BattleHandCardList { get; private set; }
        public RectTransform BattleCommandList { get; private set; }
        public GameObject BattleDetailPanel { get; private set; }
        public Text BattleDetailTitleText { get; private set; }
        public Text BattleDetailBodyText { get; private set; }
        public Button BattleDetailActionButton { get; private set; }
        public Button BattleEndTurnButton { get; private set; }
        public RectTransform BattleTopHudBar { get; private set; }
        public Text BattleHealthText { get; private set; }
        public Text BattleGoldText { get; private set; }
        public Text BattleFloorText { get; private set; }
        public Text BattleManaText { get; private set; }
        public GameObject BattleUtilityPanel { get; private set; }
        public Text BattleUtilityTitleText { get; private set; }
        public Text BattleUtilityBodyText { get; private set; }
        public Text RewardSummaryText { get; private set; }
        public Text RewardMessageText { get; private set; }
        public RectTransform RewardCommandList { get; private set; }
        public Text RunResultTitleText { get; private set; }
        public Text RunResultSummaryText { get; private set; }
        public Text RunResultMessageText { get; private set; }
        public Button RunResultNewRunButton { get; private set; }
        public Button ReturnToStartButton { get; private set; }
        public RuntimeGameScreen CurrentScreen { get; private set; }

        public event Action NewRunRequested;
        public event Action ContinueRequested;
        public event Action ConfirmationCancelled;
        public event Action ConfirmationAccepted;
        public event Action<string> RunPreparationCardToggleRequested;
        public event Action RunPreparationCancelled;
        public event Action RunPreparationConfirmed;
        public event Action<string> NodeSelectionRequested;
        public event Action<string> NodeResolutionCommandRequested;
        public event Action<string> BattleCommandRequested;
        public event Action<string, string> BattleCardDropped;
        public event Action<string> RewardCommandRequested;
        public event Action RunResultNewRunRequested;
        public event Action ReturnToStartRequested;

        public void Initialize()
        {
            if (RootCanvas != null)
            {
                return;
            }

            EnsureEventSystem();
            BuildCanvas();
            BuildStartScreen();
            BuildConfirmationScreen();
            BuildRunPreparationScreen();
            BuildNodeSelectionScreen();
            BuildNodeResolutionScreen();
            BuildBattleScreen();
            BuildRewardScreen();
            BuildRunResultScreen();
            ShowScreen(RuntimeGameScreen.Start);
        }

        public void ShowScreen(RuntimeGameScreen screen)
        {
            CurrentScreen = screen;
            if (startScreen != null)
            {
                startScreen.SetActive(screen == RuntimeGameScreen.Start);
            }
            if (confirmationScreen != null)
            {
                confirmationScreen.SetActive(
                    screen == RuntimeGameScreen.Confirmation);
            }
            if (runPreparationScreen != null)
            {
                runPreparationScreen.SetActive(
                    screen == RuntimeGameScreen.RunPreparation);
            }
            if (nodeSelectionScreen != null)
            {
                nodeSelectionScreen.SetActive(
                    screen == RuntimeGameScreen.NodeSelection);
            }
            if (nodeResolutionScreen != null)
            {
                nodeResolutionScreen.SetActive(
                    screen == RuntimeGameScreen.NodeResolution);
            }
            if (battleScreen != null)
            {
                battleScreen.SetActive(screen == RuntimeGameScreen.Battle);
            }
            if (rewardScreen != null)
            {
                rewardScreen.SetActive(screen == RuntimeGameScreen.Reward);
            }
            if (runResultScreen != null)
            {
                runResultScreen.SetActive(
                    screen == RuntimeGameScreen.Completed ||
                    screen == RuntimeGameScreen.Defeated);
            }
        }

        public void BindRunPreparation(
            IReadOnlyList<RunDeckSelectionOption> options,
            int selectedCount,
            string message,
            bool canConfirm)
        {
            if (RunPreparationCardList == null)
            {
                throw new InvalidOperationException(
                    "RuntimeGameUiRoot.Initialize must be called before binding.");
            }

            for (int index = RunPreparationCardList.childCount - 1;
                 index >= 0;
                 index--)
            {
                GameObject child =
                    RunPreparationCardList.GetChild(index).gameObject;
                if (Application.isPlaying)
                {
                    child.transform.SetParent(null, false);
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }

            int optionCount = options?.Count ?? 0;
            RunPreparationSelectedCountText.text =
                $"선택 {Mathf.Max(0, selectedCount)}장 / 보유 {optionCount}장";
            RunPreparationMessageText.text = string.IsNullOrWhiteSpace(message)
                ? "카드를 선택한 순서가 이번 런의 덱 순서가 됩니다."
                : message;
            ConfirmRunPreparationButton.interactable = canConfirm;

            for (int index = 0; index < optionCount; index++)
            {
                RunDeckSelectionOption option = options[index];
                if (option == null)
                {
                    continue;
                }

                RuntimeCardPresentation presentation =
                    RuntimeCardPresentation.FromRunDeck(option);
                CreateCardView(
                    $"Card_{option.OwnedCardId}",
                    RunPreparationCardList,
                    presentation,
                    commandId =>
                        RunPreparationCardToggleRequested?.Invoke(commandId));
            }
        }

        public void BindConfirmation(
            string title,
            string body,
            string confirmLabel)
        {
            ConfirmationTitleText.text = title ?? "확인";
            ConfirmationBodyText.text = body ?? string.Empty;
            ConfirmActionButton.GetComponentInChildren<Text>().text =
                confirmLabel ?? "확인";
        }

        public void BindNodeSelection(
            IReadOnlyList<RuntimeGameCommandOption> options,
            string summary,
            string message)
        {
            BindCommandList(
                NodeSelectionCommandList,
                options,
                commandId => NodeSelectionRequested?.Invoke(commandId));
            NodeSelectionSummaryText.text = summary ?? string.Empty;
            NodeSelectionMessageText.text = message ?? string.Empty;
        }

        public void BindNodeResolution(
            string title,
            IReadOnlyList<RuntimeGameCommandOption> options,
            string summary,
            string message)
        {
            BindCommandList(
                NodeResolutionCommandList,
                options,
                commandId =>
                    NodeResolutionCommandRequested?.Invoke(commandId));
            NodeResolutionTitleText.text = title ?? "노드 진행";
            NodeResolutionSummaryText.text = summary ?? string.Empty;
            NodeResolutionMessageText.text = message ?? string.Empty;
        }

        public void BindBattle(
            string title,
            IReadOnlyList<RuntimeGameCommandOption> options,
            string summary,
            string message)
        {
            List<RuntimeGameCommandOption> secondaryOptions = new();
            RuntimeGameCommandOption endTurn = null;
            if (options != null)
            {
                foreach (RuntimeGameCommandOption option in options)
                {
                    if (option?.CommandId == "end-turn")
                    {
                        endTurn = option;
                    }
                    else
                    {
                        secondaryOptions.Add(option);
                    }
                }
            }
            BindCommandList(
                BattleCommandList,
                secondaryOptions,
                commandId => BattleCommandRequested?.Invoke(commandId));
            if (BattleEndTurnButton != null)
            {
                BattleEndTurnButton.interactable =
                    endTurn?.Interactable == true;
            }
            BattleTitleText.text = title ?? "전투";
            BattleSummaryText.text = summary ?? string.Empty;
            BattleMessageText.text = message ?? string.Empty;
        }

        public void BindBattleHand(
            IReadOnlyList<RuntimeCardPresentation> cards)
        {
            if (BattleHandCardList == null)
            {
                throw new InvalidOperationException(
                    "RuntimeGameUiRoot.Initialize must be called before binding.");
            }

            ClearChildren(BattleHandCardList);
            int cardCount = cards?.Count ?? 0;
            for (int index = 0; index < cardCount; index++)
            {
                RuntimeCardPresentation card = cards[index];
                if (card == null)
                {
                    continue;
                }

                RuntimeCardView view = CreateCardView(
                    $"HandCard_{index}",
                    BattleHandCardList,
                    card,
                    _ => ShowBattleCardDetail(card),
                    true);
                view.GetComponent<RuntimeBattleHandCardHover>()
                    ?.Configure(index);
            }
        }

        public void BindBattleHud(
            int currentHealth,
            int maximumHealth,
            int gold,
            int floor,
            int currentMana,
            int maximumMana,
            string mapDetails,
            string deckDetails)
        {
            BattleHealthText.text =
                $"♥ {Mathf.Max(0, currentHealth)}/{Mathf.Max(1, maximumHealth)}";
            BattleGoldText.text = $"● {Mathf.Max(0, gold)}";
            BattleFloorText.text = $"층 {Mathf.Max(1, floor)}";
            BattleManaText.text =
                $"마나 {Mathf.Max(0, currentMana)}/{Mathf.Max(0, maximumMana)}";
            battleMapDetails = mapDetails ?? "이동 경로가 없습니다.";
            battleDeckDetails = deckDetails ?? "덱 정보가 없습니다.";
        }

        public void BindBattleConsumables(
            IReadOnlyList<BattleConsumableActionOption> options)
        {
            if (BattleConsumableIconList == null)
            {
                throw new InvalidOperationException(
                    "RuntimeGameUiRoot.Initialize must be called before binding.");
            }

            HideBattleConsumableTooltip();
            ClearChildren(BattleConsumableIconList);
            int optionCount = options?.Count ?? 0;
            for (int index = 0; index < optionCount; index++)
            {
                BattleConsumableActionOption option = options[index];
                if (option == null)
                {
                    continue;
                }

                CreateBattleConsumableIcon(option, index);
            }

            if (optionCount == 0)
            {
                BattleConsumableTooltipText.text = "사용 가능한 소모품 없음";
                BattleConsumableTooltipText.gameObject.SetActive(true);
            }
        }

        public void BindReward(
            IReadOnlyList<RuntimeGameCommandOption> options,
            string summary,
            string message)
        {
            BindCommandList(
                RewardCommandList,
                options,
                commandId => RewardCommandRequested?.Invoke(commandId));
            RewardSummaryText.text = summary ?? string.Empty;
            RewardMessageText.text = message ?? string.Empty;
        }

        public void BindRunResult(
            string title,
            string summary,
            string message)
        {
            RunResultTitleText.text = title ?? "런 결과";
            RunResultSummaryText.text = summary ?? string.Empty;
            RunResultMessageText.text = message ?? string.Empty;
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystem = new(
                "FinalUiEventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            eventSystem.transform.SetParent(transform, false);
        }

        private void BuildCanvas()
        {
            GameObject canvasObject = new(
                "FinalUiCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            RootCanvas = canvasObject.GetComponent<Canvas>();
            RootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            Stretch(canvasRect);

            Image background = CreateImage(
                "Background",
                canvasRect,
                BackgroundColor);
            Stretch(background.rectTransform);
        }

        private void BuildStartScreen()
        {
            startScreen = new GameObject(
                "StartScreen",
                typeof(RectTransform));
            RectTransform startRect =
                startScreen.GetComponent<RectTransform>();
            startRect.SetParent(RootCanvas.transform, false);
            Stretch(startRect);

            Image panel = CreateImage(
                "StartPanel",
                startRect,
                PanelColor);
            RectTransform panelRect = panel.rectTransform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(760f, 600f);

            VerticalLayoutGroup layout =
                panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(72, 72, 76, 64);
            layout.spacing = 22f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateText(
                "Title",
                panelRect,
                "Have a Break, and then..",
                54,
                FontStyle.Bold,
                104f);
            CreateText(
                "Subtitle",
                panelRect,
                "수집한 카드를 선택하고, 이번 런만의 방식으로 개조하세요.",
                24,
                FontStyle.Normal,
                72f);
            CreateSpacer(panelRect, 36f);

            NewRunButton = CreateButton(
                "NewRunButton",
                panelRect,
                "새 런",
                PrimaryColor,
                () => NewRunRequested?.Invoke());
            ContinueButton = CreateButton(
                "ContinueButton",
                panelRect,
                "이어하기",
                SecondaryColor,
                () => ContinueRequested?.Invoke());

            CreateText(
                "PreviewNotice",
                panelRect,
                "최종 UI 제작 중 · 현재는 시작 화면 미리보기입니다.",
                18,
                FontStyle.Italic,
                52f);
        }

        private void BuildConfirmationScreen()
        {
            confirmationScreen = BuildCommandScreen(
                "ConfirmationScreen",
                "확인",
                out Text title,
                out Text body,
                out RectTransform commandList,
                out _);
            ConfirmationTitleText = title;
            ConfirmationBodyText = body;
            CancelConfirmationButton = CreateButton(
                "Cancel",
                commandList,
                "취소",
                SecondaryColor,
                () => ConfirmationCancelled?.Invoke());
            ConfirmActionButton = CreateButton(
                "Confirm",
                commandList,
                "확인",
                PrimaryColor,
                () => ConfirmationAccepted?.Invoke());
        }

        private void BuildRunPreparationScreen()
        {
            runPreparationScreen = new GameObject(
                "RunPreparationScreen",
                typeof(RectTransform));
            RectTransform screenRect =
                runPreparationScreen.GetComponent<RectTransform>();
            screenRect.SetParent(RootCanvas.transform, false);
            Stretch(screenRect);

            Image panel = CreateImage(
                "RunPreparationPanel",
                screenRect,
                PanelColor);
            RectTransform panelRect = panel.rectTransform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(1120f, 860f);

            VerticalLayoutGroup layout =
                panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(64, 64, 48, 48);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateText(
                "Title",
                panelRect,
                "새 런 덱 준비",
                44,
                FontStyle.Bold,
                72f);
            CreateText(
                "Instructions",
                panelRect,
                "보유 카드 중 이번 런에서 사용할 카드를 선택하세요.",
                22,
                FontStyle.Normal,
                48f);
            RunPreparationSelectedCountText = CreateText(
                "SelectedCount",
                panelRect,
                "선택 0장 / 보유 0장",
                24,
                FontStyle.Bold,
                48f);

            BuildRunPreparationCardScroll(panelRect);

            RunPreparationMessageText = CreateText(
                "Message",
                panelRect,
                "카드를 선택한 순서가 이번 런의 덱 순서가 됩니다.",
                18,
                FontStyle.Normal,
                54f);

            GameObject commandRow = new(
                "CommandRow",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            commandRow.transform.SetParent(panelRect, false);
            HorizontalLayoutGroup commandLayout =
                commandRow.GetComponent<HorizontalLayoutGroup>();
            commandLayout.spacing = 20f;
            commandLayout.childControlWidth = true;
            commandLayout.childControlHeight = true;
            commandLayout.childForceExpandWidth = true;
            commandLayout.childForceExpandHeight = false;
            commandRow.GetComponent<LayoutElement>().preferredHeight = 78f;

            CancelRunPreparationButton = CreateButton(
                "CancelButton",
                commandRow.transform,
                "취소",
                SecondaryColor,
                () => RunPreparationCancelled?.Invoke());
            ConfirmRunPreparationButton = CreateButton(
                "ConfirmButton",
                commandRow.transform,
                "이 덱으로 런 시작",
                PrimaryColor,
                () => RunPreparationConfirmed?.Invoke());
        }

        private void BuildRunPreparationCardScroll(Transform parent)
        {
            GameObject scrollObject = new(
                "CardScroll",
                typeof(RectTransform),
                typeof(ScrollRect),
                typeof(LayoutElement));
            scrollObject.transform.SetParent(parent, false);
            scrollObject.GetComponent<LayoutElement>().preferredHeight = 430f;

            Image viewport = CreateImage(
                "Viewport",
                scrollObject.transform,
                new Color(0.025f, 0.04f, 0.07f, 0.75f));
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            Stretch(viewport.rectTransform);

            GameObject contentObject = new(
                "Content",
                typeof(RectTransform),
                typeof(GridLayoutGroup),
                typeof(ContentSizeFitter));
            RunPreparationCardList =
                contentObject.GetComponent<RectTransform>();
            RunPreparationCardList.SetParent(viewport.transform, false);
            RunPreparationCardList.anchorMin = new Vector2(0f, 1f);
            RunPreparationCardList.anchorMax = new Vector2(1f, 1f);
            RunPreparationCardList.pivot = new Vector2(0.5f, 1f);
            RunPreparationCardList.offsetMin = new Vector2(18f, 0f);
            RunPreparationCardList.offsetMax = new Vector2(-18f, 0f);

            GridLayoutGroup cardLayout =
                contentObject.GetComponent<GridLayoutGroup>();
            cardLayout.padding = new RectOffset(8, 8, 12, 12);
            cardLayout.spacing = new Vector2(12f, 12f);
            cardLayout.cellSize = new Vector2(226f, 344f);
            cardLayout.constraint =
                GridLayoutGroup.Constraint.FixedColumnCount;
            cardLayout.constraintCount = 4;
            ContentSizeFitter fitter =
                contentObject.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewport.rectTransform;
            scroll.content = RunPreparationCardList;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
        }

        private void BuildNodeSelectionScreen()
        {
            Text summaryText;
            RectTransform commandList;
            Text messageText;
            nodeSelectionScreen = BuildCommandScreen(
                "NodeSelectionScreen",
                "다음 노드 선택",
                out Text title,
                out summaryText,
                out commandList,
                out messageText);
            title.text = "다음 노드 선택";
            NodeSelectionSummaryText = summaryText;
            NodeSelectionCommandList = commandList;
            NodeSelectionMessageText = messageText;
        }

        private void BuildNodeResolutionScreen()
        {
            Text titleText;
            Text summaryText;
            RectTransform commandList;
            Text messageText;
            nodeResolutionScreen = BuildCommandScreen(
                "NodeResolutionScreen",
                "노드 진행",
                out titleText,
                out summaryText,
                out commandList,
                out messageText);
            NodeResolutionTitleText = titleText;
            NodeResolutionSummaryText = summaryText;
            NodeResolutionCommandList = commandList;
            NodeResolutionMessageText = messageText;
        }

        private void BuildBattleScreen()
        {
            Text titleText;
            Text summaryText;
            RectTransform commandList;
            Text messageText;
            battleScreen = BuildCommandScreen(
                "BattleScreen",
                "전투",
                out titleText,
                out summaryText,
                out commandList,
                out messageText);
            BattleTitleText = titleText;
            BattleSummaryText = summaryText;
            BattleCommandList = commandList;
            BattleMessageText = messageText;

            Transform panel = titleText.transform.parent;
            RectTransform panelRect = panel as RectTransform;
            panelRect.sizeDelta = new Vector2(1880f, 1040f);
            Image panelImage = panel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = new Color(0.035f, 0.055f, 0.09f, 0.82f);
            }
            VerticalLayoutGroup panelLayout =
                panel.GetComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(28, 28, 22, 22);
            panelLayout.spacing = 8f;

            BuildBattleTopHud(panel);
            BuildBattleConsumableBar(panel);
            BattleConsumableBar.SetSiblingIndex(
                BattleTopHudBar.GetSiblingIndex() + 1);

            BattleHandCardList = BuildBattleHand(
                panel,
                "BattleHandScroll",
                240f);
            BattleHandCardList.transform.parent.parent.SetSiblingIndex(
                BattleConsumableBar.GetSiblingIndex() + 1);
            BattleCommandList.transform.parent.parent
                .GetComponent<LayoutElement>().preferredHeight = 66f;

            titleText.GetComponent<LayoutElement>().preferredHeight = 48f;
            summaryText.GetComponent<LayoutElement>().preferredHeight = 52f;
            titleText.gameObject.SetActive(false);
            summaryText.gameObject.SetActive(false);
            messageText.GetComponent<LayoutElement>().preferredHeight = 40f;
            BuildBattleDetailPanel(battleScreen.transform);
            BuildBattleUtilityPanel(battleScreen.transform);
            BuildBattleEndTurnButton(battleScreen.transform);
        }

        private string battleMapDetails;
        private string battleDeckDetails;

        private void BuildBattleTopHud(Transform panel)
        {
            Image top = CreateImage(
                "BattleTopHud",
                panel,
                new Color(0.025f, 0.055f, 0.085f, 0.98f));
            BattleTopHudBar = top.rectTransform;
            LayoutElement topElement =
                top.gameObject.AddComponent<LayoutElement>();
            topElement.preferredHeight = 68f;
            HorizontalLayoutGroup topLayout =
                top.gameObject.AddComponent<HorizontalLayoutGroup>();
            topLayout.padding = new RectOffset(16, 16, 8, 8);
            topLayout.spacing = 10f;
            topLayout.childAlignment = TextAnchor.MiddleLeft;
            topLayout.childControlWidth = true;
            topLayout.childControlHeight = true;
            topLayout.childForceExpandWidth = false;
            topLayout.childForceExpandHeight = false;

            BattleHealthText = CreateHudLabel("Health", top.transform, 190f);
            BattleGoldText = CreateHudLabel("Gold", top.transform, 126f);
            BattleManaText = CreateHudLabel("Mana", top.transform, 170f);
            BattleFloorText = CreateHudLabel("Floor", top.transform, 126f);
            CreateHudSpacer(top.transform);
            CreateHudButton("Map", top.transform, "지도",
                () => ShowBattleUtility("지도", battleMapDetails));
            CreateHudButton("Deck", top.transform, "덱",
                () => ShowBattleUtility("덱", battleDeckDetails));
            CreateHudButton("Options", top.transform, "옵션",
                () => ShowBattleUtility(
                    "옵션",
                    "전투 일시정지\n\n계속하려면 닫기를 누르세요."));
        }

        private static Text CreateHudLabel(
            string name,
            Transform parent,
            float width)
        {
            Text text = CreateText(
                name,
                parent,
                string.Empty,
                20,
                FontStyle.Bold,
                52f);
            text.alignment = TextAnchor.MiddleLeft;
            LayoutElement element = text.GetComponent<LayoutElement>();
            element.preferredWidth = width;
            element.flexibleWidth = 0f;
            return text;
        }

        private static void CreateHudSpacer(Transform parent)
        {
            GameObject spacer = new(
                "FlexibleSpace",
                typeof(RectTransform),
                typeof(LayoutElement));
            spacer.transform.SetParent(parent, false);
            spacer.GetComponent<LayoutElement>().flexibleWidth = 1f;
        }

        private static Button CreateHudButton(
            string name,
            Transform parent,
            string label,
            UnityEngine.Events.UnityAction action)
        {
            Button button = CreateButton(
                name,
                parent,
                label,
                SecondaryColor,
                action);
            LayoutElement element = button.GetComponent<LayoutElement>();
            element.preferredWidth = 112f;
            element.preferredHeight = 52f;
            element.flexibleWidth = 0f;
            Text text = button.GetComponentInChildren<Text>();
            text.fontSize = 20;
            return button;
        }

        private void BuildBattleUtilityPanel(Transform parent)
        {
            Image panel = CreateImage(
                "BattleUtilityPanel",
                parent,
                new Color(0.025f, 0.045f, 0.075f, 0.99f));
            BattleUtilityPanel = panel.gameObject;
            RectTransform rect = panel.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(660f, 720f);

            VerticalLayoutGroup layout =
                panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.spacing = 14f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            BattleUtilityTitleText = CreateText(
                "Title", rect, string.Empty, 32, FontStyle.Bold, 64f);
            BattleUtilityBodyText = CreateText(
                "Body", rect, string.Empty, 20, FontStyle.Normal, 520f);
            BattleUtilityBodyText.alignment = TextAnchor.UpperLeft;
            CreateButton(
                "Close",
                rect,
                "닫기",
                SecondaryColor,
                () => BattleUtilityPanel.SetActive(false));
            BattleUtilityPanel.SetActive(false);
        }

        private void ShowBattleUtility(string title, string body)
        {
            HideBattleDetail();
            BattleUtilityTitleText.text = title ?? string.Empty;
            BattleUtilityBodyText.text = body ?? string.Empty;
            BattleUtilityPanel.SetActive(true);
            BattleUtilityPanel.transform.SetAsLastSibling();
        }

        private void BuildBattleDetailPanel(Transform parent)
        {
            Image panel = CreateImage(
                "BattleDetailPanel",
                parent,
                new Color(0.025f, 0.045f, 0.075f, 0.98f));
            BattleDetailPanel = panel.gameObject;
            RectTransform rect = panel.rectTransform;
            rect.anchorMin = new Vector2(0f, 0.16f);
            rect.anchorMax = new Vector2(0f, 0.84f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(24f, 0f);
            rect.sizeDelta = new Vector2(342f, 0f);

            VerticalLayoutGroup layout =
                panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 18, 18);
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            BattleDetailTitleText = CreateText(
                "Title",
                rect,
                string.Empty,
                26,
                FontStyle.Bold,
                54f);
            BattleDetailBodyText = CreateText(
                "Body",
                rect,
                string.Empty,
                18,
                FontStyle.Normal,
                330f);
            BattleDetailBodyText.alignment = TextAnchor.UpperLeft;
            BattleDetailActionButton = CreateButton(
                "UseCard",
                rect,
                "사용",
                PrimaryColor,
                UseInspectedBattleCard);
            CreateButton(
                "Close",
                rect,
                "닫기",
                SecondaryColor,
                HideBattleDetail);
            BattleDetailPanel.SetActive(false);
        }

        private void BuildBattleEndTurnButton(Transform parent)
        {
            BattleEndTurnButton = CreateButton(
                "BattleEndTurn",
                parent,
                "턴 종료",
                new Color(0.72f, 0.48f, 0.16f, 1f),
                () => BattleCommandRequested?.Invoke("end-turn"));
            RectTransform rect =
                BattleEndTurnButton.transform as RectTransform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-28f, 24f);
            rect.sizeDelta = new Vector2(190f, 78f);
        }

        private string inspectedBattleCardCommand;

        private void ShowBattleCardDetail(RuntimeCardPresentation card)
        {
            if (card == null || BattleDetailPanel == null)
            {
                return;
            }

            inspectedBattleCardCommand = card.CommandId;
            BattleDetailTitleText.text = card.DisplayName;
            string stats = card.HasMonsterStats
                ? $"\n공격 {card.Attack}  생명력 {card.Health}"
                : string.Empty;
            string block = string.IsNullOrWhiteSpace(card.BlockReason)
                ? string.Empty
                : $"\n\n사용 불가: {card.BlockReason}";
            BattleDetailBodyText.text =
                $"{card.TypeLabel} · {card.RarityLabel}\n" +
                $"마나 {card.ManaCost}{stats}\n\n" +
                $"{card.EffectText}{block}";
            BattleDetailActionButton.GetComponentInChildren<Text>().text =
                "사용";
            BattleDetailActionButton.interactable = card.Interactable;
            BattleDetailPanel.SetActive(true);
            BattleDetailPanel.transform.SetAsLastSibling();
        }

        public void ShowBattleFieldDetail(
            RuntimeBattleFieldSlotPresentation slot)
        {
            if (slot == null || !slot.Occupied || BattleDetailPanel == null)
            {
                return;
            }

            inspectedBattleCardCommand = slot.ClickCommandId;
            BattleDetailTitleText.text = slot.Title;
            BattleDetailBodyText.text = string.IsNullOrWhiteSpace(slot.Detail)
                ? "상세 정보가 없습니다."
                : slot.Detail;
            BattleDetailActionButton.GetComponentInChildren<Text>().text =
                "행동";
            BattleDetailActionButton.interactable =
                slot.Interactable &&
                !string.IsNullOrWhiteSpace(slot.ClickCommandId);
            BattleDetailPanel.SetActive(true);
            BattleDetailPanel.transform.SetAsLastSibling();
        }

        private void UseInspectedBattleCard()
        {
            if (string.IsNullOrWhiteSpace(inspectedBattleCardCommand))
            {
                return;
            }

            string command = inspectedBattleCardCommand;
            HideBattleDetail();
            BattleCommandRequested?.Invoke(command);
        }

        private void HideBattleDetail()
        {
            inspectedBattleCardCommand = null;
            BattleDetailPanel?.SetActive(false);
        }

        private void BuildBattleConsumableBar(Transform panel)
        {
            Image bar = CreateImage(
                "BattleConsumableBar",
                panel,
                new Color(0.035f, 0.075f, 0.12f, 0.98f));
            BattleConsumableBar = bar.rectTransform;
            LayoutElement barLayout =
                bar.gameObject.AddComponent<LayoutElement>();
            barLayout.preferredHeight = 88f;

            HorizontalLayoutGroup layout =
                bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 8, 8);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            Text label = CreateText(
                "Label",
                BattleConsumableBar,
                "소모품",
                20,
                FontStyle.Bold,
                68f);
            LayoutElement labelLayout = label.GetComponent<LayoutElement>();
            labelLayout.preferredWidth = 76f;
            labelLayout.flexibleWidth = 0f;

            GameObject iconListObject = new(
                "Icons",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            iconListObject.transform.SetParent(BattleConsumableBar, false);
            BattleConsumableIconList =
                iconListObject.GetComponent<RectTransform>();
            HorizontalLayoutGroup iconLayout =
                iconListObject.GetComponent<HorizontalLayoutGroup>();
            iconLayout.spacing = 8f;
            iconLayout.childAlignment = TextAnchor.MiddleLeft;
            iconLayout.childControlWidth = true;
            iconLayout.childControlHeight = true;
            iconLayout.childForceExpandWidth = false;
            iconLayout.childForceExpandHeight = false;
            LayoutElement iconListLayout =
                iconListObject.GetComponent<LayoutElement>();
            iconListLayout.preferredWidth = 246f;
            iconListLayout.preferredHeight = 68f;
            iconListLayout.flexibleWidth = 0f;

            BattleConsumableTooltipText = CreateText(
                "Tooltip",
                BattleConsumableBar,
                string.Empty,
                17,
                FontStyle.Normal,
                68f);
            BattleConsumableTooltipText.alignment = TextAnchor.MiddleLeft;
            LayoutElement tooltipLayout =
                BattleConsumableTooltipText.GetComponent<LayoutElement>();
            tooltipLayout.flexibleWidth = 1f;
            BattleConsumableTooltipText.gameObject.SetActive(false);
        }

        private void CreateBattleConsumableIcon(
            BattleConsumableActionOption option,
            int index)
        {
            string commandId = $"consumable:{option.ItemId}";
            GameObject iconObject = new(
                $"Consumable_{index}_{option.ItemId}",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement),
                typeof(RuntimeConsumableTooltipTrigger));
            iconObject.transform.SetParent(BattleConsumableIconList, false);

            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = RuntimeConsumableIconCatalog.Load(option.ItemId);
            icon.preserveAspect = true;
            icon.color = icon.sprite != null
                ? Color.white
                : SecondaryColor;

            Button button = iconObject.GetComponent<Button>();
            button.targetGraphic = icon;
            button.interactable = option.CanUse;
            button.onClick.AddListener(
                () => BattleCommandRequested?.Invoke(commandId));
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.72f, 0.82f, 0.94f, 1f);
            colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.62f);
            button.colors = colors;

            LayoutElement iconLayout =
                iconObject.GetComponent<LayoutElement>();
            iconLayout.preferredWidth = 68f;
            iconLayout.preferredHeight = 68f;
            iconLayout.flexibleWidth = 0f;
            iconLayout.flexibleHeight = 0f;

            Text count = CreateText(
                "Count",
                iconObject.transform,
                $"×{option.RemainingCount}",
                16,
                FontStyle.Bold,
                24f);
            count.alignment = TextAnchor.MiddleCenter;
            count.gameObject.AddComponent<Outline>().effectColor =
                new Color(0f, 0f, 0f, 0.95f);
            RectTransform countRect = count.rectTransform;
            countRect.anchorMin = new Vector2(0.48f, 0f);
            countRect.anchorMax = new Vector2(1f, 0.34f);
            countRect.offsetMin = Vector2.zero;
            countRect.offsetMax = Vector2.zero;

            RuntimeConsumableTooltipTrigger tooltip =
                iconObject.GetComponent<RuntimeConsumableTooltipTrigger>();
            tooltip.Configure(
                () => ShowBattleConsumableTooltip(option),
                HideBattleConsumableTooltip);
        }

        private void ShowBattleConsumableTooltip(
            BattleConsumableActionOption option)
        {
            if (BattleConsumableTooltipText == null || option == null)
            {
                return;
            }

            string unavailable = string.IsNullOrWhiteSpace(option.BlockReason)
                ? string.Empty
                : $"\n{option.BlockReason}";
            BattleConsumableTooltipText.text =
                $"{option.DisplayName} ×{option.RemainingCount}\n" +
                $"{option.Item.RulesText}{unavailable}";
            BattleConsumableTooltipText.gameObject.SetActive(true);
        }

        private void HideBattleConsumableTooltip()
        {
            if (BattleConsumableTooltipText == null)
            {
                return;
            }

            BattleConsumableTooltipText.text = string.Empty;
            BattleConsumableTooltipText.gameObject.SetActive(false);
        }

        private void BuildRewardScreen()
        {
            rewardScreen = BuildCommandScreen(
                "RewardScreen",
                "전투 보상",
                out _,
                out Text summaryText,
                out RectTransform commandList,
                out Text messageText);
            RewardSummaryText = summaryText;
            RewardCommandList = commandList;
            RewardMessageText = messageText;
        }

        private void BuildRunResultScreen()
        {
            runResultScreen = BuildCommandScreen(
                "RunResultScreen",
                "런 결과",
                out Text title,
                out Text summary,
                out RectTransform commandList,
                out Text message);
            RunResultTitleText = title;
            RunResultSummaryText = summary;
            RunResultMessageText = message;
            RunResultNewRunButton = CreateButton(
                "NewRun",
                commandList,
                "새 런 시작",
                PrimaryColor,
                () => RunResultNewRunRequested?.Invoke());
            ReturnToStartButton = CreateButton(
                "ReturnToStart",
                commandList,
                "시작 화면으로",
                SecondaryColor,
                () => ReturnToStartRequested?.Invoke());
        }

        private GameObject BuildCommandScreen(
            string screenName,
            string title,
            out Text titleText,
            out Text summaryText,
            out RectTransform commandList,
            out Text messageText)
        {
            GameObject screen = new(
                screenName,
                typeof(RectTransform));
            RectTransform screenRect = screen.GetComponent<RectTransform>();
            screenRect.SetParent(RootCanvas.transform, false);
            Stretch(screenRect);

            Image panel = CreateImage(
                $"{screenName}Panel",
                screenRect,
                PanelColor);
            RectTransform panelRect = panel.rectTransform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(1120f, 860f);

            VerticalLayoutGroup layout =
                panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(64, 64, 48, 48);
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            titleText = CreateText(
                "Title",
                panelRect,
                title,
                42,
                FontStyle.Bold,
                68f);
            summaryText = CreateText(
                "Summary",
                panelRect,
                string.Empty,
                22,
                FontStyle.Normal,
                64f);
            commandList = BuildCommandScroll(panelRect);
            messageText = CreateText(
                "Message",
                panelRect,
                string.Empty,
                18,
                FontStyle.Normal,
                58f);
            return screen;
        }

        private static RectTransform BuildCommandScroll(Transform parent)
        {
            GameObject scrollObject = new(
                "CommandScroll",
                typeof(RectTransform),
                typeof(ScrollRect),
                typeof(LayoutElement));
            scrollObject.transform.SetParent(parent, false);
            scrollObject.GetComponent<LayoutElement>().preferredHeight = 560f;

            Image viewport = CreateImage(
                "Viewport",
                scrollObject.transform,
                new Color(0.025f, 0.04f, 0.07f, 0.75f));
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            Stretch(viewport.rectTransform);

            GameObject contentObject = new(
                "Content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            RectTransform content =
                contentObject.GetComponent<RectTransform>();
            content.SetParent(viewport.transform, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = new Vector2(18f, 0f);
            content.offsetMax = new Vector2(-18f, 0f);

            VerticalLayoutGroup commandLayout =
                contentObject.GetComponent<VerticalLayoutGroup>();
            commandLayout.padding = new RectOffset(8, 8, 12, 12);
            commandLayout.spacing = 10f;
            commandLayout.childControlWidth = true;
            commandLayout.childControlHeight = true;
            commandLayout.childForceExpandWidth = true;
            commandLayout.childForceExpandHeight = false;
            ContentSizeFitter fitter =
                contentObject.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewport.rectTransform;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return content;
        }

        private static RectTransform BuildCardScroll(
            Transform parent,
            string name,
            float preferredHeight)
        {
            GameObject scrollObject = new(
                name,
                typeof(RectTransform),
                typeof(ScrollRect),
                typeof(LayoutElement));
            scrollObject.transform.SetParent(parent, false);
            scrollObject.GetComponent<LayoutElement>().preferredHeight =
                preferredHeight;

            Image viewport = CreateImage(
                "Viewport",
                scrollObject.transform,
                new Color(0.025f, 0.04f, 0.07f, 0.75f));
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            Stretch(viewport.rectTransform);

            GameObject contentObject = new(
                "Content",
                typeof(RectTransform),
                typeof(GridLayoutGroup),
                typeof(ContentSizeFitter));
            RectTransform content =
                contentObject.GetComponent<RectTransform>();
            content.SetParent(viewport.transform, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = new Vector2(18f, 0f);
            content.offsetMax = new Vector2(-18f, 0f);

            GridLayoutGroup layout =
                contentObject.GetComponent<GridLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = new Vector2(12f, 12f);
            layout.cellSize = new Vector2(226f, 344f);
            layout.constraint =
                GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 4;

            ContentSizeFitter fitter =
                contentObject.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewport.rectTransform;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return content;
        }

        private static RectTransform BuildBattleHand(
            Transform parent,
            string name,
            float preferredHeight)
        {
            GameObject scrollObject = new(
                name,
                typeof(RectTransform),
                typeof(ScrollRect),
                typeof(LayoutElement));
            scrollObject.transform.SetParent(parent, false);
            scrollObject.GetComponent<LayoutElement>().preferredHeight =
                preferredHeight;

            Image viewport = CreateImage(
                "Viewport",
                scrollObject.transform,
                new Color(0.015f, 0.025f, 0.04f, 0.28f));
            Stretch(viewport.rectTransform);

            GameObject contentObject = new(
                "Content",
                typeof(RectTransform),
                typeof(RuntimeBattleHandLayout));
            RectTransform content =
                contentObject.GetComponent<RectTransform>();
            content.SetParent(viewport.transform, false);
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 0f);
            content.offsetMin = new Vector2(96f, -92f);
            content.offsetMax = new Vector2(-96f, 0f);

            ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewport.rectTransform;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = false;
            return content;
        }

        private RuntimeCardView CreateCardView(
            string name,
            Transform parent,
            RuntimeCardPresentation presentation,
            Action<string> clicked,
            bool draggable = false)
        {
            GameObject cardObject = new(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement),
                typeof(RuntimeCardView));
            cardObject.transform.SetParent(parent, false);
            RuntimeCardView view =
                cardObject.GetComponent<RuntimeCardView>();
            view.Bind(presentation, clicked);
            if (draggable)
            {
                view.ClickButton.interactable = true;
                cardObject.AddComponent<RuntimeBattleHandCardHover>();
                RuntimeCardDragHandler drag =
                    cardObject.AddComponent<RuntimeCardDragHandler>();
                drag.Configure(
                    view,
                    RootCanvas,
                    (cardCommand, targetCommand) =>
                        BattleCardDropped?.Invoke(
                            cardCommand,
                            targetCommand));
            }
            return view;
        }

        private static void BindCommandList(
            RectTransform commandList,
            IReadOnlyList<RuntimeGameCommandOption> options,
            Action<string> command)
        {
            ClearChildren(commandList);
            int optionCount = options?.Count ?? 0;
            for (int index = 0; index < optionCount; index++)
            {
                RuntimeGameCommandOption option = options[index];
                if (option == null)
                {
                    continue;
                }

                string commandId = option.CommandId;
                Button button = CreateButton(
                    $"Command_{index}",
                    commandList,
                    option.Label,
                    option.Interactable ? PrimaryColor : SecondaryColor,
                    () => command?.Invoke(commandId));
                button.interactable = option.Interactable;
                if (option.Interactable &&
                    commandId.StartsWith(
                        "enemy:",
                        StringComparison.OrdinalIgnoreCase))
                {
                    RuntimeCardDropZone dropZone =
                        button.gameObject.AddComponent<RuntimeCardDropZone>();
                    Image image = button.GetComponent<Image>();
                    dropZone.Configure(
                        commandId,
                        image,
                        image.color,
                        new Color(0.72f, 0.25f, 0.18f, 1f));
                }
            }
        }

        private static void ClearChildren(RectTransform parent)
        {
            for (int index = parent.childCount - 1; index >= 0; index--)
            {
                GameObject child = parent.GetChild(index).gameObject;
                if (Application.isPlaying)
                {
                    child.transform.SetParent(null, false);
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        private static Image CreateImage(
            string name,
            Transform parent,
            Color color)
        {
            GameObject imageObject = new(
                name,
                typeof(RectTransform),
                typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            string value,
            int fontSize,
            FontStyle fontStyle,
            float preferredHeight)
        {
            GameObject textObject = new(
                name,
                typeof(RectTransform),
                typeof(Text),
                typeof(LayoutElement));
            textObject.transform.SetParent(parent, false);

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            LayoutElement layout = textObject.GetComponent<LayoutElement>();
            layout.preferredHeight = preferredHeight;
            return text;
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            string label,
            Color color,
            UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject = new(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.GetComponent<Image>();
            image.color = color;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.16f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
            button.colors = colors;

            LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
            layout.preferredHeight = 82f;

            CreateText(
                "Label",
                buttonObject.transform,
                label,
                28,
                FontStyle.Bold,
                82f);
            RectTransform labelRect =
                buttonObject.transform.GetChild(0) as RectTransform;
            Stretch(labelRect);
            return button;
        }

        private static void CreateSpacer(Transform parent, float height)
        {
            GameObject spacer = new(
                "Spacer",
                typeof(RectTransform),
                typeof(LayoutElement));
            spacer.transform.SetParent(parent, false);
            spacer.GetComponent<LayoutElement>().preferredHeight = height;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
