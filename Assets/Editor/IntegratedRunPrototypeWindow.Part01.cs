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
        private void DrawRunInventory()
        {
            EditorGUILayout.LabelField(
                "런 소모아이템",
                EditorStyles.miniBoldLabel);
            RunConsumableInventoryOption[] inventory =
                runConsumables.CreateInventoryOptions(progress);
            EditorGUILayout.LabelField(inventory.Length == 0
                ? "보유 아이템 없음"
                : string.Join(", ", inventory.Select(option =>
                    option.DisplayLabel)));

            RunEnchantHammerTargetOption[] targets =
                runConsumables.CreateHammerTargets(
                    progress,
                    selectedUpgradeCardId);
            if (targets.Length > 0)
            {
                int selectedIndex = Mathf.Max(
                    0,
                    Array.FindIndex(targets, option => option.IsSelected));
                string[] labels = targets
                    .Select(option => string.IsNullOrWhiteSpace(option.BlockReason)
                        ? option.DisplayLabel
                        : $"{option.DisplayLabel} · {option.BlockReason}")
                    .ToArray();
                int nextIndex = EditorGUILayout.Popup(
                    "인첸트 망치 대상",
                    selectedIndex,
                    labels);
                if (nextIndex >= 0 && nextIndex < targets.Length &&
                    runConsumables.SelectHammerTarget(
                        progress,
                        targets[nextIndex].OwnedCardId))
                {
                    selectedUpgradeCardId = targets[nextIndex].OwnedCardId;
                }

                RunEnchantHammerTargetOption selected =
                    runConsumables.SelectedHammerTarget(
                        progress,
                        selectedUpgradeCardId);
                using (new EditorGUI.DisabledScope(selected?.CanUse != true))
                {
                    if (GUILayout.Button("인첸트 망치 사용 · 슬롯 +1"))
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
                }
            }

            DrawMutationScroll();
        }

        private void DrawMutationScroll()
        {
            RunMutationScrollOption[] options =
                runConsumables.CreateMutationOptions(
                    progress,
                    enchantDatabase.Enchants);
            if (options.Length == 0)
            {
                return;
            }

            EditorGUILayout.LabelField(
                "변이 주문서",
                EditorStyles.miniBoldLabel);
            foreach (RunMutationScrollOption option in options)
            {
                using (new EditorGUI.DisabledScope(!option.CanUse))
                {
                    if (!GUILayout.Button(option.DisplayText))
                    {
                        continue;
                    }
                }

                if (runConsumables.TryUseMutationScroll(
                        progress,
                        enchantDatabase.Enchants,
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
            EditorGUILayout.LabelField("다음 노드 선택", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            RunNodeSelectionOption[] options =
                nodeSelection.CreateOptions(campaign);
            foreach (RunNodeSelectionOption option in options)
            {
                if (GUILayout.Button(
                        option.StackedLabel,
                        GUILayout.MinHeight(62f)))
                {
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
                    EditorGUILayout.EndHorizontal();
                    return;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawNonBattleNode()
        {
            RunNodeChoice node = campaign.ActiveNode;
            if (node == null)
            {
                EditorGUILayout.HelpBox("현재 노드가 없습니다.", MessageType.Error);
                return;
            }

            switch (node.NodeType)
            {
                case RunNodeType.Shop:
                    DrawShop();
                    break;
                case RunNodeType.SituationEvent:
                    DrawSituationEvent();
                    break;
                case RunNodeType.RestOrUpgrade:
                    DrawRestOrUpgrade();
                    break;
                default:
                    EditorGUILayout.HelpBox(
                        $"지원하지 않는 비전투 노드: {node.NodeType}",
                        MessageType.Error);
                    break;
            }
        }

        private void DrawSituationEvent()
        {
            EditorGUILayout.LabelField(
                "상황 이벤트",
                EditorStyles.boldLabel);
            RunSituationEventOption[] options =
                situationEvent.CreateOptions(campaign);
            foreach (RunSituationEventOption option in options)
            {
                if (!GUILayout.Button(
                        option.DisplayText,
                        GUILayout.Height(36f))) continue;
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
            EditorGUILayout.LabelField("회복 · 강화", EditorStyles.boldLabel);
            RestUpgradeConfig rules = prototypeConfig.RestUpgradeConfig;
            if (GUILayout.Button(
                    restUpgrade.RestButtonLabel(rules),
                    GUILayout.Height(34f)))
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

            RunRestUpgradeCardOption[] options =
                restUpgrade.CreateCardOptions(
                    campaign,
                    progress,
                    selectedUpgradeCardId);
            if (options.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "강화할 카드가 없습니다.",
                    MessageType.Info);
                return;
            }

            int selectedIndex = Array.FindIndex(
                options,
                option => option.IsSelected);
            selectedIndex = Mathf.Max(0, selectedIndex);
            string[] labels = options
                .Select(option => option.DisplayLabel)
                .ToArray();
            int nextIndex = EditorGUILayout.Popup(
                "강화할 카드",
                selectedIndex,
                labels);
            if (nextIndex >= 0 && nextIndex < options.Length &&
                restUpgrade.SelectCard(
                    campaign,
                    progress,
                    options[nextIndex].OwnedCardId))
            {
                selectedUpgradeCardId = options[nextIndex].OwnedCardId;
            }

            if (GUILayout.Button(
                    restUpgrade.UpgradeButtonLabel(rules),
                    GUILayout.Height(34f)))
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
                    return;
                }

                message = $"강화 실패: {failure}";
            }
        }

    }
}
