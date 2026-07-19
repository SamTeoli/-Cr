using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleOutcomeEvaluator
    {
        [SerializeField] private BattlePlayerState player;
        [SerializeField] private BattleEnemyTracker enemies;

        private BattleOutcomeEvaluator()
        {
        }

        public BattleOutcomeEvaluator(BattlePlayerState player, BattleEnemyTracker enemies)
        {
            this.player = player ?? throw new ArgumentNullException(nameof(player));
            this.enemies = enemies ?? throw new ArgumentNullException(nameof(enemies));
        }

        public BattleOutcome Evaluate()
        {
            if (player.IsDefeated)
            {
                return BattleOutcome.Defeat;
            }

            return enemies.Count == 0 ? BattleOutcome.Victory : BattleOutcome.Ongoing;
        }
    }
}
