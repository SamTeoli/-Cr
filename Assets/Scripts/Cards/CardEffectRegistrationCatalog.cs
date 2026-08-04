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
            ICardEffectHandler handler = null,
            EffectTargetSpec targetSpec = null)
        {
            CatalogCardId = catalogCardId?.Trim();
            Route = route;
            Handler = handler;
            TargetSpec = targetSpec;
        }

        public string CatalogCardId { get; }
        public CardEffectRoute Route { get; }
        public ICardEffectHandler Handler { get; }
        public EffectTargetSpec TargetSpec { get; }
        public bool DefersSkillResolution => Route == CardEffectRoute.BanishSkill;

        public EffectTargetSpec ResolveTargetSpec(CardData card)
        {
            return card?.EffectTargetSpec != null
                ? card.EffectTargetSpec
                : TargetSpec;
        }
    }

    public static class CardEffectRegistrationCatalog
    {
        private static readonly Dictionary<string, CardEffectRegistration> Registrations =
            new(StringComparer.OrdinalIgnoreCase);

        static CardEffectRegistrationCatalog()
        {
            RegisterBuiltIn(TestContentIds.C01, CardEffectRoute.Summon,
                new C01CardEffectHandler(),
                BuiltInEffectTargetSpecs.EnemyMonsterSingleAfterPlacement);
            RegisterBuiltIn(TestContentIds.C02, CardEffectRoute.Summon, new C02CardEffectHandler());
            RegisterBuiltIn(TestContentIds.C03, CardEffectRoute.Passive, new C03CardEffectHandler());
            RegisterBuiltIn(TestContentIds.C04, CardEffectRoute.Passive,
                new C04CardEffectHandler(),
                BuiltInEffectTargetSpecs.EnemyMonsterSingleOnResolution);
            RegisterBuiltIn(TestContentIds.C05, CardEffectRoute.TargetedSkill,
                new C05CardEffectHandler(),
                BuiltInEffectTargetSpecs.EnemyMonsterSingleOnActivation);
            RegisterBuiltIn(TestContentIds.C06, CardEffectRoute.TargetedSkill,
                new C06CardEffectHandler(),
                BuiltInEffectTargetSpecs.EnemyMonsterSingleOnActivation);
            RegisterBuiltIn(TestContentIds.C07, CardEffectRoute.BanishSkill,
                new C07CardEffectHandler(),
                BuiltInEffectTargetSpecs.HandCardSingleOnActivation);
            RegisterBuiltIn(TestContentIds.C08, CardEffectRoute.TrapInstallation,
                new C08CardEffectHandler(),
                BuiltInEffectTargetSpecs.EnemyMonsterSingleOnResolution);
            RegisterBuiltIn(TestContentIds.C09, CardEffectRoute.TrapInstallation,
                new C09CardEffectHandler(),
                BuiltInEffectTargetSpecs.AllyMonsterSingleOnResolution);
            RegisterBuiltIn(TestContentIds.C10, CardEffectRoute.TrapInstallation,
                new C10CardEffectHandler(),
                BuiltInEffectTargetSpecs.EnemyMonsterSingleOnResolution);
            RegisterBuiltIn(TestContentIds.C11, CardEffectRoute.Passive,
                new C11CardEffectHandler(),
                BuiltInEffectTargetSpecs.AllyMonsterSingleOnResolution);
            RegisterBuiltIn(TestContentIds.C12, CardEffectRoute.Passive,
                new C12CardEffectHandler(),
                BuiltInEffectTargetSpecs.EnemyMonsterSingleOnResolution);
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
            ICardEffectHandler handler = null,
            EffectTargetSpec targetSpec = null)
        {
            TryRegister(new CardEffectRegistration(
                catalogCardId,
                route,
                handler,
                targetSpec));
        }
    }
}
