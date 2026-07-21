using System;
using System.Collections.Generic;

namespace HaveABreak.Cards
{
    // Disposable C01-C12 test-content module. Replace this file as a unit
    // when the validated card set is retired; the generic battle runtime
    // does not depend on one resolver file per card.
    public static class TestContentIds
    {
        public const string C01 = "C01";
        public const string C02 = "C02";
        public const string C03 = "C03";
        public const string C04 = "C04";
        public const string C05 = "C05";
        public const string C06 = "C06";
        public const string C07 = "C07";
        public const string C08 = "C08";
        public const string C09 = "C09";
        public const string C10 = "C10";
        public const string C11 = "C11";
        public const string C12 = "C12";

        public const string E01 = "E01";
        public const string E02 = "E02";
        public const string E03 = "E03";
        public const string E04 = "E04";
        public const string E05 = "E05";
        public const string E06 = "E06";
        public const string E07 = "E07";
        public const string E08 = "E08";
    }

    public static class C01SleeperKeeperResolver
    {
        private const string EffectId = "C01-SUMMON";

        public static bool TryResolve(
            BattleEventRecord summonedEvent,
            BattleMonsterState sourceMonster,
            EnchantFixedTargetDeclaration targetDeclaration,
            BattleEnemyPositionState enemyPositions,
            BattleEnemyMovementLockState movementLocks,
            BattleEventLog eventLog,
            BattleEffectResolutionTracker resolutions,
            out C01SleeperKeeperResult result)
        {
            result = default;
            if (summonedEvent == null || sourceMonster == null || enemyPositions == null ||
                eventLog == null || resolutions == null ||
                summonedEvent.EventType != BattleEventType.MonsterSummoned ||
                eventLog.Find(summonedEvent.EventId) != summonedEvent ||
                !string.Equals(
                    summonedEvent.ActorId,
                    sourceMonster.BattleCardId,
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    sourceMonster.Card.SourceCard.CatalogCardId,
                    TestContentIds.C01,
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    targetDeclaration.SourceBattleCardId,
                    sourceMonster.BattleCardId,
                    StringComparison.OrdinalIgnoreCase) ||
                !resolutions.TryBegin(EffectId, summonedEvent.EventId))
            {
                return false;
            }

            string targetEnemyId = EnchantFixedTargetResolver.Resolve(
                targetDeclaration, enemyPositions);
            if (string.IsNullOrWhiteSpace(targetEnemyId))
            {
                result = new C01SleeperKeeperResult(
                    null,
                    false,
                    EnemyPositionMoveFailure.EnemyNotFound,
                    0,
                    Array.Empty<EnemyPositionMoveRecord>());
                return true;
            }

            bool moved = BattleEnemyMovementResolver.TryMoveOneStep(
                enemyPositions,
                movementLocks,
                targetEnemyId,
                EnemyMoveDirection.Left,
                out List<EnemyPositionMoveRecord> moves,
                out EnemyPositionMoveFailure movementFailure);

            if (moved)
            {
                foreach (EnemyPositionMoveRecord move in moves)
                {
                    eventLog.Record(
                        BattleEventType.EnemyMoved,
                        move.Pushed ? "C01Push" : "C01MoveTarget",
                        sourceMonster.BattleCardId,
                        sourceMonster.BattleCardId,
                        move.EnemyId,
                        parentEventId: summonedEvent.EventId,
                        sourceEffectId: EffectId,
                        beforeValue: (int)move.From,
                        afterValue: (int)move.To);
                }
            }

            int defenseGained = 0;
            if (moved && sourceMonster.Card.CurrentLevel >= 5)
            {
                defenseGained = 1;
            }
            else if (!moved && movementFailure == EnemyPositionMoveFailure.MovementLocked)
            {
                defenseGained = sourceMonster.Card.CurrentLevel >= 4 ? 3 : 2;
            }

            if (defenseGained > 0)
            {
                int beforeDefense = sourceMonster.Defense;
                sourceMonster.ApplyDefense(defenseGained);
                eventLog.Record(
                    BattleEventType.StatusApplied,
                    "C01Defense",
                    sourceMonster.BattleCardId,
                    sourceMonster.BattleCardId,
                    sourceMonster.BattleCardId,
                    parentEventId: summonedEvent.EventId,
                    sourceEffectId: EffectId,
                    beforeValue: beforeDefense,
                    afterValue: sourceMonster.Defense);
            }

            result = new C01SleeperKeeperResult(
                targetEnemyId,
                moved,
                movementFailure,
                defenseGained,
                moves);
            return true;
        }
    }

