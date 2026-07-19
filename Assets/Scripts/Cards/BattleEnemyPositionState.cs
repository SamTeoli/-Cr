using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleEnemyPositionState
    {
        [SerializeField] private string leftEnemyId;
        [SerializeField] private string centerEnemyId;
        [SerializeField] private string rightEnemyId;

        public bool TryPlace(string enemyId, EnemyFieldPosition position)
        {
            if (string.IsNullOrWhiteSpace(enemyId) || FindPosition(enemyId).HasValue ||
                !string.IsNullOrWhiteSpace(GetOccupant(position)))
            {
                return false;
            }

            SetOccupant(position, enemyId.Trim());
            return true;
        }

        public bool TryMove(string enemyId, EnemyFieldPosition destination)
        {
            EnemyFieldPosition? current = FindPosition(enemyId);
            if (!current.HasValue || current.Value == destination ||
                !string.IsNullOrWhiteSpace(GetOccupant(destination)))
            {
                return false;
            }

            SetOccupant(current.Value, null);
            SetOccupant(destination, enemyId.Trim());
            return true;
        }

        public bool TryRemove(string enemyId)
        {
            EnemyFieldPosition? position = FindPosition(enemyId);
            if (!position.HasValue)
            {
                return false;
            }

            SetOccupant(position.Value, null);
            return true;
        }

        public string GetOccupant(EnemyFieldPosition position)
        {
            return position switch
            {
                EnemyFieldPosition.Left => leftEnemyId,
                EnemyFieldPosition.Center => centerEnemyId,
                EnemyFieldPosition.Right => rightEnemyId,
                _ => null
            };
        }

        public EnemyFieldPosition? FindPosition(string enemyId)
        {
            if (string.IsNullOrWhiteSpace(enemyId))
            {
                return null;
            }

            foreach (EnemyFieldPosition position in Enum.GetValues(typeof(EnemyFieldPosition)))
            {
                if (string.Equals(
                        GetOccupant(position), enemyId, StringComparison.OrdinalIgnoreCase))
                {
                    return position;
                }
            }

            return null;
        }

        private void SetOccupant(EnemyFieldPosition position, string enemyId)
        {
            switch (position)
            {
                case EnemyFieldPosition.Left:
                    leftEnemyId = enemyId;
                    break;
                case EnemyFieldPosition.Center:
                    centerEnemyId = enemyId;
                    break;
                case EnemyFieldPosition.Right:
                    rightEnemyId = enemyId;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(position), position, null);
            }
        }
    }
}
