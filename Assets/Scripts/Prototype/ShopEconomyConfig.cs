using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [CreateAssetMenu(fileName = "ShopEconomyConfig",
        menuName = "Have a Break/Run/Shop Economy Config")]
    public sealed class ShopEconomyConfig : ScriptableObject
    {
        [SerializeField] private int baseRerollCost = 10;
        [SerializeField] private int rerollCostIncrease = 5;

        public int BaseRerollCost => baseRerollCost;
        public int RerollCostIncrease => rerollCostIncrease;

        public int GetRerollCost(int completedRerollCount)
        {
            return baseRerollCost +
                   Mathf.Max(0, completedRerollCount) * rerollCostIncrease;
        }

        public IReadOnlyList<string> GetValidationErrors()
        {
            List<string> errors = new();
            if (baseRerollCost < 0)
                errors.Add("Shop base reroll cost cannot be negative.");
            if (rerollCostIncrease < 0)
                errors.Add("Shop reroll cost increase cannot be negative.");
            return errors;
        }
    }
}
