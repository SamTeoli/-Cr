using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    public enum BattleRuntimeEnemyTurnActionType
    {
        Move,
        Attack,
        Ability
    }

    [Serializable]
    public sealed class BattleRuntimeEnemyTurnCommand
    {
        [SerializeField] private BattleRuntimeEnemyTurnActionType actionType;
        [SerializeField] private string enemyId;
        [SerializeField] private EnemyMoveDirection moveDirection;
        [SerializeField] private int moveSteps;
        [SerializeField] private string targetBattleCardId;
        [SerializeField] private EnemyAbilityResolutionContext ability;

        private BattleRuntimeEnemyTurnCommand()
        {
        }

        private BattleRuntimeEnemyTurnCommand(
            BattleRuntimeEnemyTurnActionType actionType,
            string enemyId,
            EnemyMoveDirection moveDirection,
            int moveSteps,
            string targetBattleCardId,
            EnemyAbilityResolutionContext ability)
        {
            this.actionType = actionType;
            this.enemyId = enemyId;
            this.moveDirection = moveDirection;
            this.moveSteps = moveSteps;
            this.targetBattleCardId = targetBattleCardId;
            this.ability = ability;
        }

        public BattleRuntimeEnemyTurnActionType ActionType => actionType;
        public string EnemyId => enemyId;
        public EnemyMoveDirection MoveDirection => moveDirection;
        public int MoveSteps => moveSteps;
        public string TargetBattleCardId => targetBattleCardId;
        public EnemyAbilityResolutionContext Ability => ability;

        public static BattleRuntimeEnemyTurnCommand CreateMove(
            string enemyId,
            EnemyMoveDirection direction,
            int steps)
        {
            return new BattleRuntimeEnemyTurnCommand(
                BattleRuntimeEnemyTurnActionType.Move,
                enemyId,
                direction,
                steps,
                null,
                default);
        }

        public static BattleRuntimeEnemyTurnCommand CreateAttack(
            string enemyId,
            string targetBattleCardId)
        {
            return new BattleRuntimeEnemyTurnCommand(
                BattleRuntimeEnemyTurnActionType.Attack,
                enemyId,
                default,
                0,
                targetBattleCardId,
                default);
        }

        public static BattleRuntimeEnemyTurnCommand CreateAbility(
            EnemyAbilityResolutionContext ability)
        {
            return new BattleRuntimeEnemyTurnCommand(
                BattleRuntimeEnemyTurnActionType.Ability,
                ability.SourceEnemyId,
                default,
                0,
                null,
                ability);
        }
    }
}
