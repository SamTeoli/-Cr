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
        private GameObject runPreparationScreen;
        private GameObject nodeSelectionScreen;
        private GameObject nodeResolutionScreen;
        public Canvas RootCanvas { get; private set; }
        public Button NewRunButton { get; private set; }
        public Button ContinueButton { get; private set; }
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
        public RuntimeGameScreen CurrentScreen { get; private set; }

        public event Action NewRunRequested;
        public event Action ContinueRequested;
        public event Action<string> RunPreparationCardToggleRequested;
        public event Action RunPreparationCancelled;
        public event Action RunPreparationConfirmed;
        public event Action<string> NodeSelectionRequested;
        public event Action<string> NodeResolutionCommandRequested;

        public void Initialize()
        {
            if (RootCanvas != null)
            {
                return;
            }

            EnsureEventSystem();
            BuildCanvas();
            BuildStartScreen();
            BuildRunPreparationScreen();
            BuildNodeSelectionScreen();
            BuildNodeResolutionScreen();
            ShowScreen(RuntimeGameScreen.Start);
        }

        public void ShowScreen(RuntimeGameScreen screen)
        {
            CurrentScreen = screen;
            if (startScreen != null)
            {
                startScreen.SetActive(screen == RuntimeGameScreen.Start);
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

                string ownedCardId = option.OwnedCardId;
                CreateButton(
                    $"Card_{ownedCardId}",
                    RunPreparationCardList,
                    option.DisplayLabel,
                    option.IsSelected ? PrimaryColor : SecondaryColor,
                    () => RunPreparationCardToggleRequested?.Invoke(ownedCardId));
            }
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
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            RunPreparationCardList =
                contentObject.GetComponent<RectTransform>();
            RunPreparationCardList.SetParent(viewport.transform, false);
            RunPreparationCardList.anchorMin = new Vector2(0f, 1f);
            RunPreparationCardList.anchorMax = new Vector2(1f, 1f);
            RunPreparationCardList.pivot = new Vector2(0.5f, 1f);
            RunPreparationCardList.offsetMin = new Vector2(18f, 0f);
            RunPreparationCardList.offsetMax = new Vector2(-18f, 0f);

            VerticalLayoutGroup cardLayout =
                contentObject.GetComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(8, 8, 12, 12);
            cardLayout.spacing = 10f;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;
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
