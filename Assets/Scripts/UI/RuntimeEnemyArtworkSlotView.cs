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
        private RuntimeBattleFieldSlotView slotView;
        private Image artworkImage;
        private Image informationBackdrop;
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
            informationBackdrop.gameObject.SetActive(visible);
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
            rect.anchorMin = new Vector2(0.045f, 0.17f);
            rect.anchorMax = new Vector2(0.955f, 0.965f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            artworkObject.SetActive(false);
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
            rect.anchorMin = new Vector2(0.035f, 0.025f);
            rect.anchorMax = new Vector2(0.965f, 0.315f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            backdropObject.SetActive(false);
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
                rect.anchorMin = new Vector2(0.055f, 0.035f);
                rect.anchorMax = new Vector2(0.945f, 0.305f);
                rect.offsetMin = new Vector2(4f, 2f);
                rect.offsetMax = new Vector2(-4f, -2f);
                label.alignment = TextAnchor.MiddleCenter;
                label.fontSize = 13;
                label.fontStyle = FontStyle.Bold;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Truncate;
                label.text = CompactTitle(presentation?.Title);
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
            if (artworkImage == null || informationBackdrop == null)
            {
                return;
            }

            int artworkIndex = Mathf.Min(4, transform.childCount - 1);
            artworkImage.transform.SetSiblingIndex(Mathf.Max(0, artworkIndex));
            int backdropIndex = Mathf.Min(5, transform.childCount - 1);
            informationBackdrop.transform.SetSiblingIndex(
                Mathf.Max(0, backdropIndex));
            slotView?.LabelText?.transform.SetAsLastSibling();
        }

        private static string CompactTitle(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return string.Empty;
            }

            string[] lines = source.Split(
                new[] { '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length <= 2)
            {
                return source.Trim();
            }
            return $"{lines[0].Trim()}\n{lines[1].Trim()}";
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
