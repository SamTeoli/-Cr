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
        [SerializeField] private int consumableOfferCount = 3;
        [SerializeField] private int enchantOfferCount = 4;
        [SerializeField] private int commonEnchantPrice = 45;
        [SerializeField] private int rareEnchantPrice = 80;
        [SerializeField] private int legendaryEnchantPrice = 120;

        public int BaseRerollCost => baseRerollCost;
        public int RerollCostIncrease => rerollCostIncrease;
        public int ConsumableOfferCount => consumableOfferCount;
        public int EnchantOfferCount => enchantOfferCount;
        public int CommonEnchantPrice => commonEnchantPrice;
        public int RareEnchantPrice => rareEnchantPrice;
        public int LegendaryEnchantPrice => legendaryEnchantPrice;

        public int GetRerollCost(int completedRerollCount)
        {
            return baseRerollCost +
                   Mathf.Max(0, completedRerollCount) * rerollCostIncrease;
        }

        public int GetEnchantPrice(CardRarity rarity)
        {
            return rarity switch
            {
                CardRarity.Legendary => legendaryEnchantPrice,
                CardRarity.Rare => rareEnchantPrice,
                _ => commonEnchantPrice
            };
        }

        public IReadOnlyList<string> GetValidationErrors()
        {
            List<string> errors = new();
            if (baseRerollCost < 0)
                errors.Add("Shop base reroll cost cannot be negative.");
            if (rerollCostIncrease < 0)
                errors.Add("Shop reroll cost increase cannot be negative.");
            if (consumableOfferCount < 0)
                errors.Add("Shop consumable offer count cannot be negative.");
            if (enchantOfferCount < 0)
                errors.Add("Shop enchant offer count cannot be negative.");
            if (consumableOfferCount + enchantOfferCount == 0)
                errors.Add("Shop must offer at least one product.");
            if (commonEnchantPrice < 0 || rareEnchantPrice < 0 ||
                legendaryEnchantPrice < 0)
                errors.Add("Shop enchant prices cannot be negative.");
            return errors;
        }
    }
}
