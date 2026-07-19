using System.Collections.Generic;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleEnemyMovementValidation
    {
        [MenuItem("Have a Break/Validate Enemy Position Movement")]
        private static void ValidateFromMenu()
        {
            Validate(true);
        }

        internal static bool Validate(bool showDialog)
        {
            bool valid = ValidateWrap() &&
                         ValidatePushIntoEmptyPosition() &&
                         ValidateFullRotation() &&
                         ValidateLockedFailure();

            if (!valid)
            {
                Debug.LogError("Enemy position movement validation failed.");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Enemy Position Movement Validation",
                    valid
                        ? "Enemy position movement passed."
                        : "Enemy position movement failed. Check the Console.",
                    "OK");
            }

            return valid;
        }

        private static bool ValidateWrap()
        {
            BattleEnemyPositionState positions = new();
            bool valid = positions.TryPlace("ENEMY-A", EnemyFieldPosition.Left);
            valid &= BattleEnemyMovementResolver.TryMoveOneStep(
                         positions, null, "ENEMY-A", EnemyMoveDirection.Left,
                         out List<EnemyPositionMoveRecord> moves,
                         out EnemyPositionMoveFailure failure) &&
                     failure == EnemyPositionMoveFailure.None && moves.Count == 1 &&
                     moves[0].From == EnemyFieldPosition.Left &&
                     moves[0].To == EnemyFieldPosition.Right && !moves[0].Pushed &&
                     positions.GetOccupant(EnemyFieldPosition.Right) == "ENEMY-A";
            return valid;
        }

        private static bool ValidatePushIntoEmptyPosition()
        {
            BattleEnemyPositionState positions = new();
            bool valid = positions.TryPlace("ENEMY-A", EnemyFieldPosition.Center) &&
                         positions.TryPlace("ENEMY-B", EnemyFieldPosition.Left);
            valid &= BattleEnemyMovementResolver.TryMoveOneStep(
                         positions, null, "ENEMY-A", EnemyMoveDirection.Left,
                         out List<EnemyPositionMoveRecord> moves, out _) &&
                     moves.Count == 2 && moves[0].EnemyId == "ENEMY-A" && !moves[0].Pushed &&
                     moves[1].EnemyId == "ENEMY-B" && moves[1].Pushed &&
                     positions.GetOccupant(EnemyFieldPosition.Left) == "ENEMY-A" &&
                     positions.GetOccupant(EnemyFieldPosition.Right) == "ENEMY-B" &&
                     positions.GetOccupant(EnemyFieldPosition.Center) == null;
            return valid;
        }

        private static bool ValidateFullRotation()
        {
            BattleEnemyPositionState positions = new();
            bool valid = positions.TryPlace("ENEMY-A", EnemyFieldPosition.Center) &&
                         positions.TryPlace("ENEMY-B", EnemyFieldPosition.Left) &&
                         positions.TryPlace("ENEMY-C", EnemyFieldPosition.Right);
            valid &= BattleEnemyMovementResolver.TryMoveOneStep(
                         positions, null, "ENEMY-A", EnemyMoveDirection.Left,
                         out List<EnemyPositionMoveRecord> moves, out _) &&
                     moves.Count == 3 &&
                     positions.GetOccupant(EnemyFieldPosition.Left) == "ENEMY-A" &&
                     positions.GetOccupant(EnemyFieldPosition.Center) == "ENEMY-C" &&
                     positions.GetOccupant(EnemyFieldPosition.Right) == "ENEMY-B";
            return valid;
        }

        private static bool ValidateLockedFailure()
        {
            BattleEnemyPositionState positions = new();
            bool valid = positions.TryPlace("ENEMY-A", EnemyFieldPosition.Center) &&
                         positions.TryPlace("ENEMY-B", EnemyFieldPosition.Left);
            BattleEnemyMovementLockState locks = new();
            valid &= locks.TryLock("ENEMY-B");
            valid &= !BattleEnemyMovementResolver.TryMoveOneStep(
                         positions, locks, "ENEMY-A", EnemyMoveDirection.Left,
                         out List<EnemyPositionMoveRecord> moves,
                         out EnemyPositionMoveFailure failure) &&
                     failure == EnemyPositionMoveFailure.MovementLocked && moves.Count == 0 &&
                     positions.GetOccupant(EnemyFieldPosition.Center) == "ENEMY-A" &&
                     positions.GetOccupant(EnemyFieldPosition.Left) == "ENEMY-B" &&
                     positions.GetOccupant(EnemyFieldPosition.Right) == null;

            valid &= locks.TryUnlock("ENEMY-B") && locks.TryLock("ENEMY-A");
            valid &= !BattleEnemyMovementResolver.TryMoveOneStep(
                         positions, locks, "ENEMY-A", EnemyMoveDirection.Right,
                         out _, out EnemyPositionMoveFailure targetFailure) &&
                     targetFailure == EnemyPositionMoveFailure.MovementLocked &&
                     positions.GetOccupant(EnemyFieldPosition.Center) == "ENEMY-A";
            return valid;
        }
    }
}
