using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HaveABreak.Cards
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1100)]
    public sealed class RuntimeEnemyArtworkSlotView : MonoBehaviour
    {
        private const float ArtworkWidthRatio = 0.97f;
        private const float ArtworkBottomAnchor = 0.135f;

        private RuntimeBattleFieldSlotView slotView;
        private Image artworkImage;
        private Image informationBackdrop;
        private Image healthBarFill;
        private Text healthValueText;
        private Outline labelOutline;
        private string appliedArtworkKey;
        private string appliedTitle;
        private bool initialized;

        public RuntimeBattleFieldSlotView SlotView => slotView;
        public Image ArtworkImage => artworkImage;
        public Image InformationBackdrop => informationBackdrop;
        public bool IsShowingArtwork =>
            artworkImage != null && artworkImage.gameObject.activeSelf &&
            artworkImage.sprite != null;

        public void Initialize(RuntimeBattleFieldSlotView slot)
        {
            slotView = slot ?? GetComponent<RuntimeBattleFieldSlotView>();
            if (slotView == null || initialized)
            {
                ApplyNow();
                return;
            }

            initialized = true;
            CreateArtworkImage();
            CreateInformationBackdrop();
            CreateHealthBarFill();
            CreateHealthValueText();
            ConfigureLabelOutline();
            ApplyNow();
        }

        public void ApplyNow()
        {
            slotView ??= GetComponent<RuntimeBattleFieldSlotView>();
            if (slotView == null)
            {
                return;
            }
            if (!initialized)
            {
                Initialize(slotView);
                return;
            }

            RuntimeBattleFieldSlotPresentation presentation =
                slotView.Presentation;
            bool shouldShow = presentation?.Zone ==
                              RuntimeBattleFieldZone.Enemy &&
                              presentation.ShowsArtwork;
            string artworkKey = shouldShow
                ? presentation.ArtworkKey
                : string.Empty;
            string title = presentation?.Title ?? string.Empty;
            if (string.Equals(
                    artworkKey,
                    appliedArtworkKey,
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(title, appliedTitle, StringComparison.Ordinal))
            {
                ConfigureArtworkWidthFit();
                MaintainSiblingOrder();
                return;
            }

            appliedArtworkKey = artworkKey;
            appliedTitle = title;
            Sprite sprite = shouldShow
                ? RuntimeEnemyArtworkCatalog.Load(artworkKey)
                : null;
            bool visible = sprite != null;
            artworkImage.sprite = sprite;
            artworkImage.color = Color.white;
            artworkImage.gameObject.SetActive(visible);
            ConfigureArtworkWidthFit();
            informationBackdrop.gameObject.SetActive(visible);
            healthBarFill.gameObject.SetActive(visible);
            healthValueText.gameObject.SetActive(visible);
            ConfigureHealthBar(visible, presentation);
            ConfigureLabel(visible, presentation);
            MaintainSiblingOrder();
        }

        private void LateUpdate()
        {
            ApplyNow();
        }

        private void CreateArtworkImage()
        {
            GameObject artworkObject = new(
                "EnemyArtwork",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            artworkObject.transform.SetParent(transform, false);
            artworkImage = artworkObject.GetComponent<Image>();
            artworkImage.preserveAspect = true;
            artworkImage.raycastTarget = false;
            artworkImage.color = Color.white;

            RectTransform rect = artworkImage.rectTransform;
            rect.anchorMin = new Vector2(0.015f, ArtworkBottomAnchor);
            rect.anchorMax = new Vector2(0.985f, ArtworkBottomAnchor);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            artworkObject.SetActive(false);
        }

        private void ConfigureArtworkWidthFit()
        {
            if (artworkImage == null || artworkImage.sprite == null)
            {
                return;
            }

            RectTransform slotRect = transform as RectTransform;
            RectTransform artworkRect = artworkImage.rectTransform;
            float spriteWidth = artworkImage.sprite.rect.width;
            float spriteHeight = artworkImage.sprite.rect.height;
            if (slotRect == null || slotRect.rect.width <= 0f ||
                spriteWidth <= 0f || spriteHeight <= 0f)
            {
                return;
            }

            // The enemy art always consumes the available slot width first.
            // Its height is then derived from the source aspect ratio, so a
            // portrait sprite is no longer shrunk merely to fit vertically.
            float pixelsPerUnit = Mathf.Max(
                0.001f,
                artworkImage.sprite.pixelsPerUnit);
            float visibleWidth =
                artworkImage.sprite.bounds.size.x * pixelsPerUnit;
            if (visibleWidth <= 0.001f)
            {
                visibleWidth = spriteWidth;
            }

            // Tight sprite geometry describes the non-transparent artwork.
            // Expand the full image rectangle by the inverse transparent-margin
            // ratio so the visible monster, rather than its PNG canvas, fills
            // the requested horizontal width.
            float visibleTargetWidth =
                slotRect.rect.width * ArtworkWidthRatio;
            float fittedWidth =
                visibleTargetWidth * spriteWidth / visibleWidth;
            float fittedHeight = fittedWidth * spriteHeight / spriteWidth;
            float fittedWidthRatio = fittedWidth / slotRect.rect.width;
            float visibleCenterOffset =
                artworkImage.sprite.bounds.center.x * pixelsPerUnit /
                spriteWidth;
            float normalizedShift =
                -visibleCenterOffset * fittedWidthRatio;
            artworkRect.anchorMin = new Vector2(
                (1f - fittedWidthRatio) * 0.5f + normalizedShift,
                ArtworkBottomAnchor);
            artworkRect.anchorMax = new Vector2(
                1f - (1f - fittedWidthRatio) * 0.5f + normalizedShift,
                ArtworkBottomAnchor);
            artworkRect.pivot = new Vector2(0.5f, 0f);
            artworkRect.offsetMin = Vector2.zero;
            artworkRect.offsetMax = new Vector2(0f, fittedHeight);
            artworkRect.localScale = Vector3.one;
        }

        private void CreateInformationBackdrop()
        {
            GameObject backdropObject = new(
                "EnemyInformationBackdrop",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            backdropObject.transform.SetParent(transform, false);
            informationBackdrop = backdropObject.GetComponent<Image>();
            informationBackdrop.color =
                new Color(0.012f, 0.018f, 0.028f, 0.78f);
            informationBackdrop.raycastTarget = false;

            RectTransform rect = informationBackdrop.rectTransform;
            rect.anchorMin = new Vector2(0.025f, 0.018f);
            rect.anchorMax = new Vector2(0.975f, 0.205f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            backdropObject.SetActive(false);
        }

        private void CreateHealthBarFill()
        {
            GameObject fillObject = new(
                "EnemyHealthBarFill",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            fillObject.transform.SetParent(transform, false);
            healthBarFill = fillObject.GetComponent<Image>();
            healthBarFill.color = new Color(0.92f, 0.08f, 0.16f, 0.96f);
            healthBarFill.raycastTarget = false;

            RectTransform rect = healthBarFill.rectTransform;
            rect.anchorMin = new Vector2(0.235f, 0.038f);
            rect.anchorMax = new Vector2(0.945f, 0.075f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            fillObject.SetActive(false);
        }

        private void CreateHealthValueText()
        {
            GameObject textObject = new(
                "EnemyHealthValue",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            textObject.transform.SetParent(transform, false);
            healthValueText = textObject.GetComponent<Text>();
            healthValueText.font = slotView.LabelText != null
                ? slotView.LabelText.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            healthValueText.fontSize = 13;
            healthValueText.fontStyle = FontStyle.Bold;
            healthValueText.alignment = TextAnchor.MiddleRight;
            healthValueText.color = Color.white;
            healthValueText.raycastTarget = false;

            RectTransform rect = healthValueText.rectTransform;
            rect.anchorMin = new Vector2(0.025f, 0.018f);
            rect.anchorMax = new Vector2(0.215f, 0.092f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = new Vector2(-3f, 0f);

            Outline outline = textObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            outline.effectDistance = new Vector2(1f, -1f);
            textObject.SetActive(false);
        }

        private void ConfigureHealthBar(
            bool artworkVisible,
            RuntimeBattleFieldSlotPresentation presentation)
        {
            if (!artworkVisible || healthBarFill == null)
            {
                return;
            }

            ResolveHealth(
                presentation?.Title,
                out int current,
                out int maximum,
                out float ratio);
            healthValueText.text = maximum > 0
                ? $"{current}/{maximum}"
                : "--/--";
            RectTransform rect = healthBarFill.rectTransform;
            rect.anchorMin = new Vector2(0.235f, 0.038f);
            rect.anchorMax = new Vector2(
                Mathf.Lerp(0.235f, 0.945f, ratio),
                0.075f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void ConfigureLabelOutline()
        {
            Text label = slotView.LabelText;
            if (label == null)
            {
                return;
            }

            labelOutline = label.GetComponent<Outline>();
            if (labelOutline == null)
            {
                labelOutline = label.gameObject.AddComponent<Outline>();
            }
            labelOutline.effectColor = new Color(0f, 0f, 0f, 0.94f);
            labelOutline.effectDistance = new Vector2(1.5f, -1.5f);
            labelOutline.useGraphicAlpha = true;
        }

        private void ConfigureLabel(
            bool artworkVisible,
            RuntimeBattleFieldSlotPresentation presentation)
        {
            Text label = slotView.LabelText;
            if (label == null)
            {
                return;
            }

            RectTransform rect = label.rectTransform;
            if (artworkVisible)
            {
                rect.anchorMin = new Vector2(0.055f, 0.078f);
                rect.anchorMax = new Vector2(0.945f, 0.198f);
                rect.offsetMin = new Vector2(3f, 0f);
                rect.offsetMax = new Vector2(-3f, 0f);
                label.alignment = TextAnchor.MiddleCenter;
                label.fontSize = 15;
                label.fontStyle = FontStyle.Bold;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Truncate;
                label.text = ExtractDisplayName(presentation?.Title);
                label.color = Color.white;
            }
            else
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(8f, 3f);
                rect.offsetMax = new Vector2(-8f, -3f);
                label.alignment = TextAnchor.MiddleCenter;
                label.fontSize = 15;
                label.fontStyle = presentation?.Occupied == true
                    ? FontStyle.Bold
                    : FontStyle.Italic;
            }
        }

        private void MaintainSiblingOrder()
        {
            if (artworkImage == null || informationBackdrop == null ||
                healthBarFill == null || healthValueText == null)
            {
                return;
            }

            int artworkIndex = Mathf.Min(4, transform.childCount - 1);
            artworkImage.transform.SetSiblingIndex(Mathf.Max(0, artworkIndex));
            int backdropIndex = Mathf.Min(5, transform.childCount - 1);
            informationBackdrop.transform.SetSiblingIndex(
                Mathf.Max(0, backdropIndex));
            int healthIndex = Mathf.Min(6, transform.childCount - 1);
            healthBarFill.transform.SetSiblingIndex(Mathf.Max(0, healthIndex));
            int healthTextIndex = Mathf.Min(7, transform.childCount - 1);
            healthValueText.transform.SetSiblingIndex(
                Mathf.Max(0, healthTextIndex));
            slotView?.LabelText?.transform.SetAsLastSibling();
        }

        private static void ResolveHealth(
            string source,
            out int current,
            out int maximum,
            out float ratio)
        {
            current = 0;
            maximum = 0;
            ratio = 1f;
            if (string.IsNullOrWhiteSpace(source))
            {
                return;
            }

            int hpIndex = source.IndexOf("HP ", StringComparison.Ordinal);
            if (hpIndex < 0)
            {
                return;
            }

            int valueStart = hpIndex + 3;
            int slashIndex = source.IndexOf('/', valueStart);
            if (slashIndex < 0 ||
                !int.TryParse(
                    source.Substring(valueStart, slashIndex - valueStart),
                    out current))
            {
                return;
            }

            int maximumEnd = slashIndex + 1;
            while (maximumEnd < source.Length &&
                   char.IsDigit(source[maximumEnd]))
            {
                maximumEnd++;
            }
            if (!int.TryParse(
                    source.Substring(
                        slashIndex + 1,
                        maximumEnd - slashIndex - 1),
                    out maximum) ||
                maximum <= 0)
            {
                maximum = 0;
                return;
            }

            ratio = Mathf.Clamp01((float)current / maximum);
        }

        private static string ExtractDisplayName(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return string.Empty;
            }

            string[] lines = source.Split(
                new[] { '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);
            return lines.Length == 0
                ? string.Empty
                : lines[0].Trim();
        }
    }

    public static class RuntimeEnemyArtworkBootstrap
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
                UnityEngine.Object.FindObjectsByType<RuntimeBattleFieldView>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            foreach (RuntimeBattleFieldView field in fields)
            {
                if (field == null)
                {
                    continue;
                }

                IReadOnlyList<RuntimeBattleFieldSlotView> enemySlots =
                    field.EnemySlots;
                for (int index = 0; index < enemySlots.Count; index++)
                {
                    RuntimeBattleFieldSlotView slot = enemySlots[index];
                    if (slot == null)
                    {
                        continue;
                    }

                    RuntimeEnemyArtworkSlotView artwork =
                        slot.GetComponent<RuntimeEnemyArtworkSlotView>();
                    if (artwork == null)
                    {
                        artwork = slot.gameObject.AddComponent<
                            RuntimeEnemyArtworkSlotView>();
                    }
                    artwork.Initialize(slot);
                    artwork.ApplyNow();
                }
            }
        }
    }
}
