using System;
using System.Collections.Generic;

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

            string catalogCardId = playResult.Card.SourceCard.CatalogCardId;
            if (string.Equals(catalogCardId, "C05", StringComparison.OrdinalIgnoreCase))
            {
                if (!C05PlatformPushResolver.TryResolve(
                        playResult.PlayedEvent,
                        playResult.Card,
                        fixedTargetEnemyId,
                        runtime.EnemyPositions,
                        runtime.EnemyMovementLocks,
                        runtime.EnemyStatuses,
                        runtime.EventLog,
                        runtime.EffectResolutions,
                        out int movedSteps,
                        out int weakenGained,
                        out int vulnerableGained))
                {
                    failure = BattleRuntimeSkillEffectFailure.ResolutionFailed;
                    return false;
                }

                result = new BattleRuntimeSkillEffectResult(
                    catalogCardId,
                    fixedTargetEnemyId,
                    movedSteps,
                    weakenGained,
                    vulnerableGained,
                    null);
                failure = BattleRuntimeSkillEffectFailure.None;
                return true;
            }

            if (string.Equals(catalogCardId, "C06", StringComparison.OrdinalIgnoreCase))
            {
                List<BattleEnemyAttackSnapshot> livingEnemies = new();
                foreach (BattleEnemyRuntimeState enemy in runtime.Enemies)
                {
                    if (enemy != null && enemy.IsAlive)
                    {
                        livingEnemies.Add(enemy.SnapshotAttack());
                    }
                }

                if (!C06EmergencyBrakeResolver.TryResolve(
                        playResult.PlayedEvent,
                        playResult.Card,
                        fixedTargetEnemyId,
                        livingEnemies,
                        runtime.EnemyStatuses,
                        runtime.EventLog,
                        runtime.EffectResolutions,
                        out string secondaryEnemyId))
                {
                    failure = BattleRuntimeSkillEffectFailure.ResolutionFailed;
                    return false;
                }

                result = new BattleRuntimeSkillEffectResult(
                    catalogCardId,
                    fixedTargetEnemyId,
                    0,
                    0,
                    0,
                    secondaryEnemyId);
                failure = BattleRuntimeSkillEffectFailure.None;
                return true;
            }

            failure = BattleRuntimeSkillEffectFailure.UnsupportedCard;
            return false;
        }
    }
}
