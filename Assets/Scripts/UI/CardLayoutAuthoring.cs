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
        public bool CaptureLayout()
        {
            if (settings == null || cardView == null)
            {
                return false;
            }

            EnsureVisualLayering();

            RectTransform artwork = FindRect("ArtworkBackground");
            RectTransform rules = FindRect("RulesBackground");
            RectTransform mana = FindRect("ManaCost");
            RectTransform cardName = FindRect("CardName");
            RectTransform effect = FindRect("Effect");
            RectTransform attack = FindRect("Attack");
            RectTransform health = FindRect("Health");
            RectTransform cardType = FindRect("CardType");
            RectTransform selection = FindRect("Selection");
            RectTransform block = FindRect("BlockReason");
            if (artwork == null || rules == null ||
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
            Rect centeredNameRect = GetRect(cardName);
            centeredNameRect.x =
                0.5f - centeredNameRect.width * 0.5f;
            settings.EditorCapture(
                GetRect(mana),
                centeredNameRect,
                GetRect(artwork),
                GetRect(rules),
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

            int backgroundIndex = 0;
            Transform[] directChildren = new Transform[cardRect.childCount];
            for (int i = 0; i < cardRect.childCount; i++)
            {
                directChildren[i] = cardRect.GetChild(i);
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
