namespace HaveABreak.Cards
{
    public static class BattleRuntimeSkillEffectService
    {
        public static bool TryResolve(
            BattleRuntimeState runtime,
            BattleRuntimeCardPlayResult playResult,
            string fixedTargetEnemyId,
            out BattleRuntimeSkillEffectResult result,
            out BattleRuntimeSkillEffectFailure failure)
        {
            result = null;
            if (runtime == null || playResult == null)
            {
                failure = BattleRuntimeSkillEffectFailure.InvalidRuntime;
                return false;
            }

            if (playResult.Card == null ||
                playResult.Card.SourceCard.CardType != CardType.Skill ||
                playResult.PlayedEvent == null ||
                runtime.EventLog.Find(playResult.PlayedEvent.EventId) !=
                playResult.PlayedEvent)
            {
                failure = BattleRuntimeSkillEffectFailure.NotSkillPlay;
                return false;
            }

            if (string.IsNullOrWhiteSpace(fixedTargetEnemyId) ||
                runtime.FindEnemy(fixedTargetEnemyId) == null)
            {
                failure = BattleRuntimeSkillEffectFailure.MissingTarget;
                return false;
            }

            if (!CardEffectRegistrationCatalog.TryFind(
                    playResult.Card.SourceCard.CatalogCardId,
                    out CardEffectRegistration registration) ||
                registration.Handler is not ITargetedSkillCardEffectHandler handler)
            {
                failure = BattleRuntimeSkillEffectFailure.UnsupportedCard;
                return false;
            }

            return handler.TryResolve(
                runtime, playResult, fixedTargetEnemyId, out result, out failure);
        }
    }
}
