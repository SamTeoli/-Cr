namespace HaveABreak.Cards
{
    public sealed class BattleRuntimeSummonEffectResult
    {
        internal BattleRuntimeSummonEffectResult(
            string catalogCardId,
            C01SleeperKeeperResult c01Result,
            C02LanternBearerResult c02Result)
        {
            CatalogCardId = catalogCardId;
            C01Result = c01Result;
            C02Result = c02Result;
        }

        public string CatalogCardId { get; }
        public C01SleeperKeeperResult C01Result { get; }
        public C02LanternBearerResult C02Result { get; }
    }
}
