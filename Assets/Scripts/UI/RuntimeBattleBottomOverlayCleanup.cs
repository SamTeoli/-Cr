using UnityEngine;

namespace HaveABreak.Cards
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(900)]
    public sealed class RuntimeBattleBottomOverlayCleanup : MonoBehaviour
    {
        public const float PromptWithoutCommandsY = 142f;
        public const float PromptWithCommandsY = 216f;

        private RuntimeGameUiRoot uiRoot;

        public void Initialize(RuntimeGameUiRoot root)
        {
            uiRoot = root ?? GetComponent<RuntimeGameUiRoot>();
            ApplyNow();
        }

        public void ApplyNow()
        {
            uiRoot ??= GetComponent<RuntimeGameUiRoot>();
            RectTransform commandList = uiRoot?.BattleCommandList;
            Transform commandScroll = commandList?.parent?.parent;
            if (commandList == null || commandScroll == null)
            {
                return;
            }

            bool hasVisibleCommand = false;
            for (int index = 0; index < commandList.childCount; index++)
            {
                GameObject child = commandList.GetChild(index).gameObject;
                if (child.activeSelf)
                {
                    hasVisibleCommand = true;
                    break;
                }
            }

            if (commandScroll.gameObject.activeSelf != hasVisibleCommand)
            {
                commandScroll.gameObject.SetActive(hasVisibleCommand);
            }

            RectTransform prompt = uiRoot.BattleMessageText?.rectTransform;
            if (prompt == null)
            {
                return;
            }

            prompt.anchoredPosition = new Vector2(
                0f,
                hasVisibleCommand
                    ? PromptWithCommandsY
                    : PromptWithoutCommandsY);
            prompt.sizeDelta = new Vector2(
                920f,
                hasVisibleCommand ? 46f : 42f);
        }

        private void OnEnable()
        {
            Initialize(GetComponent<RuntimeGameUiRoot>());
        }

        private void LateUpdate()
        {
            ApplyNow();
        }
    }

    public static class RuntimeBattleBottomOverlayCleanupBootstrap
    {
        private static float nextScanTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            Canvas.willRenderCanvases -= ApplyToLoadedRoots;
            Canvas.willRenderCanvases += ApplyToLoadedRoots;
            nextScanTime = 0f;
        }

        public static void ApplyToLoadedRoots()
        {
            if (Application.isPlaying && Time.unscaledTime < nextScanTime)
            {
                return;
            }
            nextScanTime = Time.unscaledTime + 0.2f;

            RuntimeGameUiRoot[] roots =
                Object.FindObjectsByType<RuntimeGameUiRoot>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            foreach (RuntimeGameUiRoot root in roots)
            {
                if (root == null)
                {
                    continue;
                }

                RuntimeBattleBottomOverlayCleanup cleanup =
                    root.GetComponent<RuntimeBattleBottomOverlayCleanup>();
                if (cleanup == null)
                {
                    cleanup = root.gameObject.AddComponent<
                        RuntimeBattleBottomOverlayCleanup>();
                }
                cleanup.Initialize(root);
            }
        }
    }
}
