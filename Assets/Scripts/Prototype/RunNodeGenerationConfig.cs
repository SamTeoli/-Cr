using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [CreateAssetMenu(fileName = "RunNodeGenerationConfig",
        menuName = "Have a Break/Run/Node Generation Config")]
    public sealed class RunNodeGenerationConfig : ScriptableObject
    {
        [SerializeField] private List<RunNodeType> generalNodePool = new();
        [SerializeField] private int openingChoiceCount = 3;
        [SerializeField] private int minimumChoiceCount = 2;
        [SerializeField] private int maximumChoiceCount = 4;
        [SerializeField] private int eliteUnlockIndex = 2;
        [SerializeField] private int midBossIndex = 4;
        [SerializeField] private int finalBossIndex = 11;

        public IReadOnlyList<RunNodeType> GeneralNodePool => generalNodePool;
        public int OpeningChoiceCount => openingChoiceCount;
        public int MinimumChoiceCount => minimumChoiceCount;
        public int MaximumChoiceCount => maximumChoiceCount;
        public int EliteUnlockIndex => eliteUnlockIndex;
        public int MidBossIndex => midBossIndex;
        public int FinalBossIndex => finalBossIndex;

        public IReadOnlyList<string> GetValidationErrors()
        {
            List<string> errors = new();
            if (generalNodePool == null || generalNodePool.Count == 0)
                errors.Add("Run node general pool is empty.");
            else if (generalNodePool.Exists(value =>
                         value == RunNodeType.MidBoss ||
                         value == RunNodeType.FinalBoss))
                errors.Add("Run node general pool contains a fixed boss node.");
            if (minimumChoiceCount < 1 || maximumChoiceCount < minimumChoiceCount)
                errors.Add("Run node choice count range is invalid.");
            if (openingChoiceCount < minimumChoiceCount ||
                openingChoiceCount > maximumChoiceCount)
                errors.Add("Run node opening choice count is outside the configured range.");
            if (eliteUnlockIndex < 0)
                errors.Add("Run node elite unlock index cannot be negative.");
            if (midBossIndex < 1 || finalBossIndex <= midBossIndex)
                errors.Add("Run node fixed boss indices are invalid.");
            return errors;
        }
    }
}
