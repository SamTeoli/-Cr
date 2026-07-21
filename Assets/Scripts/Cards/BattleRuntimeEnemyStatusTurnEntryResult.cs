namespace HaveABreak.Cards
{
    public sealed class BattleRuntimeEnemyStatusTurnEntryResult
    {
        internal BattleRuntimeEnemyStatusTurnEntryResult(
            string enemyId,
            int injuryDamage,
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
            EnemyId = enemyId;
            InjuryDamage = injuryDamage;
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

        public string EnemyId { get; }
        public int InjuryDamage { get; }
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
