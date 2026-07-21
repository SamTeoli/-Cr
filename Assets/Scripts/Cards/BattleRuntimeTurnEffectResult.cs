namespace HaveABreak.Cards
{
    public sealed class BattleRuntimeTurnEffectResult
    {
        internal BattleRuntimeTurnEffectResult(
            int resolvedC03Count,
            int resolvedC04Count,
            int totalDefenseGained,
            int totalAttackEnhancement,
            BattleRuntimeFriendlyStatusTurnResult friendlyStatusTurnEnd = null,
            BattleOutcome outcome = BattleOutcome.Ongoing)
        {
            ResolvedC03Count = resolvedC03Count;
            ResolvedC04Count = resolvedC04Count;
            TotalDefenseGained = totalDefenseGained;
            TotalAttackEnhancement = totalAttackEnhancement;
            FriendlyStatusTurnEnd = friendlyStatusTurnEnd;
            Outcome = outcome;
        }

        public int ResolvedC03Count { get; }
        public int ResolvedC04Count { get; }
        public int TotalDefenseGained { get; }
        public int TotalAttackEnhancement { get; }
        public BattleRuntimeFriendlyStatusTurnResult FriendlyStatusTurnEnd
        {
            get;
        }
        public BattleOutcome Outcome { get; }
    }
}
