namespace HaveABreak.Cards
{
    public readonly struct EnemyPositionMoveRecord
    {
        public EnemyPositionMoveRecord(
            string enemyId,
            EnemyFieldPosition from,
            EnemyFieldPosition to,
            bool pushed)
        {
            EnemyId = enemyId;
            From = from;
            To = to;
            Pushed = pushed;
        }

        public string EnemyId { get; }
        public EnemyFieldPosition From { get; }
        public EnemyFieldPosition To { get; }
        public bool Pushed { get; }
    }
}
