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
            RunLifecycleRequest request = runLifecycle.CreateNewRunRequest(
                campaign != null || progress != null,
                config,
                permanentRewards);
            if (!request.CanProceed)
            {
                message = request.Message;
                return;
            }

            if (request.ConfirmationRequired)
            {
                pendingRunRequest = request;
                return;
            }

            BeginRunPreparation();
        }

        private void RequestContinueRun()
        {
            RunLifecycleRequest request = runLifecycle.CreateContinueRequest(
                campaign,
                progress);
            if (!request.CanProceed)
            {
                message = request.Message;
                return;
            }

            if (request.ConfirmationRequired)
            {
                pendingRunRequest = request;
                return;
            }

            ContinueRun();
        }

        private void DrawRunActionConfirmation()
        {
            RunLifecycleRequest request = pendingRunRequest;
            if (request == null)
            {
                return;
            }

            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(request.Title, titleStyle);
            GUILayout.Space(12f);
            GUILayout.Label(request.Body, wrappedStyle);
            GUILayout.Space(18f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("취소", GUILayout.Height(44f)))
            {
                pendingRunRequest = null;
            }
            if (GUILayout.Button(
                    request.ConfirmLabel,
                    GUILayout.Height(44f)))
            {
                RunLifecycleRequestKind kind = request.Kind;
                pendingRunRequest = null;
                if (kind == RunLifecycleRequestKind.StartNewRun)
                {
                    BeginRunPreparation();
                }
                else if (kind == RunLifecycleRequestKind.ContinueRun)
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
            RunPreparationCommandResult result =
                runLifecycle.BeginPreparation(config?.CardDatabase);
            if (!result.Succeeded)
            {
                message = result.Message;
                return;
            }

            runPreparationCards = result.OwnedCards;
            deckSelection.OpenWithAllOwnedCards(runPreparationCards);
            scroll = Vector2.zero;
            message = result.Message;
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
            permanentRewards = runLifecycle.LoadPermanentRewards(
                permanentRewards);
            RunCreationCommandResult result =
                runLifecycle.TryConfirmPreparation(
                    config,
                    deckSelection,
                    runPreparationCards,
                    permanentRewards,
                    Environment.TickCount & int.MaxValue);
            message = result.Message;
            if (!result.Succeeded)
            {
                return;
            }

            campaign = result.Campaign;
            progress = result.Progress;
            selectedUpgradeCardId = result.SelectedOwnedCardId;
            battleScreen.Reset();
            deckSelection.Close();
            runPreparationCards = null;
            scroll = Vector2.zero;
        }

        private void ContinueRun()
        {
            runPreparationCards = null;
            deckSelection.Close();
            permanentRewards = runLifecycle.LoadPermanentRewards(
                permanentRewards);
            RunContinueCommandResult result = runLifecycle.TryContinue(
                config?.CardDatabase,
                config?.EnchantDatabase,
                config?.EncounterDatabase,
                permanentRewards);
            message = result.Message;
            if (!result.Succeeded)
            {
                campaign = null;
                progress = null;
                battleScreen.Reset();
                return;
            }

            campaign = result.Campaign;
            progress = result.Progress;
            selectedUpgradeCardId = result.SelectedOwnedCardId;
            battleScreen.Reset();
            scroll = Vector2.zero;
        }
    }
}
