using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class RunEnchantSlot
    {
        [SerializeField] private int slotIndex;
        [SerializeField] private EnchantData enchant;
        [SerializeField] private int attachmentOrder;
        [SerializeField] private bool active;

        internal RunEnchantSlot(int slotIndex)
        {
            this.slotIndex = slotIndex;
        }

        public int SlotIndex => slotIndex;
        public EnchantData Enchant => enchant;
        public int AttachmentOrder => attachmentOrder;
        public bool Active => active;
        public bool IsEmpty => enchant == null;

        internal void Attach(EnchantData value, int order, bool isActive)
        {
            enchant = value;
            attachmentOrder = order;
            active = isActive;
        }

        internal void Clear()
        {
            enchant = null;
            attachmentOrder = 0;
            active = false;
        }

        internal void SetActive(bool value)
        {
            active = enchant != null && value;
        }
    }
}
