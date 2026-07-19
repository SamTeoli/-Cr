namespace HaveABreak.Cards
{
    public readonly struct EnchantFixedTargetDeclaration
    {
        public EnchantFixedTargetDeclaration(
            string sourceBattleCardId,
            string declaredEnemyId,
            bool targetsPosition,
            EnemyFieldPosition position)
        {
            SourceBattleCardId = sourceBattleCardId;
            DeclaredEnemyId = declaredEnemyId;
            TargetsPosition = targetsPosition;
            Position = position;
        }

        public string SourceBattleCardId { get; }
        public string DeclaredEnemyId { get; }
        public bool TargetsPosition { get; }
        public EnemyFieldPosition Position { get; }
    }
}
