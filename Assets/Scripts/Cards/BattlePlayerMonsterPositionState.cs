using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattlePlayerMonsterPositionState
    {
        [SerializeField] private string leftBattleCardId;
        [SerializeField] private string centerBattleCardId;
        [SerializeField] private string rightBattleCardId;

        public int Count
        {
            get
            {
                int count = 0;
                if (!string.IsNullOrWhiteSpace(leftBattleCardId)) count++;
                if (!string.IsNullOrWhiteSpace(centerBattleCardId)) count++;
                if (!string.IsNullOrWhiteSpace(rightBattleCardId)) count++;
                return count;
            }
        }

        public bool TryPlace(
            string battleCardId,
            PlayerMonsterFieldPosition position)
        {
            if (string.IsNullOrWhiteSpace(battleCardId) ||
                FindPosition(battleCardId).HasValue ||
                !string.IsNullOrWhiteSpace(GetOccupant(position)))
            {
                return false;
            }

            SetOccupant(position, battleCardId.Trim());
            return true;
        }

        public bool TryRemove(string battleCardId)
        {
            PlayerMonsterFieldPosition? position =
                FindPosition(battleCardId);
            if (!position.HasValue)
            {
                return false;
            }

            SetOccupant(position.Value, null);
            return true;
        }

        public bool TryGetFirstEmpty(
            out PlayerMonsterFieldPosition position)
        {
            foreach (PlayerMonsterFieldPosition candidate in
                     Enum.GetValues(typeof(PlayerMonsterFieldPosition)))
            {
                if (string.IsNullOrWhiteSpace(GetOccupant(candidate)))
                {
                    position = candidate;
                    return true;
                }
            }

            position = default;
            return false;
        }

        public string GetOccupant(PlayerMonsterFieldPosition position)
        {
            return position switch
            {
                PlayerMonsterFieldPosition.Left => leftBattleCardId,
                PlayerMonsterFieldPosition.Center => centerBattleCardId,
                PlayerMonsterFieldPosition.Right => rightBattleCardId,
                _ => null
            };
        }

        public PlayerMonsterFieldPosition? FindPosition(
            string battleCardId)
        {
            if (string.IsNullOrWhiteSpace(battleCardId))
            {
                return null;
            }

            foreach (PlayerMonsterFieldPosition position in
                     Enum.GetValues(typeof(PlayerMonsterFieldPosition)))
            {
                if (string.Equals(
                        GetOccupant(position),
                        battleCardId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return position;
                }
            }

            return null;
        }

        private void SetOccupant(
            PlayerMonsterFieldPosition position,
            string battleCardId)
        {
            switch (position)
            {
                case PlayerMonsterFieldPosition.Left:
                    leftBattleCardId = battleCardId;
                    break;
                case PlayerMonsterFieldPosition.Center:
                    centerBattleCardId = battleCardId;
                    break;
                case PlayerMonsterFieldPosition.Right:
                    rightBattleCardId = battleCardId;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(position), position, null);
            }
        }
    }
}
