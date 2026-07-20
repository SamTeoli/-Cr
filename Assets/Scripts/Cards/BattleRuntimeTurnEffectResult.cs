namespace HaveABreak.Cards
{
    public sealed class BattleRuntimeTurnEffectResult
    {
        internal BattleRuntimeTurnEffectResult(
            int resolvedC03Count,
            int resolvedC04Count,
            int totalDefenseGained,
            int totalAttackEnhancement)
        {
            ResolvedC03Count = resolvedC03Count;
            ResolvedC04Count = resolvedC04Count;
            TotalDefenseGained = totalDefenseGained;
            TotalAttackEnhancement = totalAttackEnhancement;
        }

        public int ResolvedC03Count { get; }
        public int ResolvedC04Count { get; }
        public int TotalDefenseGained { get; }
        public int TotalAttackEnhancement { get; }
    }
}
