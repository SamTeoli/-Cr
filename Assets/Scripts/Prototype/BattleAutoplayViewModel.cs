using System;
using System.Collections.Generic;
using System.Linq;

namespace HaveABreak.Cards
{
    public enum BattleAutoplayFailure
    {
        None = 0,
        InvalidState = 1,
        SnapshotUnavailable = 2,
        MissingEnemyTarget = 3,
        ConsumableActionFailed = 4,
        CardActionFailed = 5,
        AttackActionFailed = 6,
        EndTurnFailed = 7,
        Defeat = 8,
        Stalled = 9,
        SafetyLimit = 10
    }

    public sealed class BattleAutoplaySettings
    {
        public BattleAutoplaySettings(
            int maximumPlayerTurns = 60,
            int maximumCardPlaysPerTurn = 32,
            int maximumAttacksPerTurn = 16,
            int maximumConsumablesPerTurn = 8,
            int maximumStalledTurns = 5,
            bool useConsumables = true)
        {
            MaximumPlayerTurns = Math.Max(1, maximumPlayerTurns);
            MaximumCardPlaysPerTurn = Math.Max(1, maximumCardPlaysPerTurn);
            MaximumAttacksPerTurn = Math.Max(1, maximumAttacksPerTurn);
            MaximumConsumablesPerTurn = Math.Max(0, maximumConsumablesPerTurn);
            MaximumStalledTurns = Math.Max(1, maximumStalledTurns);
            UseConsumables = useConsumables;
        }

        public int MaximumPlayerTurns { get; }
        public int MaximumCardPlaysPerTurn { get; }
        public int MaximumAttacksPerTurn { get; }
        public int MaximumConsumablesPerTurn { get; }
        public int MaximumStalledTurns { get; }
        public bool UseConsumables { get; }
    }

    public sealed class BattleAutoplayCommandResult
    {
        internal BattleAutoplayCommandResult(
            bool succeeded,
            BattleAutoplayFailure failure,
            BattleOutcome outcome,
            int playerTurnsCompleted,
            int cardsPlayed,
            int attacksResolved,
            int consumablesUsed,
            int finalPlayerHealth,
            int livingEnemyCount,
            string message)
        {
            Succeeded = succeeded;
            Failure = failure;
            Outcome = outcome;
            PlayerTurnsCompleted = Math.Max(0, playerTurnsCompleted);
            CardsPlayed = Math.Max(0, cardsPlayed);
            AttacksResolved = Math.Max(0, attacksResolved);
            ConsumablesUsed = Math.Max(0, consumablesUsed);
            FinalPlayerHealth = Math.Max(0, finalPlayerHealth);
            LivingEnemyCount = Math.Max(0, livingEnemyCount);
            Message = message;
        }

        public bool Succeeded { get; }
        public BattleAutoplayFailure Failure { get; }
        public BattleOutcome Outcome { get; }
        public int PlayerTurnsCompleted { get; }
        public int CardsPlayed { get; }
        public int AttacksResolved { get; }
        public int ConsumablesUsed { get; }
        public int FinalPlayerHealth { get; }
        public int LivingEnemyCount { get; }
        public string Message { get; }
    }

    public sealed class BattleAutoplayViewModel
    {
        private static readonly BattleAutoplaySettings DefaultSettings = new();
        private readonly BattleScreenViewModel battleScreen = new();

