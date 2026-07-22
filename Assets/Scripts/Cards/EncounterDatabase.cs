using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [CreateAssetMenu(
        fileName = "EncounterDatabase",
        menuName = "Have a Break/Enemies/Encounter Database")]
    public sealed class EncounterDatabase : ScriptableObject
    {
        [SerializeField] private List<EncounterData> encounters = new();

        public IReadOnlyList<EncounterData> Encounters => encounters != null
            ? encounters
            : Array.Empty<EncounterData>();

        public bool TryGetEncounter(
            string encounterId,
            out EncounterData encounter)
        {
            encounter = null;
            if (encounters == null ||
                string.IsNullOrWhiteSpace(encounterId))
            {
                return false;
            }

            encounter = encounters.Find(item => item != null &&
                string.Equals(
                    item.EncounterId,
                    encounterId,
                    StringComparison.OrdinalIgnoreCase));
            return encounter != null;
        }

        public List<string> GetValidationErrors()
        {
            List<string> errors = new();
            if (encounters == null)
            {
                errors.Add("Encounter database list is missing.");
                return errors;
            }

            HashSet<string> ids = new(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, EnemyDefinitionData> enemiesById =
                new(StringComparer.OrdinalIgnoreCase);
            Dictionary<EnemyDefinitionData, BattleEncounterGrade>
                enemyGrades = new();
            for (int i = 0; i < encounters.Count; i++)
            {
                EncounterData encounter = encounters[i];
                if (encounter == null)
                {
                    errors.Add($"Encounter database entry {i} is null.");
                    continue;
                }

                if (!ids.Add(encounter.EncounterId ?? string.Empty))
                {
                    errors.Add(
                        $"Encounter database has duplicate ID '{encounter.EncounterId}'.");
                }

                errors.AddRange(
                    EncounterDataValidationService.ValidateEncounter(
                        encounter));

                if (encounter.EnemySlots == null)
                {
                    continue;
                }

                foreach (EncounterEnemySlot slot in encounter.EnemySlots)
                {
                    EnemyDefinitionData enemy = slot?.Enemy;
                    if (enemy == null ||
                        string.IsNullOrWhiteSpace(enemy.EnemyId))
                    {
                        continue;
                    }

                    if (enemiesById.TryGetValue(
                            enemy.EnemyId, out EnemyDefinitionData existing) &&
                        !ReferenceEquals(existing, enemy))
                    {
                        errors.Add(
                            $"Enemy database references duplicate ID '{enemy.EnemyId}' from different assets.");
                    }
                    else
                    {
                        enemiesById[enemy.EnemyId] = enemy;
                    }

                    if (enemyGrades.TryGetValue(
                            enemy, out BattleEncounterGrade existingGrade) &&
                        existingGrade != encounter.EncounterGrade)
                    {
                        errors.Add(
                            $"Enemy '{enemy.EnemyId}' is shared by encounter grades '{existingGrade}' and '{encounter.EncounterGrade}'.");
                    }
                    else
                    {
                        enemyGrades[enemy] = encounter.EncounterGrade;
                    }
                }
            }

            return errors;
        }

#if UNITY_EDITOR
        public void EditorSetEncounters(IEnumerable<EncounterData> values)
        {
            encounters = values == null
                ? new List<EncounterData>()
                : new List<EncounterData>(values);
        }
#endif
    }
}
