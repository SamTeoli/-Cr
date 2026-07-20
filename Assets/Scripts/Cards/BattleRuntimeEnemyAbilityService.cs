using System;
using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public static class BattleRuntimeEnemyAbilityService
    {
        public static bool TryResolve(
            BattleRuntimeState runtime,
            EnemyAbilityResolutionContext ability,
            out BattleRuntimeEnemyAbilityResult result,
            out BattleRuntimeEnemyAbilityFailure failure)
        {
            result = null;
            if (runtime == null)
            {
                failure = BattleRuntimeEnemyAbilityFailure.InvalidRuntime;
                return false;
            }

            if (runtime.Turn.Phase != BattleTurnPhase.EnemyTurn)
            {
                failure = BattleRuntimeEnemyAbilityFailure.InvalidTurnPhase;
                return false;
            }

            if (string.IsNullOrWhiteSpace(ability.AbilityId) ||
                string.IsNullOrWhiteSpace(ability.SourceEnemyId) ||
                ability.IsNormalAttack)
            {
                failure = BattleRuntimeEnemyAbilityFailure.InvalidAbility;
                return false;
            }

            BattleEnemyRuntimeState sourceEnemy =
                runtime.FindEnemy(ability.SourceEnemyId);
            if (sourceEnemy == null || !sourceEnemy.IsAlive ||
                runtime.EnemyStatuses.Find(ability.SourceEnemyId) == null)
            {
                failure = BattleRuntimeEnemyAbilityFailure.InvalidSourceEnemy;
                return false;
            }

            BattleEventRecord declaredEvent = runtime.EventLog.Record(
                BattleEventType.EnemyAbilityDeclared,
                "EnemyAbilityDeclared",
                ability.SourceEnemyId,
                ability.SourceEnemyId,
                ability.AbilityId,
                beforeValue: 0,
                afterValue: 1);

            runtime.TrapInstallations.PruneInactive();
            bool cancelled = false;
            bool returnedToHand = false;
            string triggeredTrapBattleCardId = null;
            IReadOnlyList<BattleRuntimeTrapInstallation> installations =
                runtime.TrapInstallations.Installations;
            for (int i = installations.Count - 1; i >= 0; i--)
            {
                BattleRuntimeTrapInstallation installation = installations[i];
                if (installation?.SourceTrap?.SourceCard == null ||
                    !string.Equals(
                        installation.SourceTrap.SourceCard.CatalogCardId,
                        "C10",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!BattleRuntimeTrapEffectService.TryCancelEnemyAbility(
                        runtime,
                        installation,
                        declaredEvent,
                        ability,
                        out bool trapCancelled,
                        out bool trapReturnedToHand) ||
                    !trapCancelled)
                {
                    continue;
                }

                cancelled = true;
                returnedToHand = trapReturnedToHand;
                triggeredTrapBattleCardId =
                    installation.SourceTrap.Ids.BattleCardId;
                break;
            }

            runtime.TrapInstallations.PruneInactive();
            BattleEventRecord resolutionEvent = runtime.EventLog.Record(
                cancelled
                    ? BattleEventType.EnemyAbilityCancelled
                    : BattleEventType.EnemyAbilityCompleted,
                cancelled
                    ? "EnemyAbilityCancelledByC10"
                    : "EnemyAbilityCompleted",
                cancelled ? triggeredTrapBattleCardId : ability.SourceEnemyId,
                ability.SourceEnemyId,
                ability.AbilityId,
                parentEventId: declaredEvent.EventId,
                beforeValue: 1,
                afterValue: cancelled ? 0 : 1);

            result = new BattleRuntimeEnemyAbilityResult(
                ability,
                declaredEvent,
                resolutionEvent,
                cancelled,
                returnedToHand,
                triggeredTrapBattleCardId);
            failure = BattleRuntimeEnemyAbilityFailure.None;
            return true;
        }
    }
}
