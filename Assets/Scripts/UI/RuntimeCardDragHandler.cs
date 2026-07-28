using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HaveABreak.Cards
{
    [DisallowMultipleComponent]
    public sealed class RuntimeCardDragHandler : MonoBehaviour,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        private RuntimeCardView cardView;
        private RectTransform dragRect;
        private Canvas rootCanvas;
        private CanvasGroup canvasGroup;
        private Transform originalParent;
        private int originalSiblingIndex;
        private Vector2 pointerOffset;
        private Vector3 originalScale;
        private Action<string, string> dropped;
        private bool dragging;

        public void Configure(
            RuntimeCardView view,
            Canvas canvas,
            Action<string, string> onDropped)
        {
            cardView = view;
            rootCanvas = canvas;
            dropped = onDropped;
            dragRect = transform as RectTransform;
            canvasGroup = gameObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (cardView?.Presentation?.Interactable != true ||
                dragRect == null || rootCanvas == null)
            {
                return;
            }

            dragging = true;
            originalParent = transform.parent;
            originalSiblingIndex = transform.GetSiblingIndex();
            originalScale = transform.localScale;
            transform.SetParent(rootCanvas.transform, true);
            transform.SetAsLastSibling();
            transform.localScale = originalScale * 1.12f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.94f;

            RectTransform canvasRect = rootCanvas.transform as RectTransform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPointer))
            {
                pointerOffset = dragRect.anchoredPosition - localPointer;
            }
            else
            {
                pointerOffset = Vector2.zero;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging)
            {
                return;
            }

            RectTransform canvasRect = rootCanvas.transform as RectTransform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint))
            {
                dragRect.anchoredPosition = localPoint + pointerOffset;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragging)
            {
                return;
            }

            dragging = false;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
            RuntimeCardDropZone zone = FindDropZone(eventData);
            if (zone != null && zone.AcceptsCards)
            {
                string cardCommand = cardView.Presentation.CommandId;
                dropped?.Invoke(cardCommand, zone.TargetCommandId);
                return;
            }

            ReturnToHand();
        }

        private RuntimeCardDropZone FindDropZone(PointerEventData eventData)
        {
            List<RaycastResult> results = new();
            EventSystem.current?.RaycastAll(eventData, results);
            foreach (RaycastResult result in results)
            {
                RuntimeCardDropZone zone =
                    result.gameObject.GetComponentInParent<RuntimeCardDropZone>();
                if (zone != null && zone.AcceptsCards)
                {
                    return zone;
                }
            }

            return null;
        }

        private void ReturnToHand()
        {
            if (originalParent == null)
            {
                return;
            }

            transform.SetParent(originalParent, false);
            transform.SetSiblingIndex(Mathf.Min(
                originalSiblingIndex,
                originalParent.childCount - 1));
            transform.localScale = originalScale;
        }
    }

    [DisallowMultipleComponent]
    public sealed class RuntimeCardDropZone : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        private Graphic highlightGraphic;
        private Color idleColor;
        private Color hoverColor;

        public bool AcceptsCards { get; private set; }
        public string TargetCommandId { get; private set; }

        public void Configure(
            string targetCommandId,
            Graphic graphic,
            Color idle,
            Color hover)
        {
            TargetCommandId = targetCommandId ?? string.Empty;
            AcceptsCards = true;
            highlightGraphic = graphic;
            idleColor = idle;
            hoverColor = hover;
            if (highlightGraphic != null)
            {
                highlightGraphic.color = idleColor;
                highlightGraphic.raycastTarget = true;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (AcceptsCards && highlightGraphic != null)
            {
                highlightGraphic.color = hoverColor;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (highlightGraphic != null)
            {
                highlightGraphic.color = idleColor;
            }
        }
    }
}
