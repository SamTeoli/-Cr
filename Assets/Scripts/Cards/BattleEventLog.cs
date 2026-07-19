using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleEventLog
    {
        [SerializeField] private List<BattleEventRecord> events = new();
        [SerializeField] private int nextEventSequence = 1;

        public IReadOnlyList<BattleEventRecord> Events => events;

        public BattleEventRecord Record(
            BattleEventType eventType,
            string cause,
            string sourceId,
            string actorId,
            string targetId,
            string parentEventId = null,
            string sourceEffectId = null,
            bool hasZoneChange = false,
            CardZone fromZone = CardZone.DrawPile,
            CardZone toZone = CardZone.DrawPile,
            int beforeValue = 0,
            int afterValue = 0,
            uint randomState = 0)
        {
            if (!string.IsNullOrWhiteSpace(parentEventId) && Find(parentEventId) == null)
            {
                throw new ArgumentException("Parent event does not exist.", nameof(parentEventId));
            }

            string eventId = $"EVENT-{nextEventSequence:D6}";
            nextEventSequence++;
            BattleEventRecord record = new(
                eventId,
                parentEventId,
                eventType,
                cause,
                sourceId,
                sourceEffectId,
                actorId,
                targetId,
                hasZoneChange,
                fromZone,
                toZone,
                beforeValue,
                afterValue,
                randomState);
            events.Add(record);
            return record;
        }

        public BattleEventRecord Find(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                return null;
            }

            return events.Find(item => item != null &&
                string.Equals(item.EventId, eventId, StringComparison.OrdinalIgnoreCase));
        }
    }
}
