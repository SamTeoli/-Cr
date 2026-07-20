namespace HaveABreak.Cards
{
    public sealed class BattleRuntimeEnemyAttackTargetResult
    {
        internal BattleRuntimeEnemyAttackTargetResult(
            string attackerEnemyId,
            EnemyFieldPosition attackerPosition,
            BattleRuntimeEnemyAttackTargetType targetType,
            BattleMonsterState targetMonster,
            PlayerMonsterFieldPosition? targetPosition,
            int equalDistanceCandidateCount)
        {
            AttackerEnemyId = attackerEnemyId;
            AttackerPosition = attackerPosition;
            TargetType = targetType;
            TargetMonster = targetMonster;
            TargetPosition = targetPosition;
            EqualDistanceCandidateCount = equalDistanceCandidateCount;
        }

        public string AttackerEnemyId { get; }
        public EnemyFieldPosition AttackerPosition { get; }
        public BattleRuntimeEnemyAttackTargetType TargetType { get; }
        public BattleMonsterState TargetMonster { get; }
        public PlayerMonsterFieldPosition? TargetPosition { get; }
        public int EqualDistanceCandidateCount { get; }
        public bool UsedTieBreaker => EqualDistanceCandidateCount > 1;
        public string TargetId => TargetType ==
            BattleRuntimeEnemyAttackTargetType.Player
                ? BattlePlayerState.PlayerTargetId
                : TargetMonster?.BattleCardId;
    }
}
