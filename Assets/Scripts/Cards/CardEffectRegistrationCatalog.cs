using System;
using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public enum CardEffectRoute
    {
        Passive,
        Summon,
        TargetedSkill,
        BanishSkill,
        TrapInstallation
    }

    public sealed class CardEffectRegistration
    {
        public CardEffectRegistration(string catalogCardId, CardEffectRoute route)
        {
            CatalogCardId = catalogCardId?.Trim();
            Route = route;
        }

        public string CatalogCardId { get; }
        public CardEffectRoute Route { get; }
        public bool DefersSkillResolution => Route == CardEffectRoute.BanishSkill;
    }

    public static class CardEffectRegistrationCatalog
    {
        private static readonly Dictionary<string, CardEffectRegistration> Registrations =
            new(StringComparer.OrdinalIgnoreCase);

        static CardEffectRegistrationCatalog()
        {
            RegisterBuiltIn(TestContentIds.C01, CardEffectRoute.Summon);
            RegisterBuiltIn(TestContentIds.C02, CardEffectRoute.Summon);
            RegisterBuiltIn(TestContentIds.C03, CardEffectRoute.Passive);
            RegisterBuiltIn(TestContentIds.C04, CardEffectRoute.Passive);
            RegisterBuiltIn(TestContentIds.C05, CardEffectRoute.TargetedSkill);
            RegisterBuiltIn(TestContentIds.C06, CardEffectRoute.TargetedSkill);
            RegisterBuiltIn(TestContentIds.C07, CardEffectRoute.BanishSkill);
            RegisterBuiltIn(TestContentIds.C08, CardEffectRoute.TrapInstallation);
            RegisterBuiltIn(TestContentIds.C09, CardEffectRoute.TrapInstallation);
            RegisterBuiltIn(TestContentIds.C10, CardEffectRoute.TrapInstallation);
            RegisterBuiltIn(TestContentIds.C11, CardEffectRoute.Passive);
            RegisterBuiltIn(TestContentIds.C12, CardEffectRoute.Passive);
        }

        public static bool TryRegister(CardEffectRegistration registration)
        {
            if (registration == null || string.IsNullOrWhiteSpace(registration.CatalogCardId) ||
                Registrations.ContainsKey(registration.CatalogCardId))
            {
                return false;
            }

            Registrations.Add(registration.CatalogCardId, registration);
            return true;
        }

        public static bool TryFind(string catalogCardId, out CardEffectRegistration registration)
        {
            if (string.IsNullOrWhiteSpace(catalogCardId))
            {
                registration = null;
                return false;
            }

            return Registrations.TryGetValue(catalogCardId.Trim(), out registration);
        }

        private static void RegisterBuiltIn(string catalogCardId, CardEffectRoute route)
        {
            TryRegister(new CardEffectRegistration(catalogCardId, route));
        }
    }
}
