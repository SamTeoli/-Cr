using System;
using System.Collections.Generic;
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
        [SerializeField] private int automaticAttackCount;
        [SerializeField] private List<int> attackTieBreakerValues = new();
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
            int automaticAttackCount,
            IEnumerable<int> attackTieBreakerValues,
            EnemyAbilityResolutionContext ability)
        {
            this.actionType = actionType;
            this.enemyId = enemyId;
            this.moveDirection = moveDirection;
            this.moveSteps = moveSteps;
            this.targetBattleCardId = targetBattleCardId;
            this.automaticAttackCount = automaticAttackCount;
            this.attackTieBreakerValues = attackTieBreakerValues == null
                ? new List<int>()
                : new List<int>(attackTieBreakerValues);
            this.ability = ability;
        }

        public BattleRuntimeEnemyTurnActionType ActionType => actionType;
        public string EnemyId => enemyId;
        public EnemyMoveDirection MoveDirection => moveDirection;
        public int MoveSteps => moveSteps;
        public string TargetBattleCardId => targetBattleCardId;
        public int AutomaticAttackCount => automaticAttackCount;
        public IReadOnlyList<int> AttackTieBreakerValues =>
            attackTieBreakerValues ??= new List<int>();
        public bool UsesAutomaticTargeting => automaticAttackCount > 0;
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
                0,
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
                0,
                null,
                default);
        }

        public static BattleRuntimeEnemyTurnCommand CreateAutomaticAttack(
            string enemyId,
            int attackCount,
            IEnumerable<int> tieBreakerValues)
        {
            return new BattleRuntimeEnemyTurnCommand(
                BattleRuntimeEnemyTurnActionType.Attack,
                enemyId,
                default,
                0,
                null,
                attackCount,
                tieBreakerValues,
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
                0,
                null,
                ability);
        }
    }
}
