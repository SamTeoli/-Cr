using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleCommonStatusState
    {
        [SerializeField] private int injury;
        [SerializeField] private int weaken;
        [SerializeField] private int vulnerable;
        [SerializeField] private int bind;
        [SerializeField] private int stun;

        public int Injury => injury;
        public int Weaken => weaken;
        public int Vulnerable => vulnerable;
        public int Bind => bind;
        public int Stun => stun;
        public bool CanAttack => bind <= 0 && stun <= 0;
        public bool CanUseAbility => stun <= 0;

        public int GetAmount(StatusKeyword keyword)
        {
            return keyword switch
            {
                StatusKeyword.Injury => injury,
                StatusKeyword.Bind => bind,
                StatusKeyword.Stun => stun,
                StatusKeyword.Weaken => weaken,
                StatusKeyword.Vulnerable => vulnerable,
                _ => 0
            };
        }

        public int Apply(StatusKeyword keyword, int amount)
        {
            return keyword switch
            {
                StatusKeyword.Injury => ApplyInjury(amount),
                StatusKeyword.Bind => ApplyBind(amount),
                StatusKeyword.Stun => ApplyStun(amount),
                StatusKeyword.Weaken => ApplyWeaken(amount),
                StatusKeyword.Vulnerable => ApplyVulnerable(amount),
                _ => 0
            };
        }

        public int ApplyInjury(int amount)
        {
            int applied = Mathf.Max(0, amount);
            injury += applied;
            return applied;
        }

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

        public int ApplyBind(int amount)
        {
            int applied = Mathf.Max(0, amount);
            bind += applied;
            return applied;
        }

        public int ApplyStun(int amount)
        {
            int applied = Mathf.Max(0, amount);
            stun += applied;
            return applied;
        }

        public int ResolveInjuryAtTurnEnd()
        {
            int damage = injury;
            injury = Mathf.Max(0, injury - 1);
            return damage;
        }

        public int ReduceBindAtTurnEnd()
        {
            int before = bind;
            bind = Mathf.Max(0, bind - 1);
            return before - bind;
        }

        public int ClearStunAtTurnEnd()
        {
            int cleared = stun;
            stun = 0;
            return cleared;
        }

        public int ReduceWeakenAtTurnEnd()
        {
            int before = weaken;
            weaken = Mathf.Max(0, weaken - 1);
            return before - weaken;
        }

        public int ClearAll()
        {
            int cleared = injury + weaken + vulnerable + bind + stun;
            injury = 0;
            weaken = 0;
            vulnerable = 0;
            bind = 0;
            stun = 0;
            return cleared;
        }
    }
}
