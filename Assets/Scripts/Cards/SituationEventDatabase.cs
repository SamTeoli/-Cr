using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [CreateAssetMenu(fileName = "SituationEventDatabase",
        menuName = "Have a Break/Run/Situation Event Database")]
    public sealed class SituationEventDatabase : ScriptableObject
    {
        [SerializeField] private List<SituationEventData> events = new();

        public IReadOnlyList<SituationEventData> Events => events;

        public SituationEventData Find(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId)) return null;
            return events.Find(value => value != null && string.Equals(
                value.EventId, eventId.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public IReadOnlyList<string> GetValidationErrors()
        {
            List<string> errors = new();
            HashSet<string> eventIds = new(StringComparer.OrdinalIgnoreCase);
            foreach (SituationEventData value in events)
            {
                if (value == null || string.IsNullOrWhiteSpace(value.EventId))
                {
                    errors.Add("Situation event has an empty ID.");
                    continue;
                }
                if (!eventIds.Add(value.EventId.Trim()))
                    errors.Add($"Duplicate situation event ID: {value.EventId}");

                HashSet<string> choiceIds = new(StringComparer.OrdinalIgnoreCase);
                foreach (SituationEventChoiceData choice in value.Choices)
                {
                    if (choice == null || string.IsNullOrWhiteSpace(choice.ChoiceId))
                        errors.Add($"Situation event {value.EventId} has an empty choice ID.");
                    else if (!choiceIds.Add(choice.ChoiceId.Trim()))
                        errors.Add($"Situation event {value.EventId} has duplicate choice ID: {choice.ChoiceId}");
                }
            }
            return errors;
        }
    }
}
