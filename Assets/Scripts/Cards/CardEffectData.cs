using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class CardEffectData
    {
        [SerializeField] private string effectId;
        [SerializeField] private EffectTrigger trigger;
        [SerializeField, HideInInspector] private EffectTarget target;
        [SerializeField] private EffectTargetSpec targetSpec;
        [SerializeField] private EffectOperation operation;
        [SerializeField] private StatusKeyword statusKeyword;
        [SerializeField] private int value;
        [SerializeField, Min(0)] private int duration;
        [SerializeField, TextArea(2, 5)] private string description;

        public CardEffectData()
        {
        }

        public CardEffectData(
            string id,
            EffectTrigger effectTrigger,
            EffectOperation effectOperation,
            int effectValue,
            EffectTargetSpec effectTargetSpec = null,
            StatusKeyword keyword = StatusKeyword.None,
            int effectDuration = 0,
            string fallbackDescription = null)
        {
            effectId = id?.Trim();
            trigger = effectTrigger;
            operation = effectOperation;
            value = effectValue;
            targetSpec = effectTargetSpec;
            statusKeyword = keyword;
            duration = Mathf.Max(0, effectDuration);
            description = fallbackDescription;
        }

        public string EffectId => effectId;
        public EffectTrigger Trigger => trigger;
        [Obsolete("Use TargetSpec instead.")]
        public EffectTarget Target => target;
        public EffectTargetSpec TargetSpec => targetSpec;
        public EffectOperation Operation => operation;
        public StatusKeyword StatusKeyword => statusKeyword;
        public int Value => value;
        public int Duration => duration;
        public string Description => description;

        public EffectTargetSpec ResolveTargetSpec(
            EffectTargetSpec cardDefault)
        {
            return targetSpec != null ? targetSpec : cardDefault;
        }
    }
}
