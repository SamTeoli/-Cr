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

    public interface IEnchantEffectHandler
    {
        string DefinitionId { get; }
    }

    public interface IMaximumHealthEnchantEffectHandler : IEnchantEffectHandler
    {
        int ModifyMaximumHealth(int currentMaximumHealth);
    }

    public interface IManaCostEnchantEffectHandler : IEnchantEffectHandler
    {
        int ModifyManaCost(int currentManaCost);
    }

    public interface IRepeatedEffectEnchantEffectHandler : IEnchantEffectHandler
    {
        RepeatedEffectParameters Modify(RepeatedEffectParameters parameters);
    }

    public interface IFixedTargetEnchantEffectHandler : IEnchantEffectHandler
    {
    }

    public interface IAttackCompletedEnchantEffectHandler : IEnchantEffectHandler
    {
    }

    public interface IMainEffectCompletedEnchantEffectHandler : IEnchantEffectHandler
    {
    }

    public interface IEnemyImpactEnchantEffectHandler : IEnchantEffectHandler
    {
    }

    public interface IGraveyardReplacementEnchantEffectHandler : IEnchantEffectHandler
    {
    }

    public sealed class MaximumHealthEnchantEffectHandler : IMaximumHealthEnchantEffectHandler
    {
        public MaximumHealthEnchantEffectHandler(string definitionId, int bonus)
        {
            DefinitionId = definitionId;
            Bonus = bonus;
        }

        public string DefinitionId { get; }
        public int Bonus { get; }
        public int ModifyMaximumHealth(int currentMaximumHealth) => currentMaximumHealth + Bonus;
    }

    public sealed class ManaCostEnchantEffectHandler : IManaCostEnchantEffectHandler
    {
        public ManaCostEnchantEffectHandler(string definitionId, int reduction, int minimum)
        {
            DefinitionId = definitionId;
            Reduction = reduction;
            Minimum = minimum;
        }

        public string DefinitionId { get; }
        public int Reduction { get; }
        public int Minimum { get; }
        public int ModifyManaCost(int currentManaCost) =>
            Math.Max(Minimum, currentManaCost - Reduction);
    }

    public sealed class RepeatedEffectEnchantEffectHandler : IRepeatedEffectEnchantEffectHandler
    {
        public RepeatedEffectEnchantEffectHandler(string definitionId, int firstValueBonus)
        {
            DefinitionId = definitionId;
            FirstValueBonus = firstValueBonus;
        }

        public string DefinitionId { get; }
        public int FirstValueBonus { get; }
        public RepeatedEffectParameters Modify(RepeatedEffectParameters parameters) =>
            new(parameters.FirstValue + FirstValueBonus, parameters.TargetCount,
                parameters.ActivationCount, parameters.ConditionThreshold);
    }

    public abstract class EnchantEffectHandlerBase : IEnchantEffectHandler
    {
        protected EnchantEffectHandlerBase(string definitionId) => DefinitionId = definitionId;
        public string DefinitionId { get; }
    }

    public sealed class AttackCompletedEnchantEffectHandler : EnchantEffectHandlerBase,
        IAttackCompletedEnchantEffectHandler
    {
        public AttackCompletedEnchantEffectHandler(string definitionId) : base(definitionId) { }
    }

    public sealed class MainEffectCompletedEnchantEffectHandler : EnchantEffectHandlerBase,
        IMainEffectCompletedEnchantEffectHandler
    {
        public MainEffectCompletedEnchantEffectHandler(string definitionId) : base(definitionId) { }
    }

    public sealed class EnemyImpactEnchantEffectHandler : EnchantEffectHandlerBase,
        IEnemyImpactEnchantEffectHandler
    {
        public EnemyImpactEnchantEffectHandler(string definitionId) : base(definitionId) { }
    }

    public sealed class FixedTargetEnchantEffectHandler : EnchantEffectHandlerBase,
        IFixedTargetEnchantEffectHandler
    {
        public FixedTargetEnchantEffectHandler(string definitionId) : base(definitionId) { }
    }

    public sealed class GraveyardReplacementEnchantEffectHandler : EnchantEffectHandlerBase,
        IGraveyardReplacementEnchantEffectHandler
    {
        public GraveyardReplacementEnchantEffectHandler(string definitionId) : base(definitionId) { }
    }

    public sealed class EnchantEffectRegistration
    {
        public EnchantEffectRegistration(
            string definitionId,
            EnchantCompatibilityRequirement compatibility,
            EnchantEffectCapability capabilities = EnchantEffectCapability.None,
            IEnchantEffectHandler handler = null)
        {
            DefinitionId = definitionId?.Trim();
            Compatibility = compatibility;
            Capabilities = capabilities;
            Handler = handler;
        }

        public string DefinitionId { get; }
        public EnchantCompatibilityRequirement Compatibility { get; }
        public EnchantEffectCapability Capabilities { get; }
        public IEnchantEffectHandler Handler { get; }
    }

    public static class EnchantEffectRegistrationCatalog
    {
        private static readonly Dictionary<string, EnchantEffectRegistration> Registrations =
            new(StringComparer.OrdinalIgnoreCase);

        static EnchantEffectRegistrationCatalog()
        {
            RegisterBuiltIn(TestContentIds.E01, EnchantCompatibilityRequirement.PositiveHealthMonster,
                handler: new MaximumHealthEnchantEffectHandler(TestContentIds.E01, 2));
            RegisterBuiltIn(TestContentIds.E02, EnchantCompatibilityRequirement.PositiveAttackMonster,
                handler: new AttackCompletedEnchantEffectHandler(TestContentIds.E02));
            RegisterBuiltIn(TestContentIds.E03, EnchantCompatibilityRequirement.MainEffectCompletion,
                handler: new MainEffectCompletedEnchantEffectHandler(TestContentIds.E03));
            RegisterBuiltIn(TestContentIds.E04, EnchantCompatibilityRequirement.SkillCostAtLeastTwo,
                handler: new ManaCostEnchantEffectHandler(TestContentIds.E04, 1, 1));
            RegisterBuiltIn(TestContentIds.E05, EnchantCompatibilityRequirement.EnemyAffectingEffect,
                handler: new EnemyImpactEnchantEffectHandler(TestContentIds.E05));
            RegisterBuiltIn(TestContentIds.E06, EnchantCompatibilityRequirement.NumericRepeatingEffect,
                handler: new RepeatedEffectEnchantEffectHandler(TestContentIds.E06, 1));
            RegisterBuiltIn(
                TestContentIds.E07,
                EnchantCompatibilityRequirement.NormalGraveyardAfterResolution,
                EnchantEffectCapability.TransferStamp,
                new GraveyardReplacementEnchantEffectHandler(TestContentIds.E07));
            RegisterBuiltIn(TestContentIds.E08, EnchantCompatibilityRequirement.FixedSingleEnemyTarget,
                handler: new FixedTargetEnchantEffectHandler(TestContentIds.E08));
        }

        public static bool TryRegister(EnchantEffectRegistration registration)
        {
            if (registration == null || string.IsNullOrWhiteSpace(registration.DefinitionId) ||
                (registration.Handler != null && !string.Equals(
                    registration.Handler.DefinitionId,
                    registration.DefinitionId,
                    StringComparison.OrdinalIgnoreCase)) ||
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

        public static bool TryGetActiveHandler<THandler>(
            RunEnchantSlot slot,
            out THandler handler) where THandler : class, IEnchantEffectHandler
        {
            handler = null;
            return slot != null && !slot.IsEmpty && slot.Active &&
                   TryFind(slot.Enchant.DefinitionId, out EnchantEffectRegistration registration) &&
                   (handler = registration.Handler as THandler) != null;
        }

        public static bool TryFindActiveHandler<THandler>(
            RunCardEnchantState enchants,
            out THandler handler) where THandler : class, IEnchantEffectHandler
        {
            handler = null;
            if (enchants == null)
            {
                return false;
            }

            foreach (RunEnchantSlot slot in enchants.SlotsInAttachmentOrder)
            {
                if (TryGetActiveHandler(slot, out handler))
                {
                    return true;
                }
            }

            return false;
        }

        private static void RegisterBuiltIn(
            string definitionId,
            EnchantCompatibilityRequirement compatibility,
            EnchantEffectCapability capabilities = EnchantEffectCapability.None,
            IEnchantEffectHandler handler = null)
        {
            TryRegister(new EnchantEffectRegistration(definitionId, compatibility, capabilities, handler));
        }
    }
}
