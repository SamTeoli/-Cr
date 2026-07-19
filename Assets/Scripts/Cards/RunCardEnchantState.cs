using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class RunCardEnchantState
    {
        public const int DefaultSlotCount = 1;
        public const int MaximumSlotCount = 3;

        [SerializeField] private CardData card;
        [SerializeField] private List<RunEnchantSlot> slots = new();
        [SerializeField] private int nextAttachmentOrder = 1;

        private RunCardEnchantState()
        {
        }

        public RunCardEnchantState(CardData card)
        {
            this.card = card ?? throw new ArgumentNullException(nameof(card));
            int slotCount = Mathf.Clamp(card.BaseEnchantSlots, DefaultSlotCount, MaximumSlotCount);
            for (int i = 0; i < slotCount; i++)
            {
                slots.Add(new RunEnchantSlot(i));
            }
        }

        public CardData Card => card;
        public IReadOnlyList<RunEnchantSlot> Slots => slots;
        public int SlotCount => slots.Count;

        public bool TryIncreaseSlotCount()
        {
            if (slots.Count >= MaximumSlotCount)
            {
                return false;
            }

            slots.Add(new RunEnchantSlot(slots.Count));
            return true;
        }

        public bool CanAttach(EnchantData enchant, int slotIndex, out EnchantAttachmentFailure failure)
        {
            if (enchant == null)
            {
                failure = EnchantAttachmentFailure.NullEnchant;
                return false;
            }

            if (slotIndex < 0 || slotIndex >= slots.Count)
            {
                failure = EnchantAttachmentFailure.InvalidSlot;
                return false;
            }

            if (!slots[slotIndex].IsEmpty)
            {
                failure = EnchantAttachmentFailure.SlotOccupied;
                return false;
            }

            if (!EnchantCompatibilityEvaluator.IsCompatible(enchant, card))
            {
                failure = EnchantAttachmentFailure.IncompatibleCardType;
                return false;
            }

            if (!enchant.AllowDuplicateOnSameCard && Contains(enchant))
            {
                failure = EnchantAttachmentFailure.DuplicateNotAllowed;
                return false;
            }

            failure = EnchantAttachmentFailure.None;
            return true;
        }

        public bool TryAttach(
            EnchantData enchant,
            int slotIndex,
            bool battleActive,
            out EnchantAttachmentFailure failure)
        {
            if (battleActive)
            {
                failure = EnchantAttachmentFailure.BattleLocked;
                return false;
            }

            if (!CanAttach(enchant, slotIndex, out failure))
            {
                return false;
            }

            slots[slotIndex].Attach(enchant, nextAttachmentOrder, true);
            nextAttachmentOrder++;
            return true;
        }

        public bool TryRemove(int slotIndex, bool battleActive, out EnchantAttachmentFailure failure)
        {
            if (battleActive)
            {
                failure = EnchantAttachmentFailure.BattleLocked;
                return false;
            }

            if (slotIndex < 0 || slotIndex >= slots.Count)
            {
                failure = EnchantAttachmentFailure.InvalidSlot;
                return false;
            }

            if (slots[slotIndex].IsEmpty)
            {
                failure = EnchantAttachmentFailure.SlotEmpty;
                return false;
            }

            slots[slotIndex].Clear();
            failure = EnchantAttachmentFailure.None;
            return true;
        }

        public void RefreshCompatibility(CardType currentCardType)
        {
            foreach (RunEnchantSlot slot in slots)
            {
                if (!slot.IsEmpty)
                {
                    slot.SetActive(slot.Enchant.IsCompatible(currentCardType));
                }
            }
        }

        public bool HasImmediateAttachmentTarget(EnchantData enchant)
        {
            if (enchant == null || !EnchantCompatibilityEvaluator.IsCompatible(enchant, card) ||
                (!enchant.AllowDuplicateOnSameCard && Contains(enchant)))
            {
                return false;
            }

            return slots.Exists(slot => slot.IsEmpty);
        }

        private bool Contains(EnchantData enchant)
        {
            return slots.Exists(slot => !slot.IsEmpty && slot.Enchant.MatchesDefinition(enchant));
        }
    }
}