        public BattleAutoplayCommandResult TryRun(
            RunEncounterProgressState progress,
            RunCampaignState campaign,
            BattleAutoplaySettings settings = null)
        {
            settings ??= DefaultSettings;
            BattleRuntimeEncounterContext context = progress?.ActiveEncounter;
            if (campaign == null || progress == null || context?.Session == null ||
                context.Runtime == null || campaign.Phase != RunCampaignPhase.Battle)
            {
                return Failure(
                    BattleAutoplayFailure.InvalidState,
                    progress,
                    0,
                    0,
                    0,
                    0,
                    "전투 자동 진행 실패: 활성 전투 상태가 아닙니다.");
            }

            battleScreen.Reset();
            int turnsCompleted = 0;
            int cardsPlayed = 0;
            int attacksResolved = 0;
            int consumablesUsed = 0;
            int stalledTurns = 0;
            int previousEnemyHealth = TotalLivingEnemyHealth(context);

            for (int turn = 0; turn < settings.MaximumPlayerTurns; turn++)
            {
                BattleScreenSnapshot snapshot =
                    battleScreen.CreateSnapshot(progress, campaign);
                if (snapshot == null || !snapshot.Available)
                {
                    return Failure(
                        BattleAutoplayFailure.SnapshotUnavailable,
                        progress,
                        turnsCompleted,
                        cardsPlayed,
                        attacksResolved,
                        consumablesUsed,
                        "전투 자동 진행 실패: " +
                        (snapshot?.ErrorText ?? "전투 화면 스냅샷 없음"));
                }

                if (snapshot.SessionFinished)
                {
                    return Finish(
                        snapshot.Outcome,
                        progress,
                        turnsCompleted,
                        cardsPlayed,
                        attacksResolved,
                        consumablesUsed);
                }

                BattleEnemyDisplayOption target = SelectPreferredEnemy(
                    snapshot,
                    progress);
                if (target == null)
                {
                    return Failure(
                        BattleAutoplayFailure.MissingEnemyTarget,
                        progress,
                        turnsCompleted,
                        cardsPlayed,
                        attacksResolved,
                        consumablesUsed,
                        "전투 자동 진행 실패: 선택 가능한 살아 있는 적이 없습니다.");
                }

                int actionsThisTurn = 0;
                if (settings.UseConsumables)
                {
                    BattleAutoplayCommandResult itemFailure = UseConsumables(
                        progress,
                        campaign,
                        settings.MaximumConsumablesPerTurn,
                        ref consumablesUsed,
                        ref actionsThisTurn,
                        turnsCompleted,
                        cardsPlayed,
                        attacksResolved);
                    if (itemFailure != null)
                    {
                        return itemFailure;
                    }
                }

                BattleAutoplayCommandResult cardFailure = PlayCards(
                    progress,
                    campaign,
                    settings.MaximumCardPlaysPerTurn,
                    ref cardsPlayed,
                    ref actionsThisTurn,
                    turnsCompleted,
                    attacksResolved,
                    consumablesUsed);
                if (cardFailure != null)
                {
                    return cardFailure;
                }

                BattleAutoplayCommandResult attackFailure = ResolveAttacks(
                    progress,
                    campaign,
                    settings.MaximumAttacksPerTurn,
                    ref attacksResolved,
                    ref actionsThisTurn,
                    turnsCompleted,
                    cardsPlayed,
                    consumablesUsed);
                if (attackFailure != null)
                {
                    return attackFailure;
                }

                snapshot = battleScreen.CreateSnapshot(progress, campaign);
                if (snapshot?.SessionFinished == true)
                {
                    return Finish(
                        snapshot.Outcome,
                        progress,
                        turnsCompleted,
                        cardsPlayed,
                        attacksResolved,
                        consumablesUsed);
                }

                int enemyHealthBeforeTurnEnd = TotalLivingEnemyHealth(context);
                BattleEndTurnCommandResult endTurn =
                    battleScreen.TryEndPlayerTurn(progress, campaign);
                if (endTurn == null || !endTurn.Succeeded)
                {
                    return Failure(
                        BattleAutoplayFailure.EndTurnFailed,
                        progress,
                        turnsCompleted,
                        cardsPlayed,
                        attacksResolved,
                        consumablesUsed,
                        "전투 자동 진행 실패: 턴 종료 명령 실패 · " +
                        (endTurn?.Message ?? "결과 없음"));
                }

                turnsCompleted++;
                BattleOutcome roundOutcome = endTurn.Result?.Outcome ??
                                             context.Session.Outcome;
                if (roundOutcome != BattleOutcome.Ongoing ||
                    context.Session.IsFinished)
                {
                    return Finish(
                        roundOutcome,
                        progress,
                        turnsCompleted,
                        cardsPlayed,
                        attacksResolved,
                        consumablesUsed);
                }

                int currentEnemyHealth = TotalLivingEnemyHealth(context);
                bool progressed = actionsThisTurn > 0 ||
                                  currentEnemyHealth < enemyHealthBeforeTurnEnd ||
                                  currentEnemyHealth < previousEnemyHealth;
                stalledTurns = progressed ? 0 : stalledTurns + 1;
                previousEnemyHealth = currentEnemyHealth;
                if (stalledTurns >= settings.MaximumStalledTurns)
                {
                    return Failure(
                        BattleAutoplayFailure.Stalled,
                        progress,
                        turnsCompleted,
                        cardsPlayed,
                        attacksResolved,
                        consumablesUsed,
                        $"전투 자동 진행 중 {stalledTurns}턴 연속 유효 행동이나 " +
                        "적 피해가 발생하지 않았습니다.");
                }
            }

            return Failure(
                BattleAutoplayFailure.SafetyLimit,
                progress,
                turnsCompleted,
                cardsPlayed,
                attacksResolved,
                consumablesUsed,
                $"전투 자동 진행이 플레이어 턴 한도 " +
                $"{settings.MaximumPlayerTurns}에 도달했습니다.");
        }

