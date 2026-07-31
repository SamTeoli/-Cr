using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HaveABreak.Cards
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1000)]
    public sealed class RuntimeBattleFieldResponsiveLayout : MonoBehaviour
    {
        public const float ReservedBottomHeight = 270f;
        public const float HorizontalInset = 18f;
        public const float TopInset = 4f;
        public const float SlotWidth = 286f;
        public const float EnemySlotHeight = 142f;
        public const float PlayerSlotHeight = 166f;
        public const float FieldCardScale = 0.53f;

        private const float EnemyRowHeight = 148f;
        private const float PlayerRowHeight = 174f;
        private const float StructureRefreshInterval = 0.2f;

        private RuntimeBattleFieldView fieldView;
        private RectTransform fieldRect;
        private float nextStructureRefresh;

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

            fieldRect.anchorMin = Vector2.zero;
            fieldRect.anchorMax = Vector2.one;
            fieldRect.pivot = new Vector2(0.5f, 0.5f);
            fieldRect.anchoredPosition = Vector2.zero;
            fieldRect.offsetMin =
                new Vector2(HorizontalInset, ReservedBottomHeight);
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

            VerticalLayoutGroup rootLayout =
                GetComponent<VerticalLayoutGroup>();
            if (rootLayout != null)
            {
                rootLayout.padding = new RectOffset(10, 10, 5, 5);
                rootLayout.spacing = 4f;
                rootLayout.childAlignment = TextAnchor.MiddleCenter;
                rootLayout.childControlWidth = true;
                rootLayout.childControlHeight = true;
                rootLayout.childForceExpandWidth = true;
                rootLayout.childForceExpandHeight = false;
            }

            ConfigureRow("EnemyRow", EnemyRowHeight);
            ConfigureRow("MonsterRow", PlayerRowHeight);
            ConfigureRow("SkillRow", PlayerRowHeight);

            ConfigureSlots(fieldView.EnemySlots, EnemySlotHeight);
            ConfigureSlots(fieldView.MonsterSlots, PlayerSlotHeight);
            ConfigureSlots(fieldView.SkillSlots, PlayerSlotHeight);
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
                row.padding = new RectOffset(4, 4, 3, 3);
                row.spacing = 14f;
                row.childAlignment = TextAnchor.MiddleCenter;
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
                labelElement.minWidth = 88f;
                labelElement.preferredWidth = 88f;
                labelElement.flexibleWidth = 0f;
            }
        }

        private static void ConfigureSlots(
            System.Collections.Generic.IReadOnlyList<
                RuntimeBattleFieldSlotView> slots,
            float height)
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
                    element.minWidth = SlotWidth;
                    element.preferredWidth = SlotWidth;
                    element.flexibleWidth = 0f;
                    element.minHeight = height;
                    element.preferredHeight = height;
                    element.flexibleHeight = 0f;
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
                cardRect.localScale = Vector3.one * FieldCardScale;
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
