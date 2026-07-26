using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HaveABreak.Cards
{
    public sealed partial class RuntimePrototypeScreen : MonoBehaviour
    {
        private void BeginSelectedBattle()
        {
            BattleEncounterGrade grade = campaign.ActiveNode.NodeType switch
            {
                RunNodeType.EliteBattle => BattleEncounterGrade.Elite,
                RunNodeType.MidBoss => BattleEncounterGrade.MidBoss,
                RunNodeType.FinalBoss => BattleEncounterGrade.FinalBoss,
                _ => BattleEncounterGrade.Normal
            };
            int selectionSeed = campaign.Seed +
                                campaign.CompletedNodeCount * 1009;
            if (!RunEncounterPoolService.TryResolve(
                    config.EncounterDatabase, config.GetEncounterPool(
                        grade, campaign.CompletedNodeCount),
                    grade, selectionSeed, out var encounter, out string poolError))
            {
                message = $"조우 선택 실패: {poolError}";
                return;
            }
            string battleId =
                $"RUN-{campaign.Seed}-NODE-{campaign.CompletedNodeCount + 1:00}";
            int seed = campaign.Seed + campaign.CompletedNodeCount * 101;
            if (!RunEncounterProgressService.TryBegin(
                    progress, battleId, encounter, seed,
                    config.RunStartProgressionConfig.BattleMaximumMana,
                    Array.Empty<string>(),
                    (uint)Mathf.Abs(seed), config.BattleRewardConfig,
                    out _, out var failure, out var flowFailure,
                    out var deckFailure, out var bootstrapFailure,
                    out var sessionFailure, out var redrawFailure,
                    out var turnFailure,
                    out List<string> validationErrors))
            {
                message = $"전투 시작 실패: {failure} / {flowFailure} / " +
                          $"{deckFailure} / {bootstrapFailure} / {sessionFailure} / " +
                          $"{redrawFailure} / {turnFailure}" +
                          (validationErrors.Count == 0
                              ? string.Empty
                              : $"\n{string.Join("\n", validationErrors)}");
                return;
            }
            battleScreen.Reset();
            message = $"{campaign.ActiveNode.DisplayName} 전투 시작.";
            SaveRun(null, true);
        }

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
