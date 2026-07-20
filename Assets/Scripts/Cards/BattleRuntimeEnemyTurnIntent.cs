using System;
using System.Collections.Generic;

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
            AutomaticAttackCount = command.AutomaticAttackCount;
            AttackTieBreakerValues = new List<int>(
                command.AttackTieBreakerValues).AsReadOnly();
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
        public int AutomaticAttackCount { get; }
        public IReadOnlyList<int> AttackTieBreakerValues { get; }
        public bool UsesAutomaticTargeting => AutomaticAttackCount > 0;
        public string AbilityId { get; }
        public bool AbilityAffectsFriendlySide { get; }
        public bool AbilityIsArea { get; }
    }
}
