namespace HaveABreak.Cards
{
    public sealed class BattleRuntimeEnemyTurnActionResult
    {
        internal BattleRuntimeEnemyTurnActionResult(
            BattleRuntimeEnemyTurnCommand command,
            BattleRuntimeEnemyMoveResult moveResult,
            BattleRuntimeEnemyAttackDeclarationResult attackDeclaration,
            BattleRuntimeEnemyAttackResolutionResult attackResolution,
            BattleRuntimeEnemyRepeatedAttackResult automaticAttackResult,
            BattleRuntimeEnemyAbilityResult abilityResult,
            StatusKeyword blockedByStatus = StatusKeyword.None,
            BattleEventRecord statusBlockEvent = null)
        {
            Command = command;
            MoveResult = moveResult;
            AttackDeclaration = attackDeclaration;
            AttackResolution = attackResolution;
            AutomaticAttackResult = automaticAttackResult;
            AbilityResult = abilityResult;
            BlockedByStatus = blockedByStatus;
            StatusBlockEvent = statusBlockEvent;
        }

        public BattleRuntimeEnemyTurnCommand Command { get; }
        public BattleRuntimeEnemyMoveResult MoveResult { get; }
        public BattleRuntimeEnemyAttackDeclarationResult AttackDeclaration { get; }
        public BattleRuntimeEnemyAttackResolutionResult AttackResolution { get; }
        public BattleRuntimeEnemyRepeatedAttackResult AutomaticAttackResult { get; }
        public BattleRuntimeEnemyAbilityResult AbilityResult { get; }
        public StatusKeyword BlockedByStatus { get; }
        public BattleEventRecord StatusBlockEvent { get; }
        public bool WasBlockedByStatus =>
            BlockedByStatus != StatusKeyword.None;
    }
}
