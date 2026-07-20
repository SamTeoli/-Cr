namespace HaveABreak.Cards
{
    public sealed class BattleRuntimeEnemyAbilityResult
    {
        internal BattleRuntimeEnemyAbilityResult(
            EnemyAbilityResolutionContext ability,
            BattleEventRecord declaredEvent,
            BattleEventRecord resolutionEvent,
            bool cancelled,
            bool returnedTrapToHand,
            string triggeredTrapBattleCardId)
        {
            Ability = ability;
            DeclaredEvent = declaredEvent;
            ResolutionEvent = resolutionEvent;
            Cancelled = cancelled;
            ReturnedTrapToHand = returnedTrapToHand;
            TriggeredTrapBattleCardId = triggeredTrapBattleCardId;
        }

        public EnemyAbilityResolutionContext Ability { get; }
        public BattleEventRecord DeclaredEvent { get; }
        public BattleEventRecord ResolutionEvent { get; }
        public bool Cancelled { get; }
        public bool ReturnedTrapToHand { get; }
        public string TriggeredTrapBattleCardId { get; }
    }
}
