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
        Banished,
        RedrawHolding
    }

    public enum CardZoneMoveFailure
    {
        None,
        NullCard,
        InvalidIds,
        DuplicateBattleCardId,
        CardNotFound,
        DestinationFull
    }

    public enum CardDrawFailure
    {
        None,
        FirstTurnSkipped,
        HandFull,
        NoCardsAvailable,
        ZoneMoveFailed
    }

    public enum CardPlayFailure
    {
        None,
        InvalidPreview,
        CardNotFound,
        CardNotInHand,
        NotEnoughMana,
        DestinationFull,
        DuplicateBarrier,
        ZoneMoveFailed
    }

    public enum StartingHandRedrawFailure
    {
        None,
        NotAvailable,
        TooManyCards,
        DuplicateCardId,
        CardNotEligible,
        CardNotInHand,
        NotEnoughCards,
        ZoneMoveFailed
    }

    public enum BattleTurnPhase
    {
        BattleSetup,
        StartingHandRedraw,
        PlayerAction,
        PlayerActionResolving,
        EnemyTurn
    }

    public enum BattleTurnFailure
    {
        None,
        InvalidPhase,
        StartingHandRedrawFailed
    }

    public enum BattleEventType
    {
        CardPlayed,
        MonsterSummoned,
        AttackDeclared,
        DamageApplied,
        HealingApplied,
        CardMoved,
        MonsterDestroyed,
        StatusApplied
    }

    public enum EffectProcessingStage
    {
        PreModification,
        Response,
        MainEffect,
        Aftermath
    }

    public enum AftermathEffectPriority
    {
        SystemRequired,
        SourceCard,
        SourceEnchant,
        AlliedCard,
        OpposingSide,
        BarrierGlobalStatus,
        Cleanup
    }

    public enum EffectQueueFailure
    {
        None,
        InvalidEvent,
        InvalidEffect,
        EventMismatch,
        DuplicateForEvent,
        SelfRepeatBlocked
    }

    public enum EffectExecutionFailure
    {
        None,
        QueueEmpty,
        SourceEventNotFound,
        UnsupportedOperation,
        TargetNotFound,
        CombatTargetNotFound,
        InvalidValue,
        InvalidZoneTransition,
        ZoneMoveFailed
    }

    public enum StateBasedCheckFailure
    {
        None,
        ParentEventNotFound,
        ZoneMoveFailed
    }

    public enum BattleOutcome
    {
        Ongoing,
        Victory,
        Defeat
    }

    public enum BattleSettlementFailure
    {
        None,
        BattleOngoing,
        PendingEffects,
        AlreadySettled,
        InvalidRunState
    }

    public enum BattleEncounterGrade
    {
        Normal,
        Elite,
        MidBoss,
        FinalBoss
    }

    public enum BattleRewardFailure
    {
        None,
        SettlementNotComplete,
        NotVictory,
        AlreadyClaimed
    }

    public enum EnchantAttachmentFailure
    {
        None,
        NullEnchant,
        InvalidSlot,
        SlotEmpty,
        SlotOccupied,
        IncompatibleCardType,
        DuplicateNotAllowed,
        BattleLocked
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
