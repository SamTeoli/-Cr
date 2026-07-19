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
        [SerializeField] private BattleMonsterRegistry monsters;

        private BattleEffectExecutor()
        {
        }

        public BattleEffectExecutor(
            BattleDeckState deck,
            BattleEventLog eventLog,
            BattleEffectQueue effectQueue,
            BattleMonsterRegistry monsters = null)
        {
            this.deck = deck ?? throw new ArgumentNullException(nameof(deck));
            this.eventLog = eventLog ?? throw new ArgumentNullException(nameof(eventLog));
            this.effectQueue = effectQueue ?? throw new ArgumentNullException(nameof(effectQueue));
            this.monsters = monsters;
        }

        public BattleDeckState Deck => deck;
        public BattleEventLog EventLog => eventLog;
        public BattleEffectQueue EffectQueue => effectQueue;
        public BattleMonsterRegistry Monsters => monsters;

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

            switch (command.Operation)
            {
                case EffectOperation.Move:
                    return TryExecuteMove(command, sourceEvent, out producedEvent, out failure);
                case EffectOperation.Damage:
                    return TryExecuteHealthChange(command, sourceEvent, false, out producedEvent, out failure);
                case EffectOperation.Heal:
                    return TryExecuteHealthChange(command, sourceEvent, true, out producedEvent, out failure);
                default:
                    failure = EffectExecutionFailure.UnsupportedOperation;
                    return false;
            }
        }

        private bool TryExecuteMove(
            BattleEffectCommand command,
            BattleEventRecord sourceEvent,
            out BattleEventRecord producedEvent,
            out EffectExecutionFailure failure)
        {
            producedEvent = null;
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

        private bool TryExecuteHealthChange(
            BattleEffectCommand command,
            BattleEventRecord sourceEvent,
            bool healing,
            out BattleEventRecord producedEvent,
            out EffectExecutionFailure failure)
        {
            producedEvent = null;
            if (command.Value < 0)
            {
                failure = EffectExecutionFailure.InvalidValue;
                return false;
            }

            BattleMonsterState target = monsters?.Find(command.TargetBattleCardId);
            if (target == null)
            {
                failure = EffectExecutionFailure.CombatTargetNotFound;
                return false;
            }

            int beforeHealth = target.CurrentHealth;
            if (healing)
            {
                target.ApplyHealing(command.Value);
            }
            else
            {
                target.ApplyDamage(command.Value);
            }

            producedEvent = eventLog.Record(
                healing ? BattleEventType.HealingApplied : BattleEventType.DamageApplied,
                healing ? "EffectHealing" : "EffectDamage",
                command.SourceId,
                command.SourceId,
                target.BattleCardId,
                parentEventId: sourceEvent.EventId,
                sourceEffectId: command.EffectId,
                beforeValue: beforeHealth,
                afterValue: target.CurrentHealth);
            failure = EffectExecutionFailure.None;
            return true;
        }
    }
}
