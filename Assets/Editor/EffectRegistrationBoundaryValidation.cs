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
                if (!CardEffectRegistrationCatalog.TryFind(
                        id, out CardEffectRegistration registration) ||
                    !HasExpectedHandler(registration))
                {
                    return false;
                }
            }

            return CardEffectRegistrationCatalog.TryFind(
                       TestContentIds.C07, out CardEffectRegistration c07) &&
                   c07.Route == CardEffectRoute.BanishSkill &&
                   c07.DefersSkillResolution;
        }

        private static bool HasExpectedHandler(CardEffectRegistration registration)
        {
            if (registration.CatalogCardId == TestContentIds.C03)
                return registration.Handler is IPlayerTurnEndCardEffectHandler;
            if (registration.CatalogCardId == TestContentIds.C04)
                return registration.Handler is IEnemyMovementMonsterCardEffectHandler;
            if (registration.CatalogCardId == TestContentIds.C08)
                return registration.Handler is IEnemyMoveTrapCardEffectHandler;
            if (registration.CatalogCardId == TestContentIds.C09)
                return registration.Handler is IIncomingAttackTrapCardEffectHandler;
            if (registration.CatalogCardId == TestContentIds.C10)
                return registration.Handler is IEnemyAbilityTrapCardEffectHandler;
            if (registration.CatalogCardId == TestContentIds.C11)
                return registration.Handler is IPlayerTurnStartCardEffectHandler;
            if (registration.CatalogCardId == TestContentIds.C12)
                return registration.Handler is IEnemyMovementBarrierCardEffectHandler;

            return registration.Route switch
            {
                CardEffectRoute.Summon =>
                    registration.Handler is ISummonCardEffectHandler,
                CardEffectRoute.TargetedSkill =>
                    registration.Handler is ITargetedSkillCardEffectHandler,
                CardEffectRoute.BanishSkill =>
                    registration.Handler is IBanishSkillCardEffectHandler,
                _ => true
            };
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
                if (!EnchantEffectRegistrationCatalog.TryFind(
                        id, out EnchantEffectRegistration registration) ||
                    !HasExpectedEnchantHandler(registration))
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

        private static bool HasExpectedEnchantHandler(EnchantEffectRegistration registration)
        {
            if (registration.DefinitionId == TestContentIds.E01)
                return registration.Handler is IMaximumHealthEnchantEffectHandler;
            if (registration.DefinitionId == TestContentIds.E02)
                return registration.Handler is IAttackCompletedEnchantEffectHandler;
            if (registration.DefinitionId == TestContentIds.E03)
                return registration.Handler is IMainEffectCompletedEnchantEffectHandler;
            if (registration.DefinitionId == TestContentIds.E04)
                return registration.Handler is IManaCostEnchantEffectHandler;
            if (registration.DefinitionId == TestContentIds.E05)
                return registration.Handler is IEnemyImpactEnchantEffectHandler;
            if (registration.DefinitionId == TestContentIds.E06)
                return registration.Handler is IRepeatedEffectEnchantEffectHandler;
            if (registration.DefinitionId == TestContentIds.E08)
                return registration.Handler is IFixedTargetEnchantEffectHandler;

            return registration.DefinitionId == TestContentIds.E07 &&
                   registration.Handler is IGraveyardReplacementEnchantEffectHandler &&
                   EnchantEffectRegistrationCatalog.HasCapability(
                       registration.DefinitionId, EnchantEffectCapability.TransferStamp);
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
