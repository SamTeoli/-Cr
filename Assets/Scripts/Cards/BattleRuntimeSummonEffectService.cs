using System;

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

            string catalogCardId =
                playResult.Card.SourceCard.CatalogCardId;
            if (string.Equals(catalogCardId, TestContentIds.C01, StringComparison.OrdinalIgnoreCase))
            {
                if (!targetDeclaration.HasValue)
                {
                    failure =
                        BattleRuntimeSummonEffectFailure.MissingTargetDeclaration;
                    return false;
                }

                if (!C01SleeperKeeperResolver.TryResolve(
                        playResult.SummonedEvent,
                        playResult.SummonedMonster,
                        targetDeclaration.Value,
                        runtime.EnemyPositions,
                        runtime.EnemyMovementLocks,
                        runtime.EventLog,
                        runtime.EffectResolutions,
                        out C01SleeperKeeperResult c01Result))
                {
                    failure = BattleRuntimeSummonEffectFailure.ResolutionFailed;
                    return false;
                }

                result = new BattleRuntimeSummonEffectResult(
                    catalogCardId, c01Result, default);
                failure = BattleRuntimeSummonEffectFailure.None;
                return true;
            }

            if (string.Equals(catalogCardId, TestContentIds.C02, StringComparison.OrdinalIgnoreCase))
            {
                if (!C02LanternBearerResolver.TryResolve(
                        playResult.SummonedEvent,
                        playResult.SummonedMonster,
                        runtime.NextSkillModifiers,
                        runtime.EventLog,
                        runtime.EffectResolutions,
                        out C02LanternBearerResult c02Result))
                {
                    failure = BattleRuntimeSummonEffectFailure.ResolutionFailed;
                    return false;
                }

                result = new BattleRuntimeSummonEffectResult(
                    catalogCardId, default, c02Result);
                failure = BattleRuntimeSummonEffectFailure.None;
                return true;
            }

            failure = BattleRuntimeSummonEffectFailure.UnsupportedCard;
            return false;
        }
    }
}
