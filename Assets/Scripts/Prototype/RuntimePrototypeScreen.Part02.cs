using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    public sealed partial class RuntimePrototypeScreen : MonoBehaviour
    {
        private void DrawShopProducts(
            string sectionLabel,
            IEnumerable<RunShopProductOption> options)
        {
            GUILayout.Label(sectionLabel);
            foreach (RunShopProductOption option in options)
            {
                GUILayout.BeginHorizontal(GUI.skin.box);
                string targetText = string.IsNullOrWhiteSpace(option.TargetLabel)
                    ? string.Empty : $"\n장착 대상: {option.TargetLabel}";
                string blockText = string.IsNullOrWhiteSpace(option.BlockReason)
                    ? string.Empty : $"\n{option.BlockReason}";
                GUILayout.Label(option.DisplayText + targetText + blockText,
                    wrappedStyle);
                bool previous = GUI.enabled;
                GUI.enabled = option.CanPurchase;
                if (GUILayout.Button(option.PurchaseButtonLabel,
                        GUILayout.Width(80f)))
                {
                    if (shop.TryBuy(campaign, progress, config.EnchantDatabase,
                            config.ShopEconomyConfig, option.SlotId, out _,
                            out string result,
                            out EnchantAttachmentFailure attachmentFailure,
                            out RunCampaignFailure failure))
                    {
                        message = result;
                        SaveRun(null);
                    }
                    else message = option.ProductType == RunShopProductType.Enchant
                        ? $"인첸트 구매 실패: {failure} / {attachmentFailure}"
                        : $"구매 실패: {failure}";
                }
                GUI.enabled = previous;
                GUILayout.EndHorizontal();
            }
        }

        private void DrawBattle()
        {
            BattleScreenSnapshot snapshot =
                battleScreen.CreateSnapshot(progress, campaign);
            if (!snapshot.Available)
            {
                Notice(snapshot.ErrorText ?? "활성 전투를 찾을 수 없습니다.");
                if (GUILayout.Button("전투 다시 시작")) BeginSelectedBattle();
                return;
            }
            if (snapshot.SessionFinished)
            {
                SettleBattle();
                return;
            }

            GUILayout.Label(snapshot.TitleText, headingStyle);
            GUILayout.Label(snapshot.PlayerSummaryText);
            GUILayout.Label(snapshot.ZoneSummaryText);
            if (!string.IsNullOrWhiteSpace(snapshot.PlayerStatusText))
            {
                GUILayout.Label(snapshot.PlayerStatusText, wrappedStyle);
            }
            GUILayout.Label(snapshot.CheckpointNoticeText, wrappedStyle);
            DrawBattleConsumables(snapshot);
            DrawEnemies(snapshot);
            DrawMonsters(snapshot);
            DrawInstalledCards(snapshot);
            DrawHand(snapshot);
            DrawRecentEvents(snapshot);

            bool previous = GUI.enabled;
            GUI.enabled = snapshot.CanEndTurn;
            if (GUILayout.Button("턴 종료", GUILayout.Height(42f)))
            {
                BattleEndTurnCommandResult command =
                    battleScreen.TryEndPlayerTurn(progress, campaign);
                message = command.Message;
                if (command.Succeeded)
                {
                    SaveRun(null);
                }
            }
            GUI.enabled = previous;
        }

        private void DrawBattleConsumables(BattleScreenSnapshot snapshot)
        {
            GUILayout.Label("소모아이템");
            GUILayout.BeginHorizontal();
            foreach (BattleConsumableActionOption option in snapshot.Consumables)
            {
                bool previous = GUI.enabled;
                GUI.enabled = option.CanUse;
                bool clicked = GUILayout.Button(option.DisplayLabel);
                GUI.enabled = previous;
                if (!clicked)
                {
                    continue;
                }

                BattleConsumableCommandResult command =
                    battleScreen.TryUseConsumable(progress, option.ItemId);
                message = command.Message;
                if (command.Succeeded)
                {
                    SaveRun(null);
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawInstalledCards(BattleScreenSnapshot snapshot)
        {
            GUILayout.Label(
                $"설치 카드 ({snapshot.InstalledCards.Length})",
                headingStyle);
            if (snapshot.InstalledCards.Length == 0)
            {
                GUILayout.Label("설치된 스킬·트랩·결계가 없습니다.");
                return;
            }

            GUILayout.BeginHorizontal();
            foreach (BattleInstalledCardDisplayOption option in
                     snapshot.InstalledCards)
            {
                GUILayout.BeginVertical(GUI.skin.box, GUILayout.MinWidth(160f));
                GUILayout.Label(option.DisplayText, wrappedStyle);
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawEnemies(BattleScreenSnapshot snapshot)
        {
            GUILayout.Label("적 필드");
            GUILayout.BeginHorizontal();
            foreach (BattleEnemyDisplayOption option in snapshot.Enemies)
            {
                GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
                GUILayout.Label(option.DisplayText, wrappedStyle);
                if (!string.IsNullOrWhiteSpace(option.StatusText))
                {
                    GUILayout.Label(option.StatusText, wrappedStyle);
                }

                if (option.IsOccupied)
                {
                    bool previous = GUI.enabled;
                    GUI.enabled = option.CanSelect;
                    if (GUILayout.Button(
                            option.IsSelected ? "선택됨" : "대상 선택"))
                    {
                        battleScreen.SelectEnemy(progress, option.EnemyId);
                    }
                    GUI.enabled = previous;
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }
    }
}
