using System;

namespace HaveABreak.Cards
{
    public static class BattleRuntimeMovementReactionService
    {
        public static bool TryResolve(
            BattleRuntimeState runtime,
            BattleEventRecord movedEvent,
            out BattleRuntimeMovementReactionResult result)
        {
            result = null;
            if (runtime == null || movedEvent == null ||
                runtime.Turn.PlayerTurnNumber < 1 ||
                movedEvent.EventType != BattleEventType.EnemyMoved ||
                runtime.EventLog.Find(movedEvent.EventId) != movedEvent)
            {
                return false;
            }

            int resolvedC04Count = 0;
            int resolvedC12Count = 0;
            int attackEnhancementGained = 0;
            int vulnerableGained = 0;
            int damageApplied = 0;

            foreach (BattleMonsterState monster in runtime.Monsters.Monsters)
            {
                if (monster == null ||
                    monster.Card.Zone != CardZone.MonsterField ||
                    !CardEffectRegistrationCatalog.TryFind(
                        monster.Card.SourceCard.CatalogCardId, out CardEffectRegistration registration) ||
                    registration.Handler is not IEnemyMovementMonsterCardEffectHandler handler)
                {
                    continue;
                }

                if (!monster.Status.CanUseAbility)
                {
                    runtime.EventLog.Record(
                        BattleEventType.PlayerMonsterActionBlocked,
                        "PlayerMonsterAbilityBlockedByStun",
                        monster.BattleCardId,
                        monster.BattleCardId,
                        monster.BattleCardId,
                        parentEventId: movedEvent.EventId,
                        beforeValue: monster.Status.Stun,
                        afterValue: monster.Status.Stun);
                    continue;
                }

                if (handler.TryResolve(runtime, monster, movedEvent, out int gained))
                {
                    resolvedC04Count++;
                    attackEnhancementGained += gained;
                }
            }

            foreach (BattleCardInstance card in
                     runtime.Deck.Zones.GetCards(CardZone.SkillField))
            {
                if (card == null ||
                    card.SourceCard.CardType != CardType.Barrier ||
                    !CardEffectRegistrationCatalog.TryFind(
                        card.SourceCard.CatalogCardId, out CardEffectRegistration registration) ||
                    registration.Handler is not IEnemyMovementBarrierCardEffectHandler handler)
                {
                    continue;
                }

                if (handler.TryResolve(runtime, card, movedEvent,
                        out int vulnerable, out int damage))
                {
                    resolvedC12Count++;
                    vulnerableGained += vulnerable;
                    damageApplied += damage;
                }
            }

            result = new BattleRuntimeMovementReactionResult(
                resolvedC04Count,
                resolvedC12Count,
                attackEnhancementGained,
                vulnerableGained,
                damageApplied);
            return true;
        }
    }
}
