using System;
using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public enum EnchantCompatibilityRequirement
    {
        None,
        PositiveHealthMonster,
        PositiveAttackMonster,
        MainEffectCompletion,
        SkillCostAtLeastTwo,
        EnemyAffectingEffect,
        NumericRepeatingEffect,
        NormalGraveyardAfterResolution,
        FixedSingleEnemyTarget
    }

    [Flags]
    public enum EnchantEffectCapability
    {
        None = 0,
        TransferStamp = 1
    }

    public sealed class EnchantEffectRegistration
    {
        public EnchantEffectRegistration(
            string definitionId,
            EnchantCompatibilityRequirement compatibility,
            EnchantEffectCapability capabilities = EnchantEffectCapability.None)
        {
            DefinitionId = definitionId?.Trim();
            Compatibility = compatibility;
            Capabilities = capabilities;
        }

        public string DefinitionId { get; }
        public EnchantCompatibilityRequirement Compatibility { get; }
        public EnchantEffectCapability Capabilities { get; }
    }

    public static class EnchantEffectRegistrationCatalog
    {
        private static readonly Dictionary<string, EnchantEffectRegistration> Registrations =
            new(StringComparer.OrdinalIgnoreCase);

        static EnchantEffectRegistrationCatalog()
        {
            RegisterBuiltIn(TestContentIds.E01, EnchantCompatibilityRequirement.PositiveHealthMonster);
            RegisterBuiltIn(TestContentIds.E02, EnchantCompatibilityRequirement.PositiveAttackMonster);
            RegisterBuiltIn(TestContentIds.E03, EnchantCompatibilityRequirement.MainEffectCompletion);
            RegisterBuiltIn(TestContentIds.E04, EnchantCompatibilityRequirement.SkillCostAtLeastTwo);
            RegisterBuiltIn(TestContentIds.E05, EnchantCompatibilityRequirement.EnemyAffectingEffect);
            RegisterBuiltIn(TestContentIds.E06, EnchantCompatibilityRequirement.NumericRepeatingEffect);
            RegisterBuiltIn(
                TestContentIds.E07,
                EnchantCompatibilityRequirement.NormalGraveyardAfterResolution,
                EnchantEffectCapability.TransferStamp);
            RegisterBuiltIn(TestContentIds.E08, EnchantCompatibilityRequirement.FixedSingleEnemyTarget);
        }

        public static bool TryRegister(EnchantEffectRegistration registration)
        {
            if (registration == null || string.IsNullOrWhiteSpace(registration.DefinitionId) ||
                Registrations.ContainsKey(registration.DefinitionId))
            {
                return false;
            }

            Registrations.Add(registration.DefinitionId, registration);
            return true;
        }

        public static bool TryFind(string definitionId, out EnchantEffectRegistration registration)
        {
            if (string.IsNullOrWhiteSpace(definitionId))
            {
                registration = null;
                return false;
            }

            return Registrations.TryGetValue(definitionId.Trim(), out registration);
        }

        public static bool HasCapability(string definitionId, EnchantEffectCapability capability)
        {
            return TryFind(definitionId, out EnchantEffectRegistration registration) &&
                   (registration.Capabilities & capability) == capability;
        }

        private static void RegisterBuiltIn(
            string definitionId,
            EnchantCompatibilityRequirement compatibility,
            EnchantEffectCapability capabilities = EnchantEffectCapability.None)
        {
            TryRegister(new EnchantEffectRegistration(definitionId, compatibility, capabilities));
        }
    }
}
