using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class EnchantAttachmentOrderValidation
    {
        internal static bool Validate()
        {
            CardData card = AssetDatabase.FindAssets("t:CardData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<CardData>)
                .FirstOrDefault(value => value != null &&
                    value.CatalogCardId == TestContentIds.C01);
            EnchantData enchant = ScriptableObject.CreateInstance<EnchantData>();
            bool valid = card != null;

            if (valid)
            {
                enchant.EditorInitialize(
                    "VALIDATION-ORDER",
                    "Validation Attachment Order",
                    CardRarity.Common,
                    new[] { CardType.Monster },
                    true);
                RunCardEnchantState state = new(card);
                valid &= state.TryIncreaseSlotCount();
                valid &= state.TryAttach(enchant, 1, false, out _);
                valid &= state.TryAttach(enchant, 0, false, out _);
                valid &= state.SlotsInAttachmentOrder
                    .Select(slot => slot.SlotIndex)
                    .SequenceEqual(new[] { 1, 0 });
                valid &= state.TryRemove(1, false, out _);
                valid &= state.Slots[1].IsEmpty && !state.Slots[0].IsEmpty;
            }

            Object.DestroyImmediate(enchant);
            if (!valid)
            {
                Debug.LogError("Enchant attachment order validation failed.");
            }

            return valid;
        }
    }
}
