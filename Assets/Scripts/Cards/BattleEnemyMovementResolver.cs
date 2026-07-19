using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public static class BattleEnemyMovementResolver
    {
        private readonly struct OccupiedStep
        {
            public OccupiedStep(string enemyId, EnemyFieldPosition position)
            {
                EnemyId = enemyId;
                Position = position;
            }

            public string EnemyId { get; }
            public EnemyFieldPosition Position { get; }
        }

        public static bool TryMoveOneStep(
            BattleEnemyPositionState positions,
            BattleEnemyMovementLockState movementLocks,
            string enemyId,
            EnemyMoveDirection direction,
            out List<EnemyPositionMoveRecord> moves,
            out EnemyPositionMoveFailure failure)
        {
            moves = new List<EnemyPositionMoveRecord>();
            if (positions == null || string.IsNullOrWhiteSpace(enemyId))
            {
                failure = EnemyPositionMoveFailure.InvalidState;
                return false;
            }

            EnemyFieldPosition? start = positions.FindPosition(enemyId);
            if (!start.HasValue)
            {
                failure = EnemyPositionMoveFailure.EnemyNotFound;
                return false;
            }

            if (movementLocks != null && movementLocks.IsLocked(enemyId))
            {
                failure = EnemyPositionMoveFailure.MovementLocked;
                return false;
            }

            List<OccupiedStep> pushed = new();
            EnemyFieldPosition cursor = Next(start.Value, direction);
            while (cursor != start.Value)
            {
                string occupant = positions.GetOccupant(cursor);
                if (string.IsNullOrWhiteSpace(occupant))
                {
                    break;
                }

                if (movementLocks != null && movementLocks.IsLocked(occupant))
                {
                    failure = EnemyPositionMoveFailure.MovementLocked;
                    return false;
                }

                pushed.Add(new OccupiedStep(occupant, cursor));
                cursor = Next(cursor, direction);
            }

            positions.SetOccupant(start.Value, null);
            for (int i = pushed.Count - 1; i >= 0; i--)
            {
                OccupiedStep step = pushed[i];
                EnemyFieldPosition destination = Next(step.Position, direction);
                positions.SetOccupant(step.Position, null);
                positions.SetOccupant(destination, step.EnemyId);
            }

            EnemyFieldPosition targetDestination = Next(start.Value, direction);
            positions.SetOccupant(targetDestination, enemyId.Trim());
            moves.Add(new EnemyPositionMoveRecord(
                enemyId.Trim(), start.Value, targetDestination, false));
            foreach (OccupiedStep step in pushed)
            {
                moves.Add(new EnemyPositionMoveRecord(
                    step.EnemyId,
                    step.Position,
                    Next(step.Position, direction),
                    true));
            }

            failure = EnemyPositionMoveFailure.None;
            return true;
        }

        private static EnemyFieldPosition Next(
            EnemyFieldPosition position,
            EnemyMoveDirection direction)
        {
            if (direction == EnemyMoveDirection.Left)
            {
                return position switch
                {
                    EnemyFieldPosition.Left => EnemyFieldPosition.Right,
                    EnemyFieldPosition.Center => EnemyFieldPosition.Left,
                    _ => EnemyFieldPosition.Center
                };
            }

            return position switch
            {
                EnemyFieldPosition.Left => EnemyFieldPosition.Center,
                EnemyFieldPosition.Center => EnemyFieldPosition.Right,
                _ => EnemyFieldPosition.Left
            };
        }
    }
}
