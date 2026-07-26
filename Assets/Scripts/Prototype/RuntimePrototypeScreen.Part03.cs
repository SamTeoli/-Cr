using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HaveABreak.Cards
{
    public sealed partial class RuntimePrototypeScreen : MonoBehaviour
    {
        private static string DescribeEnemyCommand(
            BattleRuntimeEnemyTurnCommand command)
        {
            switch (command.ActionType)
            {
                case BattleRuntimeEnemyTurnActionType.Move:
                    string direction = command.MoveDirection ==
                        EnemyMoveDirection.Left ? "왼쪽" : "오른쪽";
                    return $"{direction} 이동 {command.MoveSteps}";
                case BattleRuntimeEnemyTurnActionType.Attack:
                    int count = Mathf.Max(1, command.AutomaticAttackCount);
                    return count == 1 ? "공격" : $"공격 ×{count}";
                case BattleRuntimeEnemyTurnActionType.Ability:
                    EnemyAbilityResolutionContext ability = command.Ability;
                    string range = ability.IsAreaAbility ? "광역" : "단일";
                    string effect = ability.HasStatusEffect
                        ? $" · {DescribeStatusKeyword(ability.StatusKeyword)} " +
                          $"{ability.StatusAmount}"
                        : string.Empty;
                    return $"능력 {ability.AbilityId} ({range}{effect})";
                default:
                    return command.ActionType.ToString();
            }
        }

        private static string DescribeEnemyStatus(BattleEnemyStatusState status)
        {
            if (status == null) return string.Empty;
            List<string> values = new();
            AddStatus(values, "부상", status.Injury);
            AddStatus(values, "약화", status.Weaken);
            AddStatus(values, "취약", status.Vulnerable);
            AddStatus(values, "속박", status.Bind);
            AddStatus(values, "기절", status.Stun);
            return values.Count == 0
                ? string.Empty
                : "상태: " + string.Join(" · ", values);
        }

        private static string DescribeCommonStatus(BattleCommonStatusState status)
        {
            if (status == null) return string.Empty;
            List<string> values = new();
            AddStatus(values, "부상", status.Injury);
            AddStatus(values, "약화", status.Weaken);
            AddStatus(values, "취약", status.Vulnerable);
            AddStatus(values, "속박", status.Bind);
            AddStatus(values, "기절", status.Stun);
            return values.Count == 0
                ? string.Empty
                : "상태: " + string.Join(" · ", values);
        }

        private static void AddStatus(
            ICollection<string> values,
            string label,
            int amount)
        {
            if (amount > 0) values.Add($"{label} {amount}");
        }

        private static string DescribeStatusKeyword(StatusKeyword keyword)
        {
            return keyword switch
            {
                StatusKeyword.Injury => "부상",
                StatusKeyword.Bind => "속박",
                StatusKeyword.Stun => "기절",
                StatusKeyword.Weaken => "약화",
                StatusKeyword.Vulnerable => "취약",
                _ => keyword.ToString()
            };
        }

        private void DrawMonsters(BattleRuntimeEncounterContext context)
        {
            BattleMonsterAttackActionOption[] options =
                battleActions.CreateMonsterAttackOptions(context);
            GUILayout.Label("아군 몬스터");
            GUILayout.BeginHorizontal();
            foreach (PlayerMonsterFieldPosition position in
                     Enum.GetValues(typeof(PlayerMonsterFieldPosition)))
            {
                BattleMonsterAttackActionOption option = options
                    .FirstOrDefault(value => value.Position == position);
                BattleMonsterState monster = option?.Monster;
                GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));
                if (monster == null)
                {
                    GUILayout.Label("빈 칸");
                }
                else
                {
                    GUILayout.Label(
                        $"{monster.Card.SourceCard.DisplayName}\n공격 {monster.Attack} · " +
                        $"HP {monster.CurrentHealth}/{monster.MaximumHealth}");
                    string statusText = DescribeCommonStatus(monster.Status);
                    if (!string.IsNullOrWhiteSpace(statusText))
                    {
                        GUILayout.Label(statusText, wrappedStyle);
                    }

                    bool previous = GUI.enabled;
                    GUI.enabled = option.CanAttack;
                    if (GUILayout.Button("선택한 적 공격"))
                    {
                        BattleMonsterAttackCommandResult command =
                            battleActions.TryAttack(
                                context,
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

        private void DrawHand(BattleRuntimeEncounterContext context)
        {
            BattleHandCardActionOption[] options =
                battleActions.CreateHandOptions(context);
            GUILayout.Label($"패 ({options.Length})", headingStyle);
            foreach (BattleHandCardActionOption option in options)
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
                        battleActions.CycleBanishTarget(
                            context,
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
                        battleActions.TryPlayCard(
                            context,
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

        private void DrawRecentEvents(BattleRuntimeState runtime)
        {
            IReadOnlyList<BattleEventRecord> events = runtime.EventLog.Events;
            GUILayout.Label("최근 전투 기록", headingStyle);
            if (events.Count == 0)
            {
                GUILayout.Label("기록 없음");
                return;
            }

            foreach (BattleEventRecord record in events
                         .Skip(Mathf.Max(0, events.Count - 6)))
            {
                if (record == null) continue;
                GUILayout.Label(
                    $"{record.EventType} · {record.Cause} · " +
                    $"{record.ActorId} → {record.TargetId}",
                    wrappedStyle);
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
