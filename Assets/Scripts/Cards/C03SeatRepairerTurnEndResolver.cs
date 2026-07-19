using System;

namespace HaveABreak.Cards
{
    public static class C03SeatRepairerTurnEndResolver
    {
        private const string EffectId = "C03-TURN-END";

        public static bool TryResolve(
            BattleMonsterState sourceMonster,
            int playerTurn,
            int firstTurnEventIndex,
            BattleEventLog eventLog,
            BattleEffectResolutionTracker resolutions,
            out C03SeatRepairerResult result)
        {
            result = default;
            if (sourceMonster == null || playerTurn < 1 || eventLog == null || resolutions == null ||
                firstTurnEventIndex < 0 || firstTurnEventIndex > eventLog.Events.Count ||
                sourceMonster.Card.Zone != CardZone.MonsterField ||
                !string.Equals(
                    sourceMonster.Card.SourceCard.CatalogCardId,
                    "C03",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string turnResolutionId = $"PLAYER-TURN-{playerTurn}:{sourceMonster.BattleCardId}";
            if (!resolutions.TryBegin(EffectId, turnResolutionId))
            {
                return false;
            }

            bool attackedThisTurn = false;
            for (int i = firstTurnEventIndex; i < eventLog.Events.Count; i++)
            {
                BattleEventRecord record = eventLog.Events[i];
                if (record != null &&
                    record.EventType == BattleEventType.AttackCompleted &&
                    string.Equals(
                        record.ActorId,
                        sourceMonster.BattleCardId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    attackedThisTurn = true;
                    break;
                }
            }

            if (attackedThisTurn)
            {
                result = new C03SeatRepairerResult(true, 0, 0);
                return true;
            }

            int defenseAmount = sourceMonster.Card.CurrentLevel >= 3 ? 4 : 3;
            int beforeDefense = sourceMonster.Defense;
            int defenseGained = sourceMonster.ApplyDefense(defenseAmount);
            eventLog.Record(
                BattleEventType.StatusApplied,
                "C03TurnEndDefense",
                sourceMonster.BattleCardId,
                sourceMonster.BattleCardId,
                sourceMonster.BattleCardId,
                sourceEffectId: EffectId,
                beforeValue: beforeDefense,
                afterValue: sourceMonster.Defense);

            int counterGained = 0;
            if (sourceMonster.Card.CurrentLevel >= 5)
            {
                int beforeCounter = sourceMonster.Counter;
                counterGained = sourceMonster.ApplyCounter(1);
                eventLog.Record(
                    BattleEventType.StatusApplied,
                    "C03TurnEndCounter",
                    sourceMonster.BattleCardId,
                    sourceMonster.BattleCardId,
                    sourceMonster.BattleCardId,
                    sourceEffectId: EffectId,
                    beforeValue: beforeCounter,
                    afterValue: sourceMonster.Counter);
            }

            result = new C03SeatRepairerResult(false, defenseGained, counterGained);
            return true;
        }
    }
}
