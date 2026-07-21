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

            ValidateActionPattern(enemy, errors);

            return errors;
        }

        private static void ValidateActionPattern(
            EnemyDefinitionData enemy,
            List<string> errors)
        {
            EnemyActionPatternData pattern = enemy.ActionPattern;
            if (pattern?.Turns == null || pattern.Turns.Count == 0)
            {
                errors.Add(
                    $"Enemy '{enemy.EnemyId}' requires at least one turn pattern.");
                return;
            }

            for (int turnIndex = 0;
                 turnIndex < pattern.Turns.Count;
                 turnIndex++)
            {
                EnemyTurnPatternStep turn = pattern.Turns[turnIndex];
                if (turn == null)
                {
                    errors.Add(
                        $"Enemy '{enemy.EnemyId}' turn pattern {turnIndex} is null.");
                    continue;
                }

                if (turn.Moves &&
                    (turn.MoveSteps <= 0 ||
                     (turn.MoveDirection != EnemyMoveDirection.Left &&
                      turn.MoveDirection != EnemyMoveDirection.Right)))
                {
                    errors.Add(
                        $"Enemy '{enemy.EnemyId}' turn pattern {turnIndex} has invalid movement.");
                }

                if (turn.AttackCount < 0)
                {
                    errors.Add(
                        $"Enemy '{enemy.EnemyId}' turn pattern {turnIndex} has a negative attack count.");
                }

                if (!turn.Moves && turn.AttackCount == 0 &&
                    turn.Abilities.Count == 0)
                {
                    errors.Add(
                        $"Enemy '{enemy.EnemyId}' turn pattern {turnIndex} has no action.");
                }

                HashSet<string> abilityIds = new(
                    StringComparer.OrdinalIgnoreCase);
                for (int abilityIndex = 0;
                     abilityIndex < turn.Abilities.Count;
                     abilityIndex++)
                {
                    EnemyPatternAbilityData ability =
                        turn.Abilities[abilityIndex];
                    if (ability == null ||
                        string.IsNullOrWhiteSpace(ability.AbilityId))
                    {
                        errors.Add(
                            $"Enemy '{enemy.EnemyId}' turn pattern {turnIndex} ability {abilityIndex} requires an ID.");
                    }
                    else if (!abilityIds.Add(ability.AbilityId))
                    {
                        errors.Add(
                            $"Enemy '{enemy.EnemyId}' turn pattern {turnIndex} has duplicate ability ID '{ability.AbilityId}'.");
                    }

                    if (ability != null &&
                        (!Enum.IsDefined(
                             typeof(StatusKeyword),
                             ability.StatusKeyword) ||
                         (ability.StatusKeyword == StatusKeyword.None &&
                          ability.StatusAmount != 0) ||
                         (ability.StatusKeyword != StatusKeyword.None &&
                          ability.StatusAmount <= 0)))
                    {
                        errors.Add(
                            $"Enemy '{enemy.EnemyId}' turn pattern {turnIndex} ability {abilityIndex} has an invalid status effect.");
                    }
                }
            }
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
