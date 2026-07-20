using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public sealed class BattleRuntimeEnemyAttackDeclarationResult
    {
        internal BattleRuntimeEnemyAttackDeclarationResult(
            BattleEventRecord declaredAttack,
            BattleEnemyAttackSnapshot attacker,
            BattleMonsterState targetMonster,
            int defenseGained,
            List<string> triggeredTrapBattleCardIds)
        {
            DeclaredAttack = declaredAttack;
            Attacker = attacker;
            TargetMonster = targetMonster;
            DefenseGained = defenseGained;
            TriggeredTrapBattleCardIds = triggeredTrapBattleCardIds;
        }

        public BattleEventRecord DeclaredAttack { get; }
        public BattleEnemyAttackSnapshot Attacker { get; }
        public BattleMonsterState TargetMonster { get; }
        public int DefenseGained { get; }
        public IReadOnlyList<string> TriggeredTrapBattleCardIds { get; }
    }
}
