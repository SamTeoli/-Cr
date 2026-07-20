namespace HaveABreak.Cards
{
    public sealed class BattleRuntimeBootstrapResult
    {
        internal BattleRuntimeBootstrapResult(
            BattleRuntimeState runtime,
            BattleRuntimeSessionState session,
            EncounterData encounter)
        {
            Runtime = runtime;
            Session = session;
            Encounter = encounter;
        }

        public BattleRuntimeState Runtime { get; }
        public BattleRuntimeSessionState Session { get; }
        public EncounterData Encounter { get; }
    }
}