    public readonly struct C01SleeperKeeperResult
    {
        public C01SleeperKeeperResult(
            string resolvedTargetEnemyId,
            bool movementSucceeded,
            EnemyPositionMoveFailure movementFailure,
            int defenseGained,
            IReadOnlyList<EnemyPositionMoveRecord> moves)
        {
            ResolvedTargetEnemyId = resolvedTargetEnemyId;
            MovementSucceeded = movementSucceeded;
            MovementFailure = movementFailure;
            DefenseGained = defenseGained;
            Moves = moves;
        }

        public string ResolvedTargetEnemyId { get; }
        public bool MovementSucceeded { get; }
        public EnemyPositionMoveFailure MovementFailure { get; }
        public int DefenseGained { get; }
        public IReadOnlyList<EnemyPositionMoveRecord> Moves { get; }
    }

    public static class C02LanternBearerResolver
    {
        private const string EffectId = "C02-SUMMON";

        public static bool TryResolve(
            BattleEventRecord summonedEvent,
            BattleMonsterState sourceMonster,
            BattleNextSkillModifierState modifiers,
            BattleEventLog eventLog,
            BattleEffectResolutionTracker resolutions,
            out C02LanternBearerResult result)
        {
            result = default;
            if (summonedEvent == null || sourceMonster == null || modifiers == null ||
                eventLog == null || resolutions == null ||
                summonedEvent.EventType != BattleEventType.MonsterSummoned ||
                eventLog.Find(summonedEvent.EventId) != summonedEvent ||
                !string.Equals(summonedEvent.ActorId, sourceMonster.BattleCardId, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(sourceMonster.Card.SourceCard.CatalogCardId, TestContentIds.C02, StringComparison.OrdinalIgnoreCase) ||
                !resolutions.TryBegin(EffectId, summonedEvent.EventId))
            {
                return false;
            }

            int numericBonus = sourceMonster.Card.CurrentLevel >= 5 ? 1 : 0;
            int beforeCount = modifiers.PendingCount;
            modifiers.Add(sourceMonster.BattleCardId, 1, numericBonus);
            eventLog.Record(
                BattleEventType.StatusApplied, "C02NextSkillCost", sourceMonster.BattleCardId,
                sourceMonster.BattleCardId, "PLAYER", summonedEvent.EventId, EffectId,
                beforeValue: beforeCount, afterValue: modifiers.PendingCount);

            int defenseGained = 0;
            if (sourceMonster.Card.CurrentLevel >= 4)
            {
                int beforeDefense = sourceMonster.Defense;
                defenseGained = sourceMonster.ApplyDefense(1);
                eventLog.Record(
                    BattleEventType.StatusApplied, "C02Defense", sourceMonster.BattleCardId,
                    sourceMonster.BattleCardId, sourceMonster.BattleCardId, summonedEvent.EventId, EffectId,
                    beforeValue: beforeDefense, afterValue: sourceMonster.Defense);
            }

            result = new C02LanternBearerResult(1, numericBonus, defenseGained);
            return true;
        }
    }

    public readonly struct C02LanternBearerResult
    {
        public C02LanternBearerResult(int costReduction, int firstNumericEffectBonus, int defenseGained)
        {
            CostReduction = costReduction;
            FirstNumericEffectBonus = firstNumericEffectBonus;
            DefenseGained = defenseGained;
        }

        public int CostReduction { get; }
        public int FirstNumericEffectBonus { get; }
        public int DefenseGained { get; }
    }

    public readonly struct C03SeatRepairerResult
    {
        public C03SeatRepairerResult(bool attackedThisTurn, int defenseGained, int counterGained)
        {
            AttackedThisTurn = attackedThisTurn;
            DefenseGained = defenseGained;
            CounterGained = counterGained;
        }

        public bool AttackedThisTurn { get; }
        public int DefenseGained { get; }
        public int CounterGained { get; }
    }

    public static class C03SeatRepairerTurnEndResolver
    {
        private const string EffectId = "C03-TURN-END";

