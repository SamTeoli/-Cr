using System;
using UnityEngine;
using UnityEngine.UI;

namespace HaveABreak.Cards
{
    public sealed class RuntimeCardView : MonoBehaviour
    {
        private static readonly Color DisabledColor =
            new(0.42f, 0.42f, 0.42f, 1f);
        private static readonly Color SelectedColor =
            new(0.3f, 0.78f, 1f, 1f);
        private static readonly Color EmptyArtworkColor =
            new(0.045f, 0.055f, 0.075f, 1f);
        private static readonly Color RulesPanelColor =
            new(0.025f, 0.03f, 0.04f, 0.94f);

        public Button ClickButton { get; private set; }
        public Image FrameImage { get; private set; }
        public Image FrameOverlayImage { get; private set; }
        public Image RarityAccentImage { get; private set; }
        public Image ArtworkImage { get; private set; }
        public Text NameText { get; private set; }
        public Text ManaCostText { get; private set; }
        public Text ArtPlaceholderText { get; private set; }
        public Text MetadataText { get; private set; }
        public Text StatsText { get; private set; }
        public Text AttackText { get; private set; }
        public Text HealthText { get; private set; }
        public Text EffectText { get; private set; }
        public Text SelectionText { get; private set; }
        public Text BlockReasonText { get; private set; }
        public Text AccessibilityText { get; private set; }
        public RuntimeCardPresentation Presentation { get; private set; }

        private CardFrameTheme frameTheme;
        private CardLayoutSettings layoutSettings;
        private Font cardFont;

