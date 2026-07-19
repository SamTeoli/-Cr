using System;

namespace HaveABreak.Cards
{
    public static class C10BrokenCallLineResolver
    {
        private const string EffectId = "C10-ENEMY-ABILITY";

        public static bool TryCancel(
            BattleEventRecord abilityEvent,
            EnemyAbilityResolutionContext ability,
            BattleCardInstance sourceTrap,
            BattleCardZoneState zones,
            BattleEnemyStatusRegistry statuses,
            BattleEventLog eventLog,
            BattleEffectResolutionTracker resolutions,
            out bool cancelled,
            out bool returnedToHand)
        {
            cancelled = false;
            returnedToHand = false;
            if (abilityEvent == null || sourceTrap == null || zones == null ||
                statuses == null || eventLog == null || resolutions == null ||
                eventLog.Find(abilityEvent.EventId) != abilityEvent ||
                sourceTrap.Zone != CardZone.SkillField ||
                !string.Equals(sourceTrap.SourceCard.CatalogCardId, "C10", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(ability.AbilityId) ||
                string.IsNullOrWhiteSpace(ability.SourceEnemyId) ||
                ability.IsNormalAttack || !ability.AffectsFriendlySide ||
                ability.IsAreaAbility && sourceTrap.CurrentLevel < 4 ||
                !resolutions.TryBegin(EffectId, abilityEvent.EventId))
            {
                return false;
            }

            cancelled = true;
            if (sourceTrap.CurrentLevel >= 3)
            {
                statuses.Find(ability.SourceEnemyId)?.ApplyWeaken(1);
            }

            CardZone destination = sourceTrap.CurrentLevel >= 5
                ? CardZone.Hand
                : CardZone.Graveyard;
            if (!zones.TryMove(sourceTrap.Ids.BattleCardId, destination, out _))
            {
                return false;
            }

            returnedToHand = destination == CardZone.Hand;
            eventLog.Record(
                BattleEventType.StatusApplied, "C10CancelEnemyAbility",
                sourceTrap.Ids.BattleCardId, sourceTrap.Ids.BattleCardId,
                ability.SourceEnemyId, parentEventId: abilityEvent.EventId,
                sourceEffectId: EffectId, beforeValue: 1, afterValue: 0);
            return true;
        }
    }
}
