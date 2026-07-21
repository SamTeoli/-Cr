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
