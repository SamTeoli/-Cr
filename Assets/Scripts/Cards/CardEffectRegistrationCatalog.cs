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
        public CardEffectRegistration(
            string catalogCardId,
            CardEffectRoute route,
            ICardEffectHandler handler = null)
        {
            CatalogCardId = catalogCardId?.Trim();
            Route = route;
            Handler = handler;
        }

        public string CatalogCardId { get; }
        public CardEffectRoute Route { get; }
        public ICardEffectHandler Handler { get; }
        public bool DefersSkillResolution => Route == CardEffectRoute.BanishSkill;
    }

    public static class CardEffectRegistrationCatalog
    {
        private static readonly Dictionary<string, CardEffectRegistration> Registrations =
            new(StringComparer.OrdinalIgnoreCase);

        static CardEffectRegistrationCatalog()
        {
            RegisterBuiltIn(TestContentIds.C01, CardEffectRoute.Summon, new C01CardEffectHandler());
            RegisterBuiltIn(TestContentIds.C02, CardEffectRoute.Summon, new C02CardEffectHandler());
            RegisterBuiltIn(TestContentIds.C03, CardEffectRoute.Passive, new C03CardEffectHandler());
            RegisterBuiltIn(TestContentIds.C04, CardEffectRoute.Passive, new C04CardEffectHandler());
            RegisterBuiltIn(TestContentIds.C05, CardEffectRoute.TargetedSkill, new C05CardEffectHandler());
            RegisterBuiltIn(TestContentIds.C06, CardEffectRoute.TargetedSkill, new C06CardEffectHandler());
            RegisterBuiltIn(TestContentIds.C07, CardEffectRoute.BanishSkill, new C07CardEffectHandler());
            RegisterBuiltIn(TestContentIds.C08, CardEffectRoute.TrapInstallation, new C08CardEffectHandler());
            RegisterBuiltIn(TestContentIds.C09, CardEffectRoute.TrapInstallation, new C09CardEffectHandler());
            RegisterBuiltIn(TestContentIds.C10, CardEffectRoute.TrapInstallation, new C10CardEffectHandler());
            RegisterBuiltIn(TestContentIds.C11, CardEffectRoute.Passive, new C11CardEffectHandler());
            RegisterBuiltIn(TestContentIds.C12, CardEffectRoute.Passive, new C12CardEffectHandler());
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

        private static void RegisterBuiltIn(
            string catalogCardId,
            CardEffectRoute route,
            ICardEffectHandler handler = null)
        {
            TryRegister(new CardEffectRegistration(catalogCardId, route, handler));
        }
    }
}
