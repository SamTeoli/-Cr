using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public static class BattleRuntimeEnemyStatusTurnService
    {
        public static bool TryResolveTurnEnd(
            BattleRuntimeState runtime,
            out BattleRuntimeEnemyStatusTurnResult result)
        {
            result = null;
            if (runtime == null || runtime.EnemyStatuses == null ||
                runtime.EventLog == null || runtime.Enemies == null)
            {
                return false;
            }

            List<BattleRuntimeEnemyStatusTurnEntryResult> entries = new();
            int totalInjuryDamage = 0;
            int defeatedEnemyCount = 0;
            foreach (BattleEnemyRuntimeState enemy in runtime.Enemies)
            {
                if (enemy == null || !enemy.IsAlive ||
                    !runtime.LivingEnemies.Contains(enemy.EnemyId))
                {
                    continue;
                }

                BattleEnemyStatusState status =
                    runtime.EnemyStatuses.Find(enemy.EnemyId);
                if (status == null)
                {
                    return false;
                }

                int injuryBefore = status.Injury;
                int bindBefore = status.Bind;
                int stunBefore = status.Stun;
                int weakenBefore = status.Weaken;
                int healthBefore = enemy.Vital.CurrentHealth;
                int requestedInjuryDamage = status.ResolveInjuryAtTurnEnd();
                int injuryDamage =
                    enemy.Vital.ApplyDamage(requestedInjuryDamage);
                BattleEventRecord injuryDamageEvent = null;
                if (injuryDamage > 0)
                {
                    injuryDamageEvent = runtime.EventLog.Record(
                        BattleEventType.DamageApplied,
                        "InjuryTurnEndDamage",
                        enemy.EnemyId,
                        enemy.EnemyId,
                        enemy.EnemyId,
                        beforeValue: healthBefore,
                        afterValue: enemy.Vital.CurrentHealth);
                }

                status.ReduceBindAtTurnEnd();
                status.ClearStunAtTurnEnd();
                status.ReduceWeakenAtTurnEnd();
                RecordDecay(
                    runtime,
                    enemy.EnemyId,
                    "InjuryTurnEndDecay",
                    injuryBefore,
                    status.Injury);
                RecordDecay(
                    runtime,
                    enemy.EnemyId,
                    "BindTurnEndDecay",
                    bindBefore,
                    status.Bind);
                RecordDecay(
                    runtime,
                    enemy.EnemyId,
                    "StunTurnEndClear",
                    stunBefore,
                    status.Stun);
                RecordDecay(
                    runtime,
                    enemy.EnemyId,
                    "WeakenTurnEndDecay",
                    weakenBefore,
                    status.Weaken);

                totalInjuryDamage += injuryDamage;
                if (!enemy.IsAlive)
                {
                    defeatedEnemyCount++;
                }

                entries.Add(new BattleRuntimeEnemyStatusTurnEntryResult(
                    enemy.EnemyId,
                    injuryDamage,
                    injuryBefore,
                    status.Injury,
                    bindBefore,
                    status.Bind,
                    stunBefore,
                    status.Stun,
                    weakenBefore,
                    status.Weaken,
                    injuryDamageEvent));
            }

            result = new BattleRuntimeEnemyStatusTurnResult(
                entries,
                totalInjuryDamage,
                defeatedEnemyCount);
            return true;
        }

        private static void RecordDecay(
            BattleRuntimeState runtime,
            string enemyId,
            string cause,
            int before,
            int after)
        {
            if (before == after)
            {
                return;
            }

            runtime.EventLog.Record(
                BattleEventType.StatusApplied,
                cause,
                enemyId,
                enemyId,
                enemyId,
                beforeValue: before,
                afterValue: after);
        }
    }
}
