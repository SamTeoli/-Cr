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
        private void SaveRun(string successMessage)
        {
            if (campaign == null || progress == null)
            {
                return;
            }

            if (progress.HasActiveEncounter)
            {
                if (campaign.Phase == RunCampaignPhase.Battle &&
                    !string.IsNullOrWhiteSpace(successMessage))
                {
                    BattleStartCommandResult checkpoint = battleStart.TryStart(
                        campaign,
                        progress,
                        prototypeConfig);
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
