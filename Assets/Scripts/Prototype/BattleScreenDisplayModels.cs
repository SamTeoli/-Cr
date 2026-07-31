using System;

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
            string statusText,
            RuntimeCardPresentation cardPresentation = null)
        {
            Action = action;
            DisplayText = displayText ?? string.Empty;
            StatusText = statusText;
            CardPresentation = cardPresentation;
        }

        public BattleMonsterAttackActionOption Action { get; }
        public PlayerMonsterFieldPosition Position => Action.Position;
        public string BattleCardId => Action.BattleCardId;
        public bool IsOccupied => Action.IsOccupied;
        public bool CanAttack => Action.CanAttack;
        public string BlockReason => Action.BlockReason;
        public string DisplayText { get; }
        public string StatusText { get; }
        public RuntimeCardPresentation CardPresentation { get; }
    }

    public sealed class BattleInstalledCardDisplayOption
    {
        internal BattleInstalledCardDisplayOption(
            string battleCardId,
            string displayName,
            CardType cardType,
            bool isRegisteredTrap,
            PlayerMonsterFieldPosition position,
            RuntimeCardPresentation cardPresentation = null)
        {
            BattleCardId = battleCardId;
            DisplayName = displayName ?? string.Empty;
            CardType = cardType;
            IsRegisteredTrap = isRegisteredTrap;
            Position = position;
            CardPresentation = cardPresentation;
        }

        public string BattleCardId { get; }
        public string DisplayName { get; }
        public CardType CardType { get; }
        public bool IsRegisteredTrap { get; }
        public PlayerMonsterFieldPosition Position { get; }
        public RuntimeCardPresentation CardPresentation { get; }
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

    public sealed class BattleChainDisplayOption
    {
        internal BattleChainDisplayOption(
            BattleChainPhase phase,
            BattleChainParticipant nextParticipant,
            BattleChainLink[] links)
        {
            Phase = phase;
            NextParticipant = nextParticipant;
            Links = links ?? Array.Empty<BattleChainLink>();
        }

        public BattleChainPhase Phase { get; }
        public BattleChainParticipant NextParticipant { get; }
        public BattleChainLink[] Links { get; }
        public bool IsActive => Phase != BattleChainPhase.Idle;
        public bool CanPlayerPass =>
            Phase == BattleChainPhase.Building &&
            NextParticipant == BattleChainParticipant.Player;
        public string DisplayText
        {
            get
            {
                if (!IsActive)
                {
                    return null;
                }

                string links = string.Join(
                    " → ",
                    Array.ConvertAll(
                        Links,
                        link => $"체인 {link.LinkIndex} " +
                                $"{link.Activation.EffectId}"));
                string turn = Phase == BattleChainPhase.Building
                    ? $"다음 응답: {NextParticipant}"
                    : "역순 해결 중";
                return $"{links}\n{turn}";
            }
        }
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
            BattleEventDisplayOption[] recentEvents,
            bool selectingEnemyTarget = false,
            string pendingTargetSourceId = null,
            BattleChainDisplayOption chain = null)
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
            SelectingEnemyTarget = selectingEnemyTarget;
            PendingTargetSourceId = pendingTargetSourceId;
            Chain = chain ?? new BattleChainDisplayOption(
                BattleChainPhase.Idle,
                BattleChainParticipant.Player,
                null);
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
        public bool CanEndTurn =>
            Available && !SessionFinished &&
            !SelectingEnemyTarget && !Chain.IsActive;
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
        public bool SelectingEnemyTarget { get; }
        public string PendingTargetSourceId { get; }
        public BattleChainDisplayOption Chain { get; }
    }
}
