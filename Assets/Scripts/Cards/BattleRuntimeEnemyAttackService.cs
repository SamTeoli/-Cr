using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    public static class BattleRuntimeEnemyAttackService
    {
        public static bool TryDeclare(
            BattleRuntimeState runtime,
            string attackerEnemyId,
            string targetBattleCardId,
            out BattleRuntimeEnemyAttackDeclarationResult result,
            out BattleRuntimeEnemyAttackFailure failure)
        {
            result = null;
            if (runtime == null)
            {
                failure = BattleRuntimeEnemyAttackFailure.InvalidRuntime;
                return false;
            }

            if (runtime.Turn.Phase != BattleTurnPhase.EnemyTurn)
            {
                failure = BattleRuntimeEnemyAttackFailure.InvalidTurnPhase;
                return false;
            }

            BattleEnemyRuntimeState attacker =
                runtime.FindEnemy(attackerEnemyId);
            if (attacker == null || !attacker.IsAlive)
            {
                failure = BattleRuntimeEnemyAttackFailure.InvalidAttacker;
                return false;
            }

            BattleMonsterState target =
                runtime.Monsters.Find(targetBattleCardId);
            if (target == null ||
                target.Card.Zone != CardZone.MonsterField ||
                target.IsDestructionCandidate)
            {
                failure = BattleRuntimeEnemyAttackFailure.InvalidTarget;
                return false;
            }

            BattleEnemyAttackSnapshot attackSnapshot = attacker.SnapshotAttack();
            BattleEventRecord declaredAttack = runtime.EventLog.Record(
                BattleEventType.AttackDeclared,
                "EnemyAttackDeclared",
                attacker.EnemyId,
                attacker.EnemyId,
                target.BattleCardId,
                beforeValue: 0,
                afterValue: attackSnapshot.Attack);

            runtime.TrapInstallations.PruneInactive();
            int defenseGained = 0;
            List<string> triggeredTrapBattleCardIds = new();
            IReadOnlyList<BattleRuntimeTrapInstallation> installations =
                runtime.TrapInstallations.Installations;
            for (int i = installations.Count - 1; i >= 0; i--)
            {
                BattleRuntimeTrapInstallation installation = installations[i];
                if (installation?.SourceTrap?.SourceCard == null ||
                    !string.Equals(
                        installation.SourceTrap.SourceCard.CatalogCardId,
                        "C09",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (BattleRuntimeTrapEffectService.TryResolveIncomingAttack(
                        runtime,
                        installation,
                        declaredAttack,
                        target.BattleCardId,
                        out int gained))
                {
                    defenseGained += gained;
                    triggeredTrapBattleCardIds.Add(
                        installation.SourceTrap.Ids.BattleCardId);
                }
            }

            result = new BattleRuntimeEnemyAttackDeclarationResult(
                declaredAttack,
                attackSnapshot,
                target,
                defenseGained,
                triggeredTrapBattleCardIds);
            failure = BattleRuntimeEnemyAttackFailure.None;
            return true;
        }

        public static bool TryResolveDamage(
            BattleRuntimeState runtime,
            BattleRuntimeEnemyAttackDeclarationResult declaration,
            out BattleRuntimeEnemyAttackResolutionResult result,
            out BattleRuntimeEnemyAttackFailure failure)
        {
            result = null;
            if (runtime == null || runtime.Player == null)
            {
                failure = BattleRuntimeEnemyAttackFailure.InvalidRuntime;
                return false;
            }

            if (runtime.Turn.Phase != BattleTurnPhase.EnemyTurn)
            {
                failure = BattleRuntimeEnemyAttackFailure.InvalidTurnPhase;
                return false;
            }

            if (!IsValidDeclaration(runtime, declaration))
            {
                failure = BattleRuntimeEnemyAttackFailure.InvalidDeclaration;
                return false;
            }

            if (HasCompletedAttack(
                    runtime.EventLog, declaration.DeclaredAttack.EventId))
            {
                failure = BattleRuntimeEnemyAttackFailure.AlreadyResolved;
                return false;
            }

            BattleMonsterState target = declaration.TargetMonster;
            if (runtime.Monsters.Find(target.BattleCardId) != target ||
                target.Card.Zone != CardZone.MonsterField ||
                target.IsDestructionCandidate)
            {
                failure = BattleRuntimeEnemyAttackFailure.InvalidTarget;
                return false;
            }

            BattleEnemyStatusState attackerStatus = runtime.EnemyStatuses.Find(
                declaration.Attacker.EnemyId);
            if (attackerStatus == null)
            {
                failure = BattleRuntimeEnemyAttackFailure.StatusStateNotFound;
                return false;
            }

            int weakenReduction = Mathf.Min(
                declaration.Attacker.Attack,
                Mathf.Max(0, attackerStatus.Weaken));
            int adjustedAttack = Mathf.Max(
                0, declaration.Attacker.Attack - weakenReduction);

            int defenseBefore = target.Defense;
            int defenseConsumed = target.ConsumeDefense(adjustedAttack);
            BattleEventRecord defenseConsumedEvent = null;
            if (defenseConsumed > 0)
            {
                defenseConsumedEvent = runtime.EventLog.Record(
                    BattleEventType.StatusApplied,
                    "DefenseConsumedByEnemyAttack",
                    declaration.Attacker.EnemyId,
                    declaration.Attacker.EnemyId,
                    target.BattleCardId,
                    parentEventId: declaration.DeclaredAttack.EventId,
                    beforeValue: defenseBefore,
                    afterValue: target.Defense);
            }

            int damageAfterDefense = adjustedAttack - defenseConsumed;
            int monsterHealthBefore = target.CurrentHealth;
            int monsterDamage = target.ApplyDamage(damageAfterDefense);
            BattleEventRecord monsterDamageEvent = null;
            if (monsterDamage > 0)
            {
                monsterDamageEvent = runtime.EventLog.Record(
                    BattleEventType.DamageApplied,
                    "EnemyAttackMonsterDamage",
                    declaration.Attacker.EnemyId,
                    declaration.Attacker.EnemyId,
                    target.BattleCardId,
                    parentEventId: declaration.DeclaredAttack.EventId,
                    beforeValue: monsterHealthBefore,
                    afterValue: target.CurrentHealth);
            }

            int overflowDamage = Mathf.Max(
                0, damageAfterDefense - monsterDamage);
            int playerHealthBefore = runtime.Player.CurrentHealth;
            int playerDamage = runtime.Player.ApplyDamage(overflowDamage);
            BattleEventRecord playerDamageEvent = null;
            if (playerDamage > 0)
            {
                playerDamageEvent = runtime.EventLog.Record(
                    BattleEventType.DamageApplied,
                    "EnemyAttackOverflowDamage",
                    declaration.Attacker.EnemyId,
                    declaration.Attacker.EnemyId,
                    BattlePlayerState.PlayerTargetId,
                    parentEventId: declaration.DeclaredAttack.EventId,
                    beforeValue: playerHealthBefore,
                    afterValue: runtime.Player.CurrentHealth);
            }

            BattleStateBasedChecker stateChecker = new(
                runtime.Deck,
                runtime.Monsters,
                runtime.EventLog,
                runtime.PlayerMonsterPositions);
            string stateCheckParentId = monsterDamageEvent != null
                ? monsterDamageEvent.EventId
                : declaration.DeclaredAttack.EventId;
            if (!stateChecker.TryResolveMonsterDestruction(
                    stateCheckParentId,
                    out List<BattleEventRecord> destructionEvents,
                    out _))
            {
                failure = BattleRuntimeEnemyAttackFailure.StateCheckFailed;
                return false;
            }

            if (!BattleAttackEventService.TryRecordCompleted(
                    runtime.EventLog,
                    declaration.DeclaredAttack,
                    out BattleEventRecord completedAttack))
            {
                failure = BattleRuntimeEnemyAttackFailure.CompletionFailed;
                return false;
            }

            result = new BattleRuntimeEnemyAttackResolutionResult(
                declaration,
                weakenReduction,
                adjustedAttack,
                defenseConsumed,
                monsterDamage,
                overflowDamage,
                playerDamage,
                defenseConsumedEvent,
                monsterDamageEvent,
                playerDamageEvent,
                destructionEvents,
                completedAttack);
            failure = BattleRuntimeEnemyAttackFailure.None;
            return true;
        }

        private static bool IsValidDeclaration(
            BattleRuntimeState runtime,
            BattleRuntimeEnemyAttackDeclarationResult declaration)
        {
            return declaration != null &&
                   declaration.DeclaredAttack != null &&
                   declaration.TargetMonster != null &&
                   !string.IsNullOrWhiteSpace(
                       declaration.Attacker.EnemyId) &&
                   declaration.DeclaredAttack.EventType ==
                   BattleEventType.AttackDeclared &&
                   runtime.EventLog.Find(
                       declaration.DeclaredAttack.EventId) ==
                   declaration.DeclaredAttack &&
                   string.Equals(
                       declaration.DeclaredAttack.ActorId,
                       declaration.Attacker.EnemyId,
                       StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(
                       declaration.DeclaredAttack.TargetId,
                       declaration.TargetMonster.BattleCardId,
                       StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasCompletedAttack(
            BattleEventLog eventLog,
            string declaredAttackEventId)
        {
            foreach (BattleEventRecord item in eventLog.Events)
            {
                if (item != null &&
                    item.EventType == BattleEventType.AttackCompleted &&
                    string.Equals(
                        item.ParentEventId,
                        declaredAttackEventId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
