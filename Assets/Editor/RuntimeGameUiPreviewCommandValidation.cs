using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace HaveABreak.Editor
{
    internal static class RuntimeGameUiPreviewCommandValidation
    {
        [MenuItem("Have a Break/Tests/Validate Final UI Preview New Run Command")]
        private static void RunFromMenu()
        {
            Validate();
        }

        internal static bool Validate()
        {
            RuntimePrototypeConfig config =
                Resources.Load<RuntimePrototypeConfig>(
                    "GameData/RuntimePrototypeConfig");
            EventSystem previousEventSystem = EventSystem.current;
            GameObject host = new("Final UI Preview Command Validation");

            try
            {
                RuntimePrototypeScreen prototype =
                    host.AddComponent<RuntimePrototypeScreen>();
                prototype.Initialize(config);
                RuntimeGameUiRoot root = prototype.FinalUiRoot;

                bool start = config != null && config.IsReady &&
                             root != null &&
                             root.CurrentScreen == RuntimeGameScreen.Start &&
                             root.NewRunButton != null &&
                             root.NewRunButton.interactable;

                root?.NewRunButton?.onClick.Invoke();
                bool confirmationShown =
                    root?.CurrentScreen == RuntimeGameScreen.Confirmation;
                if (confirmationShown)
                {
                    root.ConfirmActionButton?.onClick.Invoke();
                }

                bool preparation =
                    root?.CurrentScreen == RuntimeGameScreen.RunPreparation &&
                    root.RunPreparationCardList != null &&
                    root.RunPreparationCardList.childCount > 0 &&
                    root.ConfirmRunPreparationButton.interactable;

                root?.CancelRunPreparationButton?.onClick.Invoke();
                bool returned =
                    root?.CurrentScreen == RuntimeGameScreen.Start &&
                    root.RootCanvas.gameObject.activeSelf;

                bool valid = start && preparation && returned;
                if (valid)
                {
                    Debug.Log(
                        "Final UI preview new-run command validation passed: " +
                        "the start button handles optional overwrite confirmation, " +
                        "opens deck preparation through the runtime controller, " +
                        "and cancellation returns to start.");
                }
                else
                {
                    Debug.LogError(
                        "Final UI preview new-run command validation failed. " +
                        $"start={start}, confirmationShown={confirmationShown}, " +
                        $"preparation={preparation}, returned={returned}");
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
