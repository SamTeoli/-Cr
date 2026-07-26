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
            RunSaveCommandResult result = runLifecycle.Save(
                campaign,
                progress,
                config,
                successMessage);
            if (string.IsNullOrWhiteSpace(result.Message))
            {
                return;
            }

            if (!result.Succeeded &&
                string.IsNullOrWhiteSpace(successMessage) &&
                !string.IsNullOrWhiteSpace(message))
            {
                message += "\n" + result.Message;
            }
            else
            {
                message = result.Message;
            }
        }

        private void LoadPermanentRewards()
        {
            permanentRewards = runLifecycle.LoadPermanentRewards(
                permanentRewards);
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
