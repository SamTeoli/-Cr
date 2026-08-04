using System;
using UnityEngine;
using UnityEngine.UI;

namespace HaveABreak.Cards
{
    public sealed class RuntimeCardView : MonoBehaviour
    {
        public const float ReferenceHeight = 344f;
        public const float ReferenceWidth = 242.5f;

        private static readonly Color DisabledColor =
            new(0.42f, 0.42f, 0.42f, 1f);
        private static readonly Color SelectedColor =
            new(0.3f, 0.78f, 1f, 1f);
        public Button ClickButton { get; private set; }
        public Image FrameImage { get; private set; }
        public Image FrameOverlayImage { get; private set; }
        public Image RarityAccentImage { get; private set; }
        public Image ArtworkImage { get; private set; }
        public Image TypeSurfaceImage { get; private set; }
        public Image HeaderPanelImage { get; private set; }
        public Image RulesPanelImage { get; private set; }
        public Image StatsPanelImage { get; private set; }
        public Image ManaBadgeImage { get; private set; }
        public Image TypeBadgeImage { get; private set; }
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

        private CardLayoutSettings layoutSettings;
        private CardFrameTheme frameTheme;
        private Font cardFont;
        private RectTransform visualRoot;

        public void Initialize()
        {
            if (ClickButton != null)
            {
                return;
            }

            LayoutElement element = gameObject.GetComponent<LayoutElement>() ??
                                    gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = ReferenceWidth;
            element.preferredHeight = ReferenceHeight;

            FrameImage = gameObject.GetComponent<Image>() ??
                         gameObject.AddComponent<Image>();
            FrameImage.color = Color.clear;
            FrameImage.preserveAspect = false;
            layoutSettings = Resources.Load<CardLayoutSettings>(
                "UI/CardLayoutSettings");
            frameTheme = Resources.Load<CardFrameTheme>(
                "UI/CardFrameTheme");
            cardFont = CreateCardFont();

            ClickButton = gameObject.GetComponent<Button>() ??
                          gameObject.AddComponent<Button>();
            ClickButton.targetGraphic = FrameImage;

            visualRoot = CreateVisualRoot();

            FrameOverlayImage = CreateImage(
                "RarityFrame",
                Color.white,
                Vector2.zero,
                Vector2.one);
            FrameOverlayImage.raycastTarget = false;

            TypeSurfaceImage = CreateImage(
                "TypeSurface",
                Color.clear,
                new Vector2(0.018f, 0.013f),
                new Vector2(0.982f, 0.987f));
            TypeSurfaceImage.raycastTarget = false;

            Image innerSurface = CreateImage(
                "InnerSurface",
                Color.clear,
                new Vector2(0.038f, 0.035f),
                new Vector2(0.962f, 0.965f));
            innerSurface.raycastTarget = false;

            ArtworkImage = CreateImage(
                "Artwork",
                Color.white,
                Min(Layout.Artwork),
                Max(Layout.Artwork));
            // Show the complete authored illustration without stretching or
            // cropping it to the card's artwork slot.
            ArtworkImage.preserveAspect = true;
            ArtworkImage.raycastTarget = false;
            ArtPlaceholderText = null;

            // UI siblings render from first to last. Keep the illustration
            // behind the authored frame so it cannot cover borders or
            // ornaments, while all labels created below remain on top.
            FrameOverlayImage.transform.SetSiblingIndex(
                ArtworkImage.transform.GetSiblingIndex() + 1);

            HeaderPanelImage = CreateImage(
                "HeaderPanel",
                Color.clear,
                new Vector2(0.04f, 0.86f),
                new Vector2(0.96f, 0.975f));
            HeaderPanelImage.raycastTarget = false;

            RulesPanelImage = CreateImage(
                "RulesPanel",
                Color.clear,
                Min(Layout.RulesPanel),
                Max(Layout.RulesPanel));
            RulesPanelImage.raycastTarget = false;

            StatsPanelImage = CreateImage(
                "StatsPanel",
                Color.clear,
                new Vector2(0.04f, 0.025f),
                new Vector2(0.96f, 0.09f));
            StatsPanelImage.raycastTarget = false;

            ManaBadgeImage = CreateImage(
                "ManaBadge",
                Color.clear,
                Min(Layout.Mana),
                Max(Layout.Mana));
            ManaBadgeImage.raycastTarget = false;

            TypeBadgeImage = CreateImage(
                "TypeBadge",
                Color.clear,
                Min(Layout.CardType),
                Max(Layout.CardType));
            TypeBadgeImage.raycastTarget = false;

            ManaCostText = CreateText(
                "ManaCost", Layout.ManaSize, FontStyle.Bold,
                TextAnchor.MiddleCenter, Min(Layout.Mana), Max(Layout.Mana));
            NameText = CreateText(
                "CardName", Layout.NameSize, FontStyle.Bold,
                TextAnchor.MiddleCenter, Min(Layout.CardName),
                Max(Layout.CardName));
            NameText.horizontalOverflow = HorizontalWrapMode.Overflow;
            EffectText = CreateText(
                "Effect", Layout.EffectSize, FontStyle.Normal,
                TextAnchor.MiddleCenter, Min(Layout.Effect),
                Max(Layout.Effect));
            EffectText.color = new Color(0.08f, 0.07f, 0.055f, 1f);

            AttackText = CreateText(
                "Attack", Layout.StatSize, FontStyle.Bold,
                TextAnchor.MiddleLeft, Min(Layout.Attack),
                Max(Layout.Attack));
            HealthText = CreateText(
                "Health", Layout.StatSize, FontStyle.Bold,
                TextAnchor.MiddleRight, Min(Layout.Health),
                Max(Layout.Health));
            StatsText = CreateText(
                "StatsAccessibility", 1, FontStyle.Normal,
                TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            StatsText.color = new Color(1f, 1f, 1f, 0.01f);

            MetadataText = CreateText(
                "CardType", Layout.TypeSize, FontStyle.Bold,
                TextAnchor.MiddleCenter, Min(Layout.CardType),
                Max(Layout.CardType));
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
                Color.white,
                new Vector2(0.035f, 0.965f),
                new Vector2(0.965f, 0.982f));
            RarityAccentImage.raycastTarget = false;
        }

