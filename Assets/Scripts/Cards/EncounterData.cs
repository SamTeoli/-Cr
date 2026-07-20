using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [CreateAssetMenu(
        fileName = "Encounter",
        menuName = "Have a Break/Enemies/Encounter")]
    public sealed class EncounterData : ScriptableObject
    {
        [SerializeField] private string encounterId;
        [SerializeField] private string displayName;
        [SerializeField] private BattleEncounterGrade encounterGrade;
        [SerializeField] private List<EncounterEnemySlot> enemySlots = new();

        public string EncounterId => encounterId;
        public string DisplayName => displayName;
        public BattleEncounterGrade EncounterGrade => encounterGrade;
        public IReadOnlyList<EncounterEnemySlot> EnemySlots => enemySlots;

#if UNITY_EDITOR
        public void EditorInitialize(
            string id,
            string encounterName,
            IEnumerable<EncounterEnemySlot> slots)
        {
            EditorInitialize(
                id,
                encounterName,
                BattleEncounterGrade.Normal,
                slots);
        }

        public void EditorInitialize(
            string id,
            string encounterName,
            BattleEncounterGrade grade,
            IEnumerable<EncounterEnemySlot> slots)
        {
            encounterId = id;
            displayName = encounterName;
            encounterGrade = grade;
            enemySlots = slots == null
                ? new List<EncounterEnemySlot>()
                : new List<EncounterEnemySlot>(slots);
        }
#endif

        private void OnValidate()
        {
            encounterId = encounterId?.Trim();
            displayName = displayName?.Trim();
        }
    }
}
