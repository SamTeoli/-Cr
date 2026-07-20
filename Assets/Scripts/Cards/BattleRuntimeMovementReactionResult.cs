namespace HaveABreak.Cards
{
    public sealed class BattleRuntimeMovementReactionResult
    {
        internal BattleRuntimeMovementReactionResult(
            int resolvedC04Count,
            int resolvedC12Count,
            int attackEnhancementGained,
            int vulnerableGained,
            int damageApplied)
        {
            ResolvedC04Count = resolvedC04Count;
            ResolvedC12Count = resolvedC12Count;
            AttackEnhancementGained = attackEnhancementGained;
            VulnerableGained = vulnerableGained;
            DamageApplied = damageApplied;
        }

        public int ResolvedC04Count { get; }
        public int ResolvedC12Count { get; }
        public int AttackEnhancementGained { get; }
        public int VulnerableGained { get; }
        public int DamageApplied { get; }
    }
}
