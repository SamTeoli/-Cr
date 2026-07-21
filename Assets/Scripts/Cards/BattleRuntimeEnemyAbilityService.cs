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
                ability.IsNormalAttack ||
                !HasValidStatusEffect(ability))
            {
                failure = BattleRuntimeEnemyAbilityFailure.InvalidAbility;
                return false;
            }

            BattleEnemyRuntimeState sourceEnemy =
                runtime.FindEnemy(ability.SourceEnemyId);
            BattleEnemyStatusState sourceStatus =
                runtime.EnemyStatuses.Find(ability.SourceEnemyId);
            if (sourceEnemy == null || !sourceEnemy.IsAlive ||
                sourceStatus == null)
            {
                failure = BattleRuntimeEnemyAbilityFailure.InvalidSourceEnemy;
                return false;
            }

            if (!sourceStatus.CanUseAbility)
            {
                failure =
                    BattleRuntimeEnemyAbilityFailure.ActionBlockedByStatus;
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
                        TestContentIds.C10,
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
            List<BattleEventRecord> statusApplicationEvents = new();
            int totalStatusApplied = 0;
            if (!cancelled && ability.HasStatusEffect &&
                !TryApplyStatusEffect(
                    runtime,
                    ability,
                    declaredEvent,
                    statusApplicationEvents,
                    out totalStatusApplied))
            {
                failure = BattleRuntimeEnemyAbilityFailure.InvalidAbility;
                return false;
            }

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
                triggeredTrapBattleCardId,
                statusApplicationEvents,
                totalStatusApplied);
            failure = BattleRuntimeEnemyAbilityFailure.None;
            return true;
        }

        private static bool HasValidStatusEffect(
            EnemyAbilityResolutionContext ability)
        {
            if (!Enum.IsDefined(typeof(StatusKeyword), ability.StatusKeyword))
            {
                return false;
            }

            return ability.StatusKeyword == StatusKeyword.None
                ? ability.StatusAmount == 0
                : ability.StatusAmount > 0;
        }

        private static bool TryApplyStatusEffect(
            BattleRuntimeState runtime,
            EnemyAbilityResolutionContext ability,
            BattleEventRecord declaredEvent,
            List<BattleEventRecord> applicationEvents,
            out int totalApplied)
        {
            totalApplied = 0;
            if (ability.AffectsFriendlySide)
            {
                return ability.IsAreaAbility
                    ? TryApplyToFriendlyArea(
                        runtime,
                        ability,
                        declaredEvent,
                        applicationEvents,
                        ref totalApplied)
                    : TryApplyToFriendlyTarget(
                        runtime,
                        ability,
                        declaredEvent,
                        applicationEvents,
                        ref totalApplied);
            }

            if (!ability.IsAreaAbility)
            {
                BattleEnemyStatusState source =
                    runtime.EnemyStatuses.Find(ability.SourceEnemyId);
                if (source == null)
                {
                    return false;
                }

                Apply(
                    runtime,
                    ability,
                    declaredEvent,
                    ability.SourceEnemyId,
                    source,
                    applicationEvents,
                    ref totalApplied);
                return true;
            }

            foreach (EnemyFieldPosition position in
                     Enum.GetValues(typeof(EnemyFieldPosition)))
            {
                string enemyId = runtime.EnemyPositions.GetOccupant(position);
                BattleEnemyRuntimeState enemy = runtime.FindEnemy(enemyId);
                BattleEnemyStatusState status =
                    runtime.EnemyStatuses.Find(enemyId);
                if (enemy == null || !enemy.IsAlive || status == null ||
                    !runtime.LivingEnemies.Contains(enemyId))
                {
                    continue;
                }

                Apply(
                    runtime,
                    ability,
                    declaredEvent,
                    enemyId,
                    status,
                    applicationEvents,
                    ref totalApplied);
            }

            return true;
        }

        private static bool TryApplyToFriendlyTarget(
            BattleRuntimeState runtime,
            EnemyAbilityResolutionContext ability,
            BattleEventRecord declaredEvent,
            List<BattleEventRecord> applicationEvents,
            ref int totalApplied)
        {
            if (!BattleRuntimeEnemyAttackTargetService.TrySelect(
                    runtime,
                    ability.SourceEnemyId,
                    ability.TargetTieBreakerValue,
                    out BattleRuntimeEnemyAttackTargetResult target,
                    out _))
            {
                return false;
            }

            if (target.TargetType ==
                BattleRuntimeEnemyAttackTargetType.Player)
            {
                Apply(
                    runtime,
                    ability,
                    declaredEvent,
                    BattlePlayerState.PlayerTargetId,
                    runtime.Player.Status,
                    applicationEvents,
                    ref totalApplied);
                return true;
            }

            if (target.TargetMonster == null)
            {
                return false;
            }

            Apply(
                runtime,
                ability,
                declaredEvent,
                target.TargetMonster.BattleCardId,
                target.TargetMonster.Status,
                applicationEvents,
                ref totalApplied);
            return true;
        }

        private static bool TryApplyToFriendlyArea(
            BattleRuntimeState runtime,
            EnemyAbilityResolutionContext ability,
            BattleEventRecord declaredEvent,
            List<BattleEventRecord> applicationEvents,
            ref int totalApplied)
        {
            bool foundMonster = false;
            foreach (PlayerMonsterFieldPosition position in
                     Enum.GetValues(typeof(PlayerMonsterFieldPosition)))
            {
                string battleCardId =
                    runtime.PlayerMonsterPositions.GetOccupant(position);
                BattleMonsterState monster =
                    runtime.Monsters.Find(battleCardId);
                if (monster == null ||
                    monster.Card.Zone != CardZone.MonsterField ||
                    monster.IsDestructionCandidate)
                {
                    continue;
                }

                foundMonster = true;
                Apply(
                    runtime,
                    ability,
                    declaredEvent,
                    monster.BattleCardId,
                    monster.Status,
                    applicationEvents,
                    ref totalApplied);
            }

            if (!foundMonster)
            {
                Apply(
                    runtime,
                    ability,
                    declaredEvent,
                    BattlePlayerState.PlayerTargetId,
                    runtime.Player.Status,
                    applicationEvents,
                    ref totalApplied);
            }

            return true;
        }

        private static void Apply(
            BattleRuntimeState runtime,
            EnemyAbilityResolutionContext ability,
            BattleEventRecord declaredEvent,
            string targetId,
            BattleCommonStatusState status,
            List<BattleEventRecord> applicationEvents,
            ref int totalApplied)
        {
            int before = status.GetAmount(ability.StatusKeyword);
            int applied = status.Apply(
                ability.StatusKeyword,
                ability.StatusAmount);
            if (applied <= 0)
            {
                return;
            }

            totalApplied += applied;
            applicationEvents.Add(runtime.EventLog.Record(
                BattleEventType.StatusApplied,
                "EnemyAbilityStatusApplied",
                ability.SourceEnemyId,
                ability.SourceEnemyId,
                targetId,
                parentEventId: declaredEvent.EventId,
                sourceEffectId: ability.AbilityId,
                beforeValue: before,
                afterValue: status.GetAmount(ability.StatusKeyword)));
        }

        private static void Apply(
            BattleRuntimeState runtime,
            EnemyAbilityResolutionContext ability,
            BattleEventRecord declaredEvent,
            string targetId,
            BattleEnemyStatusState status,
            List<BattleEventRecord> applicationEvents,
            ref int totalApplied)
        {
            int before = status.GetAmount(ability.StatusKeyword);
            int applied = status.Apply(
                ability.StatusKeyword,
                ability.StatusAmount);
            if (applied <= 0)
            {
                return;
            }

            totalApplied += applied;
            applicationEvents.Add(runtime.EventLog.Record(
                BattleEventType.StatusApplied,
                "EnemyAbilityStatusApplied",
                ability.SourceEnemyId,
                ability.SourceEnemyId,
                targetId,
                parentEventId: declaredEvent.EventId,
                sourceEffectId: ability.AbilityId,
                beforeValue: before,
                afterValue: status.GetAmount(ability.StatusKeyword)));
        }
    }
}
