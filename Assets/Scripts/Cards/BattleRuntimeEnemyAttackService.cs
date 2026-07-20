using System;
using System.Collections.Generic;

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
    }
}
