using UnityEngine;

namespace HaveABreak.Cards
{
    public enum ConsumableEffect
    {
        HealPlayer,
        ClearPlayerStatuses,
        RestoreMana,
        IncreaseEnchantSlot,
        ReplaceEnchant
    }

    [CreateAssetMenu(
        fileName = "ConsumableData",
        menuName = "Have a Break/Consumables/Definition")]
    public sealed class ConsumableData : ScriptableObject
    {
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea(2, 5)] private string rulesText;
        [SerializeField] private ConsumableEffect effect;
        [SerializeField] private int amount;
        [SerializeField, Min(0)] private int shopPrice;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string RulesText => rulesText;
        public ConsumableEffect Effect => effect;
        public int Amount => amount;
        public int ShopPrice => shopPrice;

#if UNITY_EDITOR
        public void EditorInitialize(
            string id,
            string itemName,
            string description,
            ConsumableEffect itemEffect,
            int effectAmount,
            int price)
        {
            itemId = id?.Trim();
            displayName = itemName?.Trim();
            rulesText = description?.Trim();
            effect = itemEffect;
            amount = effectAmount;
            shopPrice = price;
        }
#endif

        private void OnValidate()
        {
            itemId = itemId?.Trim();
            displayName = displayName?.Trim();
            rulesText = rulesText?.Trim();
            shopPrice = Mathf.Max(0, shopPrice);
        }
    }
}
