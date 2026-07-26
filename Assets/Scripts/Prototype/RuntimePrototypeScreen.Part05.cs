using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HaveABreak.Cards
{
    public sealed partial class RuntimePrototypeScreen : MonoBehaviour
    {
        private RunCardInstance SelectedUpgradeCard()
        {
            RunCardInstance selected = progress?.OwnedCards?.Cards.FirstOrDefault(card =>
                string.Equals(card.OwnedCardId, selectedUpgradeCardId,
                    StringComparison.OrdinalIgnoreCase));
            return selected ?? progress?.OwnedCards?.Cards.FirstOrDefault();
        }

        private void CycleUpgradeCard()
        {
            if (progress?.OwnedCards == null || progress.OwnedCards.Count == 0)
            {
                return;
            }
            List<RunCardInstance> cards = progress.OwnedCards.Cards.ToList();
            int index = cards.FindIndex(card =>
                card.OwnedCardId == selectedUpgradeCardId);
            selectedUpgradeCardId =
                cards[(index + 1 + cards.Count) % cards.Count].OwnedCardId;
        }

        private void SaveRun(string successMessage, bool forceActive = false)
        {
            if (campaign == null || progress == null) return;
            if (progress.HasActiveEncounter && !forceActive)
            {
                if (!string.IsNullOrWhiteSpace(successMessage))
                {
                    message = "전투 시작 체크포인트가 이미 저장되어 있습니다. " +
                              "이어하기 시 현재 전투를 처음부터 다시 시작합니다.";
                }
                return;
            }

            if (IntegratedRunSaveService.TrySave(
                    campaign, progress, out var destination, out var failure))
            {
                if (!string.IsNullOrWhiteSpace(successMessage))
                {
                    message = $"{successMessage} · {destination}";
                }
            }
            else
            {
                string prefix = string.IsNullOrWhiteSpace(message)
                    ? string.Empty
                    : message + "\n";
                string label = string.IsNullOrWhiteSpace(successMessage)
                    ? "자동 저장 실패"
                    : "저장 실패";
                message = $"{prefix}{label}: {failure}";
            }
        }

        private void LoadPermanentRewards()
        {
            if (PlayerPermanentRewardSaveService.TryLoadDefault(
                    out var loaded, out _, out _))
            {
                permanentRewards = loaded;
            }
            else
            {
                permanentRewards ??= new PlayerPermanentRewardState();
            }
        }

        private void DrawMessage()
        {
            if (!string.IsNullOrWhiteSpace(message)) Notice(message);
        }

        private void Notice(string text)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(text, wrappedStyle);
            GUILayout.EndVertical();
        }

        private static IEnumerable<T> Rotate<T>(
            IReadOnlyList<T> values,
            int seed)
        {
            if (values == null || values.Count == 0) yield break;
            int start = Mathf.Abs(seed % values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                yield return values[(start + i) % values.Count];
            }
        }
    }
}
