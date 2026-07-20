using System;
using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public static class EncounterDataValidationService
    {
        public static List<string> ValidateEnemy(EnemyDefinitionData enemy)
        {
            List<string> errors = new();
            if (enemy == null)
            {
                errors.Add("Enemy definition is null.");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(enemy.EnemyId))
            {
                errors.Add("Enemy ID is required.");
            }

            if (string.IsNullOrWhiteSpace(enemy.DisplayName))
            {
                errors.Add($"Enemy '{enemy.EnemyId}' requires a display name.");
            }

            if (enemy.Attack < 0)
            {
                errors.Add($"Enemy '{enemy.EnemyId}' attack cannot be negative.");
            }

            if (enemy.MaximumHealth <= 0)
            {
                errors.Add($"Enemy '{enemy.EnemyId}' health must be positive.");
            }

            return errors;
        }

        public static List<string> ValidateEncounter(EncounterData encounter)
        {
            List<string> errors = new();
            if (encounter == null)
            {
                errors.Add("Encounter definition is null.");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(encounter.EncounterId))
            {
                errors.Add("Encounter ID is required.");
            }

            if (string.IsNullOrWhiteSpace(encounter.DisplayName))
            {
                errors.Add(
                    $"Encounter '{encounter.EncounterId}' requires a display name.");
            }

            if (!Enum.IsDefined(
                    typeof(BattleEncounterGrade),
                    encounter.EncounterGrade))
            {
                errors.Add(
                    $"Encounter '{encounter.EncounterId}' has an invalid encounter grade.");
            }

            if (encounter.EnemySlots == null ||
                encounter.EnemySlots.Count == 0)
            {
                errors.Add(
                    $"Encounter '{encounter.EncounterId}' requires at least one enemy.");
                return errors;
            }

            if (encounter.EnemySlots.Count > 3)
            {
                errors.Add(
                    $"Encounter '{encounter.EncounterId}' cannot exceed three enemy slots.");
            }

            HashSet<string> instanceIds = new(
                StringComparer.OrdinalIgnoreCase);
            HashSet<EnemyFieldPosition> positions = new();
            for (int i = 0; i < encounter.EnemySlots.Count; i++)
            {
                EncounterEnemySlot slot = encounter.EnemySlots[i];
                if (slot == null)
                {
                    errors.Add(
                        $"Encounter '{encounter.EncounterId}' slot {i} is null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(slot.EnemyInstanceId))
                {
                    errors.Add(
                        $"Encounter '{encounter.EncounterId}' slot {i} requires an instance ID.");
                }
                else if (!instanceIds.Add(slot.EnemyInstanceId))
                {
                    errors.Add(
                        $"Encounter '{encounter.EncounterId}' has duplicate instance ID '{slot.EnemyInstanceId}'.");
                }

                if (!positions.Add(slot.Position))
                {
                    errors.Add(
                        $"Encounter '{encounter.EncounterId}' has duplicate position '{slot.Position}'.");
                }

                errors.AddRange(ValidateEnemy(slot.Enemy));
            }

            return errors;
        }
    }
}
