namespace HaveABreak.Cards
{
    public sealed class BattleRuntimeSkillEffectResult
    {
        internal BattleRuntimeSkillEffectResult(
            string catalogCardId,
            string targetEnemyId,
            int movedSteps,
            int weakenGained,
            int vulnerableGained,
            string secondaryEnemyId)
        {
            CatalogCardId = catalogCardId;
            TargetEnemyId = targetEnemyId;
            MovedSteps = movedSteps;
            WeakenGained = weakenGained;
            VulnerableGained = vulnerableGained;
            SecondaryEnemyId = secondaryEnemyId;
        }

        public string CatalogCardId { get; }
        public string TargetEnemyId { get; }
        public int MovedSteps { get; }
        public int WeakenGained { get; }
        public int VulnerableGained { get; }
        public string SecondaryEnemyId { get; }
    }
}
