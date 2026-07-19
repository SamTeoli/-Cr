using System;

namespace HaveABreak.Cards
{
    public static class C09InspectionBlanketResolver
    {
        private const string EffectId = "C09-BEFORE-DAMAGE";

        public static bool TryResolve(
            BattleEventRecord declaredAttack,
            int currentEnemyTurn,
            int eligibleEnemyTurn,
            BattleCardInstance sourceTrap,
            BattleMonsterState targetMonster,
            BattleDefenseRetentionState retention,
            BattleCardTurnTriggerState triggers,
            BattleEventLog eventLog,
            out int defenseGained)
        {
            defenseGained = 0;
            if (declaredAttack == null || sourceTrap == null || targetMonster == null ||
                retention == null || triggers == null || eventLog == null ||
                currentEnemyTurn < eligibleEnemyTurn ||
                declaredAttack.EventType != BattleEventType.AttackDeclared ||
                eventLog.Find(declaredAttack.EventId) != declaredAttack ||
                sourceTrap.Zone != CardZone.SkillField ||
                targetMonster.Card.Zone != CardZone.MonsterField ||
                !string.Equals(sourceTrap.SourceCard.CatalogCardId, "C09", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(declaredAttack.TargetId, targetMonster.BattleCardId,
                    StringComparison.OrdinalIgnoreCase) ||
                !triggers.TryUse(
                    EffectId, sourceTrap.Ids.BattleCardId, currentEnemyTurn,
                    declaredAttack.EventId, 1))
            {
                return false;
            }

            int amount = sourceTrap.CurrentLevel >= 5 ? 5 :
                sourceTrap.CurrentLevel >= 2 ? 4 : 3;
            int before = targetMonster.Defense;
            defenseGained = targetMonster.ApplyDefense(amount);
            if (sourceTrap.CurrentLevel >= 4)
            {
                retention.Mark(targetMonster.BattleCardId);
            }

            eventLog.Record(
                BattleEventType.StatusApplied, "C09InspectionBlanket",
                sourceTrap.Ids.BattleCardId, sourceTrap.Ids.BattleCardId,
                targetMonster.BattleCardId, parentEventId: declaredAttack.EventId,
                sourceEffectId: EffectId, beforeValue: before,
                afterValue: targetMonster.Defense);
            return true;
        }
    }
}
