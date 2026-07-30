using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HaveABreak.Cards
{
    [DisallowMultipleComponent]
    public sealed class RuntimeBattleHandLayout : LayoutGroup
    {
        [SerializeField] private float cardWidth =
            RuntimeCardView.ReferenceWidth;
        [SerializeField] private float cardHeight =
            RuntimeCardView.ReferenceHeight;
        [SerializeField] private float visibleSpacing = 134f;
        [SerializeField] private float arcHeight = 12f;
        [SerializeField] private float maxRotation = 5f;
        [SerializeField] private float hoveredCardSidePush = 118f;

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
            int hoveredIndex = -1;
            float sidePushAmount = 0f;

            for (int index = 0; index < count; index++)
            {
                RuntimeBattleHandCardHover candidate =
                    rectChildren[index]
                        .GetComponent<RuntimeBattleHandCardHover>();
                if (candidate != null &&
                    candidate.HoverAmount > sidePushAmount)
                {
                    hoveredIndex = Mathf.Clamp(
                        candidate.LayoutIndex,
                        0,
                        count - 1);
                    sidePushAmount = candidate.HoverAmount;
                }
            }

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
                float cardHoverAmount = hover == null
                    ? 0f
                    : hover.HoverAmount;
                RuntimeBattleHandDrawAnimation drawAnimation =
                    card.GetComponent<RuntimeBattleHandDrawAnimation>();
                float drawProgress = drawAnimation == null
                    ? 1f
                    : drawAnimation.Progress;
                float y = Mathf.Abs(normalized) * arcHeight -
                          cardHoverAmount * (hover == null
                              ? 0f
                              : hover.HoverLift) -
                          Mathf.Sin(drawProgress * Mathf.PI) * 82f;
                float sideOffset = hoveredIndex < 0
                    ? 0f
                    : layoutIndex < hoveredIndex
                        ? -hoveredCardSidePush * sidePushAmount
                        : layoutIndex > hoveredIndex
                            ? hoveredCardSidePush * sidePushAmount
                            : 0f;
                SetChildAlongAxis(
                    card,
                    0,
                    Mathf.Lerp(
                        rectTransform.rect.width + cardWidth,
                        firstX + spacing * layoutIndex + sideOffset,
                        drawProgress),
                    cardWidth);
                SetChildAlongAxis(card, 1, y, cardHeight);
                Quaternion handRotation = Quaternion.Slerp(
                    Quaternion.Euler(
                        0f,
                        0f,
                        -normalized * maxRotation),
                    Quaternion.identity,
                    cardHoverAmount);
                card.localRotation = Quaternion.Slerp(
                    Quaternion.Euler(0f, 0f, -14f),
                    handRotation,
                    drawProgress);
                float hoverScale = Mathf.Lerp(
                    1f,
                    hover == null ? 1f : hover.HoverScale,
                    cardHoverAmount);
                card.localScale = Vector3.one *
                                  Mathf.Lerp(
                                      0.72f,
                                      hoverScale,
                                      drawProgress);
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class RuntimeBattleHandDrawAnimation : MonoBehaviour
    {
        [SerializeField] private float duration = 0.42f;

        private RectTransform parentRect;
        private float elapsed;
        private float startDelay;
        private bool playing;

        public float Progress { get; private set; } = 1f;

        public void Begin(float delay = 0f)
        {
            parentRect = transform.parent as RectTransform;
            elapsed = 0f;
            startDelay = Mathf.Max(0f, delay);
            Progress = 0f;
            playing = true;
            LayoutRebuilder.MarkLayoutForRebuild(parentRect);
        }

        public void CompleteImmediately()
        {
            playing = false;
            Progress = 1f;
            parentRect ??= transform.parent as RectTransform;
            if (parentRect != null)
            {
                LayoutRebuilder.MarkLayoutForRebuild(parentRect);
            }
        }

        private void LateUpdate()
        {
            if (!playing)
            {
                return;
            }

            elapsed += Time.unscaledDeltaTime;
            if (elapsed < startDelay)
            {
                Progress = 0f;
                LayoutRebuilder.MarkLayoutForRebuild(parentRect);
                return;
            }
            float linear = Mathf.Clamp01(
                (elapsed - startDelay) /
                Mathf.Max(0.01f, duration));
            Progress = 1f - Mathf.Pow(1f - linear, 3f);
            LayoutRebuilder.MarkLayoutForRebuild(parentRect);
            if (linear < 1f)
            {
                return;
            }

            Progress = 1f;
            playing = false;
            LayoutRebuilder.MarkLayoutForRebuild(parentRect);
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
        private float hoverAmount;

        [SerializeField] private float hoverLift = 238f;
        [SerializeField] private float hoverScale = 1.16f;
        [SerializeField] private float transitionSpeed = 7.5f;

        public int LayoutIndex { get; private set; }
        public float HoverAmount { get; private set; }
        public float HoverLift => hoverLift;
        public float HoverScale => hoverScale;

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
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
        }

        private void LateUpdate()
        {
            rectTransform ??= transform as RectTransform;
            if (rectTransform == null)
            {
                return;
            }

            if (!hovered && hoverAmount <= 0.001f)
            {
                restingPosition = rectTransform.anchoredPosition;
                restingRotation = rectTransform.localRotation;
            }

            hoverAmount = Mathf.MoveTowards(
                hoverAmount,
                hovered ? 1f : 0f,
                transitionSpeed * Time.unscaledDeltaTime);
            float eased = hoverAmount * hoverAmount *
                          (3f - 2f * hoverAmount);
            HoverAmount = eased;
            if (transform.parent != null)
            {
                LayoutRebuilder.MarkLayoutForRebuild(
                    transform.parent as RectTransform);
            }
            if (!hovered && hoverAmount <= 0.001f &&
                transform.parent != null)
            {
                transform.SetSiblingIndex(Mathf.Clamp(
                    restingSiblingIndex,
                    0,
                    Mathf.Max(0, transform.parent.childCount - 1)));
            }
        }

        public void ResetPresentation()
        {
            hovered = false;
            rectTransform ??= transform as RectTransform;
            hoverAmount = 0f;
            HoverAmount = 0f;
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
