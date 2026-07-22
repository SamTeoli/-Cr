using UnityEngine;

namespace HaveABreak.Cards
{
    public static class RuntimePrototypeBootstrap
    {
        private const string ConfigResourcePath =
            "GameData/RuntimePrototypeConfig";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateRuntimePrototype()
        {
            if (Object.FindFirstObjectByType<RuntimePrototypeScreen>() != null)
            {
                return;
            }

            RuntimePrototypeConfig config =
                Resources.Load<RuntimePrototypeConfig>(ConfigResourcePath);
            if (config == null || !config.IsReady)
            {
                Debug.LogError(
                    "[Have a Break] RuntimePrototypeConfig 또는 게임 데이터베이스를 " +
                    "불러올 수 없습니다.");
                return;
            }

            GameObject host = new("Have a Break Runtime Prototype");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<RuntimePrototypeScreen>().Initialize(config);
        }
    }
}
