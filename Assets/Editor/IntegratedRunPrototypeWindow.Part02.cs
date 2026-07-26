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
        private void DrawShop()
        {
            EditorGUILayout.LabelField("상점", EditorStyles.boldLabel);
            RunShopProductOption[] options = shop.CreateOptions(
                campaign, progress, enchantDatabase,
                prototypeConfig.ShopEconomyConfig);
            DrawShopProducts("소모아이템", options.Where(option =>
                option.ProductType == RunShopProductType.Consumable));
            EditorGUILayout.Space(5f);
            DrawShopProducts("인첸트", options.Where(option =>
                option.ProductType == RunShopProductType.Enchant));
            EditorGUILayout.BeginHorizontal();
            int rerollCost = shop.GetRerollCost(
                campaign, prototypeConfig.ShopEconomyConfig);
            if (GUILayout.Button($"전체 리롤 · {rerollCost}G"))
            {
                if (shop.TryReroll(campaign, progress.RunState,
                        prototypeConfig.ShopEconomyConfig, out _,
                        out string result, out RunCampaignFailure failure))
                {
                    message = result;
                    SaveRun(null);
                }
                else message = $"리롤 실패: {failure}";
            }
            if (GUILayout.Button("상점 나가기"))
            {
                if (shop.TryLeave(campaign, progress.RunState,
                        out string result, out RunCampaignFailure failure))
                {
                    message = result;
                    SaveRun(null);
                }
                else message = $"상점 종료 실패: {failure}";
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawShopProducts(
            string sectionLabel,
            IEnumerable<RunShopProductOption> options)
        {
            EditorGUILayout.LabelField(sectionLabel, EditorStyles.miniBoldLabel);
            foreach (RunShopProductOption option in options)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                string targetText = string.IsNullOrWhiteSpace(option.TargetLabel)
                    ? string.Empty : $"\n장착 대상: {option.TargetLabel}";
                string blockText = string.IsNullOrWhiteSpace(option.BlockReason)
                    ? string.Empty : $"\n{option.BlockReason}";
                EditorGUILayout.LabelField(
                    option.DisplayText + targetText + blockText,
                    EditorStyles.wordWrappedLabel);
                using (new EditorGUI.DisabledScope(!option.CanPurchase))
                {
                    if (GUILayout.Button(option.PurchaseButtonLabel,
                            GUILayout.Width(70f)))
                    {
                        if (shop.TryBuy(campaign, progress, enchantDatabase,
                                prototypeConfig.ShopEconomyConfig, option.SlotId,
                                out _, out string result,
                                out EnchantAttachmentFailure attachmentFailure,
                                out RunCampaignFailure failure))
                        {
                            message = result;
                            SaveRun(null);
                        }
                        else message = option.ProductType ==
                                RunShopProductType.Enchant
                            ? $"인첸트 구매 실패: {failure} / {attachmentFailure}"
                            : $"구매 실패: {failure}";
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawBattle()
        {
            BattleScreenSnapshot snapshot =
                battleScreen.CreateSnapshot(progress, campaign);
            if (!snapshot.Available)
            {
                EditorGUILayout.HelpBox(
                    snapshot.ErrorText ?? "활성 전투를 찾을 수 없습니다.",
                    MessageType.Error);
                if (GUILayout.Button("전투 다시 시작"))
                {
                    BeginSelectedBattle();
                }
                return;
            }

            EditorGUILayout.LabelField(
                snapshot.TitleText,
                EditorStyles.boldLabel);
            EditorGUILayout.LabelField(snapshot.PlayerSummaryText);
            EditorGUILayout.LabelField(snapshot.ZoneSummaryText);
            if (!string.IsNullOrWhiteSpace(snapshot.PlayerStatusText))
            {
                EditorGUILayout.LabelField(
                    snapshot.PlayerStatusText,
                    EditorStyles.wordWrappedLabel);
            }
            EditorGUILayout.HelpBox(
                snapshot.CheckpointNoticeText,
                MessageType.Info);
            DrawBattleConsumables(snapshot);
            DrawBattleEnemies(snapshot);
            DrawBattleMonsters(snapshot);
            DrawBattleHand(snapshot);

            using (new EditorGUI.DisabledScope(!snapshot.CanEndTurn))
            {
                if (GUILayout.Button("턴 종료", GUILayout.Height(36f)))
                {
                    BattleEndTurnCommandResult command =
                        battleScreen.TryEndPlayerTurn(progress, campaign);
                    message = command.Message;
                    if (command.Succeeded)
                    {
                        SaveRun(null);
                    }
                }
            }

            if (snapshot.CanSettle)
            {
                EditorGUILayout.HelpBox(
                    snapshot.FinishedText,
                    snapshot.Outcome == BattleOutcome.Victory
                        ? MessageType.Info
                        : MessageType.Error);
                if (GUILayout.Button("전투 정산", GUILayout.Height(38f)))
                {
                    SettleBattle();
                }
            }
        }

        private void DrawBattleConsumables(BattleScreenSnapshot snapshot)
        {
            EditorGUILayout.LabelField(
                "소모아이템",
                EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            foreach (BattleConsumableActionOption option in snapshot.Consumables)
            {
                using (new EditorGUI.DisabledScope(!option.CanUse))
                {
                    if (GUILayout.Button(option.DisplayLabel))
                    {
                        BattleConsumableCommandResult command =
                            battleScreen.TryUseConsumable(
                                progress,
                                option.ItemId);
                        message = command.Message;
                        if (command.Succeeded)
                        {
                            SaveRun(null);
                        }
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawBattleEnemies(BattleScreenSnapshot snapshot)
        {
            EditorGUILayout.LabelField(
                "적 필드",
                EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            foreach (BattleEnemyDisplayOption option in snapshot.Enemies)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(
                    option.DisplayText,
                    EditorStyles.wordWrappedLabel);
                if (!string.IsNullOrWhiteSpace(option.StatusText))
                {
                    EditorGUILayout.LabelField(
                        option.StatusText,
                        EditorStyles.wordWrappedLabel);
                }
                if (option.IsOccupied)
                {
                    using (new EditorGUI.DisabledScope(!option.CanSelect))
                    {
                        if (GUILayout.Button(
                                option.IsSelected ? "선택됨" : "대상 선택"))
                        {
                            battleScreen.SelectEnemy(
                                progress,
                                option.EnemyId);
                        }
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
