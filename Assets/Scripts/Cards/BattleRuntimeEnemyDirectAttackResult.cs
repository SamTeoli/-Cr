namespace HaveABreak.Cards
{
    public sealed class BattleRuntimeEnemyDirectAttackResult
    {
        internal BattleRuntimeEnemyDirectAttackResult(
            BattleEnemyAttackSnapshot attacker,
            BattleEventRecord declaredAttack,
            int weakenReduction,
            int adjustedAttack,
            int playerVulnerableBonus,
            int playerDamage,
            BattleEventRecord playerDamageEvent,
            BattleEventRecord completedAttack)
        {
            Attacker = attacker;
            DeclaredAttack = declaredAttack;
            WeakenReduction = weakenReduction;
            AdjustedAttack = adjustedAttack;
            PlayerVulnerableBonus = playerVulnerableBonus;
            PlayerDamage = playerDamage;
            PlayerDamageEvent = playerDamageEvent;
            CompletedAttack = completedAttack;
        }

        public BattleEnemyAttackSnapshot Attacker { get; }
        public BattleEventRecord DeclaredAttack { get; }
        public int RawAttack => Attacker.Attack;
        public int WeakenReduction { get; }
        public int AdjustedAttack { get; }
        public int PlayerVulnerableBonus { get; }
        public int FinalIncomingDamage =>
            AdjustedAttack + PlayerVulnerableBonus;
        public int PlayerDamage { get; }
        public BattleEventRecord PlayerDamageEvent { get; }
        public BattleEventRecord CompletedAttack { get; }
    }
}
