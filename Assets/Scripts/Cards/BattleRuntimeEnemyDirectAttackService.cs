using UnityEngine;

namespace HaveABreak.Cards
{
    public static class BattleRuntimeEnemyDirectAttackService
    {
        public static bool TryResolve(
            BattleRuntimeState runtime,
            string attackerEnemyId,
            out BattleRuntimeEnemyDirectAttackResult result,
            out BattleRuntimeEnemyDirectAttackFailure failure)
        {
            result = null;
            if (runtime == null || runtime.Player == null)
            {
                failure = BattleRuntimeEnemyDirectAttackFailure.InvalidRuntime;
                return false;
            }

            if (runtime.Turn.Phase != BattleTurnPhase.EnemyTurn)
            {
                failure = BattleRuntimeEnemyDirectAttackFailure.InvalidTurnPhase;
                return false;
            }

            BattleEnemyRuntimeState attacker = runtime.FindEnemy(attackerEnemyId);
            if (attacker == null || !attacker.IsAlive ||
                !runtime.LivingEnemies.Contains(attackerEnemyId))
            {
                failure = BattleRuntimeEnemyDirectAttackFailure.InvalidAttacker;
                return false;
            }

            BattleEnemyStatusState status =
                runtime.EnemyStatuses.Find(attackerEnemyId);
            if (status == null)
            {
                failure = BattleRuntimeEnemyDirectAttackFailure.StatusStateNotFound;
                return false;
            }

            if (!status.CanAttack)
            {
                failure = BattleRuntimeEnemyDirectAttackFailure
                    .ActionBlockedByStatus;
                return false;
            }

            BattleEnemyAttackSnapshot snapshot = attacker.SnapshotAttack();
            BattleEventRecord declaredAttack = runtime.EventLog.Record(
                BattleEventType.AttackDeclared,
                "EnemyDirectAttackDeclared",
                attacker.EnemyId,
                attacker.EnemyId,
                BattlePlayerState.PlayerTargetId,
                beforeValue: 0,
                afterValue: snapshot.Attack);

            int weakenReduction = Mathf.Min(
                snapshot.Attack, Mathf.Max(0, status.Weaken));
            int adjustedAttack = Mathf.Max(
                0, snapshot.Attack - weakenReduction);
            int healthBefore = runtime.Player.CurrentHealth;
            int playerDamage = runtime.Player.ApplyDamage(adjustedAttack);
            BattleEventRecord playerDamageEvent = null;
            if (playerDamage > 0)
            {
                playerDamageEvent = runtime.EventLog.Record(
                    BattleEventType.DamageApplied,
                    "EnemyDirectAttackDamage",
                    attacker.EnemyId,
                    attacker.EnemyId,
                    BattlePlayerState.PlayerTargetId,
                    parentEventId: declaredAttack.EventId,
                    beforeValue: healthBefore,
                    afterValue: runtime.Player.CurrentHealth);
            }

            if (!BattleAttackEventService.TryRecordCompleted(
                    runtime.EventLog,
                    declaredAttack,
                    out BattleEventRecord completedAttack))
            {
                failure = BattleRuntimeEnemyDirectAttackFailure.CompletionFailed;
                return false;
            }

            result = new BattleRuntimeEnemyDirectAttackResult(
                snapshot,
                declaredAttack,
                weakenReduction,
                adjustedAttack,
                playerDamage,
                playerDamageEvent,
                completedAttack);
            failure = BattleRuntimeEnemyDirectAttackFailure.None;
            return true;
        }
    }
}
