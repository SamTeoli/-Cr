using UnityEngine;

namespace HaveABreak.Cards
{
    public static class RuntimePrototypeBootstrap
    {
        public const string FinalUiPreviewPreferenceKey =
            "HaveABreak.FinalUiPreviewOnce";

        private const string ConfigResourcePath =
            "GameData/RuntimePrototypeConfig";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateRuntimePrototype()
        {
            if (TryCreateFinalUiPreview())
            {
                return;
            }

            if (Object.FindFirstObjectByType<RuntimePrototypeScreen>() != null)
            {
                return;
            }

            RuntimePrototypeConfig config = LoadReadyConfig();
            if (config == null)
            {
                return;
            }

            CreatePrototypeHost(
                "Have a Break Runtime Prototype",
                config);
        }

        private static bool TryCreateFinalUiPreview()
        {
            if (PlayerPrefs.GetInt(FinalUiPreviewPreferenceKey, 0) != 1)
            {
                return false;
            }

            PlayerPrefs.DeleteKey(FinalUiPreviewPreferenceKey);
            PlayerPrefs.Save();

            RuntimePrototypeConfig config = LoadReadyConfig();
            if (config == null)
            {
                return true;
            }

            CreatePrototypeHost(
                "Have a Break Final UI Preview",
                config);
            return true;
        }

        private static RuntimePrototypeConfig LoadReadyConfig()
        {
            RuntimePrototypeConfig config =
                Resources.Load<RuntimePrototypeConfig>(ConfigResourcePath);
            if (config == null || !config.IsReady)
            {
                Debug.LogError(
                    "[Have a Break] RuntimePrototypeConfig 또는 게임 데이터베이스를 " +
                    "불러올 수 없습니다.");
                return null;
            }

            return config;
        }

        private static void CreatePrototypeHost(
            string hostName,
            RuntimePrototypeConfig config)
        {
            GameObject host = new(hostName);
            Object.DontDestroyOnLoad(host);
            host.AddComponent<RuntimePrototypeScreen>().Initialize(config);
        }
    }
}
