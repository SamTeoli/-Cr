namespace HaveABreak.Cards
{
    public sealed class BattleRuntimeEnemyAutoAttackResult
    {
        internal BattleRuntimeEnemyAutoAttackResult(
            BattleRuntimeEnemyAttackTargetResult target,
            BattleRuntimeEnemyAttackDeclarationResult monsterDeclaration,
            BattleRuntimeEnemyAttackResolutionResult monsterResolution,
            BattleRuntimeEnemyDirectAttackResult playerResolution)
        {
            Target = target;
            MonsterDeclaration = monsterDeclaration;
            MonsterResolution = monsterResolution;
            PlayerResolution = playerResolution;
        }

        public BattleRuntimeEnemyAttackTargetResult Target { get; }
        public BattleRuntimeEnemyAttackDeclarationResult MonsterDeclaration { get; }
        public BattleRuntimeEnemyAttackResolutionResult MonsterResolution { get; }
        public BattleRuntimeEnemyDirectAttackResult PlayerResolution { get; }
        public bool AttackedPlayer => Target.TargetType ==
            BattleRuntimeEnemyAttackTargetType.Player;
        public BattleEventRecord CompletedAttack => AttackedPlayer
            ? PlayerResolution?.CompletedAttack
            : MonsterResolution?.CompletedAttack;
    }
}
