using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HaveABreak.Cards
{
    [DisallowMultipleComponent]
    public sealed class RuntimeBattleHandLayout : LayoutGroup
    {
        [SerializeField] private float cardWidth = 226f;
        [SerializeField] private float cardHeight = 344f;
        [SerializeField] private float visibleSpacing = 126f;
        [SerializeField] private float arcHeight = 24f;
        [SerializeField] private float maxRotation = 7f;

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            float width = rectChildren.Count <= 1
                ? cardWidth
                : cardWidth + visibleSpacing * (rectChildren.Count - 1);
            SetLayoutInputForAxis(width, width, -1f, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            SetLayoutInputForAxis(cardHeight, cardHeight, -1f, 1);
        }

        public override void SetLayoutHorizontal()
        {
            LayoutCards();
        }

        public override void SetLayoutVertical()
        {
            LayoutCards();
        }

        private void LayoutCards()
        {
            int count = rectChildren.Count;
            if (count == 0)
            {
                return;
            }

            float spacing = count <= 1
                ? 0f
                : Mathf.Min(
                    visibleSpacing,
                    Mathf.Max(
                        54f,
                        (rectTransform.rect.width - cardWidth) / (count - 1)));
            float totalWidth = cardWidth + spacing * (count - 1);
            float firstX =
                Mathf.Max(0f, (rectTransform.rect.width - totalWidth) * 0.5f);

            for (int index = 0; index < count; index++)
            {
                RectTransform card = rectChildren[index];
                RuntimeBattleHandCardHover hover =
                    card.GetComponent<RuntimeBattleHandCardHover>();
                int layoutIndex = hover == null
                    ? index
                    : Mathf.Clamp(hover.LayoutIndex, 0, count - 1);
                float normalized = count <= 1
                    ? 0f
                    : layoutIndex / (float)(count - 1) * 2f - 1f;
                float y = Mathf.Abs(normalized) * arcHeight;
                SetChildAlongAxis(
                    card,
                    0,
                    firstX + spacing * layoutIndex,
                    cardWidth);
                SetChildAlongAxis(card, 1, y, cardHeight);
                card.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    -normalized * maxRotation);
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class RuntimeBattleHandCardHover : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        private RectTransform rectTransform;
        private Vector2 restingPosition;
        private Quaternion restingRotation;
        private int restingSiblingIndex;
        private bool hovered;

        public int LayoutIndex { get; private set; }

        public void Configure(int layoutIndex)
        {
            LayoutIndex = Mathf.Max(0, layoutIndex);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (hovered)
            {
                return;
            }

            hovered = true;
            rectTransform ??= transform as RectTransform;
            restingPosition = rectTransform.anchoredPosition;
            restingRotation = rectTransform.localRotation;
            restingSiblingIndex = transform.GetSiblingIndex();
            transform.SetAsLastSibling();
            rectTransform.anchoredPosition = restingPosition + Vector2.up * 92f;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one * 1.18f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ResetPresentation();
        }

        public void ResetPresentation()
        {
            if (!hovered)
            {
                return;
            }

            hovered = false;
            rectTransform ??= transform as RectTransform;
            rectTransform.anchoredPosition = restingPosition;
            rectTransform.localRotation = restingRotation;
            rectTransform.localScale = Vector3.one;
            if (transform.parent != null)
            {
                transform.SetSiblingIndex(Mathf.Clamp(
                    restingSiblingIndex,
                    0,
                    Mathf.Max(0, transform.parent.childCount - 1)));
            }
        }
    }
}
