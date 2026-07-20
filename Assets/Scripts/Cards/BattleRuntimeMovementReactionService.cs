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
                    !string.Equals(
                        monster.Card.SourceCard.CatalogCardId,
                        "C04",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (C04TerminalCatResolver.TryResolve(
                        movedEvent,
                        runtime.Turn.PlayerTurnNumber,
                        monster,
                        runtime.CardTurnTriggers,
                        runtime.EventLog,
                        out int gained))
                {
                    resolvedC04Count++;
                    attackEnhancementGained += gained;
                }
            }

            BattleEnemyRuntimeState movedEnemy =
                runtime.FindEnemy(movedEvent.TargetId);
            foreach (BattleCardInstance card in
                     runtime.Deck.Zones.GetCards(CardZone.SkillField))
            {
                if (card == null ||
                    card.SourceCard.CardType != CardType.Barrier ||
                    !string.Equals(
                        card.SourceCard.CatalogCardId,
                        "C12",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (C12RouteMapStarlightResolver.TryResolve(
                        movedEvent,
                        runtime.Turn.PlayerTurnNumber,
                        card,
                        runtime.CardTurnTriggers,
                        runtime.EnemyStatuses,
                        movedEnemy?.Vital,
                        runtime.EventLog,
                        out int vulnerable,
                        out int damage))
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
