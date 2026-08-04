using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HaveABreak.Cards
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1000)]
    public sealed class RuntimeBattleFieldResponsiveLayout : MonoBehaviour
    {
        // The battle panel itself begins 16 px above the screen bottom. These
        // panel-local reservations therefore place the field at screen Y=138,
        // exactly on the hand viewport's upper edge.
        public const float ReservedBottomHeight = 122f;
        public const float ReservedBottomWithCommands = 250f;
        public const float HorizontalInset = 8f;
        public const float TopInset = 2f;
        public const float MinimumSlotSize = 112f;
        public const float MaximumSlotSize = 244f;
        public const float MinimumFieldCardScale = 0.36f;
        public const float MaximumFieldCardScale = 0.67f;

        private const int RootHorizontalPadding = 6;
        private const int RootVerticalPadding = 5;
        private const float RootSpacing = 4f;
        private const int RowHorizontalPadding = 4;
        private const int RowVerticalPadding = 3;
        private const float RowSpacing = 10f;
        private const float ZoneLabelWidth = 70f;
        private const float DividerHeight = 3f;
        private const float StructureRefreshInterval = 0.15f;

        private RuntimeBattleFieldView fieldView;
        private RectTransform fieldRect;
        private float nextStructureRefresh;

        public float CurrentSlotSize { get; private set; } = 160f;
        public float CurrentCardScale { get; private set; } = 0.44f;

        public void Initialize(RuntimeBattleFieldView view)
        {
            fieldView = view ?? GetComponent<RuntimeBattleFieldView>();
            fieldRect = transform as RectTransform;
            ApplyStructure();
            ApplyCardScale();
        }

        public void ApplyNow(float pulseOverride = -1f)
        {
            fieldView ??= GetComponent<RuntimeBattleFieldView>();
            fieldRect ??= transform as RectTransform;
            ApplyStructure();
            ApplyCardScale();
            ApplyAvailablePulse(pulseOverride);
        }

        private void OnEnable()
        {
            Initialize(GetComponent<RuntimeBattleFieldView>());
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime >= nextStructureRefresh)
            {
                ApplyStructure();
                nextStructureRefresh =
                    Time.unscaledTime + StructureRefreshInterval;
            }

            ApplyCardScale();
            ApplyAvailablePulse();
        }

        private void ApplyStructure()
        {
            if (fieldRect == null || fieldView == null)
            {
                return;
            }

            LayoutElement fieldElement = GetComponent<LayoutElement>();
            if (fieldElement == null)
            {
                fieldElement = gameObject.AddComponent<LayoutElement>();
            }
            fieldElement.ignoreLayout = true;

            float reservedBottom = ResolveReservedBottomHeight();
            fieldRect.anchorMin = Vector2.zero;
            fieldRect.anchorMax = Vector2.one;
            fieldRect.pivot = new Vector2(0.5f, 0.5f);
            fieldRect.anchoredPosition = Vector2.zero;
            fieldRect.offsetMin =
                new Vector2(HorizontalInset, reservedBottom);
            fieldRect.offsetMax = new Vector2(-HorizontalInset, -TopInset);
            fieldRect.SetAsFirstSibling();

            RectTransform battlePanel = fieldRect.parent as RectTransform;
            if (battlePanel != null &&
                battlePanel.GetComponent<VerticalLayoutGroup>() != null &&
                battlePanel.anchorMin == Vector2.zero &&
                battlePanel.anchorMax == Vector2.one)
            {
                Vector2 maximum = battlePanel.offsetMax;
                maximum.y = -72f;
                battlePanel.offsetMax = maximum;
            }

            CurrentSlotSize = CalculateSquareSlotSize();
            CurrentCardScale = Mathf.Clamp(
                CurrentSlotSize * 0.95f / RuntimeCardView.ReferenceHeight,
                MinimumFieldCardScale,
                MaximumFieldCardScale);

            VerticalLayoutGroup rootLayout =
                GetComponent<VerticalLayoutGroup>();
            if (rootLayout != null)
            {
                rootLayout.padding = new RectOffset(
                    RootHorizontalPadding,
                    RootHorizontalPadding,
                    RootVerticalPadding,
                    RootVerticalPadding);
                rootLayout.spacing = RootSpacing;
                // Keep the three field rows packed against the hand instead
                // of vertically centering them in the remaining screen area.
                rootLayout.childAlignment = TextAnchor.LowerCenter;
                rootLayout.childControlWidth = true;
                rootLayout.childControlHeight = true;
                rootLayout.childForceExpandWidth = true;
                rootLayout.childForceExpandHeight = false;
            }

            float rowHeight = CurrentSlotSize + RowVerticalPadding * 2f;
            ConfigureRow("EnemyRow", rowHeight);
            ConfigureRow("MonsterRow", rowHeight);
            ConfigureRow("SkillRow", rowHeight);
            ConfigureDivider();

            ConfigureSlots(fieldView.EnemySlots, CurrentSlotSize);
            ConfigureSlots(fieldView.MonsterSlots, CurrentSlotSize);
            ConfigureSlots(fieldView.SkillSlots, CurrentSlotSize);
        }

        private float CalculateSquareSlotSize()
        {
            float fieldWidth = Mathf.Max(480f, fieldRect.rect.width);
            float fieldHeight = Mathf.Max(360f, fieldRect.rect.height);

            float widthForSlots = fieldWidth -
                                  RootHorizontalPadding * 2f -
                                  RowHorizontalPadding * 2f -
                                  ZoneLabelWidth -
                                  RowSpacing * 3f;
            float sizeFromWidth = widthForSlots /
                                  RuntimeBattleFieldPresentation.SlotCount;

            float fixedVertical = RootVerticalPadding * 2f +
                                  RootSpacing * 3f +
                                  DividerHeight +
                                  RowVerticalPadding * 6f;
            float sizeFromHeight =
                (fieldHeight - fixedVertical) / 3f;

            return Mathf.Clamp(
                Mathf.Min(sizeFromWidth, sizeFromHeight),
                MinimumSlotSize,
                MaximumSlotSize);
        }

        private float ResolveReservedBottomHeight()
        {
            Transform battleScreen = fieldRect?.parent?.parent;
            Transform commandScroll = battleScreen?.Find("CommandScroll");
            return commandScroll != null &&
                   commandScroll.gameObject.activeInHierarchy
                ? ReservedBottomWithCommands
                : ReservedBottomHeight;
        }

        private void ConfigureRow(string rowName, float rowHeight)
        {
            Transform rowTransform = transform.Find(rowName);
            if (rowTransform == null)
            {
                return;
            }

            LayoutElement rowElement =
                rowTransform.GetComponent<LayoutElement>();
            if (rowElement != null)
            {
                rowElement.minHeight = rowHeight;
                rowElement.preferredHeight = rowHeight;
                rowElement.flexibleHeight = 0f;
            }

            HorizontalLayoutGroup row =
                rowTransform.GetComponent<HorizontalLayoutGroup>();
            if (row != null)
            {
                row.padding = new RectOffset(
                    RowHorizontalPadding,
                    RowHorizontalPadding,
                    RowVerticalPadding,
                    RowVerticalPadding);
                row.spacing = RowSpacing;
                row.childAlignment = rowName switch
                {
                    "EnemyRow" => TextAnchor.LowerCenter,
                    "MonsterRow" => TextAnchor.UpperCenter,
                    _ => TextAnchor.MiddleCenter
                };
                row.childControlWidth = true;
                row.childControlHeight = true;
                row.childForceExpandWidth = false;
                row.childForceExpandHeight = false;
            }

            Text zoneLabel = rowTransform.Find("ZoneLabel")
                ?.GetComponent<Text>();
            if (zoneLabel == null)
            {
                return;
            }

            zoneLabel.fontSize = 18;
            zoneLabel.resizeTextForBestFit = false;
            LayoutElement labelElement =
                zoneLabel.GetComponent<LayoutElement>();
            if (labelElement != null)
            {
                labelElement.minWidth = ZoneLabelWidth;
                labelElement.preferredWidth = ZoneLabelWidth;
                labelElement.flexibleWidth = 0f;
            }
        }

        private void ConfigureDivider()
        {
            Transform divider = transform.Find("FieldCenterLine");
            LayoutElement dividerLayout = divider?.GetComponent<LayoutElement>();
            if (dividerLayout == null)
            {
                return;
            }

            dividerLayout.minHeight = DividerHeight;
            dividerLayout.preferredHeight = DividerHeight;
            dividerLayout.flexibleHeight = 0f;
        }

        private static void ConfigureSlots(
            System.Collections.Generic.IReadOnlyList<
                RuntimeBattleFieldSlotView> slots,
            float size)
        {
            if (slots == null)
            {
                return;
            }

            foreach (RuntimeBattleFieldSlotView slot in slots)
            {
                if (slot == null)
                {
                    continue;
                }

                LayoutElement element = slot.GetComponent<LayoutElement>();
                if (element != null)
                {
                    element.minWidth = size;
                    element.preferredWidth = size;
                    element.flexibleWidth = 0f;
                    element.minHeight = size;
                    element.preferredHeight = size;
                    element.flexibleHeight = 0f;
                }

                RectTransform slotRect = slot.transform as RectTransform;
                if (slotRect != null)
                {
                    slotRect.SetSizeWithCurrentAnchors(
                        RectTransform.Axis.Horizontal,
                        size);
                    slotRect.SetSizeWithCurrentAnchors(
                        RectTransform.Axis.Vertical,
                        size);
                }

                if (slot.LabelText != null)
                {
                    slot.LabelText.fontSize = 15;
                    slot.LabelText.resizeTextForBestFit = false;
                }
            }
        }

        private void ApplyCardScale()
        {
            if (fieldView == null)
            {
                return;
            }

            foreach (RuntimeBattleFieldSlotView slot in
                     fieldView.MonsterSlots.Concat(fieldView.SkillSlots))
            {
                RectTransform cardRect =
                    slot?.CardView?.transform as RectTransform;
                if (cardRect == null)
                {
                    continue;
                }

                cardRect.anchoredPosition = Vector2.zero;
                cardRect.localRotation = Quaternion.identity;
                cardRect.localScale = Vector3.one * CurrentCardScale;
                cardRect.SetAsLastSibling();

                CanvasGroup group = cardRect.GetComponent<CanvasGroup>();
                if (group != null)
                {
                    group.alpha = 1f;
                }
            }
        }

        private void ApplyAvailablePulse(float pulseOverride = -1f)
        {
            if (fieldView == null)
            {
                return;
            }

            float pulse = pulseOverride >= 0f
                ? Mathf.Clamp01(pulseOverride)
                : 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);

            foreach (RuntimeBattleFieldSlotView slot in
                     fieldView.EnemySlots
                         .Concat(fieldView.MonsterSlots)
                         .Concat(fieldView.SkillSlots))
            {
                RuntimeBattleFieldZonePlate plate =
                    slot?.GetComponent<RuntimeBattleFieldZonePlate>();
                if (plate == null)
                {
                    continue;
                }

                bool available =
                    slot.DropZone?.IsAvailableHighlighted == true;
                if (!available)
                {
                    if (plate.OuterFrame != null)
                    {
                        plate.OuterFrame.transform.localScale = Vector3.one;
                    }
                    if (plate.CenterGlyph != null)
                    {
                        plate.CenterGlyph.transform.localScale = Vector3.one;
                    }
                    continue;
                }

                Color accent = Color.Lerp(
                    new Color(0.18f, 0.76f, 1f, 1f),
                    Color.white,
                    0.06f + pulse * 0.06f);
                if (plate.OuterFrame != null)
                {
                    plate.OuterFrame.color = new Color(
                        accent.r,
                        accent.g,
                        accent.b,
                        0.84f + pulse * 0.12f);
                    plate.OuterFrame.transform.localScale =
                        Vector3.one * (1f + pulse * 0.008f);
                }
                if (plate.InnerFrame != null)
                {
                    plate.InnerFrame.color = new Color(
                        accent.r,
                        accent.g,
                        accent.b,
                        0.42f + pulse * 0.10f);
                }
                if (plate.CenterGlyph != null)
                {
                    plate.CenterGlyph.color = new Color(
                        accent.r,
                        accent.g,
                        accent.b,
                        0.13f + pulse * 0.07f);
                    plate.CenterGlyph.transform.localScale =
                        Vector3.one * (0.98f + pulse * 0.05f);
                }
            }
        }
    }

    public static class RuntimeBattleFieldResponsiveLayoutBootstrap
    {
        private static float nextScanTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            Canvas.willRenderCanvases -= ApplyToLoadedFields;
            Canvas.willRenderCanvases += ApplyToLoadedFields;
            nextScanTime = 0f;
        }

        public static void ApplyToLoadedFields()
        {
            if (Application.isPlaying && Time.unscaledTime < nextScanTime)
            {
                return;
            }
            nextScanTime = Time.unscaledTime + 0.2f;

            RuntimeBattleFieldView[] fields =
                Object.FindObjectsByType<RuntimeBattleFieldView>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            foreach (RuntimeBattleFieldView field in fields)
            {
                if (field == null)
                {
                    continue;
                }

                RuntimeBattleFieldResponsiveLayout responsive =
                    field.GetComponent<RuntimeBattleFieldResponsiveLayout>();
                if (responsive == null)
                {
                    responsive = field.gameObject.AddComponent<
                        RuntimeBattleFieldResponsiveLayout>();
                }
                responsive.Initialize(field);
            }
        }
    }
}
