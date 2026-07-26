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
                if (option.BanishTargets.Length > 0)
                {
                    string banishLabel =
                        option.SelectedBanishTarget?.DisplayLabel ??
                        "소멸 대상 없음";
                    if (GUILayout.Button(
                            banishLabel,
                            GUILayout.Width(170f)))
                    {
                        battleScreen.CycleBanishTarget(
                            progress,
                            option.BattleCardId);
                    }
                }

                bool previous = GUI.enabled;
                GUI.enabled = option.CanPlay;
                bool clicked = GUILayout.Button("사용", GUILayout.Width(75f));
                GUI.enabled = previous;
                if (clicked)
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
                else if (!option.CanPlay)
                {
                    GUILayout.Label(
                        option.BlockReason,
                        GUILayout.Width(135f));
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
