namespace HaveABreak.Cards
{
    public readonly struct RepeatedEffectParameters
    {
        public RepeatedEffectParameters(
            int firstValue,
            int targetCount,
            int activationCount,
            int conditionThreshold)
        {
            FirstValue = firstValue;
            TargetCount = targetCount;
            ActivationCount = activationCount;
            ConditionThreshold = conditionThreshold;
        }

        public int FirstValue { get; }
        public int TargetCount { get; }
        public int ActivationCount { get; }
        public int ConditionThreshold { get; }
    }
}
