using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class EnchantMutationValidation
    {
        internal static bool Validate()
        {
            CardData card = AssetDatabase.FindAssets("t:CardData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<CardData>)
                .FirstOrDefault(value => value != null &&
                    value.CatalogCardId == TestContentIds.C01);
            EnchantData original = CreateEnchant(
                "VALIDATION-MUTATION-A", CardType.Monster);
            EnchantData replacement = CreateEnchant(
                "VALIDATION-MUTATION-B", CardType.Monster);
            EnchantData incompatible = CreateEnchant(
                "VALIDATION-MUTATION-C", CardType.Skill);
            bool valid = card != null;

            if (valid)
            {
                RunCardEnchantState state = new(card);
                valid &= state.TryAttach(original, 0, false, out _);
                int attachmentOrder = state.Slots[0].AttachmentOrder;
                valid &= !state.TryReplace(0, replacement, true,
                    out EnchantAttachmentFailure lockedFailure) &&
                    lockedFailure == EnchantAttachmentFailure.BattleLocked &&
                    state.Slots[0].Enchant == original;
                valid &= !state.TryReplace(0, incompatible, false,
                    out EnchantAttachmentFailure incompatibleFailure) &&
                    incompatibleFailure ==
                    EnchantAttachmentFailure.IncompatibleCardType &&
                    state.Slots[0].Enchant == original;
                valid &= state.TryReplace(0, replacement, false,
                    out EnchantAttachmentFailure replaceFailure) &&
                    replaceFailure == EnchantAttachmentFailure.None &&
                    state.Slots[0].Enchant == replacement &&
                    state.Slots[0].AttachmentOrder == attachmentOrder &&
                    state.Slots[0].Active;
                valid &= !state.TryReplace(1, original, false,
                    out EnchantAttachmentFailure invalidSlotFailure) &&
                    invalidSlotFailure == EnchantAttachmentFailure.InvalidSlot;
            }

            Object.DestroyImmediate(original);
            Object.DestroyImmediate(replacement);
            Object.DestroyImmediate(incompatible);
            if (!valid)
            {
                Debug.LogError("Enchant mutation validation failed.");
            }

            return valid;
        }

        private static EnchantData CreateEnchant(string id, CardType cardType)
        {
            EnchantData enchant = ScriptableObject.CreateInstance<EnchantData>();
            enchant.EditorInitialize(
                id,
                id,
                CardRarity.Common,
                new[] { cardType });
            return enchant;
        }
    }
}
