namespace HaveABreak.Cards
{
    public readonly struct C02LanternBearerResult
    {
        public C02LanternBearerResult(int costReduction, int firstNumericEffectBonus, int defenseGained)
        {
            CostReduction = costReduction;
            FirstNumericEffectBonus = firstNumericEffectBonus;
            DefenseGained = defenseGained;
        }

        public int CostReduction { get; }
        public int FirstNumericEffectBonus { get; }
        public int DefenseGained { get; }
    }
}
