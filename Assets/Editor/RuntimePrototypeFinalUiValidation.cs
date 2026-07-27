using System.Reflection;
using System;
using HaveABreak.Cards;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HaveABreak.Editor
{
    internal static class RuntimePrototypeFinalUiValidation
    {
        public static void RunBatchMode()
        {
            if (!Validate())
            {
                throw new InvalidOperationException(
                    "Final UI prototype bridge validation failed.");
            }
        }

        internal static bool Validate()
        {
            RuntimePrototypeConfig config =
                Resources.Load<RuntimePrototypeConfig>(
                    "GameData/RuntimePrototypeConfig");
            EventSystem previousEventSystem = EventSystem.current;
            GameObject host = new("Final UI Prototype Validation");

            try
            {
                RuntimePrototypeScreen prototype =
                    host.AddComponent<RuntimePrototypeScreen>();
                prototype.Initialize(config);
                RuntimeGameUiRoot root = prototype.FinalUiRoot;
                bool start = config != null && config.IsReady &&
                             root != null &&
                             root.CurrentScreen == RuntimeGameScreen.Start &&
                             root.RootCanvas.gameObject.activeSelf;

                MethodInfo beginPreparation =
                    typeof(RuntimePrototypeScreen).GetMethod(
                        "BeginRunPreparation",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                beginPreparation?.Invoke(prototype, null);

                bool preparation =
                    root.CurrentScreen == RuntimeGameScreen.RunPreparation &&
                    root.RunPreparationCardList.childCount > 0 &&
                    root.ConfirmRunPreparationButton.interactable;
                int selectedBefore = ParseSelectedCount(
                    root.RunPreparationSelectedCountText.text);
                Button firstCard = preparation
                    ? root.RunPreparationCardList.GetChild(0)
                        .GetComponent<Button>()
                    : null;
                bool hadCardButton = firstCard != null;
                firstCard?.onClick.Invoke();
                int selectedAfter = ParseSelectedCount(
                    root.RunPreparationSelectedCountText.text);
                bool toggle = hadCardButton &&
                              selectedAfter == selectedBefore - 1;

                root.CancelRunPreparationButton.onClick.Invoke();
                bool cancel = root.CurrentScreen == RuntimeGameScreen.Start &&
                              root.RootCanvas.gameObject.activeSelf;
                bool valid = start && preparation && toggle && cancel;
                if (valid)
                {
                    Debug.Log(
                        "Final UI prototype bridge validation passed: " +
                        "start, preparation, card toggle, and cancellation.");
                }
                else
                {
                    Debug.LogError(
                        "Final UI prototype bridge validation failed. " +
                        $"start={start}, preparation={preparation}, " +
                        $"toggle={toggle} ({selectedBefore}->{selectedAfter}), " +
                        $"cancel={cancel}");
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

        private static int ParseSelectedCount(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return -1;
            }

            string[] parts = value.Split(' ');
            return parts.Length > 1 &&
                   int.TryParse(parts[1].TrimEnd('장'), out int count)
                ? count
                : -1;
        }
    }
}
