namespace HaveABreak.Cards
{
    public sealed class BattleRuntimeEnemyTurnActionResult
    {
        internal BattleRuntimeEnemyTurnActionResult(
            BattleRuntimeEnemyTurnCommand command,
            BattleRuntimeEnemyMoveResult moveResult,
            BattleRuntimeEnemyAttackDeclarationResult attackDeclaration,
            BattleRuntimeEnemyAttackResolutionResult attackResolution,
            BattleRuntimeEnemyAbilityResult abilityResult)
        {
            Command = command;
            MoveResult = moveResult;
            AttackDeclaration = attackDeclaration;
            AttackResolution = attackResolution;
            AbilityResult = abilityResult;
        }

        public BattleRuntimeEnemyTurnCommand Command { get; }
        public BattleRuntimeEnemyMoveResult MoveResult { get; }
        public BattleRuntimeEnemyAttackDeclarationResult AttackDeclaration { get; }
        public BattleRuntimeEnemyAttackResolutionResult AttackResolution { get; }
        public BattleRuntimeEnemyAbilityResult AbilityResult { get; }
    }
}
