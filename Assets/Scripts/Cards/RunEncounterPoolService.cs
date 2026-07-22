using System;
using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public static class RunEncounterPoolService
    {
        public static bool TryResolve(
            EncounterDatabase database,
            IReadOnlyList<string> encounterIds,
            BattleEncounterGrade grade,
            int seed,
            out EncounterData encounter,
            out string error)
        {
            encounter = null;
            error = null;
            if (database == null || encounterIds == null || encounterIds.Count == 0)
            {
                error = $"{grade} encounter pool is empty.";
                return false;
            }

            int index = PositiveMod(seed, encounterIds.Count);
            string encounterId = encounterIds[index];
            if (!database.TryGetEncounter(encounterId, out encounter))
            {
                error = $"Encounter '{encounterId}' was not found.";
                return false;
            }

            if (encounter.EncounterGrade != grade)
            {
                error = $"Encounter '{encounterId}' is {encounter.EncounterGrade}, not {grade}.";
                encounter = null;
                return false;
            }

            return true;
        }

        public static List<string> Validate(
            EncounterDatabase database,
            IReadOnlyDictionary<BattleEncounterGrade, IReadOnlyList<string>> pools)
        {
            List<string> errors = new();
            foreach (BattleEncounterGrade grade in Enum.GetValues(typeof(BattleEncounterGrade)))
            {
                if (pools == null || !pools.TryGetValue(grade, out var ids) ||
                    ids == null || ids.Count == 0)
                {
                    errors.Add($"{grade} encounter pool requires at least one entry.");
                    continue;
                }

                HashSet<string> unique = new(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < ids.Count; i++)
                {
                    string id = ids[i];
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        errors.Add($"{grade} encounter pool entry {i} has an empty ID.");
                    }
                    else if (!unique.Add(id))
                    {
                        errors.Add($"{grade} encounter pool has duplicate ID '{id}'.");
                    }
                    else if (database == null || !database.TryGetEncounter(id, out var encounter))
                    {
                        errors.Add($"{grade} encounter pool references missing encounter '{id}'.");
                    }
                    else if (encounter.EncounterGrade != grade)
                    {
                        errors.Add($"Encounter '{id}' is {encounter.EncounterGrade}, not {grade}.");
                    }
                }
            }

            return errors;
        }

        public static List<string> ValidatePool(EncounterDatabase database,
            IReadOnlyList<string> ids, BattleEncounterGrade grade, string label)
        {
            List<string> errors = new();
            if (ids == null || ids.Count == 0)
            {
                errors.Add($"{label} encounter pool requires at least one entry.");
                return errors;
            }
            HashSet<string> unique = new(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i];
                if (string.IsNullOrWhiteSpace(id))
                    errors.Add($"{label} encounter pool entry {i} has an empty ID.");
                else if (!unique.Add(id))
                    errors.Add($"{label} encounter pool has duplicate ID '{id}'.");
                else if (database == null || !database.TryGetEncounter(id, out var encounter))
                    errors.Add($"{label} encounter pool references missing encounter '{id}'.");
                else if (encounter.EncounterGrade != grade)
                    errors.Add($"Encounter '{id}' is {encounter.EncounterGrade}, not {grade}.");
            }
            return errors;
        }

        private static int PositiveMod(int value, int modulus)
        {
            int remainder = value % modulus;
            return remainder < 0 ? remainder + modulus : remainder;
        }
    }
}
