using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(CardLayoutAuthoring))]
public sealed class CardLayoutAuthoringEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Hierarchy에서 카드의 자식 UI를 선택하고 Rect Tool로 " +
            "직접 이동/크기 조절한 뒤 아래 버튼을 누르세요.",
            MessageType.Info);

        CardLayoutAuthoring authoring =
            (CardLayoutAuthoring)target;
        if (GUILayout.Button("Save Layout To Settings"))
        {
            if (authoring.CaptureLayout())
            {
                Debug.Log("Card layout saved to CardLayoutSettings.");
            }
            else
            {
                Debug.LogError("Card layout could not be saved.");
            }
        }
    }
}

public static class CardLayoutPreviewMenu
{
    [MenuItem("Have a Break/UI/Open Scene Card Layout Editor")]
    public static void Open()
    {
        CardLayoutSettings settings =
            Resources.Load<CardLayoutSettings>("UI/CardLayoutSettings");
        if (settings == null)
        {
            Debug.LogError("CardLayoutSettings is missing.");
            return;
        }

        GameObject previous =
            GameObject.Find("CardLayoutEditor");
        if (previous != null)
        {
            Object.DestroyImmediate(previous);
        }

        GameObject root = new("CardLayoutEditor");
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(900f, 700f);
        root.AddComponent<GraphicRaycaster>();

        GameObject cardObject = new(
            "EditableCard",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement),
            typeof(RuntimeCardView));
        RectTransform cardRect =
            cardObject.GetComponent<RectTransform>();
        cardRect.SetParent(root.transform, false);
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(452f, 688f);
        cardRect.anchoredPosition = Vector2.zero;

        RuntimeCardView view =
            cardObject.GetComponent<RuntimeCardView>();
        view.Bind(
            new RuntimeCardPresentation(
                "layout-preview",
                "카드 이름",
                "PREVIEW",
                CardType.Monster,
                CardRarity.Rare,
                3,
                2,
                6,
                "효과 텍스트를 이 영역에서 직접 조정합니다.",
                false,
                0,
                true,
                string.Empty,
                "카드 레이아웃 미리보기"),
            null);

        CardLayoutAuthoring authoring =
            root.AddComponent<CardLayoutAuthoring>();
        authoring.Initialize(settings, view);
        Selection.activeGameObject = cardObject;
        SceneView.lastActiveSceneView?.FrameSelected();
        Debug.Log(
            "Scene card layout editor created. " +
            "Move child RectTransforms, then select CardLayoutEditor " +
            "and click Save Layout To Settings.");
    }
}
