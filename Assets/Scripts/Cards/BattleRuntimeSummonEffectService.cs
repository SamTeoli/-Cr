namespace HaveABreak.Cards
{
    public static class BattleRuntimeSummonEffectService
    {
        public static bool TryResolve(
            BattleRuntimeState runtime,
            BattleRuntimeCardPlayResult playResult,
            EnchantFixedTargetDeclaration? targetDeclaration,
            out BattleRuntimeSummonEffectResult result,
            out BattleRuntimeSummonEffectFailure failure)
        {
            result = null;
            if (runtime == null || playResult == null)
            {
                failure = BattleRuntimeSummonEffectFailure.InvalidRuntime;
                return false;
            }

            if (!playResult.MonsterWasSummoned ||
                playResult.SummonedMonster == null ||
                playResult.SummonedEvent == null ||
                runtime.EventLog.Find(playResult.SummonedEvent.EventId) !=
                playResult.SummonedEvent)
            {
                failure = BattleRuntimeSummonEffectFailure.NotMonsterSummon;
                return false;
            }

            if (!CardEffectRegistrationCatalog.TryFind(
                    playResult.Card.SourceCard.CatalogCardId,
                    out CardEffectRegistration registration) ||
                registration.Handler is not ISummonCardEffectHandler handler)
            {
                failure = BattleRuntimeSummonEffectFailure.UnsupportedCard;
                return false;
            }

            if (playResult.Card.SourceCard.HasEnchantCompatibilityTag(
                    EnchantCompatibilityTag.FixedSingleEnemyTarget))
            {
                string resolvedTargetId = targetDeclaration.HasValue
                    ? EnchantFixedTargetResolver.Resolve(
                        targetDeclaration.Value,
                        runtime.EnemyPositions)
                    : null;
                if (!targetDeclaration.HasValue ||
                    !System.String.Equals(
                        targetDeclaration.Value.SourceBattleCardId,
                        playResult.Card.Ids.BattleCardId,
                        System.StringComparison.OrdinalIgnoreCase) ||
                    !EffectTargetResolver.TryResolveSingleTarget(
                        runtime,
                        registration.ResolveTargetSpec(
                            playResult.Card.SourceCard),
                        resolvedTargetId,
                        out _))
                {
                    failure = BattleRuntimeSummonEffectFailure
                        .MissingTargetDeclaration;
                    return false;
                }
            }

            return handler.TryResolve(
                runtime, playResult, targetDeclaration, out result, out failure);
        }
    }
}
