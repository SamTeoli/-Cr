using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class RuntimeGameUiPreviewMenu
    {
        [MenuItem("Have a Break/Play Final UI Preview")]
        private static void Play()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning(
                    "Play 모드를 종료한 뒤 최종 UI 미리보기를 실행하세요.");
                return;
            }

            PlayerPrefs.SetInt(
                RuntimePrototypeBootstrap.FinalUiPreviewPreferenceKey,
                1);
            PlayerPrefs.Save();
            EditorApplication.EnterPlaymode();
        }
    }
}
