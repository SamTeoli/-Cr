using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class RunLifecycleSourceBoundaryValidation
    {
        private static readonly string[] ScreenFiles =
        {
            "Assets/Scripts/Prototype/RuntimePrototypeScreen.cs",
            "Assets/Scripts/Prototype/RuntimePrototypeScreen.Part04.cs",
            "Assets/Scripts/Prototype/RuntimePrototypeScreen.Part05.cs",
            "Assets/Editor/IntegratedRunPrototypeWindow.cs",
            "Assets/Editor/IntegratedRunPrototypeWindow.Part04.cs",
            "Assets/Editor/IntegratedRunPrototypeWindow.Part05.cs"
        };

        private static readonly string[] ForbiddenTokens =
        {
            "RunSaveSlotService.TryInspectDefault(",
            "IntegratedRunSaveService.TryLoad(",
            "IntegratedRunSaveService.TrySave(",
            "PlayerPermanentRewardSaveService.TryLoadDefault(",
            "RunActionConfirmationPolicy.ShouldConfirmNewRun(",
            "RunActionConfirmationPolicy.ShouldConfirmContinue(",
            "new RunEncounterProgressState(",
            "new RunCampaignState("
        };

        private static readonly string[] RequiredTokens =
        {
            "runLifecycle.CreateNewRunRequest(",
            "runLifecycle.CreateContinueRequest(",
            "runLifecycle.BeginPreparation(",
            "runLifecycle.TryConfirmPreparation(",
            "runLifecycle.TryContinue(",
            "runLifecycle.Save(",
            "runLifecycle.LoadPermanentRewards("
        };

        [MenuItem("Have a Break/Validate Run Lifecycle Source Boundary")]
        private static void ValidateFromMenu()
        {
            Debug.Log(Validate()
                ? "Run lifecycle source boundary passed."
                : "Run lifecycle source boundary failed.");
        }

        internal static bool Validate()
        {
            string combined = string.Empty;
            foreach (string path in ScreenFiles)
            {
                if (!File.Exists(path))
                {
                    Debug.LogError(
                        $"Run lifecycle source boundary missing file: {path}");
                    return false;
                }

                string source = File.ReadAllText(path);
                combined += "\n" + source;
                foreach (string forbidden in ForbiddenTokens)
                {
                    if (!source.Contains(forbidden, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    Debug.LogError(
                        $"Run lifecycle direct dependency remains: " +
                        $"{path} / {forbidden}");
                    return false;
                }
            }

            foreach (string required in RequiredTokens)
            {
                if (combined.Contains(required, StringComparison.Ordinal))
                {
                    continue;
                }

                Debug.LogError(
                    $"Run lifecycle ViewModel connection missing: {required}");
                return false;
            }

            return true;
        }
    }
}
