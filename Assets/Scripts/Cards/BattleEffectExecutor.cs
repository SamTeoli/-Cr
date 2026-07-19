using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleEffectExecutor
    {
        [SerializeField] private BattleDeckState deck;
        [SerializeField] private BattleEventLog eventLog;
        [SerializeField] private BattleEffectQueue effectQueue;

        private BattleEffectExecutor()
        {
        }

        public BattleEffectExecutor(
            BattleDeckState deck,
            BattleEventLog eventLog,
            BattleEffectQueue effectQueue)
        {
            this.deck = deck ?? throw new ArgumentNullException(nameof(deck));
            this.eventLog = eventLog ?? throw new ArgumentNullException(nameof(eventLog));
            this.effectQueue = effectQueue ?? throw new ArgumentNullException(nameof(effectQueue));
        }

        public BattleDeckState Deck => deck;
        public BattleEventLog EventLog => eventLog;
        public BattleEffectQueue EffectQueue => effectQueue;

        public bool TryExecuteNext(
            out BattleEffectCommand command,
            out BattleEventRecord producedEvent,
            out EffectExecutionFailure failure)
        {
            producedEvent = null;
            if (!effectQueue.TryDequeue(out command))
            {
                failure = EffectExecutionFailure.QueueEmpty;
                return false;
            }

            BattleEventRecord sourceEvent = eventLog.Find(command.EventId);
            if (sourceEvent == null)
            {
                failure = EffectExecutionFailure.SourceEventNotFound;
                return false;
            }

            if (command.Operation != EffectOperation.Move)
            {
                failure = EffectExecutionFailure.UnsupportedOperation;
                return false;
            }

            if (!command.HasDestinationZone)
            {
                failure = EffectExecutionFailure.InvalidZoneTransition;
                return false;
            }

            BattleCardInstance target = deck.Zones.Find(command.TargetBattleCardId);
            if (target == null)
            {
                failure = EffectExecutionFailure.TargetNotFound;
                return false;
            }

            CardZone fromZone = target.Zone;
            CardZone requestedDestination = command.DestinationZone;
            if (fromZone == CardZone.DrawPile ||
                requestedDestination == CardZone.DrawPile ||
                requestedDestination == CardZone.RedrawHolding)
            {
                failure = EffectExecutionFailure.InvalidZoneTransition;
                return false;
            }

            CardZone actualDestination = target.IsTemporary && requestedDestination == CardZone.Graveyard
                ? CardZone.Banished
                : requestedDestination;
            if (fromZone == actualDestination)
            {
                failure = EffectExecutionFailure.InvalidZoneTransition;
                return false;
            }

            if (!deck.Zones.TryMove(target.Ids.BattleCardId, actualDestination, out _))
            {
                failure = EffectExecutionFailure.ZoneMoveFailed;
                return false;
            }

            producedEvent = eventLog.Record(
                BattleEventType.CardMoved,
                "EffectMove",
                command.SourceId,
                command.SourceId,
                target.Ids.BattleCardId,
                parentEventId: sourceEvent.EventId,
                sourceEffectId: command.EffectId,
                hasZoneChange: true,
                fromZone: fromZone,
                toZone: actualDestination);
            failure = EffectExecutionFailure.None;
            return true;
        }
    }
}
