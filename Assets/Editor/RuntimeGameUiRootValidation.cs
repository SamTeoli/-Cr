using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HaveABreak.Editor
{
    internal static class RuntimeGameUiRootValidation
    {
        [MenuItem("Have a Break/Tests/Validate Final UGUI Start Screen")]
        private static void RunFromMenu()
        {
            Validate();
        }

        internal static bool Validate()
        {
            EventSystem previousEventSystem = EventSystem.current;
            GameObject host = new("Final UI Root Validation");
            bool newRunRequested = false;
            bool continueRequested = false;

            try
            {
                RuntimeGameUiRoot root =
                    host.AddComponent<RuntimeGameUiRoot>();
                root.NewRunRequested += () => newRunRequested = true;
                root.ContinueRequested += () => continueRequested = true;
                root.Initialize();

                CanvasScaler scaler =
                    root.RootCanvas.GetComponent<CanvasScaler>();
                bool structure = root.CurrentScreen ==
                                     RuntimeGameScreen.Start &&
                                 root.RootCanvas.renderMode ==
                                     RenderMode.ScreenSpaceOverlay &&
                                 scaler.uiScaleMode ==
                                     CanvasScaler.ScaleMode.ScaleWithScreenSize &&
                                 scaler.referenceResolution ==
                                     new Vector2(1920f, 1080f) &&
                                 root.NewRunButton != null &&
                                 root.ContinueButton != null &&
                                 root.GetComponentInChildren<
                                     InputSystemUIInputModule>(true) != null &&
                                 root.GetComponentInChildren<
                                     StandaloneInputModule>(true) == null;

                root.NewRunButton.onClick.Invoke();
                root.ContinueButton.onClick.Invoke();
                bool commands = newRunRequested && continueRequested;

                root.ShowScreen(RuntimeGameScreen.Battle);
                bool routing = root.CurrentScreen ==
                               RuntimeGameScreen.Battle &&
                               !root.NewRunButton.gameObject
                                   .transform.parent.parent.gameObject.activeSelf;

                bool valid = structure && commands && routing;
                if (valid)
                {
                    Debug.Log(
                        "Final UGUI start screen validation passed: " +
                        "canvas scaling, Input System module, start layout, " +
                        "button commands, " +
                        "and screen visibility.");
                }
                else
                {
                    Debug.LogError(
                        "Final UGUI start screen validation failed. " +
                        $"structure={structure}, commands={commands}, " +
                        $"routing={routing}");
                }

                return valid;
            }
            finally
            {
                Object.DestroyImmediate(host);
                if (previousEventSystem != null)
                {
                    EventSystem.current = previousEventSystem;
                }
            }
        }
    }
}
