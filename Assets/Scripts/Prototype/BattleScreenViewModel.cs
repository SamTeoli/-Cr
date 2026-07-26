using System;
using System.Collections.Generic;
using System.Linq;

namespace HaveABreak.Cards
{
    public sealed class BattleEnemyDisplayOption
    {
        internal BattleEnemyDisplayOption(
            BattleEnemyTargetOption target,
            string intentText,
            string displayText,
            string statusText)
        {
            Target = target;
            IntentText = intentText ?? "없음";
            DisplayText = displayText ?? string.Empty;
            StatusText = statusText;
        }

        public BattleEnemyTargetOption Target { get; }
        public EnemyFieldPosition Position => Target.Position;
        public string EnemyId => Target.EnemyId;
        public bool IsOccupied => Target.IsOccupied;
        public bool IsSelected => Target.IsSelected;
        public bool CanSelect => Target.CanSelect;
        public string IntentText { get; }
        public string DisplayText { get; }
        public string StatusText { get; }
    }

    public sealed class BattleMonsterDisplayOption
    {
        internal BattleMonsterDisplayOption(
            BattleMonsterAttackActionOption action,
            string displayText,
            string statusText)
        {
            Action = action;
            DisplayText = displayText ?? string.Empty;
            StatusText = statusText;
        }

        public BattleMonsterAttackActionOption Action { get; }
        public PlayerMonsterFieldPosition Position => Action.Position;
        public string BattleCardId => Action.BattleCardId;
        public bool IsOccupied => Action.IsOccupied;
        public bool CanAttack => Action.CanAttack;
        public string BlockReason => Action.BlockReason;
        public string DisplayText { get; }
        public string StatusText { get; }
    }

    public sealed class BattleInstalledCardDisplayOption
    {
        internal BattleInstalledCardDisplayOption(
            string battleCardId,
            string displayName,
            CardType cardType,
            bool isRegisteredTrap)
        {
            BattleCardId = battleCardId;
            DisplayName = displayName ?? string.Empty;
            CardType = cardType;
            IsRegisteredTrap = isRegisteredTrap;
        }

        public string BattleCardId { get; }
        public string DisplayName { get; }
        public CardType CardType { get; }
        public bool IsRegisteredTrap { get; }
        public string DisplayText =>
            $"{DisplayName}\n{CardType}" +
            (IsRegisteredTrap ? " · 대기 중" : string.Empty);
    }

    public sealed class BattleEventDisplayOption
    {
        internal BattleEventDisplayOption(BattleEventRecord record)
        {
            Record = record;
        }

        public BattleEventRecord Record { get; }
        public string DisplayText => Record == null
            ? string.Empty
            : $"{Record.EventType} · {Record.Cause} · " +
              $"{Record.ActorId} → {Record.TargetId}";
    }

    public sealed class BattleScreenSnapshot
    {
        internal BattleScreenSnapshot(
            bool available,
            string errorText,
            string titleText,
            string playerSummaryText,
            string zoneSummaryText,
            string playerStatusText,
            string checkpointNoticeText,
            BattleOutcome outcome,
            bool sessionFinished,
            BattleConsumableActionOption[] consumables,
            BattleEnemyDisplayOption[] enemies,
            BattleMonsterDisplayOption[] monsters,
            BattleInstalledCardDisplayOption[] installedCards,
            BattleHandCardActionOption[] hand,
            BattleEventDisplayOption[] recentEvents)
        {
            Available = available;
            ErrorText = errorText;
            TitleText = titleText;
            PlayerSummaryText = playerSummaryText;
            ZoneSummaryText = zoneSummaryText;
            PlayerStatusText = playerStatusText;
            CheckpointNoticeText = checkpointNoticeText;
            Outcome = outcome;
            SessionFinished = sessionFinished;
            Consumables = consumables ??
                          Array.Empty<BattleConsumableActionOption>();
            Enemies = enemies ?? Array.Empty<BattleEnemyDisplayOption>();
            Monsters = monsters ?? Array.Empty<BattleMonsterDisplayOption>();
            InstalledCards = installedCards ??
                             Array.Empty<BattleInstalledCardDisplayOption>();
            Hand = hand ?? Array.Empty<BattleHandCardActionOption>();
            RecentEvents = recentEvents ??
                           Array.Empty<BattleEventDisplayOption>();
        }

        public bool Available { get; }
        public string ErrorText { get; }
        public string TitleText { get; }
        public string PlayerSummaryText { get; }
        public string ZoneSummaryText { get; }
        public string PlayerStatusText { get; }
        public string CheckpointNoticeText { get; }
        public BattleOutcome Outcome { get; }
        public bool SessionFinished { get; }
        public bool CanEndTurn => Available && !SessionFinished;
        public bool CanSettle => Available && SessionFinished;
        public string FinishedText => CanSettle
            ? $"전투 종료: {Outcome}. 정산을 진행하세요."
            : null;
        public BattleConsumableActionOption[] Consumables { get; }
        public BattleEnemyDisplayOption[] Enemies { get; }
        public BattleMonsterDisplayOption[] Monsters { get; }
        public BattleInstalledCardDisplayOption[] InstalledCards { get; }
        public BattleHandCardActionOption[] Hand { get; }
        public BattleEventDisplayOption[] RecentEvents { get; }
    }

    public sealed class BattleScreenViewModel
    {
        private const string CheckpointNotice =
            "전투 중 이어하기는 현재 전투의 시작 체크포인트에서 재개됩니다.";
        private readonly BattlePlayerActionViewModel actions = new();

