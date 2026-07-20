namespace HaveABreak.Cards
{
    public enum BattleRuntimeEnemyTurnFailure
    {
        None,
        InvalidRuntime,
        InvalidTurnPhase,
        InvalidCommands,
        InvalidAction,
        MoveFailed,
        AttackDeclarationFailed,
        AttackDamageFailed,
        AbilityFailed,
        PlayerTurnStartFailed
    }
}