        private BattleAutoplayCommandResult UseConsumables(
            RunEncounterProgressState progress,
            RunCampaignState campaign,
            int limit,
            ref int consumablesUsed,
            ref int actionsThisTurn,
            int turnsCompleted,
            int cardsPlayed,
            int attacksResolved)
        {
            HashSet<string> attempted = new(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < limit; index++)
            {
                BattleScreenSnapshot snapshot =
                    battleScreen.CreateSnapshot(progress, campaign);
                if (snapshot?.SessionFinished == true)
                {
                    return null;
                }

                BattleConsumableActionOption option = snapshot?.Consumables
                    .FirstOrDefault(value => value != null && value.CanUse &&
                        !attempted.Contains(value.ItemId));
                if (option == null)
                {
                    return null;
                }

                attempted.Add(option.ItemId);
                BattleConsumableCommandResult command =
                    battleScreen.TryUseConsumable(progress, option.ItemId);
                if (command == null || !command.Succeeded)
                {
                    return Failure(
                        BattleAutoplayFailure.ConsumableActionFailed,
                        progress,
                        turnsCompleted,
                        cardsPlayed,
                        attacksResolved,
                        consumablesUsed,
                        "전투 자동 진행 실패: 소모아이템 명령 실패 · " +
                        (command?.Message ?? option.ItemId));
                }

                consumablesUsed++;
                actionsThisTurn++;
            }

            return null;
        }

