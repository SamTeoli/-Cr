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

        private void SaveRun(string successMessage)
        {
            if (campaign == null || progress == null) return;
            if (progress.HasActiveEncounter)
            {
                if (campaign.Phase == RunCampaignPhase.Battle &&
                    !string.IsNullOrWhiteSpace(successMessage))
                {
                    BattleStartCommandResult checkpoint = battleStart.TryStart(
                        campaign,
                        progress,
                        config);
                    message = checkpoint.Succeeded
                        ? $"{successMessage} · {checkpoint.SaveDestination}"
                        : checkpoint.Message;
                }
                else if (!string.IsNullOrWhiteSpace(successMessage))
                {
                    message = "활성 조우가 완료되기 전에는 현재 진행을 " +
                              "별도 저장하지 않습니다.";
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
