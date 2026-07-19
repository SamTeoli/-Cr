using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleEventRecord
    {
        [SerializeField] private string eventId;
        [SerializeField] private string parentEventId;
        [SerializeField] private BattleEventType eventType;
        [SerializeField] private string cause;
        [SerializeField] private string sourceId;
        [SerializeField] private string sourceEffectId;
        [SerializeField] private string actorId;
        [SerializeField] private string targetId;
        [SerializeField] private CardZone fromZone;
        [SerializeField] private CardZone toZone;
        [SerializeField] private bool hasZoneChange;
        [SerializeField] private int beforeValue;
        [SerializeField] private int afterValue;
        [SerializeField] private uint randomState;

        internal BattleEventRecord(
            string eventId,
            string parentEventId,
            BattleEventType eventType,
            string cause,
            string sourceId,
            string sourceEffectId,
            string actorId,
            string targetId,
            bool hasZoneChange,
            CardZone fromZone,
            CardZone toZone,
            int beforeValue,
            int afterValue,
            uint randomState)
        {
            this.eventId = eventId;
            this.parentEventId = parentEventId;
            this.eventType = eventType;
            this.cause = cause;
            this.sourceId = sourceId;
            this.sourceEffectId = sourceEffectId;
            this.actorId = actorId;
            this.targetId = targetId;
            this.hasZoneChange = hasZoneChange;
            this.fromZone = fromZone;
            this.toZone = toZone;
            this.beforeValue = beforeValue;
            this.afterValue = afterValue;
            this.randomState = randomState;
        }

        public string EventId => eventId;
        public string ParentEventId => parentEventId;
        public BattleEventType EventType => eventType;
        public string Cause => cause;
        public string SourceId => sourceId;
        public string SourceEffectId => sourceEffectId;
        public string ActorId => actorId;
        public string TargetId => targetId;
        public bool HasZoneChange => hasZoneChange;
        public CardZone FromZone => fromZone;
        public CardZone ToZone => toZone;
        public int BeforeValue => beforeValue;
        public int AfterValue => afterValue;
        public uint RandomState => randomState;
    }
}
