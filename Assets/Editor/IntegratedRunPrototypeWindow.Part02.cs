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
            BattleRuntimeEncounterContext context = progress.ActiveEncounter;
            BattleRuntimeSessionState session = context?.Session;
            if (session?.Runtime == null)
            {
                EditorGUILayout.HelpBox(
                    "활성 전투를 찾을 수 없습니다.", MessageType.Error);
                if (GUILayout.Button("전투 다시 시작"))
                {
                    BeginSelectedBattle();
                }
                return;
            }

            battleActions.Refresh(context);
            BattleRuntimeState runtime = session.Runtime;
            EditorGUILayout.LabelField(
                $"{context.Encounter.DisplayName} · 턴 {runtime.Turn.PlayerTurnNumber}",
                EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                $"HP {runtime.Player.CurrentHealth}/{runtime.Player.MaximumHealth}    " +
                $"마력 {runtime.CardPlay.Mana.CurrentMana}/" +
                $"{runtime.CardPlay.Mana.MaximumMana}    결과 {session.Outcome}");
            DrawBattleConsumables();
            DrawBattleEnemies(context);
            DrawBattleMonsters(context);
            DrawBattleHand(context);

            using (new EditorGUI.DisabledScope(session.IsFinished))
            {
                if (GUILayout.Button("턴 종료", GUILayout.Height(36f)))
                {
                    int tieBreaker = campaign.Seed +
                                     context.Session.CompletedRoundCount * 10;
                    BattleEndTurnCommandResult command =
                        battleActions.TryEndPlayerTurn(context, tieBreaker);
                    message = command.Message;
                    if (command.Succeeded)
                    {
                        SaveRun(null);
                    }
                }
            }

            if (session.IsFinished)
            {
                EditorGUILayout.HelpBox(
                    $"전투 종료: {session.Outcome}. 정산을 진행하세요.",
                    session.Outcome == BattleOutcome.Victory
                        ? MessageType.Info
                        : MessageType.Error);
                if (GUILayout.Button("전투 정산", GUILayout.Height(38f)))
                {
                    SettleBattle();
                }
            }
        }

        private void DrawBattleConsumables()
        {
            EditorGUILayout.LabelField(
                "소모아이템",
                EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            foreach (BattleConsumableActionOption option in
                     battleActions.CreateConsumableOptions(progress))
            {
                using (new EditorGUI.DisabledScope(!option.CanUse))
                {
                    if (GUILayout.Button(option.DisplayLabel))
                    {
                        BattleConsumableCommandResult command =
                            battleActions.TryUseConsumable(
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

        private void DrawBattleEnemies(BattleRuntimeEncounterContext context)
        {
            BattleEnemyTargetOption[] targets =
                battleActions.CreateEnemyTargets(context);
            EditorGUILayout.LabelField(
                "적 필드",
                EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            foreach (EnemyFieldPosition position in
                     Enum.GetValues(typeof(EnemyFieldPosition)))
            {
                BattleEnemyTargetOption option = targets.FirstOrDefault(value =>
                    value.Position == position);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                if (option?.IsOccupied != true)
                {
                    EditorGUILayout.LabelField("빈 칸");
                }
                else
                {
                    BattleEnemyRuntimeState enemy = option.Enemy;
                    BattleEnemyStatusState status = option.Status;
                    EditorGUILayout.LabelField(
                        $"{option.DisplayName}\n" +
                        $"HP {enemy.Vital.CurrentHealth}/{option.MaximumHealth} · " +
                        $"공격 {enemy.Attack}\n" +
                        $"부상 {status?.Injury ?? 0} 약화 {status?.Weaken ?? 0} " +
                        $"취약 {status?.Vulnerable ?? 0} 속박 {status?.Bind ?? 0} " +
                        $"기절 {status?.Stun ?? 0}",
                        EditorStyles.wordWrappedLabel);
                    using (new EditorGUI.DisabledScope(!option.CanSelect))
                    {
                        if (GUILayout.Button(
                                option.IsSelected ? "선택됨" : "대상 선택"))
                        {
                            battleActions.SelectEnemy(
                                context,
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
