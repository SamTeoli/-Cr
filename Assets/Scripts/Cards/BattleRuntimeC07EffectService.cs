namespace HaveABreak.Cards
{
    public static class BattleRuntimeC07EffectService
    {
        public static bool TryResolve(
            BattleRuntimeState runtime,
            BattleRuntimeCardPlayResult playResult,
            string selectedBanishBattleCardId,
            out BattleRuntimeC07EffectResult result)
        {
            result = null;
            if (runtime == null || playResult == null || playResult.Card == null ||
                playResult.PlayedEvent == null ||
                runtime.EventLog.Find(playResult.PlayedEvent.EventId) !=
                playResult.PlayedEvent)
            {
                return false;
            }

            if (!CardEffectRegistrationCatalog.TryFind(
                    playResult.Card.SourceCard.CatalogCardId,
                    out CardEffectRegistration registration) ||
                registration.Handler is not IBanishSkillCardEffectHandler handler)
            {
                return false;
            }

            return handler.TryResolve(
                runtime, playResult, selectedBanishBattleCardId, out result);
        }
    }
}
