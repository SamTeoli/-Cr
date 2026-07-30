using UnityEngine;
using UnityEngine.UI;

namespace HaveABreak.Cards
{
    [ExecuteAlways]
    public sealed class CardLayoutAuthoring : MonoBehaviour
    {
        [SerializeField] private CardLayoutSettings settings;
        [SerializeField] private RuntimeCardView cardView;

        public CardLayoutSettings Settings => settings;
        public RuntimeCardView CardView => cardView;

        public void Initialize(
            CardLayoutSettings layoutSettings,
            RuntimeCardView view)
        {
            settings = layoutSettings;
            cardView = view;
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            UnityEditor.EditorApplication.delayCall +=
                ResetPreviewToSavedLayout;
        }

        public void ResetPreviewToSavedLayout()
        {
            if (this == null || settings == null || cardView == null)
            {
                return;
            }

            RectTransform cardRect = cardView.transform as RectTransform;
            if (cardRect == null)
            {
                return;
            }

            UnityEditor.Undo.RecordObject(
                cardRect,
                "Reset Card Layout Preview");
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(
                RuntimeCardView.ReferenceWidth,
                RuntimeCardView.ReferenceHeight);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.localScale = Vector3.one;
            cardRect.localRotation = Quaternion.identity;
            RectTransform visualRoot = EnsureVisualRoot(cardRect);
            if (visualRoot != null)
            {
                UnityEditor.Undo.RecordObject(
                    visualRoot,
                    "Reset Card Layout Preview");
                visualRoot.anchorMin = new Vector2(0.5f, 0.5f);
                visualRoot.anchorMax = new Vector2(0.5f, 0.5f);
                visualRoot.pivot = new Vector2(0.5f, 0.5f);
                visualRoot.sizeDelta = new Vector2(
                    RuntimeCardView.ReferenceWidth,
                    RuntimeCardView.ReferenceHeight);
                visualRoot.anchoredPosition = Vector2.zero;
                visualRoot.localScale = Vector3.one;
                visualRoot.localRotation = Quaternion.identity;
            }

            ApplyRect("ManaCost", settings.Mana);
            ApplyRect("CardName", settings.CardName);
            ApplyRect("Artwork", settings.Artwork);
            ApplyRect("Effect", settings.Effect);
            ApplyRect("Attack", settings.Attack);
            ApplyRect("Health", settings.Health);
            ApplyRect("CardType", settings.CardType);
            ApplyRect("Selection", settings.Selection);
            ApplyRect("BlockReason", settings.BlockReason);

            ApplyFontSize("ManaCost", settings.ManaSize);
            ApplyFontSize("CardName", settings.NameSize);
            ApplyFontSize("Effect", settings.EffectSize);
            ApplyFontSize("Attack", settings.StatSize);
            ApplyFontSize("Health", settings.StatSize);
            ApplyFontSize("CardType", settings.TypeSize);
            RemoveLegacyBackgroundObjects();
        }

        private RectTransform EnsureVisualRoot(RectTransform cardRect)
        {
            RectTransform existing = FindRect("CardVisualRoot");
            if (existing != null)
            {
                return existing;
            }

            GameObject visualObject = new(
                "CardVisualRoot",
                typeof(RectTransform));
            UnityEditor.Undo.RegisterCreatedObjectUndo(
                visualObject,
                "Create Fixed Card Visual Root");
            RectTransform visual =
                visualObject.GetComponent<RectTransform>();
            visual.SetParent(cardRect, false);

            Transform[] children = new Transform[cardRect.childCount];
            for (int index = 0; index < cardRect.childCount; index++)
            {
                children[index] = cardRect.GetChild(index);
            }
            foreach (Transform child in children)
            {
                if (child == visual ||
                    child.GetComponent<CardLayoutAuthoring>() != null)
                {
                    continue;
                }
                UnityEditor.Undo.SetTransformParent(
                    child,
                    visual,
                    "Move Card Visual Into Fixed Root");
            }

            RectTransform overlay = FindRect("FrameOverlay");
            if (overlay != null)
            {
                overlay.anchorMin = Vector2.zero;
                overlay.anchorMax = Vector2.one;
                overlay.offsetMin = Vector2.zero;
                overlay.offsetMax = Vector2.zero;
            }
            return visual;
        }

        public bool CaptureLayout()
        {
            if (settings == null || cardView == null)
            {
                return false;
            }

            EnsureVisualLayering();

            RemoveLegacyBackgroundObjects();
            RectTransform artwork = FindRect("Artwork");
            RectTransform mana = FindRect("ManaCost");
            RectTransform cardName = FindRect("CardName");
            RectTransform effect = FindRect("Effect");
            RectTransform attack = FindRect("Attack");
            RectTransform health = FindRect("Health");
            RectTransform cardType = FindRect("CardType");
            RectTransform selection = FindRect("Selection");
            RectTransform block = FindRect("BlockReason");
            if (artwork == null ||
                mana == null || cardName == null || effect == null ||
                attack == null || health == null || cardType == null ||
                selection == null || block == null)
            {
                return false;
            }

            Text manaText = mana.GetComponent<Text>();
            Text nameText = cardName.GetComponent<Text>();
            Text effectText = effect.GetComponent<Text>();
            Text attackText = attack.GetComponent<Text>();
            Text typeText = cardType.GetComponent<Text>();
            if (manaText == null || nameText == null ||
                effectText == null || attackText == null ||
                typeText == null)
            {
                return false;
            }

            UnityEditor.Undo.RecordObject(
                settings,
                "Save Card Layout");
            settings.EditorCapture(
                GetRect(mana),
                GetRect(cardName),
                GetRect(artwork),
                settings.RulesPanel,
                GetRect(effect),
                GetRect(attack),
                GetRect(health),
                GetRect(cardType),
                GetRect(selection),
                GetRect(block),
                manaText.fontSize,
                nameText.fontSize,
                effectText.fontSize,
                attackText.fontSize,
                typeText.fontSize);
            UnityEditor.EditorUtility.SetDirty(settings);
            UnityEditor.AssetDatabase.SaveAssets();
            return true;
        }

