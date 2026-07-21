using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public sealed class BattleRuntimeEnemyAttackResolutionResult
    {
        internal BattleRuntimeEnemyAttackResolutionResult(
            BattleRuntimeEnemyAttackDeclarationResult declaration,
            int weakenReduction,
            int adjustedAttack,
            int monsterVulnerableBonus,
            int defenseConsumed,
            int monsterDamage,
            int overflowDamage,
            int playerVulnerableBonus,
            int playerDamage,
            BattleEventRecord defenseConsumedEvent,
            BattleEventRecord monsterDamageEvent,
            BattleEventRecord playerDamageEvent,
            List<BattleEventRecord> destructionEvents,
            BattleEventRecord completedAttack)
        {
            Declaration = declaration;
            WeakenReduction = weakenReduction;
            AdjustedAttack = adjustedAttack;
            MonsterVulnerableBonus = monsterVulnerableBonus;
            DefenseConsumed = defenseConsumed;
            MonsterDamage = monsterDamage;
            OverflowDamage = overflowDamage;
            PlayerVulnerableBonus = playerVulnerableBonus;
            PlayerDamage = playerDamage;
            DefenseConsumedEvent = defenseConsumedEvent;
            MonsterDamageEvent = monsterDamageEvent;
            PlayerDamageEvent = playerDamageEvent;
            DestructionEvents = destructionEvents;
            CompletedAttack = completedAttack;
        }

        public BattleRuntimeEnemyAttackDeclarationResult Declaration { get; }
        public int RawAttack => Declaration.Attacker.Attack;
        public int WeakenReduction { get; }
        public int AdjustedAttack { get; }
        public int MonsterVulnerableBonus { get; }
        public int DamageBeforeDefense =>
            AdjustedAttack + MonsterVulnerableBonus;
        public int DefenseConsumed { get; }
        public int MonsterDamage { get; }
        public int OverflowDamage { get; }
        public int PlayerVulnerableBonus { get; }
        public int PlayerDamage { get; }
        public BattleEventRecord DefenseConsumedEvent { get; }
        public BattleEventRecord MonsterDamageEvent { get; }
        public BattleEventRecord PlayerDamageEvent { get; }
        public IReadOnlyList<BattleEventRecord> DestructionEvents { get; }
        public BattleEventRecord CompletedAttack { get; }
    }
}
