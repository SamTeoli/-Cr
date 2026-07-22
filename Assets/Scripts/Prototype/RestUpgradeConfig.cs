using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [CreateAssetMenu(fileName = "RestUpgradeConfig",
        menuName = "Have a Break/Run/Rest And Upgrade Config")]
    public sealed class RestUpgradeConfig : ScriptableObject
    {
        [SerializeField, Range(0f, 1f)] private float healingRatio = 0.3f;
        [SerializeField, Min(1)] private int upgradeLevelIncrease = 1;

        public float HealingRatio => healingRatio;
        public int UpgradeLevelIncrease => upgradeLevelIncrease;
        public int GetHealingAmount(int maximumHealth)
        {
            return Mathf.CeilToInt(Mathf.Max(0, maximumHealth) * healingRatio);
        }

        public void EditorInitialize(float ratio, int levelIncrease)
        {
            healingRatio = ratio;
            upgradeLevelIncrease = levelIncrease;
        }

        public IReadOnlyList<string> GetValidationErrors()
        {
            List<string> errors = new();
            if (healingRatio < 0f || healingRatio > 1f)
                errors.Add("Rest healing ratio must be between zero and one.");
            if (upgradeLevelIncrease < 1)
                errors.Add("Card upgrade level increase must be positive.");
            return errors;
        }
    }
}
