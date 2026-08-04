using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HaveABreak.Cards
{
    public sealed partial class RuntimePrototypeScreen : MonoBehaviour
    {
        private void DrawMonsters(BattleScreenSnapshot snapshot)
        {
            GUILayout.Label("아군 몬스터");
            GUILayout.BeginHorizontal();
            foreach (BattleMonsterDisplayOption option in snapshot.Monsters)
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
                    GUI.enabled = option.CanAttack;
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
                    GUI.enabled = previous;
                    if (!string.IsNullOrWhiteSpace(option.BlockReason))
                    {
                        GUILayout.Label(option.BlockReason, wrappedStyle);
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawHand(BattleScreenSnapshot snapshot)
        {
            GUILayout.Label($"패 ({snapshot.Hand.Length})", headingStyle);
            foreach (BattleHandCardActionOption option in snapshot.Hand)
            {
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label(option.DisplayText, wrappedStyle);
                bool previous = GUI.enabled;
                bool selectingThisCard = string.Equals(
                    pendingBanishSourceCardId,
                    option.BattleCardId,
                    StringComparison.OrdinalIgnoreCase);
                GUI.enabled = option.CanPlay || selectingThisCard;
                bool clicked = GUILayout.Button(
                    selectingThisCard ? "선택 취소" : "사용",
                    GUILayout.Width(90f));
                GUI.enabled = previous;
                if (clicked)
                {
                    if (selectingThisCard)
                    {
                        pendingBanishSourceCardId = null;
                        pendingBanishTargetCardId = null;
                        message = "카드 활성화를 취소했습니다.";
                    }
                    else if (!BeginBanishTargetSelectionIfRequired(
                                 option.BattleCardId))
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
                if (!string.IsNullOrWhiteSpace(
                        pendingBanishSourceCardId) &&
                    !selectingThisCard)
                {
                    BattleHandCardActionOption source =
                        snapshot.Hand.FirstOrDefault(card =>
                            string.Equals(
                                card.BattleCardId,
                                pendingBanishSourceCardId,
                                StringComparison.OrdinalIgnoreCase));
                    bool canTarget = source?.BanishTargets.Any(target =>
                        string.Equals(
                            target.BattleCardId,
                            option.BattleCardId,
                            StringComparison.OrdinalIgnoreCase)) == true;
                    GUI.enabled = canTarget;
                    if (GUILayout.Button(
                            string.Equals(
                                pendingBanishTargetCardId,
                                option.BattleCardId,
                                StringComparison.OrdinalIgnoreCase)
                                ? "대상 선택됨"
                                : "효과 대상",
                            GUILayout.Width(90f)))
                    {
                        pendingBanishTargetCardId =
                            option.BattleCardId;
                        message =
                            $"{option.Card.SourceCard.DisplayName}을(를) " +
                            "효과 대상으로 선택했습니다.";
                    }
                    GUI.enabled = previous;
                }
                else if (!option.CanPlay)
                {
                    GUILayout.Label(
                        option.BlockReason,
                        GUILayout.Width(135f));
                }
                GUILayout.EndHorizontal();
            }
            if (!string.IsNullOrWhiteSpace(pendingBanishSourceCardId))
            {
                GUILayout.BeginHorizontal();
                bool previous = GUI.enabled;
                GUI.enabled = !string.IsNullOrWhiteSpace(
                    pendingBanishTargetCardId);
                if (GUILayout.Button("선택한 대상으로 발동"))
                {
                    string sourceId = pendingBanishSourceCardId;
                    string targetId = pendingBanishTargetCardId;
                    pendingBanishSourceCardId = null;
                    pendingBanishTargetCardId = null;
                    if (battleScreen.SelectBanishTarget(
                            progress,
                            sourceId,
                            targetId))
                    {
                        BattleCardPlayCommandResult command =
                            battleScreen.TryPlayCard(progress, sourceId);
                        message = command.Message;
                        if (command.Succeeded)
                        {
                            SaveRun(null);
                        }
                    }
                }
                GUI.enabled = previous;
                if (GUILayout.Button("활성화 취소"))
                {
                    pendingBanishSourceCardId = null;
                    pendingBanishTargetCardId = null;
                    message = "카드 활성화를 취소했습니다.";
                }
                GUILayout.EndHorizontal();
            }
        }

        private void DrawRecentEvents(BattleScreenSnapshot snapshot)
        {
            GUILayout.Label("최근 전투 기록", headingStyle);
            if (snapshot.RecentEvents.Length == 0)
            {
                GUILayout.Label("기록 없음");
                return;
            }

            foreach (BattleEventDisplayOption option in snapshot.RecentEvents)
            {
                GUILayout.Label(option.DisplayText, wrappedStyle);
            }
        }

        private void DrawRewards()
        {
            RunBattleRewardSnapshot snapshot = battleReward.CreateSnapshot(
                campaign,
                progress,
                config.EnchantDatabase);
            if (!snapshot.Available)
            {
                Notice(snapshot.ErrorText ?? "정산된 승리 전투가 없습니다.");
                return;
            }

            GUILayout.Label("전투 보상", headingStyle);
            GUILayout.Label(snapshot.GoldLabel);
            if (!string.IsNullOrWhiteSpace(snapshot.ErrorText))
            {
                Notice(snapshot.ErrorText);
            }
            DrawEnchantRewardOptions(snapshot.EnchantOptions);
            DrawConsumableRewardOptions(snapshot.ConsumableOptions);

            bool previous = GUI.enabled;
            GUI.enabled = snapshot.CanComplete;
            if (GUILayout.Button(
                    "보상 완료 · 다음 노드",
                    GUILayout.Height(46f)))
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
            GUI.enabled = previous;
            if (!snapshot.CanComplete &&
                string.IsNullOrWhiteSpace(snapshot.ErrorText))
            {
                GUILayout.Label(
                    "필수 보상을 모두 선택하면 다음 노드로 이동할 수 있습니다.",
                    wrappedStyle);
            }
        }
    }
}
