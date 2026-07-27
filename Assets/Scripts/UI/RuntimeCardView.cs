using System;
using UnityEngine;
using UnityEngine.UI;

namespace HaveABreak.Cards
{
    public sealed class RuntimeCardView : MonoBehaviour
    {
        private static readonly Color DisabledColor =
            new(0.13f, 0.14f, 0.17f, 1f);
        private static readonly Color SelectedColor =
            new(0.16f, 0.58f, 0.78f, 1f);

        public Button ClickButton { get; private set; }
        public Image FrameImage { get; private set; }
        public Image RarityAccentImage { get; private set; }
        public Text NameText { get; private set; }
        public Text ManaCostText { get; private set; }
        public Text ArtPlaceholderText { get; private set; }
        public Text MetadataText { get; private set; }
        public Text StatsText { get; private set; }
        public Text EffectText { get; private set; }
        public Text SelectionText { get; private set; }
        public Text BlockReasonText { get; private set; }
        public Text AccessibilityText { get; private set; }
        public RuntimeCardPresentation Presentation { get; private set; }

        public void Initialize()
        {
            if (ClickButton != null)
            {
                return;
            }

            FrameImage = gameObject.GetComponent<Image>() ??
                         gameObject.AddComponent<Image>();
            ClickButton = gameObject.GetComponent<Button>() ??
                          gameObject.AddComponent<Button>();
            ClickButton.targetGraphic = FrameImage;
            LayoutElement element = gameObject.GetComponent<LayoutElement>() ??
                                    gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 226f;
            element.preferredHeight = 344f;

            VerticalLayoutGroup layout =
                gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            RarityAccentImage = CreateImage("RarityAccent", 8f);
            NameText = CreateText("CardName", 23, FontStyle.Bold, 44f);
            ManaCostText = CreateText("ManaCost", 20, FontStyle.Bold, 30f);

            Image art = CreateImage("ArtPlaceholder", 86f);
            art.color = new Color(0.055f, 0.07f, 0.1f, 1f);
            ArtPlaceholderText = CreateText(
                "ArtPlaceholderLabel",
                17,
                FontStyle.Italic,
                86f,
                art.transform);
            ArtPlaceholderText.text = "아트 자리";
            Stretch(ArtPlaceholderText.rectTransform);

            MetadataText = CreateText(
                "Metadata",
                15,
                FontStyle.Bold,
                28f);
            StatsText = CreateText("Stats", 20, FontStyle.Bold, 30f);
            EffectText = CreateText("Effect", 16, FontStyle.Normal, 62f);
            SelectionText = CreateText(
                "Selection",
                16,
                FontStyle.Bold,
                28f);
            BlockReasonText = CreateText(
                "BlockReason",
                14,
                FontStyle.Bold,
                34f);
            BlockReasonText.color = new Color(1f, 0.55f, 0.48f, 1f);
            AccessibilityText = CreateText(
                "AccessibilityText",
                1,
                FontStyle.Normal,
                1f);
            AccessibilityText.color = new Color(1f, 1f, 1f, 0.01f);
        }

        public void Bind(
            RuntimeCardPresentation presentation,
            Action<string> clicked)
        {
            if (presentation == null)
            {
                throw new ArgumentNullException(nameof(presentation));
            }

            Initialize();
            Presentation = presentation;
            NameText.text = presentation.DisplayName;
            ManaCostText.text = $"마력 {presentation.ManaCost}";
            MetadataText.text =
                $"{presentation.TypeLabel} · {presentation.RarityLabel} · " +
                $"{presentation.ContentId}";
            StatsText.text = presentation.HasMonsterStats
                ? $"공격 {presentation.Attack}  생명 {presentation.Health}"
                : string.Empty;
            StatsText.gameObject.SetActive(presentation.HasMonsterStats);
            EffectText.text = presentation.EffectText;
            SelectionText.text = presentation.Selected
                ? $"[선택 {presentation.SelectionOrder}번]"
                : string.Empty;
            SelectionText.gameObject.SetActive(presentation.Selected);
            BlockReasonText.text = presentation.Interactable
                ? string.Empty
                : string.IsNullOrWhiteSpace(presentation.BlockReason)
                    ? "[사용 불가]"
                    : $"[사용 불가] {presentation.BlockReason}";
            BlockReasonText.gameObject.SetActive(!presentation.Interactable);
            AccessibilityText.text = presentation.AccessibilityText;

            FrameImage.color = presentation.Interactable
                ? (presentation.Selected
                    ? SelectedColor
                    : presentation.TypeColor)
                : DisabledColor;
            RarityAccentImage.color = presentation.RarityColor;
            ClickButton.interactable = presentation.Interactable;
            ClickButton.onClick.RemoveAllListeners();
            string commandId = presentation.CommandId;
            ClickButton.onClick.AddListener(
                () => clicked?.Invoke(commandId));
        }

        private Image CreateImage(string objectName, float preferredHeight)
        {
            GameObject child = new(
                objectName,
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement));
            child.transform.SetParent(transform, false);
            child.GetComponent<LayoutElement>().preferredHeight =
                preferredHeight;
            return child.GetComponent<Image>();
        }

        private Text CreateText(
            string objectName,
            int fontSize,
            FontStyle style,
            float preferredHeight,
            Transform parent = null)
        {
            GameObject child = new(
                objectName,
                typeof(RectTransform),
                typeof(Text),
                typeof(LayoutElement));
            child.transform.SetParent(parent ?? transform, false);
            Text text = child.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            child.GetComponent<LayoutElement>().preferredHeight =
                preferredHeight;
            return text;
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
