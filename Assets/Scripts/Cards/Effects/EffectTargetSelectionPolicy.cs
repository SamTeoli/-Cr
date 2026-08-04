using System;

namespace HaveABreak.Cards
{
    public static class EffectTargetSelectionPolicy
    {
        public static bool TrySelectLowestHealthAllyMonster(
            BattleRuntimeState runtime,
            EffectTargetSpec spec,
            out EffectTargetCandidate selected)
        {
            selected = null;
            if (runtime == null ||
                spec == null ||
                spec.Kind != EffectTargetKind.Monster ||
                spec.Side != EffectTargetSide.Ally)
            {
                return false;
            }

            BattleMonsterState selectedMonster = null;
            foreach (EffectTargetCandidate candidate in
                     EffectTargetResolver.GetLegalTargets(runtime, spec))
            {
                BattleMonsterState monster =
                    runtime.Monsters.Find(candidate.TargetId);
                if (monster == null ||
                    selectedMonster != null &&
                    (monster.CurrentHealth >
                         selectedMonster.CurrentHealth ||
                     monster.CurrentHealth ==
                         selectedMonster.CurrentHealth &&
                     string.Compare(
                         monster.BattleCardId,
                         selectedMonster.BattleCardId,
                         StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    continue;
                }

                selected = candidate;
                selectedMonster = monster;
            }

            return selected != null;
        }
    }
}
