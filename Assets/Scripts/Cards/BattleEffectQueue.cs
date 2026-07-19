using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleEffectQueue
    {
        [SerializeField] private List<BattleEffectCommand> pending = new();
        [SerializeField] private List<string> registrationHistory = new();
        [SerializeField] private int nextCreationOrder = 1;

        public IReadOnlyList<BattleEffectCommand> Pending => pending;
        public int Count => pending.Count;

        public bool TryRegister(
            BattleEffectCommand command,
            BattleEventRecord sourceEvent,
            out EffectQueueFailure failure)
        {
            if (sourceEvent == null || string.IsNullOrWhiteSpace(sourceEvent.EventId))
            {
                failure = EffectQueueFailure.InvalidEvent;
                return false;
            }

            if (command == null ||
                string.IsNullOrWhiteSpace(command.EffectId) ||
                string.IsNullOrWhiteSpace(command.EventId))
            {
                failure = EffectQueueFailure.InvalidEffect;
                return false;
            }

            if (!string.Equals(command.EventId, sourceEvent.EventId, StringComparison.OrdinalIgnoreCase))
            {
                failure = EffectQueueFailure.EventMismatch;
                return false;
            }

            if (!command.AllowRepeatedTrigger &&
                string.Equals(command.EffectId, sourceEvent.SourceEffectId, StringComparison.OrdinalIgnoreCase) &&
                command.TriggerEventType == sourceEvent.EventType)
            {
                failure = EffectQueueFailure.SelfRepeatBlocked;
                return false;
            }

            string registrationKey = GetRegistrationKey(command.EffectId, command.EventId);
            int existingCount = registrationHistory.FindAll(item =>
                string.Equals(item, registrationKey, StringComparison.OrdinalIgnoreCase)).Count;
            int registrationLimit = command.AllowRepeatedTrigger
                ? command.MaximumRegistrationsPerEvent
                : 1;
            if (existingCount >= registrationLimit)
            {
                failure = EffectQueueFailure.DuplicateForEvent;
                return false;
            }

            command.AssignCreationOrder(nextCreationOrder);
            nextCreationOrder++;
            pending.Add(command);
            registrationHistory.Add(registrationKey);
            failure = EffectQueueFailure.None;
            return true;
        }

        public bool TryDequeue(out BattleEffectCommand command)
        {
            command = null;
            if (pending.Count == 0)
            {
                return false;
            }

            int selectedIndex = 0;
            for (int i = 1; i < pending.Count; i++)
            {
                if (ComesBefore(pending[i], pending[selectedIndex]))
                {
                    selectedIndex = i;
                }
            }

            command = pending[selectedIndex];
            pending.RemoveAt(selectedIndex);
            return true;
        }

        private static bool ComesBefore(BattleEffectCommand candidate, BattleEffectCommand current)
        {
            if (candidate.Stage != current.Stage)
            {
                return candidate.Stage < current.Stage;
            }

            if (candidate.Stage == EffectProcessingStage.Response)
            {
                return candidate.CreationOrder > current.CreationOrder;
            }

            if (candidate.Stage == EffectProcessingStage.Aftermath)
            {
                if (candidate.AftermathPriority != current.AftermathPriority)
                {
                    return candidate.AftermathPriority < current.AftermathPriority;
                }

                if (candidate.Required != current.Required)
                {
                    return candidate.Required;
                }
            }

            return candidate.CreationOrder < current.CreationOrder;
        }

        private static string GetRegistrationKey(string effectId, string eventId)
        {
            return $"{effectId}|{eventId}";
        }
    }
}