        private RectTransform CreateVisualRoot()
        {
            GameObject visualObject = new(
                "CardVisualRoot",
                typeof(RectTransform));
            RectTransform rect =
                visualObject.GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(
                ReferenceWidth,
                ReferenceHeight);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            return rect;
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
            ArtworkImage.color = Color.white;
            ArtworkImage.material = null;
            ArtworkImage.gameObject.SetActive(presentation.Artwork != null);

            bool hasStats = presentation.HasMonsterStats;
            AttackText.text = hasStats
                ? $"공격력 {presentation.Attack}"
                : string.Empty;
            HealthText.text = hasStats
                ? $"생명력 {presentation.Health}"
                : string.Empty;
            AttackText.gameObject.SetActive(true);
            HealthText.gameObject.SetActive(true);
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
            ClickButton.onClick.AddListener(() => clicked?.Invoke(commandId));
        }

        private void ApplyRarityFrame(RuntimeCardPresentation presentation)
        {
            Color baseColor = presentation.RarityColor;
            Color displayColor = !presentation.Interactable
                ? Color.Lerp(baseColor, DisabledColor, 0.55f)
                : presentation.Selected
                    ? Color.Lerp(baseColor, SelectedColor, 0.18f)
                    : baseColor;

            FrameImage.color = Color.clear;
            CardFrameTheme.RarityFrame frame = frameTheme != null
                ? frameTheme.GetFrame(presentation.Rarity, presentation.CardType)
                : null;
            FrameOverlayImage.sprite = frame?.FrameSprite;
            FrameOverlayImage.type = Image.Type.Simple;
            FrameOverlayImage.preserveAspect = false;
            FrameOverlayImage.color = frame?.FrameSprite != null
                ? (!presentation.Interactable
                    ? new Color(0.62f, 0.62f, 0.62f, 1f)
                    : Color.white)
                : displayColor;
            RarityAccentImage.color = presentation.Rarity == CardRarity.Common
                ? Color.Lerp(displayColor, Color.black, 0.25f)
                : Color.Lerp(displayColor, Color.white, 0.28f);
            TypeSurfaceImage.color = Color.clear;
            HeaderPanelImage.color = Color.clear;
            RulesPanelImage.color = Color.clear;
            StatsPanelImage.color = Color.clear;
            ManaBadgeImage.color = Color.clear;
            TypeBadgeImage.color = Color.clear;
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
            child.transform.SetParent(
                parent ?? visualRoot ?? transform,
                false);
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
            child.transform.SetParent(
                parent ?? visualRoot ?? transform,
                false);
            Text text = child.GetComponent<Text>();
            text.font = cardFont ?? Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.alignByGeometry = true;
            SetRect(text.rectTransform, anchorMin, anchorMax);
            Shadow shadow = child.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.82f);
            shadow.effectDistance = new Vector2(1f, -1f);
            shadow.useGraphicAlpha = true;
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

        private CardLayoutSettings Layout =>
            layoutSettings != null ? layoutSettings : DefaultLayout.Instance;

        private Font CreateCardFont()
        {
            string preferredFont = layoutSettings != null
                ? layoutSettings.KoreanOsFontName
                : "Malgun Gothic";
            string[] candidates =
            {
                preferredFont,
                "Malgun Gothic",
                "맑은 고딕",
                "Noto Sans CJK KR",
                "Noto Sans KR",
                "Arial Unicode MS"
            };
            Font font = Font.CreateDynamicFontFromOSFont(candidates, 96);
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