        public void Initialize()
        {
            if (ClickButton != null)
            {
                return;
            }

            LayoutElement element = gameObject.GetComponent<LayoutElement>() ??
                                    gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 226f;
            element.preferredHeight = 344f;

            FrameImage = gameObject.GetComponent<Image>() ??
                         gameObject.AddComponent<Image>();
            FrameImage.preserveAspect = false;
            frameTheme = Resources.Load<CardFrameTheme>("UI/CardFrameTheme");
            layoutSettings = Resources.Load<CardLayoutSettings>(
                "UI/CardLayoutSettings");
            cardFont = CreateCardFont();

            ClickButton = gameObject.GetComponent<Button>() ??
                          gameObject.AddComponent<Button>();
            ClickButton.targetGraphic = FrameImage;

            Image artworkBackground = CreateImage(
                "ArtworkBackground",
                EmptyArtworkColor,
                Min(Layout.Artwork),
                Max(Layout.Artwork));
            ArtworkImage = CreateImage(
                "Artwork",
                Color.white,
                Vector2.zero,
                Vector2.one,
                artworkBackground.transform);
            ArtworkImage.preserveAspect = true;
            ArtPlaceholderText = CreateText(
                "ArtPlaceholder",
                14,
                FontStyle.Italic,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                Vector2.one,
                artworkBackground.transform);
            ArtPlaceholderText.text = "일러스트";
            ArtPlaceholderText.color = new Color(0.62f, 0.66f, 0.72f, 1f);

            CreateImage(
                "RulesBackground",
                RulesPanelColor,
                Min(Layout.RulesPanel),
                Max(Layout.RulesPanel));

            FrameOverlayImage = CreateImage(
                "FrameOverlay",
                Color.white,
                Vector2.zero,
                Vector2.one);
            FrameOverlayImage.preserveAspect = false;
            FrameOverlayImage.raycastTarget = false;

            ManaCostText = CreateText(
                "ManaCost", Layout.ManaSize, FontStyle.Bold,
                TextAnchor.MiddleCenter, Min(Layout.Mana), Max(Layout.Mana));
            EnableBestFit(ManaCostText, 8, Layout.ManaSize);
            NameText = CreateText(
                "CardName", Layout.NameSize, FontStyle.Bold,
                TextAnchor.MiddleCenter, Min(Layout.CardName),
                Max(Layout.CardName));
            EnableBestFit(NameText, 9, Layout.NameSize);
            EffectText = CreateText(
                "Effect", Layout.EffectSize, FontStyle.Normal,
                TextAnchor.MiddleCenter, Min(Layout.Effect),
                Max(Layout.Effect));
            EnableBestFit(EffectText, 8, Layout.EffectSize);

            AttackText = CreateText(
                "Attack", Layout.StatSize, FontStyle.Bold,
                TextAnchor.MiddleCenter, Min(Layout.Attack),
                Max(Layout.Attack));
            EnableBestFit(AttackText, 8, Layout.StatSize);
            HealthText = CreateText(
                "Health", Layout.StatSize, FontStyle.Bold,
                TextAnchor.MiddleCenter, Min(Layout.Health),
                Max(Layout.Health));
            EnableBestFit(HealthText, 8, Layout.StatSize);
            StatsText = CreateText(
                "StatsAccessibility", 1, FontStyle.Normal,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            StatsText.color = new Color(1f, 1f, 1f, 0.01f);

            MetadataText = CreateText(
                "CardType", Layout.TypeSize, FontStyle.Bold,
                TextAnchor.MiddleCenter, Min(Layout.CardType),
                Max(Layout.CardType));
            EnableBestFit(MetadataText, 8, Layout.TypeSize);
            SelectionText = CreateText(
                "Selection", 10, FontStyle.Bold, TextAnchor.MiddleRight,
                Min(Layout.Selection), Max(Layout.Selection));
            BlockReasonText = CreateText(
                "BlockReason", 12, FontStyle.Bold, TextAnchor.MiddleCenter,
                Min(Layout.BlockReason),
                Max(Layout.BlockReason));
            BlockReasonText.color = new Color(1f, 0.55f, 0.48f, 1f);

            AccessibilityText = CreateText(
                "AccessibilityText", 1, FontStyle.Normal,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            AccessibilityText.color = new Color(1f, 1f, 1f, 0.01f);

            RarityAccentImage = CreateImage(
                "RarityAccent",
                Color.clear,
                Vector2.zero,
                Vector2.zero);
            RarityAccentImage.gameObject.SetActive(false);
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
            ApplyRarityFrame(presentation);

            ManaCostText.text = presentation.ManaCost.ToString();
            NameText.text = presentation.DisplayName;
            EffectText.text = presentation.EffectText;
            MetadataText.text = presentation.TypeLabel;

            ArtworkImage.sprite = presentation.Artwork;
            ArtworkImage.gameObject.SetActive(presentation.Artwork != null);
            ArtPlaceholderText.gameObject.SetActive(
                presentation.Artwork == null);

            bool hasStats = presentation.HasMonsterStats;
            AttackText.text = hasStats ? presentation.Attack.ToString() : "";
            HealthText.text = hasStats ? presentation.Health.ToString() : "";
            AttackText.gameObject.SetActive(hasStats);
            HealthText.gameObject.SetActive(hasStats);
            StatsText.text = hasStats
                ? $"공격 {presentation.Attack}, 생명력 {presentation.Health}"
                : string.Empty;

            SelectionText.text = presentation.Selected
                ? $"선택 {presentation.SelectionOrder}"
                : string.Empty;
            SelectionText.gameObject.SetActive(presentation.Selected);
            BlockReasonText.text = presentation.Interactable
                ? string.Empty
                : string.IsNullOrWhiteSpace(presentation.BlockReason)
                    ? "사용 불가"
                    : presentation.BlockReason;
            BlockReasonText.gameObject.SetActive(!presentation.Interactable);
            AccessibilityText.text = presentation.AccessibilityText;

            ClickButton.interactable = presentation.Interactable;
            ClickButton.onClick.RemoveAllListeners();
            string commandId = presentation.CommandId;
            ClickButton.onClick.AddListener(() =>
            {
                RuntimeCardDragHandler dragHandler =
                    GetComponent<RuntimeCardDragHandler>();
                if (dragHandler != null &&
                    dragHandler.ConsumeClickSuppression())
                {
                    return;
                }

                clicked?.Invoke(commandId);
            });
        }

        private void ApplyRarityFrame(RuntimeCardPresentation presentation)
        {
            CardFrameTheme.RarityFrame rarityFrame =
                frameTheme?.GetFrame(
                    presentation.Rarity,
                    presentation.CardType);
            Sprite frameSprite = rarityFrame?.FrameSprite;
            Color baseColor = frameSprite == null
                ? rarityFrame?.FallbackColor ?? presentation.RarityColor
                : Color.white;
            Color displayColor = !presentation.Interactable
                ? Color.Lerp(baseColor, DisabledColor, 0.55f)
                : presentation.Selected
                    ? Color.Lerp(baseColor, SelectedColor, 0.18f)
                    : baseColor;

            FrameImage.sprite = null;
            FrameImage.type = Image.Type.Simple;
            FrameImage.color = frameSprite == null
                ? displayColor
                : Color.clear;

            FrameOverlayImage.sprite = frameSprite;
            FrameOverlayImage.type = Image.Type.Simple;
            FrameOverlayImage.color = frameSprite == null
                ? Color.clear
                : displayColor;
        }

        private Image CreateImage(
            string objectName,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Transform parent = null)
        {
            GameObject child = new(
                objectName,
                typeof(RectTransform),
                typeof(Image));
            child.transform.SetParent(parent ?? transform, false);
            Image image = child.GetComponent<Image>();
            image.color = color;
            SetRect(image.rectTransform, anchorMin, anchorMax);
            return image;
        }

        private Text CreateText(
            string objectName,
            int fontSize,
            FontStyle style,
            TextAnchor alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Transform parent = null)
        {
            GameObject child = new(
                objectName,
                typeof(RectTransform),
                typeof(Text));
            child.transform.SetParent(parent ?? transform, false);
            Text text = child.GetComponent<Text>();
            text.font = cardFont ?? Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            SetRect(text.rectTransform, anchorMin, anchorMax);
            return text;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnableBestFit(
            Text text,
            int minimumSize,
            int maximumSize)
        {
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = minimumSize;
            text.resizeTextMaxSize = maximumSize;
        }

        private CardLayoutSettings Layout =>
            layoutSettings != null ? layoutSettings : DefaultLayout.Instance;

        private Font CreateCardFont()
        {
            string fontName = layoutSettings != null
                ? layoutSettings.KoreanOsFontName
                : "Malgun Gothic";
            Font font = Font.CreateDynamicFontFromOSFont(fontName, 32);
            return font != null
                ? font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static Vector2 Min(Rect rect) =>
            new(rect.xMin, rect.yMin);

        private static Vector2 Max(Rect rect) =>
            new(rect.xMax, rect.yMax);

        private static class DefaultLayout
        {
            public static readonly CardLayoutSettings Instance = Create();

            private static CardLayoutSettings Create()
            {
                CardLayoutSettings value =
                    ScriptableObject.CreateInstance<CardLayoutSettings>();
                value.hideFlags = HideFlags.HideAndDontSave;
                return value;
            }
        }
    }
}
