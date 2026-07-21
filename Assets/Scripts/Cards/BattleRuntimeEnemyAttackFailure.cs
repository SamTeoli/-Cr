namespace HaveABreak.Cards
{
    public enum BattleRuntimeEnemyAttackFailure
    {
        None,
        InvalidRuntime,
        InvalidTurnPhase,
        InvalidAttacker,
        InvalidTarget,
        InvalidDeclaration,
        AlreadyResolved,
        StatusStateNotFound,
        ActionBlockedByStatus,
        StateCheckFailed,
        CompletionFailed
    }
}
