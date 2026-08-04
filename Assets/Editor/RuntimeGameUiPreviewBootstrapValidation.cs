using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class RuntimeGameUiPreviewBootstrapValidation
    {
        private const string MenuPath =
            "Assets/Editor/RuntimeGameUiPreviewMenu.cs";
        private const string BootstrapPath =
            "Assets/Scripts/Prototype/RuntimePrototypeBootstrap.cs";

        private static readonly string[] RequiredMenuTokens =
        {
            "RuntimePrototypeBootstrap.FinalUiPreviewPreferenceKey",
            "PlayerPrefs.SetInt(",
            "EditorApplication.EnterPlaymode();"
        };

        private static readonly string[] RequiredBootstrapTokens =
        {
            "TryCreateFinalUiPreview()",
            "RuntimePrototypeConfig config = LoadReadyConfig();",
            "CreatePrototypeHost(",
            "host.AddComponent<RuntimePrototypeScreen>().Initialize(config);"
        };

        [MenuItem("Have a Break/Tests/Validate Final UI Preview Bootstrap")]
        private static void RunFromMenu()
        {
            Validate();
        }

        internal static bool Validate()
        {
            if (!File.Exists(MenuPath) || !File.Exists(BootstrapPath))
            {
                Debug.LogError(
                    "Final UI preview bootstrap validation failed: " +
                    "preview menu or runtime bootstrap source is missing.");
                return false;
            }

            string menu = File.ReadAllText(MenuPath);
            string bootstrap = File.ReadAllText(BootstrapPath);
            bool menuConnected = ContainsAll(menu, RequiredMenuTokens);
            bool controllerConnected =
                ContainsAll(bootstrap, RequiredBootstrapTokens);
            bool noDetachedRoot = !bootstrap.Contains(
                "host.AddComponent<RuntimeGameUiRoot>().Initialize();",
                StringComparison.Ordinal);

            bool valid = menuConnected && controllerConnected && noDetachedRoot;
            if (valid)
            {
                Debug.Log(
                    "Final UI preview bootstrap validation passed: preview " +
                    "launch creates the configured runtime controller instead " +
                    "of a detached UI root.");
            }
            else
            {
                Debug.LogError(
                    "Final UI preview bootstrap validation failed. " +
                    $"menuConnected={menuConnected}, " +
                    $"controllerConnected={controllerConnected}, " +
                    $"noDetachedRoot={noDetachedRoot}");
            }

            return valid;
        }

        private static bool ContainsAll(
            string source,
            string[] requiredTokens)
        {
            foreach (string token in requiredTokens)
            {
                if (!source.Contains(token, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
