using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [CreateAssetMenu(fileName = "RunEncounterProgressionConfig",
        menuName = "Have a Break/Run/Encounter Progression Config")]
    public sealed class RunEncounterProgressionConfig : ScriptableObject
    {
        [Serializable]
        public sealed class PoolRule
        {
            [SerializeField] private BattleEncounterGrade grade;
            [SerializeField] private int firstNodeIndex;
            [SerializeField] private int lastNodeIndex = 11;
            [SerializeField] private List<string> encounterIds = new();

            public BattleEncounterGrade Grade => grade;
            public int FirstNodeIndex => firstNodeIndex;
            public int LastNodeIndex => lastNodeIndex;
            public IReadOnlyList<string> EncounterIds
            {
                get
                {
                    if (encounterIds == null)
                        return Array.Empty<string>();
                    return encounterIds;
                }
            }
            public bool Contains(int nodeIndex) =>
                nodeIndex >= firstNodeIndex && nodeIndex <= lastNodeIndex;
        }

        [SerializeField] private List<PoolRule> rules = new();

        public bool TryGetPool(BattleEncounterGrade grade, int nodeIndex,
            out IReadOnlyList<string> encounterIds)
        {
            encounterIds = Array.Empty<string>();
            PoolRule match = null;
            foreach (PoolRule rule in rules ?? new List<PoolRule>())
            {
                if (rule == null || rule.Grade != grade || !rule.Contains(nodeIndex))
                    continue;
                if (match != null)
                    return false;
                match = rule;
            }
            if (match == null)
                return false;
            encounterIds = match.EncounterIds;
            return true;
        }

        public List<string> GetValidationErrors(EncounterDatabase database)
        {
            List<string> errors = new();
            Dictionary<BattleEncounterGrade, List<PoolRule>> byGrade = new();
            foreach (PoolRule rule in rules ?? new List<PoolRule>())
            {
                if (rule == null) { errors.Add("Encounter progression rule is null."); continue; }
                if (rule.FirstNodeIndex < 0 || rule.LastNodeIndex < rule.FirstNodeIndex)
                    errors.Add($"{rule.Grade} encounter progression range is invalid.");
                if (!byGrade.TryGetValue(rule.Grade, out List<PoolRule> gradeRules))
                    byGrade[rule.Grade] = gradeRules = new List<PoolRule>();
                foreach (PoolRule other in gradeRules)
                    if (rule.FirstNodeIndex <= other.LastNodeIndex &&
                        other.FirstNodeIndex <= rule.LastNodeIndex)
                        errors.Add($"{rule.Grade} encounter progression ranges overlap.");
                gradeRules.Add(rule);
                errors.AddRange(RunEncounterPoolService.ValidatePool(
                    database, rule.EncounterIds, rule.Grade,
                    $"{rule.Grade} nodes {rule.FirstNodeIndex}-{rule.LastNodeIndex}"));
            }
            foreach (BattleEncounterGrade grade in Enum.GetValues(typeof(BattleEncounterGrade)))
                if (!byGrade.ContainsKey(grade))
                    errors.Add($"Encounter progression grade {grade} is missing.");
            return errors;
        }
    }
}