        private void RemoveLegacyBackgroundObjects()
        {
            if (this == null || cardView == null || settings == null)
            {
                return;
            }

            RectTransform artwork = FindRect("Artwork");
            RectTransform artworkBackground =
                FindRect("ArtworkBackground");
            if (artwork != null && artworkBackground != null &&
                artwork.parent == artworkBackground)
            {
                artwork.SetParent(cardView.transform, false);
                artwork.anchorMin = new Vector2(
                    settings.Artwork.xMin,
                    settings.Artwork.yMin);
                artwork.anchorMax = new Vector2(
                    settings.Artwork.xMax,
                    settings.Artwork.yMax);
                artwork.offsetMin = Vector2.zero;
                artwork.offsetMax = Vector2.zero;
            }

            DestroyLegacyObject("ArtPlaceholder");
            DestroyLegacyObject("ArtworkBackground");
            DestroyLegacyObject("RulesBackground");
        }

        private void DestroyLegacyObject(string objectName)
        {
            RectTransform target = FindRect(objectName);
            if (target == null)
            {
                return;
            }

            UnityEditor.Undo.DestroyObjectImmediate(target.gameObject);
        }

        private void ApplyRect(string objectName, Rect value)
        {
            RectTransform target = FindRect(objectName);
            if (target == null)
            {
                return;
            }

            UnityEditor.Undo.RecordObject(
                target,
                "Reset Card Layout Preview");
            target.anchorMin = new Vector2(value.xMin, value.yMin);
            target.anchorMax = new Vector2(value.xMax, value.yMax);
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
            target.localScale = Vector3.one;
            target.localRotation = Quaternion.identity;
        }

        private void ApplyFontSize(string objectName, int size)
        {
            RectTransform target = FindRect(objectName);
            Text text = target == null ? null : target.GetComponent<Text>();
            if (text == null)
            {
                return;
            }

            UnityEditor.Undo.RecordObject(
                text,
                "Reset Card Layout Preview");
            text.fontSize = size;
            text.resizeTextForBestFit = false;
        }

        private void EnsureVisualLayering()
        {
            RectTransform cardRect = cardView.transform as RectTransform;
            Image rootFrame = cardView.GetComponent<Image>();
            if (cardRect == null || rootFrame == null)
            {
                return;
            }

            RectTransform overlayRect = FindRect("FrameOverlay");
            Image overlay;
            if (overlayRect == null)
            {
                GameObject overlayObject = new(
                    "FrameOverlay",
                    typeof(RectTransform),
                    typeof(Image));
                overlayRect = overlayObject.GetComponent<RectTransform>();
                overlayRect.SetParent(cardRect, false);
                overlayRect.anchorMin = Vector2.zero;
                overlayRect.anchorMax = Vector2.one;
                overlayRect.offsetMin = Vector2.zero;
                overlayRect.offsetMax = Vector2.zero;
                overlay = overlayObject.GetComponent<Image>();
            }
            else
            {
                overlay = overlayRect.GetComponent<Image>();
            }

            if (overlay == null)
            {
                return;
            }

            if (rootFrame.sprite != null)
            {
                overlay.sprite = rootFrame.sprite;
                overlay.color = rootFrame.color;
                overlay.material = rootFrame.material;
                overlay.type = rootFrame.type;
                overlay.preserveAspect = rootFrame.preserveAspect;
                rootFrame.sprite = null;
                rootFrame.color = Color.clear;
            }

            overlay.raycastTarget = false;

            RectTransform visualRoot = FindRect("CardVisualRoot");
            RectTransform contentRoot = visualRoot != null
                ? visualRoot
                : cardRect;
            int backgroundIndex = 0;
            Transform[] directChildren =
                new Transform[contentRoot.childCount];
            for (int i = 0; i < contentRoot.childCount; i++)
            {
                directChildren[i] = contentRoot.GetChild(i);
            }

            foreach (Transform child in directChildren)
            {
                if (child == overlayRect)
                {
                    continue;
                }

                if (child.GetComponent<Image>() != null &&
                    child.GetComponent<Text>() == null)
                {
                    child.SetSiblingIndex(backgroundIndex++);
                }
            }

            overlayRect.SetSiblingIndex(backgroundIndex);
        }

        private RectTransform FindRect(string objectName)
        {
            Transform[] children =
                cardView.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name == objectName)
                {
                    return child as RectTransform;
                }
            }

            return null;
        }

        private Rect GetRect(RectTransform rect)
        {
            RectTransform cardRect = cardView.transform as RectTransform;
            if (cardRect == null ||
                Mathf.Approximately(cardRect.rect.width, 0f) ||
                Mathf.Approximately(cardRect.rect.height, 0f))
            {
                return default;
            }

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector3 bottomLeft =
                cardRect.InverseTransformPoint(corners[0]);
            Vector3 topRight =
                cardRect.InverseTransformPoint(corners[2]);
            Rect bounds = cardRect.rect;

            return new Rect(
                (bottomLeft.x - bounds.xMin) / bounds.width,
                (bottomLeft.y - bounds.yMin) / bounds.height,
                (topRight.x - bottomLeft.x) / bounds.width,
                (topRight.y - bottomLeft.y) / bounds.height);
        }
#endif
    }
}
