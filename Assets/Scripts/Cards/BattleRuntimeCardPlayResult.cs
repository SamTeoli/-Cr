namespace HaveABreak.Cards
{
    public sealed class BattleRuntimeCardPlayResult
    {
        internal BattleRuntimeCardPlayResult(
            BattleCardInstance card,
            CardPlayPreview preview,
            BattleEventRecord playedEvent,
            BattleEventRecord summonedEvent,
            BattleMonsterState summonedMonster)
        {
            Card = card;
            Preview = preview;
            PlayedEvent = playedEvent;
            SummonedEvent = summonedEvent;
            SummonedMonster = summonedMonster;
        }

        public BattleCardInstance Card { get; }
        public CardPlayPreview Preview { get; }
        public BattleEventRecord PlayedEvent { get; }
        public BattleEventRecord SummonedEvent { get; }
        public BattleMonsterState SummonedMonster { get; }
        public bool MonsterWasSummoned => SummonedEvent != null;
    }
}
