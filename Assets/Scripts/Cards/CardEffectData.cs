using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class CardEffectData
    {
        [SerializeField] private string effectId = "E01";
        [SerializeField] private EffectTrigger trigger;
        [SerializeField] private EffectTarget target;
        [SerializeField] private EffectOperation operation;
        [SerializeField] private StatusKeyword statusKeyword;
        [SerializeField] private int value;
        [SerializeField, Min(0)] private int duration;
        [SerializeField, TextArea(2, 5)] private string description;

        public string EffectId => effectId;
        public EffectTrigger Trigger => trigger;
        public EffectTarget Target => target;
        public EffectOperation Operation => operation;
        public StatusKeyword StatusKeyword => statusKeyword;
        public int Value => value;
        public int Duration => duration;
        public string Description => description;
    }
}