        public static bool TryResolve(
            BattleMonsterState sourceMonster,
            int playerTurn,
            int firstTurnEventIndex,
            BattleEventLog eventLog,
            BattleEffectResolutionTracker resolutions,
            out C03SeatRepairerResult result)
        {
            result = default;
            if (sourceMonster == null || playerTurn < 1 || eventLog == null || resolutions == null ||
                firstTurnEventIndex < 0 || firstTurnEventIndex > eventLog.Events.Count ||
                sourceMonster.Card.Zone != CardZone.MonsterField ||
                !string.Equals(
                    sourceMonster.Card.SourceCard.CatalogCardId,
                    TestContentIds.C03,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string turnResolutionId = $"PLAYER-TURN-{playerTurn}:{sourceMonster.BattleCardId}";
            if (!resolutions.TryBegin(EffectId, turnResolutionId))
            {
                return false;
            }

            bool attackedThisTurn = false;
            for (int i = firstTurnEventIndex; i < eventLog.Events.Count; i++)
            {
                BattleEventRecord record = eventLog.Events[i];
                if (record != null &&
                    record.EventType == BattleEventType.AttackCompleted &&
                    string.Equals(
                        record.ActorId,
                        sourceMonster.BattleCardId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    attackedThisTurn = true;
                    break;
                }
            }

            if (attackedThisTurn)
            {
                result = new C03SeatRepairerResult(true, 0, 0);
                return true;
            }

            int defenseAmount = sourceMonster.Card.CurrentLevel >= 3 ? 4 : 3;
            int beforeDefense = sourceMonster.Defense;
            int defenseGained = sourceMonster.ApplyDefense(defenseAmount);
            eventLog.Record(
                BattleEventType.StatusApplied,
                "C03TurnEndDefense",
                sourceMonster.BattleCardId,
                sourceMonster.BattleCardId,
                sourceMonster.BattleCardId,
                sourceEffectId: EffectId,
                beforeValue: beforeDefense,
                afterValue: sourceMonster.Defense);

            int counterGained = 0;
            if (sourceMonster.Card.CurrentLevel >= 5)
            {
                int beforeCounter = sourceMonster.Counter;
                counterGained = sourceMonster.ApplyCounter(1);
                eventLog.Record(
                    BattleEventType.StatusApplied,
                    "C03TurnEndCounter",
                    sourceMonster.BattleCardId,
                    sourceMonster.BattleCardId,
                    sourceMonster.BattleCardId,
                    sourceEffectId: EffectId,
                    beforeValue: beforeCounter,
                    afterValue: sourceMonster.Counter);
            }

            result = new C03SeatRepairerResult(false, defenseGained, counterGained);
            return true;
        }
    }

    public static class C04TerminalCatResolver
    {
        private const string EffectId = "C04-ENEMY-MOVED";

        public static bool TryResolve(
            BattleEventRecord movedEvent,
            int enemyTurnNumber,
            BattleMonsterState sourceMonster,
            BattleCardTurnTriggerState triggers,
            BattleEventLog eventLog,
            out int attackEnhancement)
        {
            attackEnhancement = 0;
            if (movedEvent == null || sourceMonster == null || triggers == null || eventLog == null ||
                movedEvent.EventType != BattleEventType.EnemyMoved ||
                eventLog.Find(movedEvent.EventId) != movedEvent ||
                sourceMonster.Card.Zone != CardZone.MonsterField ||
                !string.Equals(sourceMonster.Card.SourceCard.CatalogCardId, TestContentIds.C04,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string commandKey = string.IsNullOrWhiteSpace(movedEvent.ParentEventId)
                ? movedEvent.EventId
                : movedEvent.ParentEventId;
            int maximumUses = sourceMonster.Card.CurrentLevel >= 5 ? 2 : 1;
            if (!triggers.TryUse(
                    EffectId, sourceMonster.BattleCardId, enemyTurnNumber, commandKey, maximumUses))
            {
                return false;
            }

            int amount = sourceMonster.Card.CurrentLevel >= 3 ? 2 : 1;
            int before = sourceMonster.AttackEnhancement;
            attackEnhancement = sourceMonster.ApplyAttackEnhancement(amount);
            eventLog.Record(
                BattleEventType.StatusApplied, "C04AttackEnhancement",
                sourceMonster.BattleCardId, sourceMonster.BattleCardId, sourceMonster.BattleCardId,
                parentEventId: movedEvent.EventId, sourceEffectId: EffectId,
                beforeValue: before, afterValue: sourceMonster.AttackEnhancement);
            return true;
        }
    }

    public static class C05PlatformPushResolver
    {
        private const string EffectId = "C05-MAIN";

        public static bool TryResolve(
            BattleEventRecord playedEvent,
            BattleCardInstance sourceSkill,
            string fixedTargetEnemyId,
            BattleEnemyPositionState positions,
            BattleEnemyMovementLockState movementLocks,
            BattleEnemyStatusRegistry statuses,
            BattleEventLog eventLog,
            BattleEffectResolutionTracker resolutions,
            out int movedSteps,
            out int weakenGained,
            out int vulnerableGained)
        {
            movedSteps = 0;
            weakenGained = 0;
            vulnerableGained = 0;
            if (playedEvent == null || sourceSkill == null || positions == null ||
                statuses == null || eventLog == null || resolutions == null ||
                playedEvent.EventType != BattleEventType.CardPlayed ||
                eventLog.Find(playedEvent.EventId) != playedEvent ||
                !string.Equals(sourceSkill.SourceCard.CatalogCardId, TestContentIds.C05, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(playedEvent.ActorId, sourceSkill.Ids.BattleCardId, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(fixedTargetEnemyId) ||
                positions.FindPosition(fixedTargetEnemyId) == null ||
                statuses.Find(fixedTargetEnemyId) == null ||
                !resolutions.TryBegin(EffectId, playedEvent.EventId))
            {
                return false;
            }

            int requestedSteps = sourceSkill.CurrentLevel >= 5 ? 2 : 1;
            for (int step = 0; step < requestedSteps; step++)
            {
                if (!BattleEnemyMovementResolver.TryMoveOneStep(
                        positions, movementLocks, fixedTargetEnemyId, EnemyMoveDirection.Right,
                        out List<EnemyPositionMoveRecord> moves, out _))
                {
                    break;
                }

                movedSteps++;
                foreach (EnemyPositionMoveRecord move in moves)
                {
                    eventLog.Record(
                        BattleEventType.EnemyMoved, "C05Move", sourceSkill.Ids.BattleCardId,
                        sourceSkill.Ids.BattleCardId, move.EnemyId,
                        parentEventId: playedEvent.EventId, sourceEffectId: EffectId,
                        beforeValue: (int)move.From, afterValue: (int)move.To);
                }
            }

            BattleEnemyStatusState target = statuses.Find(fixedTargetEnemyId);
            int weakenAmount = sourceSkill.CurrentLevel >= 2 ? 2 : 1;
            int beforeWeaken = target.Weaken;
            weakenGained = target.ApplyWeaken(weakenAmount);
            eventLog.Record(
                BattleEventType.StatusApplied, "C05Weaken", sourceSkill.Ids.BattleCardId,
                sourceSkill.Ids.BattleCardId, target.EnemyId,
                parentEventId: playedEvent.EventId, sourceEffectId: EffectId,
                beforeValue: beforeWeaken, afterValue: target.Weaken);

            if (movedSteps > 0 && sourceSkill.CurrentLevel >= 4)
            {
                int beforeVulnerable = target.Vulnerable;
                vulnerableGained = target.ApplyVulnerable(1);
                eventLog.Record(
                    BattleEventType.StatusApplied, "C05Vulnerable", sourceSkill.Ids.BattleCardId,
                    sourceSkill.Ids.BattleCardId, target.EnemyId,
                    parentEventId: playedEvent.EventId, sourceEffectId: EffectId,
                    beforeValue: beforeVulnerable, afterValue: target.Vulnerable);
            }

            return true;
        }
    }

    public static class C06EmergencyBrakeResolver
    {
        private const string EffectId = "C06-MAIN";

        public static bool TryResolve(
            BattleEventRecord playedEvent,
            BattleCardInstance sourceSkill,
            string fixedTargetEnemyId,
            IEnumerable<BattleEnemyAttackSnapshot> livingEnemies,
            BattleEnemyStatusRegistry statuses,
            BattleEventLog eventLog,
            BattleEffectResolutionTracker resolutions,
            out string secondaryEnemyId)
        {
            secondaryEnemyId = null;
            if (playedEvent == null || sourceSkill == null || livingEnemies == null ||
                statuses == null || eventLog == null || resolutions == null ||
                playedEvent.EventType != BattleEventType.CardPlayed ||
                eventLog.Find(playedEvent.EventId) != playedEvent ||
                !string.Equals(sourceSkill.SourceCard.CatalogCardId, TestContentIds.C06, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(playedEvent.ActorId, sourceSkill.Ids.BattleCardId, StringComparison.OrdinalIgnoreCase) ||
                statuses.Find(fixedTargetEnemyId) == null ||
                !resolutions.TryBegin(EffectId, playedEvent.EventId))
            {
                return false;
            }

            BattleEnemyStatusState target = statuses.Find(fixedTargetEnemyId);
            int bindAmount = sourceSkill.CurrentLevel >= 4 ? 2 : 1;
            int beforeBind = target.Bind;
            int bindGained = target.ApplyBind(bindAmount);
            if (bindGained == 0)
            {
                return true;
            }

            eventLog.Record(
                BattleEventType.StatusApplied, "C06Bind", sourceSkill.Ids.BattleCardId,
                sourceSkill.Ids.BattleCardId, target.EnemyId,
                parentEventId: playedEvent.EventId, sourceEffectId: EffectId,
                beforeValue: beforeBind, afterValue: target.Bind);

            if (sourceSkill.CurrentLevel >= 2)
            {
                ApplyWeaken(target, sourceSkill, playedEvent, eventLog);
            }

            if (sourceSkill.CurrentLevel >= 5)
            {
                int highestAttack = int.MinValue;
                foreach (BattleEnemyAttackSnapshot enemy in livingEnemies)
                {
                    if (string.IsNullOrWhiteSpace(enemy.EnemyId) ||
                        string.Equals(enemy.EnemyId, fixedTargetEnemyId, StringComparison.OrdinalIgnoreCase) ||
                        statuses.Find(enemy.EnemyId) == null)
                    {
                        continue;
                    }

                    if (enemy.Attack > highestAttack ||
                        enemy.Attack == highestAttack && string.Compare(
                            enemy.EnemyId, secondaryEnemyId, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        highestAttack = enemy.Attack;
                        secondaryEnemyId = enemy.EnemyId;
                    }
                }

                BattleEnemyStatusState secondary = statuses.Find(secondaryEnemyId);
                if (secondary != null)
                {
                    ApplyWeaken(secondary, sourceSkill, playedEvent, eventLog);
                }
            }

            return true;
        }

        private static void ApplyWeaken(
            BattleEnemyStatusState target, BattleCardInstance source,
            BattleEventRecord parent, BattleEventLog eventLog)
        {
            int before = target.Weaken;
            target.ApplyWeaken(1);
            eventLog.Record(
                BattleEventType.StatusApplied, "C06Weaken", source.Ids.BattleCardId,
                source.Ids.BattleCardId, target.EnemyId,
                parentEventId: parent.EventId, sourceEffectId: EffectId,
                beforeValue: before, afterValue: target.Weaken);
        }
    }

    public static class C07LostTicketResolver
    {
        private const string EffectId = "C07-MAIN";

        public static bool TryResolve(
            BattleEventRecord playedEvent,
            BattleCardInstance sourceSkill,
            string selectedBanishBattleCardId,
            BattleDeckState deck,
            BattleMonsterRegistry monsters,
            BattleEventLog eventLog,
            BattleEffectResolutionTracker resolutions,
            out int drawnCount,
            out bool banished,
            out int defendedMonsterCount)
        {
            drawnCount = 0;
            banished = false;
            defendedMonsterCount = 0;
            if (playedEvent == null || sourceSkill == null || deck == null ||
                monsters == null || eventLog == null || resolutions == null ||
                playedEvent.EventType != BattleEventType.CardPlayed ||
                eventLog.Find(playedEvent.EventId) != playedEvent ||
                !string.Equals(sourceSkill.SourceCard.CatalogCardId, TestContentIds.C07, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(playedEvent.ActorId, sourceSkill.Ids.BattleCardId, StringComparison.OrdinalIgnoreCase) ||
                !resolutions.TryBegin(EffectId, playedEvent.EventId))
            {
                return false;
            }

            int drawAttempts = sourceSkill.CurrentLevel >= 2 ? 3 : 2;
            for (int i = 0; i < drawAttempts; i++)
            {
                if (deck.TryDraw(out BattleCardInstance drawn, out _))
                {
                    drawnCount++;
                    eventLog.Record(
                        BattleEventType.CardMoved, "C07Draw",
                        sourceSkill.Ids.BattleCardId, sourceSkill.Ids.BattleCardId,
                        drawn.Ids.BattleCardId, parentEventId: playedEvent.EventId,
                        sourceEffectId: EffectId, hasZoneChange: true,
                        fromZone: CardZone.DrawPile, toZone: CardZone.Hand);
                }
            }

            bool optionalBanish = sourceSkill.CurrentLevel >= 4;
            if (string.IsNullOrWhiteSpace(selectedBanishBattleCardId))
            {
                return optionalBanish;
            }

            BattleCardInstance selected = deck.Zones.Find(selectedBanishBattleCardId);
            if (selected == null || selected.Zone != CardZone.Hand ||
                string.Equals(selected.Ids.BattleCardId, sourceSkill.Ids.BattleCardId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            int selectedManaCost = selected.Resolved.ManaCost;
            if (!deck.TryBanish(selected.Ids.BattleCardId, out _))
            {
                return false;
            }

            banished = true;
            eventLog.Record(
                BattleEventType.CardMoved, "C07Banish",
                sourceSkill.Ids.BattleCardId, sourceSkill.Ids.BattleCardId,
                selected.Ids.BattleCardId, parentEventId: playedEvent.EventId,
                sourceEffectId: EffectId, hasZoneChange: true,
                fromZone: CardZone.Hand, toZone: CardZone.Banished);

            if (sourceSkill.CurrentLevel >= 5 && selectedManaCost >= 2)
            {
                foreach (BattleMonsterState monster in monsters.Monsters)
                {
                    if (monster == null || monster.Card.Zone != CardZone.MonsterField) continue;
                    monster.ApplyDefense(2);
                    defendedMonsterCount++;
                }
            }

            return true;
        }
    }

    public static class C08ClosingDoorResolver
    {
        private const string EffectId = "C08-ENEMY-MOVE";

        public static bool TryReplace(
            BattleEventRecord moveAttemptEvent,
            int currentEnemyTurn,
            int eligibleEnemyTurn,
            int requestedSteps,
            string movingEnemyId,
            BattleCardInstance sourceTrap,
            BattleEnemyMovementLockState movementLocks,
            BattleEnemyStatusRegistry statuses,
            BattleCardTurnTriggerState triggers,
            BattleEventLog eventLog,
            out int replacementSteps)
        {
            replacementSteps = requestedSteps;
            if (moveAttemptEvent == null || sourceTrap == null || statuses == null ||
                triggers == null || eventLog == null || currentEnemyTurn < eligibleEnemyTurn ||
                requestedSteps <= 0 || string.IsNullOrWhiteSpace(movingEnemyId) ||
                eventLog.Find(moveAttemptEvent.EventId) != moveAttemptEvent ||
                sourceTrap.Zone != CardZone.SkillField ||
                !string.Equals(sourceTrap.SourceCard.CatalogCardId, TestContentIds.C08, StringComparison.OrdinalIgnoreCase) ||
                movementLocks != null && movementLocks.IsLocked(movingEnemyId) ||
                statuses.Find(movingEnemyId) == null)
            {
                return false;
            }

            int maximumUses = sourceTrap.CurrentLevel >= 4 ? 2 : 1;
            if (!triggers.TryUse(
                    EffectId, sourceTrap.Ids.BattleCardId, currentEnemyTurn,
                    moveAttemptEvent.EventId, maximumUses))
            {
                return false;
            }

            replacementSteps = 0;
            BattleEnemyStatusState target = statuses.Find(movingEnemyId);
            target.ApplyBind(sourceTrap.CurrentLevel >= 5 ? 2 : 1);
            if (sourceTrap.CurrentLevel >= 2)
            {
                target.ApplyWeaken(1);
            }

            eventLog.Record(
                BattleEventType.StatusApplied, "C08ClosingDoor",
                sourceTrap.Ids.BattleCardId, sourceTrap.Ids.BattleCardId, target.EnemyId,
                parentEventId: moveAttemptEvent.EventId, sourceEffectId: EffectId,
                beforeValue: requestedSteps, afterValue: replacementSteps);
            return true;
        }
    }

    public static class C09InspectionBlanketResolver
    {
        private const string EffectId = "C09-BEFORE-DAMAGE";

        public static bool TryResolve(
            BattleEventRecord declaredAttack,
            int currentEnemyTurn,
            int eligibleEnemyTurn,
            BattleCardInstance sourceTrap,
            BattleMonsterState targetMonster,
            BattleDefenseRetentionState retention,
            BattleCardTurnTriggerState triggers,
            BattleEventLog eventLog,
            out int defenseGained)
        {
            defenseGained = 0;
            if (declaredAttack == null || sourceTrap == null || targetMonster == null ||
                retention == null || triggers == null || eventLog == null ||
                currentEnemyTurn < eligibleEnemyTurn ||
                declaredAttack.EventType != BattleEventType.AttackDeclared ||
                eventLog.Find(declaredAttack.EventId) != declaredAttack ||
                sourceTrap.Zone != CardZone.SkillField ||
                targetMonster.Card.Zone != CardZone.MonsterField ||
                !string.Equals(sourceTrap.SourceCard.CatalogCardId, TestContentIds.C09, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(declaredAttack.TargetId, targetMonster.BattleCardId,
                    StringComparison.OrdinalIgnoreCase) ||
                !triggers.TryUse(
                    EffectId, sourceTrap.Ids.BattleCardId, currentEnemyTurn,
                    declaredAttack.EventId, 1))
            {
                return false;
            }

            int amount = sourceTrap.CurrentLevel >= 5 ? 5 :
                sourceTrap.CurrentLevel >= 2 ? 4 : 3;
            int before = targetMonster.Defense;
            defenseGained = targetMonster.ApplyDefense(amount);
            if (sourceTrap.CurrentLevel >= 4)
            {
                retention.Mark(targetMonster.BattleCardId);
            }

            eventLog.Record(
                BattleEventType.StatusApplied, "C09InspectionBlanket",
                sourceTrap.Ids.BattleCardId, sourceTrap.Ids.BattleCardId,
                targetMonster.BattleCardId, parentEventId: declaredAttack.EventId,
                sourceEffectId: EffectId, beforeValue: before,
                afterValue: targetMonster.Defense);
            return true;
        }
    }

    public static class C10BrokenCallLineResolver
    {
        private const string EffectId = "C10-ENEMY-ABILITY";

        public static bool TryCancel(
            BattleEventRecord abilityEvent,
            EnemyAbilityResolutionContext ability,
            BattleCardInstance sourceTrap,
            BattleCardZoneState zones,
            BattleEnemyStatusRegistry statuses,
            BattleEventLog eventLog,
            BattleEffectResolutionTracker resolutions,
            out bool cancelled,
            out bool returnedToHand)
        {
            cancelled = false;
            returnedToHand = false;
            if (abilityEvent == null || sourceTrap == null || zones == null ||
                statuses == null || eventLog == null || resolutions == null ||
                eventLog.Find(abilityEvent.EventId) != abilityEvent ||
                sourceTrap.Zone != CardZone.SkillField ||
                !string.Equals(sourceTrap.SourceCard.CatalogCardId, TestContentIds.C10, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(ability.AbilityId) ||
                string.IsNullOrWhiteSpace(ability.SourceEnemyId) ||
                ability.IsNormalAttack || !ability.AffectsFriendlySide ||
                ability.IsAreaAbility && sourceTrap.CurrentLevel < 4 ||
                !resolutions.TryBegin(EffectId, abilityEvent.EventId))
            {
                return false;
            }

            cancelled = true;
            if (sourceTrap.CurrentLevel >= 3)
            {
                statuses.Find(ability.SourceEnemyId)?.ApplyWeaken(1);
            }

            CardZone destination = sourceTrap.CurrentLevel >= 5
                ? CardZone.Hand
                : CardZone.Graveyard;
            if (!zones.TryMove(sourceTrap.Ids.BattleCardId, destination, out _))
            {
                return false;
            }

            returnedToHand = destination == CardZone.Hand;
            eventLog.Record(
                BattleEventType.StatusApplied, "C10CancelEnemyAbility",
                sourceTrap.Ids.BattleCardId, sourceTrap.Ids.BattleCardId,
                ability.SourceEnemyId, parentEventId: abilityEvent.EventId,
                sourceEffectId: EffectId, beforeValue: 1, afterValue: 0);
            return true;
        }
    }

    public static class C11LateNightWaitingRoomResolver
    {
        private const string EffectId = "C11-TURN-START";

        public static bool TryResolve(
            BattleCardInstance sourceBarrier,
            int playerTurn,
            BattleDeckState deck,
            BattleMonsterRegistry monsters,
            BattleEventLog eventLog,
            BattleEffectResolutionTracker resolutions,
            out int drawnCount,
            out string defendedMonsterId)
        {
            drawnCount = 0;
            defendedMonsterId = null;
            if (sourceBarrier == null || playerTurn < 1 || deck == null ||
                monsters == null || eventLog == null || resolutions == null ||
                sourceBarrier.Zone != CardZone.SkillField ||
                !string.Equals(sourceBarrier.SourceCard.CatalogCardId, TestContentIds.C11,
                    StringComparison.OrdinalIgnoreCase) ||
                !resolutions.TryBegin(
                    EffectId, $"PLAYER-TURN-{playerTurn}:{sourceBarrier.Ids.BattleCardId}"))
            {
                return false;
            }

            int threshold = sourceBarrier.CurrentLevel >= 2 ? 5 : 4;
            if (deck.Zones.Count(CardZone.Hand) > threshold)
            {
                return true;
            }

            int attempts = sourceBarrier.CurrentLevel >= 5 ? 2 : 1;
            for (int i = 0; i < attempts; i++)
            {
                if (deck.TryDraw(out BattleCardInstance drawn, out _))
                {
                    drawnCount++;
                    eventLog.Record(
                        BattleEventType.CardMoved, "C11TurnStartDraw",
                        sourceBarrier.Ids.BattleCardId, sourceBarrier.Ids.BattleCardId,
                        drawn.Ids.BattleCardId, sourceEffectId: EffectId,
                        hasZoneChange: true, fromZone: CardZone.DrawPile, toZone: CardZone.Hand);
                }
            }

            if (sourceBarrier.CurrentLevel >= 4 && drawnCount > 0)
            {
                BattleMonsterState lowest = null;
                foreach (BattleMonsterState monster in monsters.Monsters)
                {
                    if (monster == null || monster.Card.Zone != CardZone.MonsterField) continue;
                    if (lowest == null || monster.CurrentHealth < lowest.CurrentHealth ||
                        monster.CurrentHealth == lowest.CurrentHealth &&
                        string.Compare(monster.BattleCardId, lowest.BattleCardId,
                            StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        lowest = monster;
                    }
                }

                if (lowest != null)
                {
                    lowest.ApplyDefense(1);
                    defendedMonsterId = lowest.BattleCardId;
                }
            }

            return true;
        }
    }

    public static class C12RouteMapStarlightResolver
    {
        private const string EffectId = "C12-ENEMY-MOVED";

        public static bool TryResolve(
            BattleEventRecord movedEvent,
            int enemyTurnNumber,
            BattleCardInstance sourceBarrier,
            BattleCardTurnTriggerState triggers,
            BattleEnemyStatusRegistry statuses,
            BattleEnemyVitalState targetVital,
            BattleEventLog eventLog,
            out int vulnerableGained,
            out int damageApplied)
        {
            vulnerableGained = 0;
            damageApplied = 0;
            if (movedEvent == null || sourceBarrier == null || triggers == null ||
                statuses == null || eventLog == null ||
                movedEvent.EventType != BattleEventType.EnemyMoved ||
                eventLog.Find(movedEvent.EventId) != movedEvent ||
                sourceBarrier.Zone != CardZone.SkillField ||
                !string.Equals(sourceBarrier.SourceCard.CatalogCardId, TestContentIds.C12,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            BattleEnemyStatusState target = statuses.Find(movedEvent.TargetId);
            if (target == null)
            {
                return false;
            }

            int maximumUses = sourceBarrier.CurrentLevel >= 4 ? 2 : 1;
            if (!triggers.TryUse(
                    EffectId, sourceBarrier.Ids.BattleCardId, enemyTurnNumber,
                    movedEvent.EventId, maximumUses))
            {
                return false;
            }

            int vulnerableAmount = sourceBarrier.CurrentLevel >= 2 ? 2 : 1;
            int before = target.Vulnerable;
            vulnerableGained = target.ApplyVulnerable(vulnerableAmount);
            eventLog.Record(
                BattleEventType.StatusApplied, "C12Vulnerable",
                sourceBarrier.Ids.BattleCardId, sourceBarrier.Ids.BattleCardId, target.EnemyId,
                parentEventId: movedEvent.EventId, sourceEffectId: EffectId,
                beforeValue: before, afterValue: target.Vulnerable);

            if (sourceBarrier.CurrentLevel >= 5 && targetVital != null &&
                string.Equals(targetVital.EnemyId, target.EnemyId, StringComparison.OrdinalIgnoreCase))
            {
                int beforeHealth = targetVital.CurrentHealth;
                damageApplied = targetVital.ApplyDamage(1);
                eventLog.Record(
                    BattleEventType.DamageApplied, "C12DirectDamage",
                    sourceBarrier.Ids.BattleCardId, sourceBarrier.Ids.BattleCardId, target.EnemyId,
                    parentEventId: movedEvent.EventId, sourceEffectId: EffectId,
                    beforeValue: beforeHealth, afterValue: targetVital.CurrentHealth);
            }

            return true;
        }
    }
}
