namespace HaveABreak.Cards
{
    public enum BattleRuntimeEnemyDirectAttackFailure
    {
        None,
        InvalidRuntime,
        InvalidTurnPhase,
        InvalidAttacker,
        StatusStateNotFound,
        ActionBlockedByStatus,
        CompletionFailed
    }
}
