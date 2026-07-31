using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HaveABreak.Cards
{
    [DisallowMultipleComponent]
    public sealed class RuntimeCardDragHandler : MonoBehaviour,
        IPointerDownHandler,
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
        private Quaternion originalRotation;
        private Action<string, string> dropped;
        private bool dragging;
        private bool suppressNextClick;

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

        public void OnPointerDown(PointerEventData eventData)
        {
            suppressNextClick = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (cardView?.Presentation?.Interactable != true ||
                dragRect == null || rootCanvas == null)
            {
                return;
            }

            dragging = true;
            suppressNextClick = true;
            GetComponent<RuntimeBattleHandCardHover>()?.ResetPresentation();
            GetComponent<RuntimeBattleHandDrawAnimation>()?.CompleteImmediately();
            RuntimeCardDropZone.SetActivePresentation(cardView.Presentation);
            originalParent = transform.parent;
            originalSiblingIndex = transform.GetSiblingIndex();
            originalScale = transform.localScale;
            originalRotation = transform.localRotation;
            transform.SetParent(rootCanvas.transform, true);
            transform.SetAsLastSibling();
            transform.localRotation = Quaternion.identity;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 1f;

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
            RuntimeCardDropZone zone = FindDropZone(eventData);
            RuntimeCardPresentation presentation = cardView?.Presentation;
            string cardCommand = presentation?.CommandId;
            string targetCommand = zone?.TargetCommandId;

            RuntimeCardDropZone.SetActivePresentation(null);
            ReturnToHand();

            if (zone != null &&
                zone.Accepts(presentation) &&
                !string.IsNullOrWhiteSpace(cardCommand) &&
                !string.IsNullOrWhiteSpace(targetCommand))
            {
                dropped?.Invoke(cardCommand, targetCommand);
            }
        }

        public bool ConsumeClickSuppression()
        {
            if (!suppressNextClick)
            {
                return false;
            }

            suppressNextClick = false;
            return true;
        }

        private void OnDisable()
        {
            if (!dragging)
            {
                return;
            }

            dragging = false;
            RuntimeCardDropZone.SetActivePresentation(null);
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
                if (zone != null && zone.Accepts(cardView?.Presentation))
                {
                    return zone;
                }
            }

            return null;
        }

        private void ReturnToHand()
        {
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.alpha = 1f;
            }

            if (originalParent == null)
            {
                return;
            }

            Transform parent = originalParent;
            int siblingIndex = originalSiblingIndex;
            Vector3 scale = originalScale;
            originalParent = null;

            transform.SetParent(parent, false);
            transform.SetSiblingIndex(Mathf.Clamp(
                siblingIndex,
                0,
                Mathf.Max(0, parent.childCount - 1)));
            transform.localScale = scale;
            transform.localRotation = originalRotation;
        }
    }

    [DisallowMultipleComponent]
    public sealed class RuntimeCardDropZone : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        private static readonly HashSet<RuntimeCardDropZone> RegisteredZones =
            new();
        private static RuntimeCardPresentation activePresentation;

        private Graphic highlightGraphic;
        private Color idleColor;
        private Color availableColor;
        private Color pointerColor;
        private Func<RuntimeCardPresentation, bool> acceptsPresentation;
        private bool pointerInside;

        public bool AcceptsCards { get; private set; }
        public string TargetCommandId { get; private set; }
        public bool IsAvailableHighlighted { get; private set; }

        public static void SetActivePresentation(
            RuntimeCardPresentation presentation)
        {
            activePresentation = presentation;
            RegisteredZones.RemoveWhere(zone => zone == null);
            foreach (RuntimeCardDropZone zone in RegisteredZones)
            {
                zone.RefreshHighlight();
            }
        }

        private void OnEnable()
        {
            RegisteredZones.Add(this);
            RefreshHighlight();
        }

        private void OnDisable()
        {
            Unregister();
        }

        private void OnDestroy()
        {
            Unregister();
        }

        public void Configure(
            string targetCommandId,
            Graphic graphic,
            Color idle,
            Color hover,
            Func<RuntimeCardPresentation, bool> cardPredicate = null)
        {
            RegisteredZones.Add(this);
            TargetCommandId = targetCommandId ?? string.Empty;
            AcceptsCards = !string.IsNullOrWhiteSpace(TargetCommandId);
            acceptsPresentation = cardPredicate;
            highlightGraphic = graphic;
            idleColor = idle;
            availableColor = hover;
            pointerColor = Color.Lerp(hover, Color.white, 0.28f);
            if (highlightGraphic != null)
            {
                highlightGraphic.raycastTarget = true;
            }
            RefreshHighlight();
        }

        public bool Accepts(RuntimeCardPresentation presentation)
        {
            return AcceptsCards && presentation != null &&
                   (acceptsPresentation == null ||
                    acceptsPresentation(presentation));
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerInside = true;
            RefreshHighlight();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            RefreshHighlight();
        }

        private void Unregister()
        {
            RegisteredZones.Remove(this);
            pointerInside = false;
            IsAvailableHighlighted = false;
            if (highlightGraphic != null)
            {
                highlightGraphic.color = idleColor;
            }
        }

        private void RefreshHighlight()
        {
            bool available = Accepts(activePresentation);
            IsAvailableHighlighted = available;
            if (highlightGraphic == null)
            {
                return;
            }

            highlightGraphic.color = available
                ? pointerInside ? pointerColor : availableColor
                : idleColor;
        }
    }
}