        public void Reset()
        {
            actions.Reset();
        }

        public BattleScreenSnapshot CreateSnapshot(
            RunEncounterProgressState progress,
            RunCampaignState campaign)
        {
            BattleRuntimeEncounterContext context = progress?.ActiveEncounter;
            BattleRuntimeSessionState session = context?.Session;
            BattleRuntimeState runtime = session?.Runtime;
            if (runtime == null)
            {
                actions.Reset();
                return new BattleScreenSnapshot(
                    false,
                    "활성 전투를 찾을 수 없습니다.",
                    null,
                    null,
                    null,
                    null,
                    CheckpointNotice,
                    BattleOutcome.Ongoing,
                    false,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null);
            }

            actions.Refresh(context);
            int tieBreaker = GetTieBreaker(campaign, session);
            Dictionary<string, string> intents =
                BuildEnemyIntentLabels(context, tieBreaker);
            BattleEnemyDisplayOption[] enemies = actions
                .CreateEnemyTargets(context)
                .Select(target => CreateEnemyOption(target, intents))
                .ToArray();
            BattleMonsterDisplayOption[] monsters = actions
                .CreateMonsterAttackOptions(context)
                .Select(CreateMonsterOption)
                .ToArray();
            BattleInstalledCardDisplayOption[] installed = runtime.Deck.Zones
                .GetCards(CardZone.SkillField)
                .Where(card => card != null)
                .Select(card => new BattleInstalledCardDisplayOption(
                    card.Ids.BattleCardId,
                    card.SourceCard.DisplayName,
                    card.SourceCard.CardType,
                    runtime.TrapInstallations.Find(
                        card.Ids.BattleCardId) != null))
                .ToArray();
            IReadOnlyList<BattleEventRecord> eventLog =
                runtime.EventLog.Events;
            BattleEventDisplayOption[] recentEvents = eventLog
                .Skip(Math.Max(0, eventLog.Count - 6))
                .Where(record => record != null)
                .Select(record => new BattleEventDisplayOption(record))
                .ToArray();
            string encounterName = string.IsNullOrWhiteSpace(
                    context.Encounter?.DisplayName)
                ? "전투"
                : context.Encounter.DisplayName;
            string playerStatus = DescribeStatus(runtime.Player.Status);
            return new BattleScreenSnapshot(
                true,
                null,
                $"{encounterName} · 턴 {runtime.Turn.PlayerTurnNumber}",
                $"HP {runtime.Player.CurrentHealth}/" +
                $"{runtime.Player.MaximumHealth}   " +
                $"마력 {runtime.CardPlay.Mana.CurrentMana}/" +
                $"{runtime.CardPlay.Mana.MaximumMana}   " +
                $"단계 {runtime.Turn.Phase}   결과 {session.Outcome}",
                $"드로우 {runtime.Deck.Zones.Count(CardZone.DrawPile)} · " +
                $"묘지 {runtime.Deck.Zones.Count(CardZone.Graveyard)} · " +
                $"소멸 {runtime.Deck.Zones.Count(CardZone.Banished)} · " +
                $"설치 {runtime.Deck.Zones.Count(CardZone.SkillField)}/" +
                $"{BattleCardZoneState.MaximumSkillFieldSize}",
                string.IsNullOrWhiteSpace(playerStatus)
                    ? null
                    : $"플레이어 {playerStatus}",
                CheckpointNotice,
                session.Outcome,
                session.IsFinished,
                actions.CreateConsumableOptions(progress),
                enemies,
                monsters,
                installed,
                actions.CreateHandOptions(context),
                recentEvents);
        }

        public bool SelectEnemy(
            RunEncounterProgressState progress,
            string enemyId)
        {
            return actions.SelectEnemy(
                progress?.ActiveEncounter,
                enemyId);
        }

        public BattleBanishTargetOption CycleBanishTarget(
            RunEncounterProgressState progress,
            string sourceBattleCardId)
        {
            return actions.CycleBanishTarget(
                progress?.ActiveEncounter,
                sourceBattleCardId);
        }

        public bool SelectBanishTarget(
            RunEncounterProgressState progress,
            string sourceBattleCardId,
            string targetBattleCardId)
        {
            return actions.SelectBanishTarget(
                progress?.ActiveEncounter,
                sourceBattleCardId,
                targetBattleCardId);
        }

        public BattleConsumableCommandResult TryUseConsumable(
            RunEncounterProgressState progress,
            string itemId)
        {
            return actions.TryUseConsumable(progress, itemId);
        }

        public BattleCardPlayCommandResult TryPlayCard(
            RunEncounterProgressState progress,
            string battleCardId)
        {
            return actions.TryPlayCard(
                progress?.ActiveEncounter,
                battleCardId);
        }

        public BattleMonsterAttackCommandResult TryAttack(
            RunEncounterProgressState progress,
            string battleCardId)
        {
            return actions.TryAttack(
                progress?.ActiveEncounter,
                battleCardId);
        }

        public BattleEndTurnCommandResult TryEndPlayerTurn(
            RunEncounterProgressState progress,
            RunCampaignState campaign)
        {
            BattleRuntimeSessionState session =
                progress?.ActiveEncounter?.Session;
            int tieBreaker = GetTieBreaker(campaign, session);
            return actions.TryEndPlayerTurn(
                progress?.ActiveEncounter,
                tieBreaker);
        }

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
                DescribeStatus(monster.Status));
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
                    if (ability == null)
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
