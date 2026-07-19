namespace HaveABreak.Cards
{
    public readonly struct ResolvedCardData
    {
        public ResolvedCardData(
            CardData source,
            int requestedLevel,
            int level,
            int manaCost,
            int attack,
            int health,
            string rulesText)
        {
            Source = source;
            RequestedLevel = requestedLevel;
            Level = level;
            ManaCost = manaCost;
            Attack = attack;
            Health = health;
            RulesText = rulesText;
        }

        public CardData Source { get; }
        public int RequestedLevel { get; }
        public int Level { get; }
        public int ManaCost { get; }
        public int Attack { get; }
        public int Health { get; }
        public string RulesText { get; }
        public bool WasLevelClamped => RequestedLevel != Level;
    }
}
