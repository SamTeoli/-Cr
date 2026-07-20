using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class EncounterEnemySlot
    {
        [SerializeField] private string enemyInstanceId;
        [SerializeField] private EnemyDefinitionData enemy;
        [SerializeField] private EnemyFieldPosition position;

        private EncounterEnemySlot()
        {
        }

        public EncounterEnemySlot(
            string enemyInstanceId,
            EnemyDefinitionData enemy,
            EnemyFieldPosition position)
        {
            this.enemyInstanceId = enemyInstanceId?.Trim();
            this.enemy = enemy;
            this.position = position;
        }

        public string EnemyInstanceId => enemyInstanceId;
        public EnemyDefinitionData Enemy => enemy;
        public EnemyFieldPosition Position => position;
    }
}