        private BattleAutoplayCommandResult PlayCards(
            RunEncounterProgressState progress,
            RunCampaignState campaign,
            int limit,
            ref int cardsPlayed,
            ref int actionsThisTurn,
            int turnsCompleted,
            int attacksResolved,
            int consumablesUsed)
        {
            for (int index = 0; index < limit; index++)
            {
                BattleScreenSnapshot snapshot =
                    battleScreen.CreateSnapshot(progress, campaign);
                if (snapshot?.SessionFinished == true)
                {
                    return null;
                }

                SelectPreferredEnemy(snapshot, progress);
                BattleHandCardActionOption option = snapshot?.Hand
                    .Where(value => value != null && value.CanPlay)
                    .OrderBy(CardPriority)
                    .ThenBy(value => value.Card.Resolved.ManaCost)
                    .ThenBy(value => value.Card.SourceCard.CatalogCardId,
                        StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
                if (option == null)
                {
                    return null;
                }

                BattleCardPlayCommandResult command =
                    battleScreen.TryPlayCard(progress, option.BattleCardId);
                if (command == null || !command.Succeeded)
                {
                    return Failure(
                        BattleAutoplayFailure.CardActionFailed,
                        progress,
                        turnsCompleted,
                        cardsPlayed,
                        attacksResolved,
                        consumablesUsed,
                        "전투 자동 진행 실패: 카드 사용 명령 실패 · " +
                        (command?.Message ?? option.DisplayText));
                }

                cardsPlayed++;
                actionsThisTurn++;
            }

            return null;
        }

        private BattleAutoplayCommandResult ResolveAttacks(
            RunEncounterProgressState progress,
            RunCampaignState campaign,
            int limit,
            ref int attacksResolved,
            ref int actionsThisTurn,
            int turnsCompleted,
            int cardsPlayed,
            int consumablesUsed)
        {
            for (int index = 0; index < limit; index++)
            {
                BattleScreenSnapshot snapshot =
                    battleScreen.CreateSnapshot(progress, campaign);
                if (snapshot?.SessionFinished == true)
                {
                    return null;
                }

                BattleEnemyDisplayOption target = SelectPreferredEnemy(
                    snapshot,
                    progress);
                if (target == null)
                {
                    return null;
                }

                BattleMonsterDisplayOption attacker = snapshot.Monsters
                    .Where(value => value != null && value.CanAttack)
                    .OrderBy(value => value.Position)
                    .FirstOrDefault();
                if (attacker == null)
                {
                    return null;
                }

                BattleMonsterAttackCommandResult command =
                    battleScreen.TryAttack(progress, attacker.BattleCardId);
                if (command == null || !command.Succeeded)
                {
                    return Failure(
                        BattleAutoplayFailure.AttackActionFailed,
                        progress,
                        turnsCompleted,
                        cardsPlayed,
                        attacksResolved,
                        consumablesUsed,
                        "전투 자동 진행 실패: 몬스터 공격 명령 실패 · " +
                        (command?.Message ?? attacker.BattleCardId));
                }

                attacksResolved++;
                actionsThisTurn++;
            }

            return null;
        }

        private BattleEnemyDisplayOption SelectPreferredEnemy(
            BattleScreenSnapshot snapshot,
            RunEncounterProgressState progress)
        {
            BattleEnemyDisplayOption target = snapshot?.Enemies
                .Where(value => value?.CanSelect == true)
                .OrderBy(value => value.Target.Enemy.Vital.CurrentHealth)
                .ThenBy(value => value.Position)
                .FirstOrDefault();
            if (target != null && !target.IsSelected)
            {
                battleScreen.SelectEnemy(progress, target.EnemyId);
            }

            return target;
        }

        private static int CardPriority(BattleHandCardActionOption option)
        {
            string cardType = option?.Card?.SourceCard?.CardType.ToString() ??
                              string.Empty;
            int typePriority = cardType switch
            {
                "Monster" => 0,
                "Skill" => 1,
                "Trap" => 2,
                "Barrier" => 3,
                _ => 4
            };
            bool isC07 = string.Equals(
                option?.Card?.SourceCard?.CatalogCardId,
                TestContentIds.C07,
                StringComparison.OrdinalIgnoreCase);
            return typePriority * 10 + (isC07 ? 9 : 0);
        }

        private static int TotalLivingEnemyHealth(
            BattleRuntimeEncounterContext context)
        {
            return context?.Runtime?.Enemies
                       .Where(enemy => enemy != null && enemy.IsAlive)
                       .Sum(enemy => Math.Max(0, enemy.Vital.CurrentHealth)) ?? 0;
        }

        private static BattleAutoplayCommandResult Finish(
            BattleOutcome outcome,
            RunEncounterProgressState progress,
            int turnsCompleted,
            int cardsPlayed,
            int attacksResolved,
            int consumablesUsed)
        {
            if (outcome == BattleOutcome.Victory)
            {
                return new BattleAutoplayCommandResult(
                    true,
                    BattleAutoplayFailure.None,
                    outcome,
                    turnsCompleted,
                    cardsPlayed,
                    attacksResolved,
                    consumablesUsed,
                    PlayerHealth(progress),
                    LivingEnemyCount(progress),
                    $"전투 자동 진행 승리 · 턴 {turnsCompleted}, 카드 " +
                    $"{cardsPlayed}, 공격 {attacksResolved}, 아이템 " +
                    $"{consumablesUsed}");
            }

            return Failure(
                BattleAutoplayFailure.Defeat,
                progress,
                turnsCompleted,
                cardsPlayed,
                attacksResolved,
                consumablesUsed,
                $"전투 자동 진행 종료 · {outcome}");
        }

        private static BattleAutoplayCommandResult Failure(
            BattleAutoplayFailure failure,
            RunEncounterProgressState progress,
            int turnsCompleted,
            int cardsPlayed,
            int attacksResolved,
            int consumablesUsed,
            string message)
        {
            return new BattleAutoplayCommandResult(
                false,
                failure,
                progress?.ActiveEncounter?.Session?.Outcome ??
                    BattleOutcome.Ongoing,
                turnsCompleted,
                cardsPlayed,
                attacksResolved,
                consumablesUsed,
                PlayerHealth(progress),
                LivingEnemyCount(progress),
                message);
        }

        private static int PlayerHealth(RunEncounterProgressState progress)
        {
            return progress?.ActiveEncounter?.Runtime?.Player?.CurrentHealth ?? 0;
        }

        private static int LivingEnemyCount(RunEncounterProgressState progress)
        {
            return progress?.ActiveEncounter?.Runtime?.Enemies.Count(enemy =>
                enemy != null && enemy.IsAlive) ?? 0;
        }
    }
}
