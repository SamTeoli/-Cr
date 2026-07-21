using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleEnemyStatusState
    {
        [SerializeField] private string enemyId;
        [SerializeField] private int weaken;
        [SerializeField] private int vulnerable;
        [SerializeField] private int bind;
        [SerializeField] private bool bindImmune;

        public BattleEnemyStatusState(string enemyId)
        {
            if (string.IsNullOrWhiteSpace(enemyId))
            {
                throw new ArgumentException("Enemy ID is required.", nameof(enemyId));
            }

            this.enemyId = enemyId.Trim();
        }

        public string EnemyId => enemyId;
        public int Weaken => weaken;
        public int Vulnerable => vulnerable;
        public int Bind => bind;
        public bool BindImmune => bindImmune;

        public int ApplyWeaken(int amount)
        {
            int applied = Mathf.Max(0, amount);
            weaken += applied;
            return applied;
        }

        public int ApplyVulnerable(int amount)
        {
            int applied = Mathf.Max(0, amount);
            vulnerable += applied;
            return applied;
        }

        public int ConsumeVulnerable()
        {
            int consumed = vulnerable;
            vulnerable = 0;
            return consumed;
        }

        public void SetBindImmune(bool immune)
        {
            bindImmune = immune;
        }

        public int ApplyBind(int amount)
        {
            if (bindImmune)
            {
                return 0;
            }

            int applied = Mathf.Max(0, amount);
            bind += applied;
            return applied;
        }
    }
}

