using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    public static class BattleAttackEventService
    {
        public static bool TryRecordCompleted(
            BattleEventLog eventLog,
            BattleEventRecord declaredAttack,
            out BattleEventRecord completedAttack)
        {
            completedAttack = null;
            if (eventLog == null || declaredAttack == null ||
                declaredAttack.EventType != BattleEventType.AttackDeclared ||
                eventLog.Find(declaredAttack.EventId) != declaredAttack ||
                string.IsNullOrWhiteSpace(declaredAttack.ActorId) ||
                string.IsNullOrWhiteSpace(declaredAttack.TargetId))
            {
                return false;
            }

            foreach (BattleEventRecord existing in eventLog.Events)
            {
                if (existing != null && existing.EventType == BattleEventType.AttackCompleted &&
                    string.Equals(
                        existing.ParentEventId,
                        declaredAttack.EventId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            completedAttack = eventLog.Record(
                BattleEventType.AttackCompleted,
                "AttackCompleted",
                declaredAttack.SourceId,
                declaredAttack.ActorId,
                declaredAttack.TargetId,
                parentEventId: declaredAttack.EventId);
            return true;
        }
    }

    public enum BattleRuntimePlayerAttackFailure
    {
        None,
        InvalidRuntime,
        InvalidTurnPhase,
        InvalidAttacker,
        InvalidTarget,
        InvalidDeclaration,
        ActionBlockedByStatus,
        AttackAlreadyUsed,
        BeginActionFailed,
        EnemyCleanupFailed,
        CompletionFailed,
        CompleteActionFailed
    }

    public sealed class BattleRuntimePlayerAttackDeclaration
    {
        internal BattleRuntimePlayerAttackDeclaration(
            BattleMonsterState attacker,
            BattleEnemyRuntimeState target,
            int baseAttack,
            int weakenReduction,
            int adjustedAttack,
            int vulnerableBonus,
            BattleEventRecord declaredAttack)
        {
            Attacker = attacker;
            Target = target;
            BaseAttack = baseAttack;
            WeakenReduction = weakenReduction;
            AdjustedAttack = adjustedAttack;
            VulnerableBonus = vulnerableBonus;
            DeclaredAttack = declaredAttack;
        }

        public BattleMonsterState Attacker { get; }
        public BattleEnemyRuntimeState Target { get; }
        public int BaseAttack { get; }
        public int WeakenReduction { get; }
        public int AdjustedAttack { get; }
        public int VulnerableBonus { get; }
        public int FinalDamage => AdjustedAttack + VulnerableBonus;
        public BattleEventRecord DeclaredAttack { get; }
    }

    public sealed class BattleRuntimePlayerAttackResult
    {
        internal BattleRuntimePlayerAttackResult(
            BattleMonsterState attacker,
            BattleEnemyRuntimeState target,
            int baseAttack,
            int weakenReduction,
            int adjustedAttack,
            int vulnerableBonus,
            int damageApplied,
            bool targetDefeated,
            BattleEventRecord declaredAttack,
            BattleEventRecord vulnerableConsumedEvent,
            BattleEventRecord damageEvent,
            BattleEventRecord completedAttack)
        {
            Attacker = attacker;
            Target = target;
            BaseAttack = baseAttack;
            WeakenReduction = weakenReduction;
            AdjustedAttack = adjustedAttack;
            VulnerableBonus = vulnerableBonus;
            DamageApplied = damageApplied;
            TargetDefeated = targetDefeated;
            DeclaredAttack = declaredAttack;
            VulnerableConsumedEvent = vulnerableConsumedEvent;
            DamageEvent = damageEvent;
            CompletedAttack = completedAttack;
        }

        public BattleMonsterState Attacker { get; }
        public BattleEnemyRuntimeState Target { get; }
        public int BaseAttack { get; }
        public int WeakenReduction { get; }
        public int AdjustedAttack { get; }
        public int VulnerableBonus { get; }
        public int FinalDamage => AdjustedAttack + VulnerableBonus;
        public int DamageApplied { get; }
        public bool TargetDefeated { get; }
        public BattleEventRecord DeclaredAttack { get; }
        public BattleEventRecord VulnerableConsumedEvent { get; }
        public BattleEventRecord DamageEvent { get; }
        public BattleEventRecord CompletedAttack { get; }
    }

    public static class BattleRuntimePlayerAttackService
    {
        private const string PlayerAttackUsageEffectId =
            "SYSTEM-PLAYER-MONSTER-ATTACK";

        public static bool TryResolve(
            BattleRuntimeState runtime,
            string attackerBattleCardId,
            string targetEnemyId,
            out BattleRuntimePlayerAttackResult result,
            out BattleRuntimePlayerAttackFailure failure)
        {
            result = null;
            if (!TryDeclare(
                    runtime,
                    attackerBattleCardId,
                    targetEnemyId,
                    out BattleRuntimePlayerAttackDeclaration declaration,
                    out failure))
            {
                return false;
            }

            return TryResolveDeclared(
                runtime,
                declaration,
                out result,
                out failure);
        }

        public static bool TryDeclare(
            BattleRuntimeState runtime,
            string attackerBattleCardId,
            string targetEnemyId,
            out BattleRuntimePlayerAttackDeclaration declaration,
            out BattleRuntimePlayerAttackFailure failure)
        {
            declaration = null;
            if (runtime == null)
            {
                failure = BattleRuntimePlayerAttackFailure.InvalidRuntime;
                return false;
            }

            if (!runtime.Turn.CanAcceptPlayerAction)
            {
                failure = BattleRuntimePlayerAttackFailure.InvalidTurnPhase;
                return false;
            }

            BattleMonsterState attacker =
                runtime.Monsters.Find(attackerBattleCardId);
            if (attacker == null ||
                attacker.Card.Zone != CardZone.MonsterField ||
                attacker.IsDestructionCandidate ||
                !runtime.PlayerMonsterPositions.FindPosition(
                    attackerBattleCardId).HasValue)
            {
                failure = BattleRuntimePlayerAttackFailure.InvalidAttacker;
                return false;
            }

            if (!attacker.Status.CanAttack)
            {
                failure =
                    BattleRuntimePlayerAttackFailure.ActionBlockedByStatus;
                return false;
            }

            BattleEnemyRuntimeState target = runtime.FindEnemy(targetEnemyId);
            BattleEnemyStatusState targetStatus =
                runtime.EnemyStatuses.Find(targetEnemyId);
            if (target == null || !target.IsAlive || targetStatus == null ||
                !runtime.LivingEnemies.Contains(targetEnemyId) ||
                !runtime.EnemyPositions.FindPosition(targetEnemyId).HasValue)
            {
                failure = BattleRuntimePlayerAttackFailure.InvalidTarget;
                return false;
            }

            if (!runtime.Turn.TryBeginPlayerAction(out _))
            {
                failure = BattleRuntimePlayerAttackFailure.BeginActionFailed;
                return false;
            }

            int playerTurn = runtime.Turn.PlayerTurnNumber;
            if (!runtime.CardTurnTriggers.TryUse(
                    PlayerAttackUsageEffectId,
                    attacker.BattleCardId,
                    playerTurn,
                    $"PLAYER-ATTACK-{playerTurn}",
                    1))
            {
                runtime.Turn.TryCompletePlayerAction(out _);
                failure =
                    BattleRuntimePlayerAttackFailure.AttackAlreadyUsed;
                return false;
            }

            int weakenReduction = Mathf.Min(
                attacker.Attack,
                Mathf.Max(0, attacker.Status.Weaken));
            int adjustedAttack = Mathf.Max(
                0, attacker.Attack - weakenReduction);
            int vulnerableBonus = adjustedAttack > 0
                ? targetStatus.Vulnerable
                : 0;
            int finalDamage = Mathf.Max(
                0, adjustedAttack + vulnerableBonus);
            BattleEventRecord declaredAttack = runtime.EventLog.Record(
                BattleEventType.AttackDeclared,
                "PlayerMonsterAttackDeclared",
                attacker.BattleCardId,
                attacker.BattleCardId,
                target.EnemyId,
                beforeValue: 0,
                afterValue: finalDamage);

            declaration = new BattleRuntimePlayerAttackDeclaration(
                attacker,
                target,
                attacker.Attack,
                weakenReduction,
                adjustedAttack,
                vulnerableBonus,
                declaredAttack);
            failure = BattleRuntimePlayerAttackFailure.None;
            return true;
        }

        public static bool TryResolveDeclared(
            BattleRuntimeState runtime,
            BattleRuntimePlayerAttackDeclaration declaration,
            out BattleRuntimePlayerAttackResult result,
            out BattleRuntimePlayerAttackFailure failure)
        {
            result = null;
            if (runtime == null)
            {
                failure = BattleRuntimePlayerAttackFailure.InvalidRuntime;
                return false;
            }

            if (runtime.Turn.Phase != BattleTurnPhase.PlayerActionResolving ||
                declaration?.Attacker == null ||
                declaration.Target == null ||
                declaration.DeclaredAttack == null ||
                runtime.EventLog.Find(
                    declaration.DeclaredAttack.EventId) !=
                declaration.DeclaredAttack)
            {
                failure =
                    BattleRuntimePlayerAttackFailure.InvalidDeclaration;
                return false;
            }

            BattleMonsterState attacker = declaration.Attacker;
            BattleEnemyRuntimeState target = declaration.Target;
            BattleEnemyStatusState targetStatus =
                runtime.EnemyStatuses.Find(target.EnemyId);
            if (attacker.Card.Zone != CardZone.MonsterField ||
                attacker.IsDestructionCandidate ||
                !runtime.PlayerMonsterPositions.FindPosition(
                    attacker.BattleCardId).HasValue ||
                !attacker.Status.CanAttack ||
                !target.IsAlive ||
                targetStatus == null ||
                !runtime.LivingEnemies.Contains(target.EnemyId) ||
                !runtime.EnemyPositions.FindPosition(target.EnemyId).HasValue)
            {
                runtime.Turn.TryCompletePlayerAction(out _);
                failure = BattleRuntimePlayerAttackFailure.InvalidTarget;
                return false;
            }

            BattleEventRecord declaredAttack = declaration.DeclaredAttack;
            int vulnerableBonus = declaration.VulnerableBonus;
            BattleEventRecord vulnerableConsumedEvent = null;
            if (vulnerableBonus > 0)
            {
                targetStatus.ConsumeVulnerable();
                vulnerableConsumedEvent = runtime.EventLog.Record(
                    BattleEventType.StatusApplied,
                    "VulnerableConsumedByPlayerAttack",
                    attacker.BattleCardId,
                    attacker.BattleCardId,
                    target.EnemyId,
                    parentEventId: declaredAttack.EventId,
                    beforeValue: vulnerableBonus,
                    afterValue: targetStatus.Vulnerable);
            }

            int healthBefore = target.Vital.CurrentHealth;
            int damageApplied = target.Vital.ApplyDamage(
                declaration.FinalDamage);
            BattleEventRecord damageEvent = damageApplied > 0
                ? runtime.EventLog.Record(
                    BattleEventType.DamageApplied,
                    "PlayerMonsterAttackEnemyDamage",
                    attacker.BattleCardId,
                    attacker.BattleCardId,
                    target.EnemyId,
                    parentEventId: declaredAttack.EventId,
                    beforeValue: healthBefore,
                    afterValue: target.Vital.CurrentHealth)
                : null;

            bool targetDefeated = !target.IsAlive;
            if (targetDefeated &&
                (!runtime.LivingEnemies.TryRemove(target.EnemyId) ||
                 !runtime.EnemyPositions.TryRemove(target.EnemyId)))
            {
                runtime.Turn.TryCompletePlayerAction(out _);
                failure =
                    BattleRuntimePlayerAttackFailure.EnemyCleanupFailed;
                return false;
            }

            if (!BattleAttackEventService.TryRecordCompleted(
                    runtime.EventLog,
                    declaredAttack,
                    out BattleEventRecord completedAttack))
            {
                runtime.Turn.TryCompletePlayerAction(out _);
                failure = BattleRuntimePlayerAttackFailure.CompletionFailed;
                return false;
            }

            if (!runtime.Turn.TryCompletePlayerAction(out _))
            {
                failure =
                    BattleRuntimePlayerAttackFailure.CompleteActionFailed;
                return false;
            }

            result = new BattleRuntimePlayerAttackResult(
                attacker,
                target,
                declaration.BaseAttack,
                declaration.WeakenReduction,
                declaration.AdjustedAttack,
                declaration.VulnerableBonus,
                damageApplied,
                targetDefeated,
                declaredAttack,
                vulnerableConsumedEvent,
                damageEvent,
                completedAttack);
            failure = BattleRuntimePlayerAttackFailure.None;
            return true;
        }
    }
}
