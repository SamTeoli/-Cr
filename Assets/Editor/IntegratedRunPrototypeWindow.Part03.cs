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
        private void DrawBattleMonsters(BattleScreenSnapshot snapshot)
        {
            EditorGUILayout.LabelField(
                "아군 몬스터",
                EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            foreach (BattleMonsterDisplayOption option in snapshot.Monsters)
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
                    using (new EditorGUI.DisabledScope(!option.CanAttack))
                    {
                        if (GUILayout.Button("선택한 적 공격"))
                        {
                            BattleMonsterAttackCommandResult command =
                                battleScreen.TryAttack(
                                    progress,
                                    option.BattleCardId);
                            message = command.Message;
                            if (command.Succeeded)
                            {
                                SaveRun(null);
                            }
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(option.BlockReason))
                    {
                        EditorGUILayout.LabelField(
                            option.BlockReason,
                            EditorStyles.wordWrappedLabel);
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawBattleHand(BattleScreenSnapshot snapshot)
        {
            EditorGUILayout.LabelField(
                $"패 ({snapshot.Hand.Length})",
                EditorStyles.miniBoldLabel);
            foreach (BattleHandCardActionOption option in snapshot.Hand)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField(
                    option.DisplayText,
                    EditorStyles.wordWrappedLabel);
                if (option.BanishTargets.Length > 0)
                {
                    int selectedIndex = Math.Max(
                        0,
                        Array.FindIndex(
                            option.BanishTargets,
                            value => value.IsSelected));
                    int nextIndex = EditorGUILayout.Popup(
                        selectedIndex,
                        option.BanishTargets
                            .Select(value => value.DisplayName)
                            .ToArray(),
                        GUILayout.Width(130f));
                    if (nextIndex >= 0 &&
                        nextIndex < option.BanishTargets.Length)
                    {
                        battleScreen.SelectBanishTarget(
                            progress,
                            option.BattleCardId,
                            option.BanishTargets[nextIndex].BattleCardId);
                    }
                }

                using (new EditorGUI.DisabledScope(!option.CanPlay))
                {
                    if (GUILayout.Button("사용", GUILayout.Width(65f)))
                    {
                        BattleCardPlayCommandResult command =
                            battleScreen.TryPlayCard(
                                progress,
                                option.BattleCardId);
                        message = command.Message;
                        if (command.Succeeded)
                        {
                            SaveRun(null);
                        }
                    }
                }
                if (!option.CanPlay)
                {
                    EditorGUILayout.LabelField(
                        option.BlockReason,
                        GUILayout.Width(130f));
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawRewards()
        {
            RunBattleRewardSnapshot snapshot = battleReward.CreateSnapshot(
                campaign,
                progress,
                enchantDatabase);
            if (!snapshot.Available)
            {
                EditorGUILayout.HelpBox(
                    snapshot.ErrorText ?? "정산된 승리 전투가 없습니다.",
                    MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField("전투 보상", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(snapshot.GoldLabel);
            if (!string.IsNullOrWhiteSpace(snapshot.ErrorText))
            {
                EditorGUILayout.HelpBox(snapshot.ErrorText, MessageType.Error);
            }
            DrawEnchantRewardOptions(snapshot.EnchantOptions);
            DrawConsumableRewardOptions(snapshot.ConsumableOptions);

            using (new EditorGUI.DisabledScope(!snapshot.CanComplete))
            {
                if (GUILayout.Button(
                        "보상 완료 · 다음 노드",
                        GUILayout.Height(40f)))
                {
                    if (battleReward.TryComplete(
                            campaign,
                            progress,
                            out string result,
                            out RunEncounterProgressFailure failure))
                    {
                        message = result;
                        SaveRun(null);
                    }
                    else
                    {
                        message = $"보상 미완료: {failure}";
                    }
                }
            }
            if (!snapshot.CanComplete &&
                string.IsNullOrWhiteSpace(snapshot.ErrorText))
            {
                EditorGUILayout.HelpBox(
                    "필수 보상을 모두 선택하면 다음 노드로 이동할 수 있습니다.",
                    MessageType.Info);
            }
        }

        private void DrawEnchantRewardOptions(
            IEnumerable<RunBattleEnchantRewardOption> options)
        {
            RunBattleEnchantRewardOption[] snapshot = options?.ToArray() ??
                Array.Empty<RunBattleEnchantRewardOption>();
            if (snapshot.Length == 0)
            {
                return;
            }

            EditorGUILayout.LabelField(
                "인첸트 보상",
                EditorStyles.miniBoldLabel);
            foreach (RunBattleEnchantRewardOption option in snapshot)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                string targetText = string.IsNullOrWhiteSpace(option.TargetLabel)
                    ? string.Empty
                    : $"\n장착 대상: {option.TargetLabel}";
                string blockText = string.IsNullOrWhiteSpace(option.BlockReason)
                    ? string.Empty
                    : $"\n{option.BlockReason}";
                EditorGUILayout.LabelField(
                    option.DisplayText + targetText + blockText,
                    EditorStyles.wordWrappedLabel);
                using (new EditorGUI.DisabledScope(!option.CanClaim))
                {
                    if (GUILayout.Button(
                            option.IsSelected ? "선택 완료" : "선택",
                            GUILayout.Width(90f)))
                    {
                        if (battleReward.TryClaimEnchant(
                                campaign,
                                progress,
                                enchantDatabase,
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
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
