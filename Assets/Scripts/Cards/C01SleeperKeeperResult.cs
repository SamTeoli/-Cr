using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public readonly struct C01SleeperKeeperResult
    {
        public C01SleeperKeeperResult(
            string resolvedTargetEnemyId,
            bool movementSucceeded,
            EnemyPositionMoveFailure movementFailure,
            int defenseGained,
            IReadOnlyList<EnemyPositionMoveRecord> moves)
        {
            ResolvedTargetEnemyId = resolvedTargetEnemyId;
            MovementSucceeded = movementSucceeded;
            MovementFailure = movementFailure;
            DefenseGained = defenseGained;
            Moves = moves;
        }

        public string ResolvedTargetEnemyId { get; }
        public bool MovementSucceeded { get; }
        public EnemyPositionMoveFailure MovementFailure { get; }
        public int DefenseGained { get; }
        public IReadOnlyList<EnemyPositionMoveRecord> Moves { get; }
    }
}
