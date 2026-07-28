using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HaveABreak.Editor
{
    internal static class RuntimeCardDragHandlerValidation
    {
        [MenuItem("Have a Break/Validate Runtime Card Drag Handler")]
        private static void ValidateFromMenu()
        {
            Debug.Log(Validate()
                ? "Runtime card drag handler validation passed."
                : "Runtime card drag handler validation failed.");
        }

        internal static bool Validate()
        {
            GameObject root = null;
            GameObject eventSystemObject = null;
            try
            {
                eventSystemObject = new GameObject(
                    "RuntimeCardDragValidationEventSystem",
                    typeof(EventSystem));

                root = new GameObject("RuntimeCardDragValidationRoot");
                GameObject canvasObject = new(
                    "Canvas",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(GraphicRaycaster));
                canvasObject.transform.SetParent(root.transform, false);
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                RectTransform canvasRect =
                    canvasObject.GetComponent<RectTransform>();
                canvasRect.sizeDelta = new Vector2(1920f, 1080f);

                GameObject handObject = new(
                    "Hand",
                    typeof(RectTransform));
                handObject.transform.SetParent(canvasObject.transform, false);
                RectTransform handRect =
                    handObject.GetComponent<RectTransform>();
                handRect.sizeDelta = new Vector2(1200f, 420f);

                GameObject siblingObject = new(
                    "Sibling",
                    typeof(RectTransform));
                siblingObject.transform.SetParent(handObject.transform, false);

                GameObject cardObject = new(
                    "Card",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button),
                    typeof(LayoutElement),
                    typeof(RuntimeCardView),
                    typeof(RuntimeCardDragHandler));
                cardObject.transform.SetParent(handObject.transform, false);
                RectTransform cardRect =
                    cardObject.GetComponent<RectTransform>();
                cardRect.sizeDelta = new Vector2(226f, 344f);
                cardRect.anchoredPosition = new Vector2(120f, 60f);

                int clickedCount = 0;
                int droppedCount = 0;
                RuntimeCardView view =
                    cardObject.GetComponent<RuntimeCardView>();
                view.Bind(
                    new RuntimeCardPresentation(
                        "play:test-card",
                        "검증 카드",
                        "TEST-CARD",
                        CardType.Monster,
                        CardRarity.Common,
                        1,
                        3,
                        4,
                        "검증 효과",
                        false,
                        0,
                        true,
                        null,
                        "검증 카드"),
                    _ => clickedCount++);

                RuntimeCardDragHandler drag =
                    cardObject.GetComponent<RuntimeCardDragHandler>();
                drag.Configure(
                    view,
                    canvas,
                    (_, _) => droppedCount++);

                Transform originalParent = cardObject.transform.parent;
                int originalSiblingIndex =
                    cardObject.transform.GetSiblingIndex();
                Vector3 originalScale = cardObject.transform.localScale;

                PointerEventData pointer = new(EventSystem.current)
                {
                    position = new Vector2(500f, 420f),
                    pressPosition = new Vector2(500f, 420f)
                };
                drag.OnPointerDown(pointer);
                drag.OnBeginDrag(pointer);
                Vector2 beginPosition = cardRect.anchoredPosition;

                pointer.position = new Vector2(640f, 510f);
                drag.OnDrag(pointer);
                Vector2 movedPosition = cardRect.anchoredPosition;

                bool beganCorrectly =
                    cardObject.transform.parent == canvasObject.transform &&
                    cardObject.transform.localScale.x > originalScale.x;
                Vector2 movement = movedPosition - beginPosition;
                bool followedPointer =
                    Mathf.Abs(movement.x - 140f) < 0.1f &&
                    Mathf.Abs(movement.y - 90f) < 0.1f;

                drag.OnEndDrag(pointer);
                CanvasGroup canvasGroup =
                    cardObject.GetComponent<CanvasGroup>();
                bool restored =
                    cardObject.transform.parent == originalParent &&
                    cardObject.transform.GetSiblingIndex() ==
                    originalSiblingIndex &&
                    Vector3.Distance(
                        cardObject.transform.localScale,
                        originalScale) < 0.001f &&
                    canvasGroup != null &&
                    canvasGroup.blocksRaycasts &&
                    Mathf.Approximately(canvasGroup.alpha, 1f) &&
                    droppedCount == 0;

                view.ClickButton.onClick.Invoke();
                bool dragClickSuppressed = clickedCount == 0;
                view.ClickButton.onClick.Invoke();
                bool nextClickAccepted = clickedCount == 1;

                bool valid = beganCorrectly && followedPointer && restored &&
                             dragClickSuppressed && nextClickAccepted;
                if (!valid)
                {
                    Debug.LogError(
                        "Runtime card drag handler validation failed: " +
                        $"began={beganCorrectly}, followed={followedPointer}, " +
                        $"restored={restored}, " +
                        $"suppressed={dragClickSuppressed}, " +
                        $"nextClick={nextClickAccepted}");
                }
                else
                {
                    Debug.Log(
                        "Runtime card drag handler validation passed: " +
                        "pointer offset, layout restore, and click suppression.");
                }

                return valid;
            }
            finally
            {
                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }
                if (eventSystemObject != null)
                {
                    Object.DestroyImmediate(eventSystemObject);
                }
            }
        }
    }
}
