using System;
using System.Collections.Generic;
using System.Linq;

namespace HaveABreak.Cards
{
    public sealed partial class BattleScreenViewModel
    {
        private static BattleEnemyDisplayOption CreateEnemyOption(
            BattleEnemyTargetOption target,
            IReadOnlyDictionary<string, string> intents)
        {
            if (target?.IsOccupied != true)
            {
                return new BattleEnemyDisplayOption(
                    target,
                    "없음",
                    "빈 칸",
                    null);
            }

            BattleEnemyRuntimeState enemy = target.Enemy;
            string selection = target.IsSelected ? "▶ " : string.Empty;
            string intent = intents != null &&
                            intents.TryGetValue(target.EnemyId, out string value)
                ? value
                : "없음";
            return new BattleEnemyDisplayOption(
                target,
                intent,
                $"{selection}{target.DisplayName}\n" +
                $"HP {enemy.Vital.CurrentHealth}/{target.MaximumHealth} · " +
                $"공격 {enemy.Attack}\n다음 행동: {intent}",
                DescribeStatus(target.Status));
        }

        private static BattleMonsterDisplayOption CreateMonsterOption(
            BattleMonsterAttackActionOption action)
        {
            BattleMonsterState monster = action?.Monster;
            if (monster == null)
            {
                return new BattleMonsterDisplayOption(
                    action,
                    "빈 칸",
                    null);
            }

            return new BattleMonsterDisplayOption(
                action,
                $"{monster.Card.SourceCard.DisplayName}\n" +
                $"공격 {monster.Attack} · HP {monster.CurrentHealth}/" +
                $"{monster.MaximumHealth}",
                DescribeStatus(monster.Status),
                CreateFieldCardPresentation(
                    monster.Card,
                    monster.Attack,
                    monster.CurrentHealth));
        }

        private static RuntimeCardPresentation CreateFieldCardPresentation(
            BattleCardInstance card,
            int? attackOverride = null,
            int? healthOverride = null)
        {
            if (card?.SourceCard == null)
            {
                return null;
            }

            bool isMonster = card.SourceCard.CardType == CardType.Monster;
            int? attack = isMonster
                ? attackOverride ?? card.Resolved.Attack
                : null;
            int? health = isMonster
                ? healthOverride ?? card.Resolved.Health
                : null;
            string accessibility =
                $"필드 카드 {card.SourceCard.DisplayName}, " +
                $"{card.SourceCard.CardType}, {card.SourceCard.Rarity}, " +
                $"마력 {card.Resolved.ManaCost}. {card.Resolved.RulesText}";

            return new RuntimeCardPresentation(
                card.Ids.BattleCardId,
                card.SourceCard.DisplayName,
                card.SourceCard.CatalogCardId,
                card.SourceCard.CardType,
                card.SourceCard.Rarity,
                card.Resolved.ManaCost,
                attack,
                health,
                card.Resolved.RulesText,
                false,
                0,
                true,
                null,
                accessibility,
                card.SourceCard.Artwork);
        }

        private static Dictionary<string, string> BuildEnemyIntentLabels(
            BattleRuntimeEncounterContext context,
            int tieBreaker)
        {
            Dictionary<string, List<string>> actionsByEnemy = new(
                StringComparer.OrdinalIgnoreCase);
            if (context?.Session == null ||
                !BattleRuntimeEnemyPatternService.TryCreateCommands(
                    context.Session,
                    context.Encounter,
                    tieBreaker,
                    out List<BattleRuntimeEnemyTurnCommand> commands,
                    out _))
            {
                return new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);
            }

            foreach (BattleRuntimeEnemyTurnCommand command in commands)
            {
                if (command == null ||
                    string.IsNullOrWhiteSpace(command.EnemyId))
                {
                    continue;
                }

                if (!actionsByEnemy.TryGetValue(
                        command.EnemyId,
                        out List<string> labels))
                {
                    labels = new List<string>();
                    actionsByEnemy.Add(command.EnemyId, labels);
                }

                labels.Add(DescribeEnemyCommand(command));
            }

            return actionsByEnemy.ToDictionary(
                pair => pair.Key,
                pair => string.Join(" → ", pair.Value),
                StringComparer.OrdinalIgnoreCase);
        }

        private static string DescribeEnemyCommand(
            BattleRuntimeEnemyTurnCommand command)
        {
            switch (command.ActionType)
            {
                case BattleRuntimeEnemyTurnActionType.Move:
                    string direction = command.MoveDirection ==
                        EnemyMoveDirection.Left
                        ? "왼쪽"
                        : "오른쪽";
                    return $"{direction} 이동 {command.MoveSteps}";
                case BattleRuntimeEnemyTurnActionType.Attack:
                    int count = Math.Max(1, command.AutomaticAttackCount);
                    return count == 1 ? "공격" : $"공격 ×{count}";
                case BattleRuntimeEnemyTurnActionType.Ability:
                    EnemyAbilityResolutionContext ability = command.Ability;
                    if (string.IsNullOrWhiteSpace(ability.AbilityId))
                    {
                        return "능력";
                    }
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

        private static string DescribeStatus(BattleEnemyStatusState status)
        {
            if (status == null)
            {
                return null;
            }

            List<string> values = new();
            AddStatus(values, "부상", status.Injury);
            AddStatus(values, "약화", status.Weaken);
            AddStatus(values, "취약", status.Vulnerable);
            AddStatus(values, "속박", status.Bind);
            AddStatus(values, "기절", status.Stun);
            return values.Count == 0
                ? null
                : "상태: " + string.Join(" · ", values);
        }

        private static string DescribeStatus(BattleCommonStatusState status)
        {
            if (status == null)
            {
                return null;
            }

            List<string> values = new();
            AddStatus(values, "부상", status.Injury);
            AddStatus(values, "약화", status.Weaken);
            AddStatus(values, "취약", status.Vulnerable);
            AddStatus(values, "속박", status.Bind);
            AddStatus(values, "기절", status.Stun);
            return values.Count == 0
                ? null
                : "상태: " + string.Join(" · ", values);
        }

        private static void AddStatus(
            ICollection<string> values,
            string label,
            int amount)
        {
            if (amount > 0)
            {
                values.Add($"{label} {amount}");
            }
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

        private static int GetTieBreaker(
            RunCampaignState campaign,
            BattleRuntimeSessionState session)
        {
            int seed = campaign?.Seed ?? 0;
            int completedRounds = session?.CompletedRoundCount ?? 0;
            return seed + completedRounds * 10;
        }
    }
}
