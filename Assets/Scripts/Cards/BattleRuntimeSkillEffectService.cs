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

            if (!CardEffectRegistrationCatalog.TryFind(
                    playResult.Card.SourceCard.CatalogCardId,
                    out CardEffectRegistration registration) ||
                registration.Handler is not ITargetedSkillCardEffectHandler handler)
            {
                failure = BattleRuntimeSkillEffectFailure.UnsupportedCard;
                return false;
            }

            if (!EffectTargetResolver.TryResolveSingleTarget(
                    runtime,
                    registration.ResolveTargetSpec(
                        playResult.Card.SourceCard),
                    fixedTargetEnemyId,
                    out EffectTargetCandidate target))
            {
                failure = BattleRuntimeSkillEffectFailure.MissingTarget;
                return false;
            }

            return handler.TryResolve(
                runtime, playResult, target.TargetId, out result, out failure);
        }
    }
}
