using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HaveABreak.Cards
{
    public sealed partial class RuntimePrototypeScreen : MonoBehaviour
    {
        private void DrawRunInventory()
        {
            GUILayout.Label("런 소모아이템", headingStyle);
            RunConsumableInventoryOption[] inventory =
                runConsumables.CreateInventoryOptions(progress);
            GUILayout.Label(inventory.Length == 0
                ? "보유 아이템 없음"
                : string.Join(", ", inventory.Select(option =>
                    option.DisplayLabel)));

            RunEnchantHammerTargetOption selected =
                runConsumables.SelectedHammerTarget(
                    progress,
                    selectedUpgradeCardId);
            if (selected != null)
            {
                selectedUpgradeCardId = selected.OwnedCardId;
                GUILayout.BeginHorizontal(GUI.skin.box);
                string blockText = string.IsNullOrWhiteSpace(selected.BlockReason)
                    ? string.Empty
                    : $" · {selected.BlockReason}";
                GUILayout.Label($"망치 대상: {selected.DisplayLabel}{blockText}");
                if (GUILayout.Button("다음 카드", GUILayout.Width(100f)))
                {
                    selected = runConsumables.CycleHammerTarget(
                        progress,
                        selectedUpgradeCardId);
                    selectedUpgradeCardId = selected?.OwnedCardId;
                }

                bool previous = GUI.enabled;
                GUI.enabled = selected?.CanUse == true;
                if (GUILayout.Button("인첸트 슬롯 +1", GUILayout.Width(140f)))
                {
                    if (runConsumables.TryUseEnchantHammer(
                            progress,
                            selectedUpgradeCardId,
                            out RunEnchantHammerTargetOption used,
                            out string result,
                            out PrototypeConsumableFailure failure))
                    {
                        selectedUpgradeCardId = used.OwnedCardId;
                        message = result;
                        SaveRun(null);
                    }
                    else
                    {
                        message = $"인첸트 망치 사용 실패: {failure}";
                    }
                }
                GUI.enabled = previous;
                GUILayout.EndHorizontal();
            }

            DrawMutationScroll();
        }

        private void DrawMutationScroll()
        {
            RunMutationScrollOption[] options =
                runConsumables.CreateMutationOptions(
                    progress,
                    config.EnchantDatabase.Enchants);
            if (options.Length == 0)
            {
                return;
            }

            GUILayout.Label("변이 주문서", headingStyle);
            foreach (RunMutationScrollOption option in options)
            {
                bool previous = GUI.enabled;
                GUI.enabled = option.CanUse;
                bool clicked = GUILayout.Button(option.DisplayText);
                GUI.enabled = previous;
                if (!clicked)
                {
                    continue;
                }

                if (runConsumables.TryUseMutationScroll(
                        progress,
                        config.EnchantDatabase.Enchants,
                        option.OwnedCardId,
                        option.SlotIndex,
                        option.ReplacementDefinitionId,
                        out _,
                        out string result,
                        out EnchantAttachmentFailure attachmentFailure,
                        out PrototypeConsumableFailure failure))
                {
                    message = result;
                    SaveRun(null);
                    return;
                }

                message =
                    $"변이 주문서 사용 실패: {failure} / {attachmentFailure}";
            }
        }

        private void DrawNodeSelection()
        {
            GUILayout.Label("다음 노드 선택", headingStyle);
            RunNodeSelectionOption[] options =
                nodeSelection.CreateOptions(campaign);
            foreach (RunNodeSelectionOption option in options)
            {
                if (!GUILayout.Button(
                        option.InlineLabel,
                        GUILayout.Height(46f))) continue;
                if (!nodeSelection.TrySelect(
                        campaign,
                        option.NodeId,
                        out RunNodeSelectionOption selected,
                        out RunCampaignFailure failure))
                {
                    message = $"노드 선택 실패: {failure}";
                }
                else if (selected.IsBattle)
                {
                    BeginSelectedBattle();
                }
                else
                {
                    message = $"{selected.DisplayName} 노드에 들어왔습니다.";
                    SaveRun(null);
                }
                return;
            }
        }

        private void DrawNonBattleNode()
        {
            if (campaign.ActiveNode == null)
            {
                Notice("현재 노드가 없습니다.");
                return;
            }
            switch (campaign.ActiveNode.NodeType)
            {
                case RunNodeType.Shop: DrawShop(); break;
                case RunNodeType.SituationEvent: DrawSituationEvent(); break;
                case RunNodeType.RestOrUpgrade: DrawRestOrUpgrade(); break;
                default: Notice($"지원하지 않는 노드: {campaign.ActiveNode.NodeType}"); break;
            }
        }

        private void DrawSituationEvent()
        {
            GUILayout.Label("상황 이벤트", headingStyle);
            RunSituationEventOption[] options =
                situationEvent.CreateOptions(campaign);
            foreach (RunSituationEventOption option in options)
            {
                if (!GUILayout.Button(
                        option.DisplayText,
                        GUILayout.Height(42f))) continue;
                if (situationEvent.TryResolve(
                        campaign,
                        progress.RunState,
                        option.ChoiceId,
                        out _,
                        out string result,
                        out RunCampaignFailure failure))
                {
                    message = result;
                    SaveRun(null);
                    return;
                }

                message = $"이벤트 처리 실패: {failure}";
            }
        }

        private void DrawRestOrUpgrade()
        {
            GUILayout.Label("회복 · 강화", headingStyle);
            RestUpgradeConfig rules = config.RestUpgradeConfig;
            if (GUILayout.Button(
                    restUpgrade.RestButtonLabel(rules),
                    GUILayout.Height(38f)))
            {
                if (restUpgrade.TryRest(
                        campaign,
                        progress.RunState,
                        rules,
                        out _,
                        out string result,
                        out RunCampaignFailure failure))
                {
                    message = result;
                    SaveRun(null);
                    return;
                }

                message = $"회복 실패: {failure}";
            }

            RunRestUpgradeCardOption selected = restUpgrade.SelectedCard(
                campaign,
                progress,
                selectedUpgradeCardId);
            if (selected != null)
            {
                selectedUpgradeCardId = selected.OwnedCardId;
            }

            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label(selected == null
                ? "강화할 카드 없음"
                : $"강화 대상: {selected.DisplayName} Lv.{selected.CurrentLevel}");
            if (GUILayout.Button("다음 카드", GUILayout.Width(100f)))
            {
                selected = restUpgrade.CycleCard(
                    campaign,
                    progress,
                    selectedUpgradeCardId);
                selectedUpgradeCardId = selected?.OwnedCardId;
            }
            if (GUILayout.Button(
                    restUpgrade.UpgradeButtonLabel(rules),
                    GUILayout.Width(150f)))
            {
                if (restUpgrade.TryUpgrade(
                        campaign,
                        progress,
                        rules,
                        selectedUpgradeCardId,
                        out RunRestUpgradeCardOption upgraded,
                        out string result,
                        out RunCampaignFailure failure))
                {
                    selectedUpgradeCardId = upgraded.OwnedCardId;
                    message = result;
                    SaveRun(null);
                    GUILayout.EndHorizontal();
                    return;
                }

                message = $"강화 실패: {failure}";
            }
            GUILayout.EndHorizontal();
        }

        private void DrawShop()
        {
            GUILayout.Label("상점", headingStyle);
            RunShopProductOption[] options = shop.CreateOptions(
                campaign, progress, config.EnchantDatabase,
                config.ShopEconomyConfig);
            DrawShopProducts("소모아이템", options.Where(option =>
                option.ProductType == RunShopProductType.Consumable));
            DrawShopProducts("인첸트", options.Where(option =>
                option.ProductType == RunShopProductType.Enchant));
            GUILayout.BeginHorizontal();
            int rerollCost = shop.GetRerollCost(
                campaign, config.ShopEconomyConfig);
            if (GUILayout.Button($"전체 리롤 · {rerollCost}G"))
            {
                if (shop.TryReroll(campaign, progress.RunState,
                        config.ShopEconomyConfig, out _, out string result,
                        out RunCampaignFailure failure))
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
            GUILayout.EndHorizontal();
        }

    }
}
