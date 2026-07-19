namespace HaveABreak.Cards
{
    public readonly struct C03SeatRepairerResult
    {
        public C03SeatRepairerResult(bool attackedThisTurn, int defenseGained, int counterGained)
        {
            AttackedThisTurn = attackedThisTurn;
            DefenseGained = defenseGained;
            CounterGained = counterGained;
        }

        public bool AttackedThisTurn { get; }
        public int DefenseGained { get; }
        public int CounterGained { get; }
    }
}
