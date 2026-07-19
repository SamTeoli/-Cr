using System;

namespace HaveABreak.Cards
{
    public static class C02LanternBearerResolver
    {
        private const string EffectId = "C02-SUMMON";

        public static bool TryResolve(
            BattleEventRecord summonedEvent,
            BattleMonsterState sourceMonster,
            BattleNextSkillModifierState modifiers,
            BattleEventLog eventLog,
            BattleEffectResolutionTracker resolutions,
            out C02LanternBearerResult result)
        {
            result = default;
            if (summonedEvent == null || sourceMonster == null || modifiers == null ||
                eventLog == null || resolutions == null ||
                summonedEvent.EventType != BattleEventType.MonsterSummoned ||
                eventLog.Find(summonedEvent.EventId) != summonedEvent ||
                !string.Equals(summonedEvent.ActorId, sourceMonster.BattleCardId, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(sourceMonster.Card.SourceCard.CatalogCardId, "C02", StringComparison.OrdinalIgnoreCase) ||
                !resolutions.TryBegin(EffectId, summonedEvent.EventId))
            {
                return false;
            }

            int numericBonus = sourceMonster.Card.CurrentLevel >= 5 ? 1 : 0;
            int beforeCount = modifiers.PendingCount;
            modifiers.Add(sourceMonster.BattleCardId, 1, numericBonus);
            eventLog.Record(
                BattleEventType.StatusApplied, "C02NextSkillCost", sourceMonster.BattleCardId,
                sourceMonster.BattleCardId, "PLAYER", summonedEvent.EventId, EffectId,
                beforeValue: beforeCount, afterValue: modifiers.PendingCount);

            int defenseGained = 0;
            if (sourceMonster.Card.CurrentLevel >= 4)
            {
                int beforeDefense = sourceMonster.Defense;
                defenseGained = sourceMonster.ApplyDefense(1);
                eventLog.Record(
                    BattleEventType.StatusApplied, "C02Defense", sourceMonster.BattleCardId,
                    sourceMonster.BattleCardId, sourceMonster.BattleCardId, summonedEvent.EventId, EffectId,
                    beforeValue: beforeDefense, afterValue: sourceMonster.Defense);
            }

            result = new C02LanternBearerResult(1, numericBonus, defenseGained);
            return true;
        }
    }
}
