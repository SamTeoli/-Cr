using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class EnchantEffectData
    {
        [SerializeField] private string trigger;
        [SerializeField] private string conditionAndTarget;
        [SerializeField] private string resolution;
        [SerializeField] private string limitation;

        public EnchantEffectData(
            string trigger,
            string conditionAndTarget,
            string resolution,
            string limitation)
        {
            this.trigger = trigger;
            this.conditionAndTarget = conditionAndTarget;
            this.resolution = resolution;
            this.limitation = limitation;
        }

        public string Trigger => trigger;
        public string ConditionAndTarget => conditionAndTarget;
        public string Resolution => resolution;
        public string Limitation => limitation;
    }
}
