using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [CreateAssetMenu(
        fileName = "EnemyDefinition",
        menuName = "Have a Break/Enemies/Enemy Definition")]
    public sealed class EnemyDefinitionData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string enemyId;
        [SerializeField] private string displayName;

        [Header("Base Combat Stats")]
        [SerializeField, Min(0)] private int attack;
        [SerializeField, Min(1)] private int maximumHealth = 1;

        [Header("Turn Pattern")]
        [SerializeField] private EnemyActionPatternData actionPattern = new();

        public string EnemyId => enemyId;
        public string DisplayName => displayName;
        public int Attack => attack;
        public int MaximumHealth => maximumHealth;
        public EnemyActionPatternData ActionPattern =>
            actionPattern ??= new EnemyActionPatternData();

#if UNITY_EDITOR
        public void EditorInitialize(
            string id,
            string enemyName,
            int baseAttack,
            int health)
        {
            enemyId = id;
            displayName = enemyName;
            attack = baseAttack;
            maximumHealth = health;
        }

        public void EditorSetActionPattern(EnemyActionPatternData pattern)
        {
            actionPattern = pattern;
        }
#endif

        private void OnValidate()
        {
            enemyId = enemyId?.Trim();
            displayName = displayName?.Trim();
            attack = Mathf.Max(0, attack);
            maximumHealth = Mathf.Max(1, maximumHealth);
            actionPattern ??= new EnemyActionPatternData();
        }
    }

    [Serializable]
    public sealed class EnemyActionPatternData
    {
        [SerializeField]
        private List<EnemyTurnPatternStep> turns = new()
        {
            new EnemyTurnPatternStep()
        };

        public EnemyActionPatternData()
        {
        }

        public EnemyActionPatternData(
            IEnumerable<EnemyTurnPatternStep> patternTurns)
        {
            turns = patternTurns == null
                ? new List<EnemyTurnPatternStep>()
                : new List<EnemyTurnPatternStep>(patternTurns);
        }

        public IReadOnlyList<EnemyTurnPatternStep> Turns =>
            turns ??= new List<EnemyTurnPatternStep>();

        public bool TryGetTurn(
            int playerTurnNumber,
            out EnemyTurnPatternStep turn)
        {
            turn = null;
            if (playerTurnNumber <= 0 || Turns.Count == 0)
            {
                return false;
            }

            turn = Turns[(playerTurnNumber - 1) % Turns.Count];
            return turn != null;
        }
    }

    [Serializable]
    public sealed class EnemyTurnPatternStep
    {
        [SerializeField] private bool moves;
        [SerializeField] private EnemyMoveDirection moveDirection;
        [SerializeField, Min(1)] private int moveSteps = 1;
        [SerializeField, Min(0)] private int attackCount = 1;
        [SerializeField]
        private List<EnemyPatternAbilityData> abilities = new();

        public EnemyTurnPatternStep()
        {
        }

        public EnemyTurnPatternStep(
            bool movesBeforeAttack,
            EnemyMoveDirection direction,
            int steps,
            int automaticAttackCount,
            IEnumerable<EnemyPatternAbilityData> turnAbilities = null)
        {
            moves = movesBeforeAttack;
            moveDirection = direction;
            moveSteps = steps;
            attackCount = automaticAttackCount;
            abilities = turnAbilities == null
                ? new List<EnemyPatternAbilityData>()
                : new List<EnemyPatternAbilityData>(turnAbilities);
        }

        public bool Moves => moves;
        public EnemyMoveDirection MoveDirection => moveDirection;
        public int MoveSteps => moveSteps;
        public int AttackCount => attackCount;
        public IReadOnlyList<EnemyPatternAbilityData> Abilities =>
            abilities ??= new List<EnemyPatternAbilityData>();
    }

    [Serializable]
    public sealed class EnemyPatternAbilityData
    {
        [SerializeField] private string abilityId;
        [SerializeField] private bool affectsFriendlySide = true;
        [SerializeField] private bool isAreaAbility;

        private EnemyPatternAbilityData()
        {
        }

        public EnemyPatternAbilityData(
            string id,
            bool affectsAllies,
            bool isArea)
        {
            abilityId = id?.Trim();
            affectsFriendlySide = affectsAllies;
            isAreaAbility = isArea;
        }

        public string AbilityId => abilityId;
        public bool AffectsFriendlySide => affectsFriendlySide;
        public bool IsAreaAbility => isAreaAbility;
    }
}
