using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [CreateAssetMenu(fileName = "BattleRewardConfig", menuName = "Have a Break/Battle Reward Config")]
    public sealed class BattleRewardConfig : ScriptableObject
    {
        [Serializable]
        public sealed class GradeRule
        {
            [SerializeField] private BattleEncounterGrade grade;
            [SerializeField] private List<int> goldOptions = new();
            [SerializeField] private int enchantChoiceCount;
            [SerializeField] private CardRarity minimumEnchantRarity;
            [SerializeField] private int consumableItemRewardCount;
            [SerializeField] private bool grantsPermanentReward;

            public BattleEncounterGrade Grade => grade;
            public IReadOnlyList<int> GoldOptions => goldOptions ?? Array.Empty<int>();
            public int EnchantChoiceCount => enchantChoiceCount;
            public CardRarity MinimumEnchantRarity => minimumEnchantRarity;
            public int ConsumableItemRewardCount => consumableItemRewardCount;
            public bool GrantsPermanentReward => grantsPermanentReward;
        }

        [SerializeField] private List<GradeRule> rules = new();

        public bool TryGetRule(BattleEncounterGrade grade, out GradeRule rule)
        {
            rule = rules?.Find(value => value != null && value.Grade == grade);
            return rule != null;
        }

        public List<string> GetValidationErrors()
        {
            List<string> errors = new();
            HashSet<BattleEncounterGrade> grades = new();
            foreach (GradeRule rule in rules ?? new List<GradeRule>())
            {
                if (rule == null) { errors.Add("Battle reward rule is null."); continue; }
                if (!grades.Add(rule.Grade)) errors.Add($"Duplicate battle reward grade: {rule.Grade}.");
                if (rule.GoldOptions.Count == 0) errors.Add($"Battle reward grade {rule.Grade} requires gold options.");
                foreach (int gold in rule.GoldOptions) if (gold < 0) errors.Add($"Battle reward grade {rule.Grade} has negative gold.");
                if (rule.EnchantChoiceCount < 0 || rule.ConsumableItemRewardCount < 0)
                    errors.Add($"Battle reward grade {rule.Grade} has a negative reward count.");
            }
            foreach (BattleEncounterGrade grade in Enum.GetValues(typeof(BattleEncounterGrade)))
                if (!grades.Contains(grade)) errors.Add($"Battle reward grade {grade} is missing.");
            return errors;
        }
    }
}
