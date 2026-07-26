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
        private void SettleBattle()
        {
            BattleRuntimeEncounterContext context = progress.ActiveEncounter;
            if (!RunEncounterProgressService.TrySettleActive(
                    progress,
                    out RunEncounterProgressFailure progressFailure,
                    out BattleRuntimeEncounterFlowFailure flowFailure,
                    out BattleRuntimeSessionFailure sessionFailure,
                    out BattleSettlementFailure settlementFailure))
            {
                message =
                    $"정산 실패: {progressFailure} / {flowFailure} / " +
                    $"{sessionFailure} / {settlementFailure}";
                return;
            }

            if (context.Settlement.SettledOutcome == BattleOutcome.Defeat)
            {
                RunEncounterProgressService.TryCompleteActive(progress, out _);
                RunCampaignService.MarkBattleReward(
                    campaign, BattleOutcome.Defeat);
                message = "패배 정산 완료 · 런 종료";
                SaveRun(null);
                return;
            }

            if (!context.VictoryRewards.TryClaimGold(
                    out BattleRewardFailure rewardFailure))
            {
                message = $"골드 보상 실패: {rewardFailure}";
                return;
            }

            if (context.VictoryRewards.GrantsFinalBossPermanentReward)
            {
                if (!BattleVictoryPermanentRewardService.TryCreate(
                        progress,
                        out BattleVictoryPermanentRewardService permanent,
                        out BattleVictoryPermanentRewardFailure createFailure))
                {
                    message = $"영구 보상 생성 실패: {createFailure}";
                    return;
                }

                if (!permanent.TryClaim(
                        "PERMANENT-FIRST-RUN-CLEAR",
                        out BattleVictoryPermanentRewardFailure claimFailure))
                {
                    message = $"영구 보상 수령 실패: {claimFailure}";
                    return;
                }
            }

            RunCampaignService.MarkBattleReward(
                campaign, BattleOutcome.Victory);
            message =
                $"승리 정산 완료 · 골드 {context.VictoryRewards.GoldReward} 획득";
        }

        private void SaveRun(string successMessage)
        {
            if (campaign == null || progress == null)
            {
                return;
            }

            if (IntegratedRunSaveService.TrySave(
                    campaign, progress, out RunSaveDestination destination,
                    out RunCampaignFailure failure))
            {
                if (!string.IsNullOrWhiteSpace(successMessage))
                {
                    message = $"{successMessage} · {destination}";
                }
            }
            else if (!string.IsNullOrWhiteSpace(successMessage))
            {
                message = $"저장 실패: {failure}";
            }
        }

        private void LoadDatabases()
        {
            cardDatabase =
                AssetDatabase.LoadAssetAtPath<CardDatabase>(CardDatabasePath);
            enchantDatabase =
                AssetDatabase.LoadAssetAtPath<EnchantDatabase>(EnchantDatabasePath);
            encounterDatabase =
                AssetDatabase.LoadAssetAtPath<EncounterDatabase>(EncounterDatabasePath);
            prototypeConfig = Resources.Load<RuntimePrototypeConfig>(
                "GameData/RuntimePrototypeConfig");
            if (!DatabasesReady())
            {
                message = "Card/Enchant/Encounter 데이터베이스를 확인하세요.";
            }
        }

        private void LoadPermanentRewards()
        {
            if (PlayerPermanentRewardSaveService.TryLoadDefault(
                    out PlayerPermanentRewardState loaded,
                    out _, out _))
            {
                permanentRewards = loaded;
            }
            else
            {
                permanentRewards ??= new PlayerPermanentRewardState();
            }
        }

        private bool DatabasesReady()
        {
            return cardDatabase != null && enchantDatabase != null &&
                   encounterDatabase != null && prototypeConfig != null &&
                   prototypeConfig.IsReady;
        }

        private void DrawMessage()
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                EditorGUILayout.HelpBox(message, MessageType.None);
            }
        }

        private static IEnumerable<T> Rotate<T>(
            IReadOnlyList<T> values,
            int seed)
        {
            if (values == null || values.Count == 0)
            {
                yield break;
            }

            int start = Mathf.Abs(seed % values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                yield return values[(start + i) % values.Count];
            }
        }
    }
}
