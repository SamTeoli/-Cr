using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HaveABreak.Cards
{
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
        public Canvas RootCanvas { get; private set; }
        public Button NewRunButton { get; private set; }
        public Button ContinueButton { get; private set; }
        public RuntimeGameScreen CurrentScreen { get; private set; }

        public event Action NewRunRequested;
        public event Action ContinueRequested;

        public void Initialize()
        {
            if (RootCanvas != null)
            {
                return;
            }

            EnsureEventSystem();
            BuildCanvas();
            BuildStartScreen();
            ShowScreen(RuntimeGameScreen.Start);
        }

        public void ShowScreen(RuntimeGameScreen screen)
        {
            CurrentScreen = screen;
            if (startScreen != null)
            {
                startScreen.SetActive(screen == RuntimeGameScreen.Start);
            }
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
