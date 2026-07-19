namespace HaveABreak.Cards
{
    public enum CardType
    {
        Monster,
        Skill,
        Trap,
        Barrier
    }

    public enum CardRarity
    {
        Common,
        Rare,
        Legendary
    }

    public enum CardZone
    {
        DrawPile,
        Hand,
        MonsterField,
        SkillField,
        Graveyard,
        Banished
    }

    public enum EffectTrigger
    {
        None,
        OnUse,
        OnSummoned,
        OnDestroyed,
        OnAttack,
        OnAttacked,
        TurnStart,
        TurnEnd,
        EnemyTurnStart,
        Persistent
    }

    public enum EffectTarget
    {
        Self,
        Player,
        FriendlyMonster,
        EnemyMonster,
        AllFriendlyMonsters,
        AllEnemyMonsters,
        Hand,
        Deck,
        Field
    }

    public enum EffectOperation
    {
        None,
        Damage,
        Heal,
        Draw,
        GainMana,
        ModifyAttack,
        ModifyHealth,
        ApplyStatus,
        Move,
        CreateCard,
        Custom
    }

    public enum StatusKeyword
    {
        None,
        Injury,
        Bind,
        Stun,
        Weaken,
        Vulnerable
    }
}
