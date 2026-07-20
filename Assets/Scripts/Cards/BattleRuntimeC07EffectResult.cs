namespace HaveABreak.Cards
{
    public sealed class BattleRuntimeC07EffectResult
    {
        internal BattleRuntimeC07EffectResult(
            int drawnCount,
            bool banished,
            int defendedMonsterCount)
        {
            DrawnCount = drawnCount;
            Banished = banished;
            DefendedMonsterCount = defendedMonsterCount;
        }

        public int DrawnCount { get; }
        public bool Banished { get; }
        public int DefendedMonsterCount { get; }
    }
}
