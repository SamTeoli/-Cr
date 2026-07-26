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

            RunLifecycleRequest request = runLifecycle.CreateNewRunRequest(
                campaign != null || progress != null,
                prototypeConfig,
                permanentRewards);
            if (!request.CanProceed)
            {
                message = request.Message;
                return;
            }

            if (!request.ConfirmationRequired ||
                EditorUtility.DisplayDialog(
                    request.Title,
                    request.Body,
                    request.ConfirmLabel,
                    "취소"))
            {
                BeginRunPreparation();
            }
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

            if (!request.ConfirmationRequired ||
                EditorUtility.DisplayDialog(
                    request.Title,
                    request.Body,
                    request.ConfirmLabel,
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

            RunPreparationCommandResult result =
                runLifecycle.BeginPreparation(cardDatabase);
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
                    prototypeConfig,
                    deckSelection,
                    runPreparationCards,
                    permanentRewards,
                    20260722);
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
        }

        private void ContinueRun()
        {
            runPreparationCards = null;
            deckSelection.Close();
            permanentRewards = runLifecycle.LoadPermanentRewards(
                permanentRewards);
            RunContinueCommandResult result = runLifecycle.TryContinue(
                cardDatabase,
                enchantDatabase,
                encounterDatabase,
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
        }
    }
}
