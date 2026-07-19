using System;

namespace HaveABreak.Cards
{
    public static class C04TerminalCatResolver
    {
        private const string EffectId = "C04-ENEMY-MOVED";

        public static bool TryResolve(
            BattleEventRecord movedEvent,
            int enemyTurnNumber,
            BattleMonsterState sourceMonster,
            BattleCardTurnTriggerState triggers,
            BattleEventLog eventLog,
            out int attackEnhancement)
        {
            attackEnhancement = 0;
            if (movedEvent == null || sourceMonster == null || triggers == null || eventLog == null ||
                movedEvent.EventType != BattleEventType.EnemyMoved ||
                eventLog.Find(movedEvent.EventId) != movedEvent ||
                sourceMonster.Card.Zone != CardZone.MonsterField ||
                !string.Equals(sourceMonster.Card.SourceCard.CatalogCardId, "C04",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string commandKey = string.IsNullOrWhiteSpace(movedEvent.ParentEventId)
                ? movedEvent.EventId
                : movedEvent.ParentEventId;
            int maximumUses = sourceMonster.Card.CurrentLevel >= 5 ? 2 : 1;
            if (!triggers.TryUse(
                    EffectId, sourceMonster.BattleCardId, enemyTurnNumber, commandKey, maximumUses))
            {
                return false;
            }

            int amount = sourceMonster.Card.CurrentLevel >= 3 ? 2 : 1;
            int before = sourceMonster.AttackEnhancement;
            attackEnhancement = sourceMonster.ApplyAttackEnhancement(amount);
            eventLog.Record(
                BattleEventType.StatusApplied, "C04AttackEnhancement",
                sourceMonster.BattleCardId, sourceMonster.BattleCardId, sourceMonster.BattleCardId,
                parentEventId: movedEvent.EventId, sourceEffectId: EffectId,
                beforeValue: before, afterValue: sourceMonster.AttackEnhancement);
            return true;
        }
    }
}
