using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class SituationEventChoiceData
    {
        [SerializeField] private string choiceId;
        [SerializeField] private RunSituationEffect effect;
        [SerializeField] private int value;
        [SerializeField] private string displayText;

        public string ChoiceId => choiceId;
        public RunSituationEffect Effect => effect;
        public int Value => value;
        public string DisplayText => displayText;
    }

    [CreateAssetMenu(fileName = "SituationEvent",
        menuName = "Have a Break/Run/Situation Event")]
    public sealed class SituationEventData : ScriptableObject
    {
        [SerializeField] private string eventId;
        [SerializeField] private string displayName;
        [SerializeField, Min(0)] private int minimumCompletedNodeCount;
        [SerializeField] private int maximumCompletedNodeCount = -1;
        [SerializeField] private bool allowRepeatInRun = true;
        [SerializeField] private List<SituationEventChoiceData> choices = new();

        public string EventId => eventId;
        public string DisplayName => displayName;
        public int MinimumCompletedNodeCount => minimumCompletedNodeCount;
        public int MaximumCompletedNodeCount => maximumCompletedNodeCount;
        public bool AllowRepeatInRun => allowRepeatInRun;
        public IReadOnlyList<SituationEventChoiceData> Choices => choices;

        public bool IsAvailableAt(int completedNodeCount)
        {
            return completedNodeCount >= minimumCompletedNodeCount &&
                   (maximumCompletedNodeCount < 0 ||
                    completedNodeCount <= maximumCompletedNodeCount);
        }

        private void OnValidate()
        {
            eventId = eventId?.Trim();
            displayName = displayName?.Trim();
            minimumCompletedNodeCount = Mathf.Max(0, minimumCompletedNodeCount);
            if (maximumCompletedNodeCount < -1)
                maximumCompletedNodeCount = -1;
        }
    }
}
