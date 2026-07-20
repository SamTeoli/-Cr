using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public static class BattleRuntimeEnemyRepeatedAttackService
    {
        public static bool TryResolve(
            BattleRuntimeState runtime,
            string attackerEnemyId,
            int attackCount,
            IReadOnlyList<int> tieBreakerValues,
            out BattleRuntimeEnemyRepeatedAttackResult result,
            out BattleRuntimeEnemyRepeatedAttackFailure failure,
            out int failedAttackIndex)
        {
            result = null;
            failedAttackIndex = -1;
            if (runtime == null || runtime.Player == null)
            {
                failure = BattleRuntimeEnemyRepeatedAttackFailure.InvalidRuntime;
                return false;
            }

            if (attackCount <= 0)
            {
                failure =
                    BattleRuntimeEnemyRepeatedAttackFailure.InvalidAttackCount;
                return false;
            }

            if (tieBreakerValues == null ||
                tieBreakerValues.Count < attackCount)
            {
                failure = BattleRuntimeEnemyRepeatedAttackFailure
                    .MissingTieBreakerValues;
                return false;
            }

            List<BattleRuntimeEnemyAutoAttackResult> attacks = new();
            for (int i = 0; i < attackCount; i++)
            {
                if (runtime.Player.IsDefeated)
                {
                    result = new BattleRuntimeEnemyRepeatedAttackResult(
                        attackCount, attacks, true);
                    failure = BattleRuntimeEnemyRepeatedAttackFailure.None;
                    return true;
                }

                if (!BattleRuntimeEnemyAutoAttackService.TryResolve(
                        runtime,
                        attackerEnemyId,
                        tieBreakerValues[i],
                        out BattleRuntimeEnemyAutoAttackResult attack,
                        out _))
                {
                    failedAttackIndex = i;
                    result = new BattleRuntimeEnemyRepeatedAttackResult(
                        attackCount, attacks, false);
                    failure =
                        BattleRuntimeEnemyRepeatedAttackFailure.AttackFailed;
                    return false;
                }

                attacks.Add(attack);
            }

            result = new BattleRuntimeEnemyRepeatedAttackResult(
                attackCount, attacks, false);
            failure = BattleRuntimeEnemyRepeatedAttackFailure.None;
            return true;
        }
    }
}
