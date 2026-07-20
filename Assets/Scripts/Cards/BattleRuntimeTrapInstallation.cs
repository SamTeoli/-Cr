using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleRuntimeTrapInstallation
    {
        [SerializeField] private BattleCardInstance sourceTrap;
        [SerializeField] private string playedEventId;
        [SerializeField] private int eligibleEnemyTurn;

        private BattleRuntimeTrapInstallation()
        {
        }

        internal BattleRuntimeTrapInstallation(
            BattleCardInstance sourceTrap,
            string playedEventId,
            int eligibleEnemyTurn)
        {
            this.sourceTrap = sourceTrap;
            this.playedEventId = playedEventId;
            this.eligibleEnemyTurn = eligibleEnemyTurn;
        }

        public BattleCardInstance SourceTrap => sourceTrap;
        public string PlayedEventId => playedEventId;
        public int EligibleEnemyTurn => eligibleEnemyTurn;
    }
}
