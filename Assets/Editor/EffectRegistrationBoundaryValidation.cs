using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class EffectRegistrationBoundaryValidation
    {
        [MenuItem("Have a Break/Validate Effect Registration Boundary")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            EditorUtility.DisplayDialog(
                "Effect Registration Boundary",
                valid
                    ? "Card and enchant effect registration boundary passed."
                    : "Effect registration boundary failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            bool valid = ValidateBuiltInCards() &&
                         ValidateBuiltInEnchants() &&
                         ValidateExtensionBoundary();
            if (valid)
            {
                Debug.Log("Card and enchant effect registration boundary passed.");
            }
            else
            {
                Debug.LogError("Card and enchant effect registration boundary failed.");
            }

            return valid;
        }

        private static bool ValidateBuiltInCards()
        {
            string[] ids =
            {
                TestContentIds.C01, TestContentIds.C02, TestContentIds.C03,
                TestContentIds.C04, TestContentIds.C05, TestContentIds.C06,
                TestContentIds.C07, TestContentIds.C08, TestContentIds.C09,
                TestContentIds.C10, TestContentIds.C11, TestContentIds.C12
            };

            foreach (string id in ids)
            {
                if (!CardEffectRegistrationCatalog.TryFind(id, out _))
                {
                    return false;
                }
            }

            return CardEffectRegistrationCatalog.TryFind(
                       TestContentIds.C07, out CardEffectRegistration c07) &&
                   c07.Route == CardEffectRoute.BanishSkill &&
                   c07.DefersSkillResolution;
        }

        private static bool ValidateBuiltInEnchants()
        {
            string[] ids =
            {
                TestContentIds.E01, TestContentIds.E02, TestContentIds.E03,
                TestContentIds.E04, TestContentIds.E05, TestContentIds.E06,
                TestContentIds.E07, TestContentIds.E08
            };

            foreach (string id in ids)
            {
                if (!EnchantEffectRegistrationCatalog.TryFind(id, out _))
                {
                    return false;
                }
            }

            return EnchantEffectRegistrationCatalog.HasCapability(
                       TestContentIds.E07,
                       EnchantEffectCapability.TransferStamp) &&
                   !EnchantEffectRegistrationCatalog.HasCapability(
                       TestContentIds.E08,
                       EnchantEffectCapability.TransferStamp);
        }

        private static bool ValidateExtensionBoundary()
        {
            const string cardId = "VALIDATION-CARD-REGISTRATION";
            const string enchantId = "VALIDATION-ENCHANT-REGISTRATION";

            bool cardRegistered = CardEffectRegistrationCatalog.TryRegister(
                new CardEffectRegistration(cardId, CardEffectRoute.TargetedSkill));
            bool enchantRegistered = EnchantEffectRegistrationCatalog.TryRegister(
                new EnchantEffectRegistration(
                    enchantId,
                    EnchantCompatibilityRequirement.EnemyAffectingEffect));

            return (cardRegistered || CardEffectRegistrationCatalog.TryFind(cardId, out _)) &&
                   (enchantRegistered || EnchantEffectRegistrationCatalog.TryFind(enchantId, out _)) &&
                   !CardEffectRegistrationCatalog.TryRegister(
                       new CardEffectRegistration(cardId, CardEffectRoute.Passive)) &&
                   !EnchantEffectRegistrationCatalog.TryRegister(
                       new EnchantEffectRegistration(
                           enchantId,
                           EnchantCompatibilityRequirement.None));
        }
    }
}
