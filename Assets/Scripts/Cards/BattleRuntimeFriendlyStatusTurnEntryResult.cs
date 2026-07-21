namespace HaveABreak.Cards
{
    public sealed class BattleRuntimeFriendlyStatusTurnEntryResult
    {
        internal BattleRuntimeFriendlyStatusTurnEntryResult(
            string targetId,
            bool targetsPlayer,
            int baseInjuryDamage,
            int vulnerableBonus,
            int damageApplied,
            int healthBefore,
            int healthAfter,
            int injuryBefore,
            int injuryAfter,
            int bindBefore,
            int bindAfter,
            int stunBefore,
            int stunAfter,
            int weakenBefore,
            int weakenAfter,
            BattleEventRecord injuryDamageEvent)
        {
            TargetId = targetId;
            TargetsPlayer = targetsPlayer;
            BaseInjuryDamage = baseInjuryDamage;
            VulnerableBonus = vulnerableBonus;
            DamageApplied = damageApplied;
            HealthBefore = healthBefore;
            HealthAfter = healthAfter;
            InjuryBefore = injuryBefore;
            InjuryAfter = injuryAfter;
            BindBefore = bindBefore;
            BindAfter = bindAfter;
            StunBefore = stunBefore;
            StunAfter = stunAfter;
            WeakenBefore = weakenBefore;
            WeakenAfter = weakenAfter;
            InjuryDamageEvent = injuryDamageEvent;
        }

        public string TargetId { get; }
        public bool TargetsPlayer { get; }
        public int BaseInjuryDamage { get; }
        public int VulnerableBonus { get; }
        public int DamageApplied { get; }
        public int HealthBefore { get; }
        public int HealthAfter { get; }
        public int InjuryBefore { get; }
        public int InjuryAfter { get; }
        public int BindBefore { get; }
        public int BindAfter { get; }
        public int StunBefore { get; }
        public int StunAfter { get; }
        public int WeakenBefore { get; }
        public int WeakenAfter { get; }
        public BattleEventRecord InjuryDamageEvent { get; }
    }
}
