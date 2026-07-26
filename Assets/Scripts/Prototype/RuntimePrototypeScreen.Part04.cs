using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HaveABreak.Cards
{
    public sealed partial class RuntimePrototypeScreen : MonoBehaviour
    {
        private void DrawEnchantRewardOptions(
            IEnumerable<RunBattleEnchantRewardOption> options)
        {
            RunBattleEnchantRewardOption[] snapshot = options?.ToArray() ??
                Array.Empty<RunBattleEnchantRewardOption>();
            if (snapshot.Length == 0)
            {
                return;
            }

            GUILayout.Label("인첸트 보상", headingStyle);
            foreach (RunBattleEnchantRewardOption option in snapshot)
            {
                GUILayout.BeginHorizontal(GUI.skin.box);
                string targetText = string.IsNullOrWhiteSpace(option.TargetLabel)
                    ? string.Empty
                    : $"\n장착 대상: {option.TargetLabel}";
                string blockText = string.IsNullOrWhiteSpace(option.BlockReason)
                    ? string.Empty
                    : $"\n{option.BlockReason}";
                GUILayout.Label(
                    option.DisplayText + targetText + blockText,
                    wrappedStyle);
                bool previous = GUI.enabled;
                GUI.enabled = option.CanClaim;
                if (GUILayout.Button(
                        option.IsSelected ? "선택 완료" : "선택",
                        GUILayout.Width(90f)))
                {
                    if (battleReward.TryClaimEnchant(
                            campaign,
                            progress,
                            config.EnchantDatabase,
                            option.DefinitionId,
                            out _,
                            out string result,
                            out EnchantAttachmentFailure attachmentFailure,
                            out BattleVictoryEnchantRewardFailure failure))
                    {
                        message = result;
                        SaveRun(null);
                    }
                    else
                    {
                        message =
                            $"보상 선택 실패: {failure} / {attachmentFailure}";
                    }
                }
                GUI.enabled = previous;
                GUILayout.EndHorizontal();
            }
        }

        private void DrawConsumableRewardOptions(
            IEnumerable<RunBattleConsumableRewardOption> options)
        {
            RunBattleConsumableRewardOption[] snapshot = options?.ToArray() ??
                Array.Empty<RunBattleConsumableRewardOption>();
            if (snapshot.Length == 0)
            {
                return;
            }

            GUILayout.Label("소모아이템 보상", headingStyle);
            foreach (RunBattleConsumableRewardOption option in snapshot)
            {
                GUILayout.BeginHorizontal(GUI.skin.box);
                string blockText = string.IsNullOrWhiteSpace(option.BlockReason)
                    ? string.Empty
                    : $"\n{option.BlockReason}";
                GUILayout.Label(
                    option.DisplayText + blockText,
                    wrappedStyle);
                bool previous = GUI.enabled;
                GUI.enabled = option.CanClaim;
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
                GUI.enabled = previous;
                GUILayout.EndHorizontal();
            }
        }

        private void RequestStartNewRun()
        {
            if (config == null || !config.IsReady)
            {
                message = "게임 데이터베이스를 불러올 수 없습니다.";
                return;
            }

            bool hasCurrentRun = campaign != null || progress != null;
            bool inspected = RunSaveSlotService.TryInspectDefault(
                config.CardDatabase,
                config.EnchantDatabase,
                config.EncounterDatabase,
                permanentRewards,
                out RunSaveSlotInfo slot,
                out _);
            RunSaveSlotState slotState = slot?.State ?? RunSaveSlotState.Empty;
            if (RunActionConfirmationPolicy.ShouldConfirmNewRun(
                    hasCurrentRun, inspected, slotState))
            {
                pendingRunAction = PendingRunAction.StartNewRun;
                return;
            }

            BeginRunPreparation();
        }

        private void RequestContinueRun()
        {
            bool hasCurrentRun = campaign != null && progress != null;
            RunCampaignPhase phase = campaign?.Phase ??
                                     RunCampaignPhase.NodeSelection;
            if (RunActionConfirmationPolicy.ShouldConfirmContinue(
                    hasCurrentRun, phase))
            {
                pendingRunAction = PendingRunAction.ContinueRun;
                return;
            }

            ContinueRun();
        }

        private void DrawRunActionConfirmation()
        {
            bool startsNewRun = pendingRunAction == PendingRunAction.StartNewRun;
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(
                startsNewRun
                    ? "새 런을 시작할까요?"
                    : "전투를 처음부터 다시 시작할까요?",
                titleStyle);
            GUILayout.Space(12f);
            GUILayout.Label(
                startsNewRun
                    ? "현재 진행과 저장된 런이 새 런으로 교체됩니다. " +
                      "이 작업은 되돌릴 수 없습니다."
                    : "이어하기를 선택하면 현재 전투 진행을 버리고 " +
                      "전투 시작 체크포인트에서 다시 시작합니다.",
                wrappedStyle);
            GUILayout.Space(18f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("취소", GUILayout.Height(44f)))
            {
                pendingRunAction = PendingRunAction.None;
            }
            if (GUILayout.Button(
                    startsNewRun ? "새 런 시작" : "전투 다시 시작",
                    GUILayout.Height(44f)))
            {
                PendingRunAction confirmedAction = pendingRunAction;
                pendingRunAction = PendingRunAction.None;
                if (confirmedAction == PendingRunAction.StartNewRun)
                {
                    BeginRunPreparation();
                }
                else
                {
                    ContinueRun();
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
        }

        private void BeginRunPreparation()
        {
            runPreparationCards = new RunOwnedCardState();
            int index = 0;
            foreach (CardData card in config.CardDatabase.Cards.Where(card => card != null))
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
                config.RunStartProgressionConfig.CreateInitialRunState();
            LoadPermanentRewards();
            progress = new RunEncounterProgressState(
                run, runPreparationCards, deck, permanentRewards,
                Array.Empty<string>(), 0);
            campaign = new RunCampaignState(Environment.TickCount & int.MaxValue);
            selectedUpgradeCardId = deck.Cards.FirstOrDefault()?.OwnedCardId;
            battleActions.Reset();
            deckSelection.Close();
            runPreparationCards = null;
            scroll = Vector2.zero;
            message = "새 런을 시작했습니다.";
            SaveRun(null);
        }

        private void ContinueRun()
        {
            runPreparationCards = null;
            deckSelection.Close();
            LoadPermanentRewards();
            if (!IntegratedRunSaveService.TryLoad(
                    config.CardDatabase, config.EnchantDatabase,
                    config.EncounterDatabase, permanentRewards,
                    out campaign, out progress, out _, out RunResumeSource source,
                    out RunCampaignFailure failure))
            {
                campaign = null;
                progress = null;
                message = $"이어하기 실패: {failure}";
                return;
            }
            selectedUpgradeCardId =
                progress.OwnedCards.Cards.FirstOrDefault()?.OwnedCardId;
            battleActions.Reset();
            deckSelection.Close();
            scroll = Vector2.zero;
            battleActions.Refresh(progress?.ActiveEncounter);
            message = $"이어하기 완료: {source}";
        }

    }
}
