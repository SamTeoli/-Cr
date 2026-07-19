using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleEffectImpactRecord
    {
        [SerializeField] private string completedEventId;
        [SerializeField] private string sourceBattleCardId;
        [SerializeField] private List<string> affectedEnemyIds = new();

        internal BattleEffectImpactRecord(
            string completedEventId,
            string sourceBattleCardId,
            IEnumerable<string> affectedEnemyIds)
        {
            this.completedEventId = completedEventId;
            this.sourceBattleCardId = sourceBattleCardId;
            this.affectedEnemyIds.AddRange(affectedEnemyIds);
        }

        public string CompletedEventId => completedEventId;
        public string SourceBattleCardId => sourceBattleCardId;
        public IReadOnlyList<string> AffectedEnemyIds => affectedEnemyIds;
    }
}
