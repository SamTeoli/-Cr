using System;
using System.Collections.Generic;
using System.Linq;

namespace HaveABreak.Cards
{
    public sealed class BattleEnemyTargetOption
    {
        internal BattleEnemyTargetOption(
            EnemyFieldPosition position,
            string enemyId,
            BattleEnemyRuntimeState enemy,
            BattleEnemyStatusState status,
            string displayName,
            int maximumHealth,
            bool isSelected)
        {
            Position = position;
            EnemyId = enemyId;
            Enemy = enemy;
            Status = status;
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? enemyId
                : displayName;
            MaximumHealth = Math.Max(0, maximumHealth);
            IsSelected = isSelected;
        }

        public EnemyFieldPosition Position { get; }
        public string EnemyId { get; }
        public BattleEnemyRuntimeState Enemy { get; }
        public BattleEnemyStatusState Status { get; }
        public string DisplayName { get; }
        public int MaximumHealth { get; }
        public bool IsSelected { get; }
        public bool IsOccupied => Enemy != null &&
                                  !string.IsNullOrWhiteSpace(EnemyId);
        public bool CanSelect => IsOccupied && Enemy.IsAlive;
    }

    public sealed class BattleConsumableActionOption
    {
        internal BattleConsumableActionOption(
            ConsumableData item,
            int ownedCount,
            int consumedCount,
            bool sessionFinished)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));
            OwnedCount = Math.Max(0, ownedCount);
            ConsumedCount = Math.Max(0, consumedCount);
            SessionFinished = sessionFinished;
        }

        public ConsumableData Item { get; }
        public string ItemId => Item.ItemId;
        public string DisplayName => Item.DisplayName;
        public int OwnedCount { get; }
        public int ConsumedCount { get; }
        public int RemainingCount => Math.Max(0, OwnedCount - ConsumedCount);
        public bool SessionFinished { get; }
        public bool CanUse => !SessionFinished && RemainingCount > 0;
        public string DisplayLabel => $"{DisplayName} ×{RemainingCount}";
        public string BlockReason => SessionFinished
            ? "종료된 전투에서는 사용할 수 없음"
            : RemainingCount <= 0
                ? "남은 수량 없음"
                : null;
    }

    public sealed class BattleBanishTargetOption
    {
        internal BattleBanishTargetOption(
            BattleCardInstance source,
            BattleCardInstance target,
            bool isSelected)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Target = target ?? throw new ArgumentNullException(nameof(target));
            IsSelected = isSelected;
        }

        public BattleCardInstance Source { get; }
        public BattleCardInstance Target { get; }
        public string SourceBattleCardId => Source.Ids.BattleCardId;
        public string BattleCardId => Target.Ids.BattleCardId;
        public string DisplayName => Target.SourceCard.DisplayName;
        public bool IsSelected { get; }
        public string DisplayLabel => $"소멸: {DisplayName}";
    }

    public sealed class BattleHandCardActionOption
    {
        internal BattleHandCardActionOption(
            BattleCardInstance card,
            BattleBanishTargetOption[] banishTargets,
            bool canPlay,
            BattleRuntimePlayerCardActionFailure actionFailure,
            BattleRuntimeCardPlayFailure playFailure,
            CardPlayFailure cardFailure,
            string blockReason)
        {
            Card = card ?? throw new ArgumentNullException(nameof(card));
            BanishTargets = banishTargets ??
                            Array.Empty<BattleBanishTargetOption>();
            CanPlay = canPlay;
            ActionFailure = actionFailure;
            PlayFailure = playFailure;
            CardFailure = cardFailure;
            BlockReason = blockReason;
        }

        public BattleCardInstance Card { get; }
        public string BattleCardId => Card.Ids.BattleCardId;
        public BattleBanishTargetOption[] BanishTargets { get; }
        public BattleBanishTargetOption SelectedBanishTarget =>
            BanishTargets.FirstOrDefault(option => option.IsSelected);
        public string SelectedBanishTargetId =>
            SelectedBanishTarget?.BattleCardId;
        public bool CanPlay { get; }
        public BattleRuntimePlayerCardActionFailure ActionFailure { get; }
        public BattleRuntimeCardPlayFailure PlayFailure { get; }
        public CardPlayFailure CardFailure { get; }
        public string BlockReason { get; }
        public string DisplayText =>
            $"{Card.SourceCard.CatalogCardId} {Card.SourceCard.DisplayName} · " +
            $"비용 {Card.Resolved.ManaCost}\n{Card.Resolved.RulesText}";
    }

    public sealed class BattleMonsterAttackActionOption
    {
        internal BattleMonsterAttackActionOption(
            PlayerMonsterFieldPosition position,
            BattleMonsterState monster,
            string selectedEnemyId,
            bool sessionFinished)
        {
            Position = position;
            Monster = monster;
            SelectedEnemyId = selectedEnemyId;
            SessionFinished = sessionFinished;
        }

        public PlayerMonsterFieldPosition Position { get; }
        public BattleMonsterState Monster { get; }
        public string BattleCardId => Monster?.BattleCardId;
        public string SelectedEnemyId { get; }
        public bool SessionFinished { get; }
        public bool IsOccupied => Monster != null;
        public bool CanAttack => Monster?.Status?.CanAttack == true &&
                                 !SessionFinished &&
                                 !string.IsNullOrWhiteSpace(SelectedEnemyId);
        public string BlockReason => !IsOccupied
            ? null
            : SessionFinished
                ? "종료된 전투에서는 공격할 수 없음"
                : string.IsNullOrWhiteSpace(SelectedEnemyId)
                    ? "적 대상 필요"
                    : Monster.Status?.CanAttack != true
                        ? "속박·기절로 공격 불가"
                        : null;
    }

    public sealed class BattleConsumableCommandResult
    {
        internal BattleConsumableCommandResult(
            bool succeeded,
            BattleConsumableActionOption option,
            int appliedAmount,
            PrototypeConsumableFailure failure,
            string message)
        {
            Succeeded = succeeded;
            Option = option;
            AppliedAmount = appliedAmount;
            Failure = failure;
            Message = message;
        }

        public bool Succeeded { get; }
        public BattleConsumableActionOption Option { get; }
        public int AppliedAmount { get; }
        public PrototypeConsumableFailure Failure { get; }
        public string Message { get; }
    }

    public sealed class BattleCardPlayCommandResult
    {
        internal BattleCardPlayCommandResult(
            bool succeeded,
            BattleHandCardActionOption option,
            BattleRuntimePlayerCardActionResult result,
            BattleRuntimePlayerCardActionFailure actionFailure,
            BattleRuntimeCardPlayFailure playFailure,
            CardPlayFailure cardFailure,
            BattleOutcome outcome,
            string message)
        {
            Succeeded = succeeded;
            Option = option;
            Result = result;
            ActionFailure = actionFailure;
            PlayFailure = playFailure;
            CardFailure = cardFailure;
            Outcome = outcome;
            Message = message;
        }

        public bool Succeeded { get; }
        public BattleHandCardActionOption Option { get; }
        public BattleRuntimePlayerCardActionResult Result { get; }
        public BattleRuntimePlayerCardActionFailure ActionFailure { get; }
        public BattleRuntimeCardPlayFailure PlayFailure { get; }
        public CardPlayFailure CardFailure { get; }
        public BattleOutcome Outcome { get; }
        public string Message { get; }
    }

    public sealed class BattleMonsterAttackCommandResult
    {
        internal BattleMonsterAttackCommandResult(
            bool succeeded,
            BattleMonsterAttackActionOption option,
            BattleRuntimePlayerAttackResult result,
            BattleRuntimePlayerAttackFailure failure,
            BattleOutcome outcome,
            string message)
        {
            Succeeded = succeeded;
            Option = option;
            Result = result;
            Failure = failure;
            Outcome = outcome;
            Message = message;
        }

        public bool Succeeded { get; }
        public BattleMonsterAttackActionOption Option { get; }
        public BattleRuntimePlayerAttackResult Result { get; }
        public BattleRuntimePlayerAttackFailure Failure { get; }
        public BattleOutcome Outcome { get; }
        public string Message { get; }
    }

    public sealed class BattleEndTurnCommandResult
    {
        internal BattleEndTurnCommandResult(
            bool succeeded,
            BattleRuntimeSessionRoundResult result,
            BattleRuntimeEnemyPatternFailure patternFailure,
            BattleRuntimeSessionFailure sessionFailure,
            BattleRuntimeRoundFailure roundFailure,
            BattleTurnFailure turnFailure,
            BattleRuntimeEnemyTurnPipelineFailure pipelineFailure,
            BattleRuntimeEnemyTurnPlanFailure planFailure,
            BattleRuntimeEnemyTurnFailure enemyTurnFailure,
            int actionIndex,
            string message)
        {
            Succeeded = succeeded;
            Result = result;
            PatternFailure = patternFailure;
            SessionFailure = sessionFailure;
            RoundFailure = roundFailure;
            TurnFailure = turnFailure;
            PipelineFailure = pipelineFailure;
            PlanFailure = planFailure;
            EnemyTurnFailure = enemyTurnFailure;
            ActionIndex = actionIndex;
            Message = message;
        }

        public bool Succeeded { get; }
        public BattleRuntimeSessionRoundResult Result { get; }
        public BattleRuntimeEnemyPatternFailure PatternFailure { get; }
        public BattleRuntimeSessionFailure SessionFailure { get; }
        public BattleRuntimeRoundFailure RoundFailure { get; }
        public BattleTurnFailure TurnFailure { get; }
        public BattleRuntimeEnemyTurnPipelineFailure PipelineFailure { get; }
        public BattleRuntimeEnemyTurnPlanFailure PlanFailure { get; }
        public BattleRuntimeEnemyTurnFailure EnemyTurnFailure { get; }
        public int ActionIndex { get; }
        public string Message { get; }
    }

    public sealed class BattlePlayerActionViewModel
    {
        private readonly Dictionary<string, string> selectedBanishCardIds =
            new(StringComparer.OrdinalIgnoreCase);
        private string selectedEnemyId;

        public string SelectedEnemyId => selectedEnemyId;

        public void Reset()
        {
            selectedEnemyId = null;
            selectedBanishCardIds.Clear();
        }

        public void Refresh(BattleRuntimeEncounterContext context)
        {
            BattleRuntimeState runtime = context?.Runtime;
            if (runtime == null)
            {
                Reset();
                return;
            }

            if (string.IsNullOrWhiteSpace(selectedEnemyId) ||
                !runtime.LivingEnemies.Contains(selectedEnemyId))
            {
                selectedEnemyId = runtime.Enemies.FirstOrDefault(enemy =>
                    enemy != null && enemy.IsAlive)?.EnemyId;
            }

            HashSet<string> handIds = runtime.Deck.Zones
                .GetCards(CardZone.Hand)
                .Where(card => card != null)
                .Select(card => card.Ids.BattleCardId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (string sourceId in selectedBanishCardIds.Keys.ToArray())
            {
                string targetId = selectedBanishCardIds[sourceId];
                if (!handIds.Contains(sourceId) ||
                    !handIds.Contains(targetId) ||
                    string.Equals(
                        sourceId,
                        targetId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    selectedBanishCardIds.Remove(sourceId);
                }
            }
        }

        public BattleEnemyTargetOption[] CreateEnemyTargets(
            BattleRuntimeEncounterContext context)
        {
            if (context?.Runtime == null)
            {
                return Array.Empty<BattleEnemyTargetOption>();
            }

            Refresh(context);
            List<BattleEnemyTargetOption> options = new();
            foreach (EnemyFieldPosition position in
                     Enum.GetValues(typeof(EnemyFieldPosition)))
            {
                string enemyId =
                    context.Runtime.EnemyPositions.GetOccupant(position);
                BattleEnemyRuntimeState enemy =
                    string.IsNullOrWhiteSpace(enemyId)
                        ? null
                        : context.Runtime.FindEnemy(enemyId);
                BattleEnemyStatusState status =
                    string.IsNullOrWhiteSpace(enemyId)
                        ? null
                        : context.Runtime.EnemyStatuses.Find(enemyId);
                EncounterEnemySlot slot = context.Encounter?.EnemySlots
                    .FirstOrDefault(value => value != null &&
                        string.Equals(
                            value.EnemyInstanceId,
                            enemyId,
                            StringComparison.OrdinalIgnoreCase));
                string displayName = slot?.Enemy?.DisplayName ?? enemyId;
                int maximumHealth = slot?.Enemy?.MaximumHealth ??
                                    (enemy == null
                                        ? 0
                                        : enemy.Vital.CurrentHealth);
                options.Add(new BattleEnemyTargetOption(
                    position,
                    enemyId,
                    enemy,
                    status,
                    displayName,
                    maximumHealth,
                    !string.IsNullOrWhiteSpace(enemyId) &&
                    string.Equals(
                        selectedEnemyId,
                        enemyId,
                        StringComparison.OrdinalIgnoreCase)));
            }

            return options.ToArray();
        }

        public bool SelectEnemy(
            BattleRuntimeEncounterContext context,
            string enemyId)
        {
            BattleRuntimeState runtime = context?.Runtime;
            if (runtime == null || string.IsNullOrWhiteSpace(enemyId))
            {
                return false;
            }

            string normalized = enemyId.Trim();
            if (!runtime.LivingEnemies.Contains(normalized))
            {
                return false;
            }

            selectedEnemyId = runtime.Enemies.FirstOrDefault(enemy =>
                enemy != null && enemy.IsAlive &&
                string.Equals(
                    enemy.EnemyId,
                    normalized,
                    StringComparison.OrdinalIgnoreCase))?.EnemyId;
            return !string.IsNullOrWhiteSpace(selectedEnemyId);
        }

        public BattleConsumableActionOption[] CreateConsumableOptions(
            RunEncounterProgressState progress)
        {
            BattleRuntimeEncounterContext context = progress?.ActiveEncounter;
            IReadOnlyList<string> itemIds = progress?.RunState?.ConsumableItemIds;
            if (context?.Session == null || itemIds == null || itemIds.Count == 0)
            {
                return Array.Empty<BattleConsumableActionOption>();
            }

            List<BattleConsumableActionOption> options = new();
            HashSet<string> added = new(StringComparer.OrdinalIgnoreCase);
            foreach (string rawId in itemIds)
            {
                if (string.IsNullOrWhiteSpace(rawId))
                {
                    continue;
                }

                string itemId = rawId.Trim();
                if (!added.Add(itemId))
                {
                    continue;
                }

                ConsumableData item = PrototypeConsumableCatalog.Find(itemId);
                if (item == null ||
                    item.Effect == ConsumableEffect.IncreaseEnchantSlot ||
                    item.Effect == ConsumableEffect.ReplaceEnchant)
                {
                    continue;
                }

                int owned = itemIds.Count(value => string.Equals(
                    value,
                    itemId,
                    StringComparison.OrdinalIgnoreCase));
                int consumed = context.RunChanges.ConsumedItemIds.Count(value =>
                    string.Equals(
                        value,
                        itemId,
                        StringComparison.OrdinalIgnoreCase));
                options.Add(new BattleConsumableActionOption(
                    item,
                    owned,
                    consumed,
                    context.Session.IsFinished));
            }

            return options.ToArray();
        }

        public BattleConsumableCommandResult TryUseConsumable(
            RunEncounterProgressState progress,
            string itemId)
        {
            BattleConsumableActionOption option = CreateConsumableOptions(progress)
                .FirstOrDefault(value => string.Equals(
                    value.ItemId,
                    itemId,
                    StringComparison.OrdinalIgnoreCase));
            if (option == null || !option.CanUse)
            {
                string reason = option?.BlockReason ??
                                "현재 전투에서 사용할 수 없는 아이템입니다.";
                return new BattleConsumableCommandResult(
                    false,
                    option,
                    0,
                    default,
                    $"아이템 사용 실패: {reason}");
            }

            if (!PrototypeConsumableService.TryUseInBattle(
                    progress.ActiveEncounter,
                    option.ItemId,
                    out int applied,
                    out PrototypeConsumableFailure failure))
            {
                return new BattleConsumableCommandResult(
                    false,
                    option,
                    0,
                    failure,
                    $"아이템 사용 실패: {failure}");
            }

            return new BattleConsumableCommandResult(
                true,
                option,
                applied,
                failure,
                $"{option.DisplayName} 사용 · 적용량 {applied}");
        }

        public BattleBanishTargetOption[] CreateBanishTargets(
            BattleRuntimeEncounterContext context,
            string sourceBattleCardId)
        {
            if (context?.Runtime == null ||
                string.IsNullOrWhiteSpace(sourceBattleCardId))
            {
                return Array.Empty<BattleBanishTargetOption>();
            }

            Refresh(context);
            List<BattleCardInstance> hand = context.Runtime.Deck.Zones
                .GetCards(CardZone.Hand)
                .Where(card => card != null)
                .ToList();
            BattleCardInstance source = hand.FirstOrDefault(card =>
                string.Equals(
                    card.Ids.BattleCardId,
                    sourceBattleCardId,
                    StringComparison.OrdinalIgnoreCase));
            if (source == null || !string.Equals(
                    source.SourceCard.CatalogCardId,
                    TestContentIds.C07,
                    StringComparison.OrdinalIgnoreCase))
            {
                selectedBanishCardIds.Remove(sourceBattleCardId);
                return Array.Empty<BattleBanishTargetOption>();
            }

            BattleCardInstance[] candidates = hand
                .Where(card => !string.Equals(
                    card.Ids.BattleCardId,
                    source.Ids.BattleCardId,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (candidates.Length == 0)
            {
                selectedBanishCardIds.Remove(source.Ids.BattleCardId);
                return Array.Empty<BattleBanishTargetOption>();
            }

            string selectedId = selectedBanishCardIds.TryGetValue(
                    source.Ids.BattleCardId,
                    out string current)
                ? current
                : null;
            BattleCardInstance selected = candidates.FirstOrDefault(card =>
                string.Equals(
                    card.Ids.BattleCardId,
                    selectedId,
                    StringComparison.OrdinalIgnoreCase)) ?? candidates[0];
            selectedBanishCardIds[source.Ids.BattleCardId] =
                selected.Ids.BattleCardId;
            return candidates.Select(card => new BattleBanishTargetOption(
                    source,
                    card,
                    string.Equals(
                        card.Ids.BattleCardId,
                        selected.Ids.BattleCardId,
                        StringComparison.OrdinalIgnoreCase)))
                .ToArray();
        }

        public bool SelectBanishTarget(
            BattleRuntimeEncounterContext context,
            string sourceBattleCardId,
            string targetBattleCardId)
        {
            if (string.IsNullOrWhiteSpace(targetBattleCardId))
            {
                return false;
            }

            BattleBanishTargetOption target = CreateBanishTargets(
                    context,
                    sourceBattleCardId)
                .FirstOrDefault(option => string.Equals(
                    option.BattleCardId,
                    targetBattleCardId.Trim(),
                    StringComparison.OrdinalIgnoreCase));
            if (target == null)
            {
                return false;
            }

            selectedBanishCardIds[target.SourceBattleCardId] =
                target.BattleCardId;
            return true;
        }

        public BattleBanishTargetOption CycleBanishTarget(
            BattleRuntimeEncounterContext context,
            string sourceBattleCardId)
        {
            BattleBanishTargetOption[] options = CreateBanishTargets(
                context,
                sourceBattleCardId);
            if (options.Length == 0)
            {
                return null;
            }

            int selectedIndex = Array.FindIndex(
                options,
                option => option.IsSelected);
            int nextIndex = (selectedIndex + 1 + options.Length) % options.Length;
            BattleBanishTargetOption next = options[nextIndex];
            selectedBanishCardIds[next.SourceBattleCardId] = next.BattleCardId;
            return new BattleBanishTargetOption(
                next.Source,
                next.Target,
                true);
        }

        public BattleHandCardActionOption[] CreateHandOptions(
            BattleRuntimeEncounterContext context)
        {
            if (context?.Runtime == null)
            {
                return Array.Empty<BattleHandCardActionOption>();
            }

            Refresh(context);
            List<BattleCardInstance> hand = context.Runtime.Deck.Zones
                .GetCards(CardZone.Hand)
                .Where(card => card != null)
                .ToList();
            List<BattleHandCardActionOption> options = new();
            foreach (BattleCardInstance card in hand)
            {
                BattleBanishTargetOption[] banishTargets =
                    CreateBanishTargets(context, card.Ids.BattleCardId);
                string banishTargetId = banishTargets
                    .FirstOrDefault(option => option.IsSelected)?
                    .BattleCardId;
                bool canPlay = BattleRuntimePlayerCardActionService.TryValidate(
                    context.Runtime,
                    card.Ids.BattleCardId,
                    selectedEnemyId,
                    banishTargetId,
                    out BattleRuntimePlayerCardActionFailure actionFailure,
                    out BattleRuntimeCardPlayFailure playFailure,
                    out CardPlayFailure cardFailure);
                options.Add(new BattleHandCardActionOption(
                    card,
                    banishTargets,
                    canPlay,
                    actionFailure,
                    playFailure,
                    cardFailure,
                    canPlay
                        ? null
                        : DescribeCardBlock(
                            actionFailure,
                            playFailure,
                            cardFailure)));
            }

            return options.ToArray();
        }

        public BattleCardPlayCommandResult TryPlayCard(
    BattleRuntimeEncounterContext context,
    string battleCardId)
{
    return TryPlayCard(context, battleCardId, null);
}

public BattleCardPlayCommandResult TryPlayCard(
    BattleRuntimeEncounterContext context,
    string battleCardId,
    PlayerMonsterFieldPosition? monsterPosition)
{
    BattleHandCardActionOption option = CreateHandOptions(context)
        .FirstOrDefault(value => string.Equals(
            value.BattleCardId,
            battleCardId,
            StringComparison.OrdinalIgnoreCase));
    if (option == null)
    {
        return new BattleCardPlayCommandResult(
            false, null, null, default, default, default,
            context?.Session?.Outcome ?? BattleOutcome.Ongoing,
            "카드 사용 실패: 선택한 카드를 현재 패에서 찾을 수 없습니다.");
    }
    if (!option.CanPlay)
    {
        return new BattleCardPlayCommandResult(
            false, option, null,
            option.ActionFailure, option.PlayFailure, option.CardFailure,
            context?.Session?.Outcome ?? BattleOutcome.Ongoing,
            $"카드 사용 실패: {option.BlockReason}");
    }
    if (!BattleRuntimePlayerCardActionService.TryResolve(
            context.Runtime,
            option.BattleCardId,
            selectedEnemyId,
            option.SelectedBanishTargetId,
            monsterPosition,
            out BattleRuntimePlayerCardActionResult result,
            out BattleRuntimePlayerCardActionFailure actionFailure,
            out BattleRuntimeCardPlayFailure playFailure,
            out CardPlayFailure cardFailure))
    {
        string positionText = monsterPosition.HasValue
            ? $" / 몬스터존 {monsterPosition.Value}"
            : string.Empty;
        return new BattleCardPlayCommandResult(
            false, option, null,
            actionFailure, playFailure, cardFailure,
            context?.Session?.Outcome ?? BattleOutcome.Ongoing,
            $"카드 사용 실패{positionText}: {actionFailure} / " +
            $"{playFailure} / {cardFailure}");
    }
    selectedBanishCardIds.Remove(option.BattleCardId);
    BattleOutcome outcome = FinalizeOutcome(context);
    Refresh(context);
    string message = monsterPosition.HasValue
        ? $"{result.Play.Card.SourceCard.DisplayName} 사용 완료 · " +
          $"몬스터존 {monsterPosition.Value}"
        : $"{result.Play.Card.SourceCard.DisplayName} 사용 완료.";
    if (outcome != BattleOutcome.Ongoing)
    {
        message += $" 전투 종료 · {outcome}";
    }
    return new BattleCardPlayCommandResult(
        true, option, result,
        actionFailure, playFailure, cardFailure,
        outcome, message);
}

        public BattleMonsterAttackActionOption[] CreateMonsterAttackOptions(
            BattleRuntimeEncounterContext context)
        {
            if (context?.Runtime == null)
            {
                return Array.Empty<BattleMonsterAttackActionOption>();
            }

            Refresh(context);
            List<BattleMonsterAttackActionOption> options = new();
            foreach (PlayerMonsterFieldPosition position in
                     Enum.GetValues(typeof(PlayerMonsterFieldPosition)))
            {
                string battleCardId =
                    context.Runtime.PlayerMonsterPositions.GetOccupant(position);
                BattleMonsterState monster =
                    string.IsNullOrWhiteSpace(battleCardId)
                        ? null
                        : context.Runtime.Monsters.Find(battleCardId);
                options.Add(new BattleMonsterAttackActionOption(
                    position,
                    monster,
                    selectedEnemyId,
                    context.Session?.IsFinished == true));
            }

            return options.ToArray();
        }

        public BattleMonsterAttackCommandResult TryAttack(
            BattleRuntimeEncounterContext context,
            string battleCardId)
        {
            BattleMonsterAttackActionOption option =
                CreateMonsterAttackOptions(context)
                    .FirstOrDefault(value => string.Equals(
                        value.BattleCardId,
                        battleCardId,
                        StringComparison.OrdinalIgnoreCase));
            if (option == null || !option.CanAttack)
            {
                string reason = option?.BlockReason ??
                                "공격할 아군 몬스터를 찾을 수 없습니다.";
                return new BattleMonsterAttackCommandResult(
                    false,
                    option,
                    null,
                    default,
                    context?.Session?.Outcome ?? BattleOutcome.Ongoing,
                    $"공격 실패: {reason}");
            }

            if (!BattleRuntimePlayerAttackService.TryResolve(
                    context.Runtime,
                    option.BattleCardId,
                    selectedEnemyId,
                    out BattleRuntimePlayerAttackResult result,
                    out BattleRuntimePlayerAttackFailure failure))
            {
                return new BattleMonsterAttackCommandResult(
                    false,
                    option,
                    null,
                    failure,
                    context.Session.Outcome,
                    $"공격 실패: {failure}");
            }

            BattleOutcome outcome = FinalizeOutcome(context);
            Refresh(context);
            string message = $"공격 완료 · 피해 {result.DamageApplied}";
            if (outcome != BattleOutcome.Ongoing)
            {
                message += $" 전투 종료 · {outcome}";
            }

            return new BattleMonsterAttackCommandResult(
                true,
                option,
                result,
                failure,
                outcome,
                message);
        }

        public BattleEndTurnCommandResult TryEndPlayerTurn(
            BattleRuntimeEncounterContext context,
            int tieBreaker)
        {
            if (context?.Session == null || context.Runtime == null ||
                context.Session.IsFinished)
            {
                return new BattleEndTurnCommandResult(
                    false,
                    null,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    -1,
                    "턴 종료 실패: 활성 플레이어 턴이 없습니다.");
            }

            if (!BattleRuntimeEnemyPatternService.TryEndPlayerTurn(
                    context.Session,
                    context.Encounter,
                    tieBreaker,
                    out BattleRuntimeSessionRoundResult result,
                    out BattleRuntimeEnemyPatternFailure patternFailure,
                    out BattleRuntimeSessionFailure sessionFailure,
                    out BattleRuntimeRoundFailure roundFailure,
                    out BattleTurnFailure turnFailure,
                    out BattleRuntimeEnemyTurnPipelineFailure pipelineFailure,
                    out BattleRuntimeEnemyTurnPlanFailure planFailure,
                    out BattleRuntimeEnemyTurnFailure enemyTurnFailure,
                    out int actionIndex))
            {
                return new BattleEndTurnCommandResult(
                    false,
                    null,
                    patternFailure,
                    sessionFailure,
                    roundFailure,
                    turnFailure,
                    pipelineFailure,
                    planFailure,
                    enemyTurnFailure,
                    actionIndex,
                    $"턴 종료 실패: {patternFailure} / {sessionFailure} / " +
                    $"{roundFailure} / {turnFailure} / {pipelineFailure} / " +
                    $"{planFailure} / {enemyTurnFailure} / action {actionIndex}");
            }

            selectedBanishCardIds.Clear();
            Refresh(context);
            string message = result.Outcome == BattleOutcome.Ongoing
                ? $"적 턴 완료 · 플레이어 턴 " +
                  $"{context.Runtime.Turn.PlayerTurnNumber}"
                : $"전투 종료 · {result.Outcome}";
            return new BattleEndTurnCommandResult(
                true,
                result,
                patternFailure,
                sessionFailure,
                roundFailure,
                turnFailure,
                pipelineFailure,
                planFailure,
                enemyTurnFailure,
                actionIndex,
                message);
        }

        private static BattleOutcome FinalizeOutcome(
            BattleRuntimeEncounterContext context)
        {
            BattleRuntimeSessionState session = context?.Session;
            if (session == null)
            {
                return BattleOutcome.Ongoing;
            }

            if (!session.IsFinished &&
                BattleRuntimeSessionService.TryFinalizeTerminalOutcome(
                    session,
                    out BattleOutcome outcome,
                    out _))
            {
                return outcome;
            }

            return session.Outcome;
        }

        private static string DescribeCardBlock(
            BattleRuntimePlayerCardActionFailure actionFailure,
            BattleRuntimeCardPlayFailure playFailure,
            CardPlayFailure cardFailure)
        {
            if (actionFailure ==
                BattleRuntimePlayerCardActionFailure.MissingTarget)
            {
                return "적 대상 필요";
            }

            if (actionFailure ==
                BattleRuntimePlayerCardActionFailure.InvalidBanishSelection)
            {
                return "소멸 대상 필요";
            }

            return cardFailure switch
            {
                CardPlayFailure.NotEnoughMana => "마력 부족",
                CardPlayFailure.DestinationFull => "필드 포화",
                CardPlayFailure.DuplicateBarrier => "동일 결계 설치됨",
                _ when playFailure ==
                    BattleRuntimeCardPlayFailure.InvalidTurnPhase =>
                    "행동 불가 단계",
                _ => actionFailure.ToString()
            };
        }
    }
}
