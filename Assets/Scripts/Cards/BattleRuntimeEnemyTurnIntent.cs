using System;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleRuntimeEnemyTurnIntent
    {
        internal BattleRuntimeEnemyTurnIntent(
            BattleRuntimeEnemyTurnCommand command)
        {
            ActionType = command.ActionType;
            EnemyId = command.EnemyId;
            MoveDirection = command.MoveDirection;
            MoveSteps = command.MoveSteps;
            TargetBattleCardId = command.TargetBattleCardId;
            AbilityId = command.Ability.AbilityId;
            AbilityAffectsFriendlySide =
                command.Ability.AffectsFriendlySide;
            AbilityIsArea = command.Ability.IsAreaAbility;
        }

        public BattleRuntimeEnemyTurnActionType ActionType { get; }
        public string EnemyId { get; }
        public EnemyMoveDirection MoveDirection { get; }
        public int MoveSteps { get; }
        public string TargetBattleCardId { get; }
        public string AbilityId { get; }
        public bool AbilityAffectsFriendlySide { get; }
        public bool AbilityIsArea { get; }
    }
}
