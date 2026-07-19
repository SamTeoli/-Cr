using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleEffectCommand
    {
        [SerializeField] private string effectId;
        [SerializeField] private string sourceId;
        [SerializeField] private string eventId;
        [SerializeField] private EffectProcessingStage stage;
        [SerializeField] private AftermathEffectPriority aftermathPriority;
        [SerializeField] private bool required;
        [SerializeField] private BattleEventType triggerEventType;
        [SerializeField] private bool allowRepeatedTrigger;
        [SerializeField, Min(1)] private int maximumRegistrationsPerEvent = 1;
        [SerializeField] private int creationOrder;
        [SerializeField] private EffectOperation operation;
        [SerializeField] private string targetBattleCardId;
        [SerializeField] private CardZone destinationZone;
        [SerializeField] private bool hasDestinationZone;
        [SerializeField] private int value;
        [SerializeField] private bool normalResolutionGraveyardMove;

        public BattleEffectCommand(
            string effectId,
            string sourceId,
            string eventId,
            EffectProcessingStage stage,
            bool required,
            BattleEventType triggerEventType,
            AftermathEffectPriority aftermathPriority = AftermathEffectPriority.SourceCard,
            bool allowRepeatedTrigger = false,
            int maximumRegistrationsPerEvent = 1,
            EffectOperation operation = EffectOperation.None,
            string targetBattleCardId = null,
            CardZone destinationZone = CardZone.DrawPile,
            bool hasDestinationZone = false,
            int value = 0,
            bool normalResolutionGraveyardMove = false)
        {
            this.effectId = effectId?.Trim();
            this.sourceId = sourceId?.Trim();
            this.eventId = eventId?.Trim();
            this.stage = stage;
            this.required = required;
            this.triggerEventType = triggerEventType;
            this.aftermathPriority = aftermathPriority;
            this.allowRepeatedTrigger = allowRepeatedTrigger;
            this.maximumRegistrationsPerEvent = Mathf.Max(1, maximumRegistrationsPerEvent);
            this.operation = operation;
            this.targetBattleCardId = targetBattleCardId?.Trim();
            this.destinationZone = destinationZone;
            this.hasDestinationZone = hasDestinationZone;
            this.value = value;
            this.normalResolutionGraveyardMove = normalResolutionGraveyardMove;
        }

        public string EffectId => effectId;
        public string SourceId => sourceId;
        public string EventId => eventId;
        public EffectProcessingStage Stage => stage;
        public AftermathEffectPriority AftermathPriority => aftermathPriority;
        public bool Required => required;
        public BattleEventType TriggerEventType => triggerEventType;
        public bool AllowRepeatedTrigger => allowRepeatedTrigger;
        public int MaximumRegistrationsPerEvent => maximumRegistrationsPerEvent;
        public int CreationOrder => creationOrder;
        public EffectOperation Operation => operation;
        public string TargetBattleCardId => targetBattleCardId;
        public CardZone DestinationZone => destinationZone;
        public bool HasDestinationZone => hasDestinationZone;
        public int Value => value;
        public bool NormalResolutionGraveyardMove => normalResolutionGraveyardMove;

        internal void AssignCreationOrder(int value)
        {
            creationOrder = value;
        }
    }
}
