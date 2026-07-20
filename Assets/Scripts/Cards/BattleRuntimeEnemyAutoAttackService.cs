namespace HaveABreak.Cards
{
    public static class BattleRuntimeEnemyAutoAttackService
    {
        public static bool TryResolve(
            BattleRuntimeState runtime,
            string attackerEnemyId,
            int tieBreakerValue,
            out BattleRuntimeEnemyAutoAttackResult result,
            out BattleRuntimeEnemyAutoAttackFailure failure)
        {
            result = null;
            if (!BattleRuntimeEnemyAttackTargetService.TrySelect(
                    runtime,
                    attackerEnemyId,
                    tieBreakerValue,
                    out BattleRuntimeEnemyAttackTargetResult target,
                    out _))
            {
                failure = BattleRuntimeEnemyAutoAttackFailure.TargetSelectionFailed;
                return false;
            }

            if (target.TargetType ==
                BattleRuntimeEnemyAttackTargetType.Player)
            {
                if (!BattleRuntimeEnemyDirectAttackService.TryResolve(
                        runtime,
                        attackerEnemyId,
                        out BattleRuntimeEnemyDirectAttackResult playerResolution,
                        out _))
                {
                    failure =
                        BattleRuntimeEnemyAutoAttackFailure.DirectPlayerAttackFailed;
                    return false;
                }

                result = new BattleRuntimeEnemyAutoAttackResult(
                    target, null, null, playerResolution);
                failure = BattleRuntimeEnemyAutoAttackFailure.None;
                return true;
            }

            if (!BattleRuntimeEnemyAttackService.TryDeclare(
                    runtime,
                    attackerEnemyId,
                    target.TargetId,
                    out BattleRuntimeEnemyAttackDeclarationResult declaration,
                    out _))
            {
                failure = BattleRuntimeEnemyAutoAttackFailure
                    .MonsterAttackDeclarationFailed;
                return false;
            }

            if (!BattleRuntimeEnemyAttackService.TryResolveDamage(
                    runtime,
                    declaration,
                    out BattleRuntimeEnemyAttackResolutionResult resolution,
                    out _))
            {
                failure = BattleRuntimeEnemyAutoAttackFailure
                    .MonsterAttackResolutionFailed;
                return false;
            }

            result = new BattleRuntimeEnemyAutoAttackResult(
                target, declaration, resolution, null);
            failure = BattleRuntimeEnemyAutoAttackFailure.None;
            return true;
        }
    }
}
