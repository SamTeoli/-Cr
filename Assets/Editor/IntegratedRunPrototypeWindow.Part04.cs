using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.EditorTools
{
    public sealed partial class IntegratedRunPrototypeWindow : EditorWindow
    {
        private void DrawConsumableRewardOptions(
            IEnumerable<RunBattleConsumableRewardOption> options)
        {
            RunBattleConsumableRewardOption[] snapshot = options?.ToArray() ??
                Array.Empty<RunBattleConsumableRewardOption>();
            if (snapshot.Length == 0)
            {
                return;
            }

            EditorGUILayout.LabelField(
                "소모아이템 보상",
                EditorStyles.miniBoldLabel);
            foreach (RunBattleConsumableRewardOption option in snapshot)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                string blockText = string.IsNullOrWhiteSpace(option.BlockReason)
                    ? string.Empty
                    : $"\n{option.BlockReason}";
                EditorGUILayout.LabelField(
                    option.DisplayText + blockText,
                    EditorStyles.wordWrappedLabel);
                using (new EditorGUI.DisabledScope(!option.CanClaim))
                {
                    if (GUILayout.Button(
                            option.RewardClaimed && option.IsSelected
                                ? "수령 완료"
                                : "받기",
                            GUILayout.Width(90f)))
                    {
                        if (battleReward.TryClaimConsumable(
                                campaign,
                                progress,
                                option.ItemId,
                                out _,
                                out string result,
                                out BattleVictoryConsumableRewardFailure failure))
                        {
                            message = result;
                            SaveRun(null);
                        }
                        else
                        {
                            message = $"소모아이템 보상 실패: {failure}";
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void RequestStartNewRun()
        {
            if (!DatabasesReady())
            {
                LoadDatabases();
                return;
            }

            bool hasCurrentRun = campaign != null || progress != null;
            bool inspected = RunSaveSlotService.TryInspectDefault(
                cardDatabase,
                enchantDatabase,
                encounterDatabase,
                permanentRewards,
                out RunSaveSlotInfo slot,
                out _);
            RunSaveSlotState slotState = slot?.State ?? RunSaveSlotState.Empty;
            if (!RunActionConfirmationPolicy.ShouldConfirmNewRun(
                    hasCurrentRun, inspected, slotState) ||
                EditorUtility.DisplayDialog(
                    "새 런을 시작할까요?",
                    "현재 진행과 저장된 런이 새 런으로 교체됩니다. " +
                    "이 작업은 되돌릴 수 없습니다.",
                    "새 런 시작",
                    "취소"))
            {
                BeginRunPreparation();
            }
        }

        private void RequestContinueRun()
        {
            bool hasCurrentRun = campaign != null && progress != null;
            RunCampaignPhase phase = campaign?.Phase ??
                                     RunCampaignPhase.NodeSelection;
            if (!RunActionConfirmationPolicy.ShouldConfirmContinue(
                    hasCurrentRun, phase) ||
                EditorUtility.DisplayDialog(
                    "전투를 처음부터 다시 시작할까요?",
                    "이어하기를 선택하면 현재 전투 진행을 버리고 " +
                    "전투 시작 체크포인트에서 다시 시작합니다.",
                    "전투 다시 시작",
                    "취소"))
            {
                ContinueRun();
            }
        }

        private void BeginRunPreparation()
        {
            if (!DatabasesReady())
            {
                LoadDatabases();
                return;
            }

            runPreparationCards = new RunOwnedCardState();
            int index = 0;
            foreach (CardData card in cardDatabase.Cards.Where(card => card != null))
            {
                RunCardInstance ownedCard = new(
                    card, $"OWNED-RUN-{++index:00}-{card.CatalogCardId}", 1);
                if (!runPreparationCards.TryAdd(ownedCard, out _)) continue;
            }
            deckSelection.OpenWithAllOwnedCards(runPreparationCards);

            scroll = Vector2.zero;
            message = "런에 사용할 덱을 선택한 뒤 확정하세요.";
        }

        private void CancelRunPreparation()
        {
            runPreparationCards = null;
            deckSelection.Close();
            scroll = Vector2.zero;
            message = "새 런 준비를 취소했습니다.";
        }

        private void ConfirmRunPreparation()
        {
            if (!deckSelection.TryCreateDeck(
                    runPreparationCards,
                    out RunDeckState deck, out RunDeckFailure failure))
            {
                message = $"새 런 덱 확정 실패: {failure}";
                return;
            }

            RunBattleState run =
                prototypeConfig.RunStartProgressionConfig.CreateInitialRunState();
            LoadPermanentRewards();
            progress = new RunEncounterProgressState(
                run, runPreparationCards, deck, permanentRewards,
                Array.Empty<string>(), 0);
            campaign = new RunCampaignState(20260722);
            battleScreen.Reset();
            selectedUpgradeCardId = deck.Cards.FirstOrDefault()?.OwnedCardId;
            deckSelection.Close();
            runPreparationCards = null;
            message = "새 통합 런을 시작했습니다.";
            SaveRun(null);
        }

        private void ContinueRun()
        {
            runPreparationCards = null;
            deckSelection.Close();
            LoadPermanentRewards();
            if (!IntegratedRunSaveService.TryLoad(
                    cardDatabase, enchantDatabase, encounterDatabase,
                    permanentRewards,
                    out campaign, out progress, out _, out RunResumeSource source,
                    out RunCampaignFailure failure))
            {
                campaign = null;
                progress = null;
                battleScreen.Reset();
                message = $"이어하기 실패: {failure}";
                return;
            }

            selectedUpgradeCardId =
                progress.OwnedCards.Cards.FirstOrDefault()?.OwnedCardId;
            deckSelection.Close();
            battleScreen.Reset();
            message = $"이어하기 완료: {source}";
        }
    }
}
