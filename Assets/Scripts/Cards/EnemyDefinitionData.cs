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

        public string EnemyId => enemyId;
        public string DisplayName => displayName;
        public int Attack => attack;
        public int MaximumHealth => maximumHealth;

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
#endif

        private void OnValidate()
        {
            enemyId = enemyId?.Trim();
            displayName = displayName?.Trim();
            attack = Mathf.Max(0, attack);
            maximumHealth = Mathf.Max(1, maximumHealth);
        }
    }
}
