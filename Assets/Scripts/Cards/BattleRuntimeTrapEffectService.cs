using System;

namespace HaveABreak.Cards
{
    public static class BattleRuntimeTrapEffectService
    {
        public static bool TryRegisterInstallation(
            BattleRuntimeState runtime,
            BattleRuntimeCardPlayResult playResult,
            out BattleRuntimeTrapInstallation installation)
        {
            installation = null;
            if (runtime == null || playResult == null || playResult.Card == null ||
                playResult.PlayedEvent == null ||
                playResult.Card.SourceCard.CardType != CardType.Trap ||
                playResult.Card.Zone != CardZone.SkillField ||
                runtime.EventLog.Find(playResult.PlayedEvent.EventId) !=
                playResult.PlayedEvent ||
                runtime.Turn.PlayerTurnNumber < 1)
            {
                return false;
            }

            BattleRuntimeTrapInstallation candidate =
                new BattleRuntimeTrapInstallation(
                    playResult.Card,
                    playResult.PlayedEvent.EventId,
                    runtime.Turn.PlayerTurnNumber);
            if (!runtime.TrapInstallations.TryAdd(candidate))
            {
                return false;
            }

            installation = candidate;
            return true;
        }

        public static bool TryReplaceEnemyMove(
            BattleRuntimeState runtime,
            BattleRuntimeTrapInstallation installation,
            BattleEventRecord moveAttemptEvent,
            int requestedSteps,
            string movingEnemyId,
            out int replacementSteps)
        {
            replacementSteps = requestedSteps;
            if (!TryGetHandler(runtime, installation, out IEnemyMoveTrapCardEffectHandler handler))
            {
                return false;
            }

            return handler.TryResolve(runtime, installation, moveAttemptEvent,
                requestedSteps, movingEnemyId, out replacementSteps);
        }

        public static bool TryResolveIncomingAttack(
            BattleRuntimeState runtime,
            BattleRuntimeTrapInstallation installation,
            BattleEventRecord declaredAttack,
            string targetBattleCardId,
            out int defenseGained)
        {
            defenseGained = 0;
            if (!TryGetHandler(runtime, installation, out IIncomingAttackTrapCardEffectHandler handler))
            {
                return false;
            }

            return handler.TryResolve(runtime, installation, declaredAttack,
                targetBattleCardId, out defenseGained);
        }

        public static bool TryCancelEnemyAbility(
            BattleRuntimeState runtime,
            BattleRuntimeTrapInstallation installation,
            BattleEventRecord abilityEvent,
            EnemyAbilityResolutionContext ability,
            out bool cancelled,
            out bool returnedToHand)
        {
            cancelled = false;
            returnedToHand = false;
            if (!TryGetHandler(runtime, installation, out IEnemyAbilityTrapCardEffectHandler handler))
            {
                return false;
            }

            return handler.TryResolve(runtime, installation, abilityEvent, ability,
                out cancelled, out returnedToHand);
        }

        private static bool TryGetHandler<THandler>(
            BattleRuntimeState runtime,
            BattleRuntimeTrapInstallation installation,
            out THandler handler) where THandler : class, ICardEffectHandler
        {
            handler = null;
            return runtime != null &&
                   installation != null &&
                   installation.SourceTrap != null &&
                   runtime.TrapInstallations.Find(
                       installation.SourceTrap.Ids.BattleCardId) == installation &&
                   runtime.Turn.Phase == BattleTurnPhase.EnemyTurn &&
                   runtime.Turn.PlayerTurnNumber >= installation.EligibleEnemyTurn &&
                   installation.SourceTrap.Zone == CardZone.SkillField &&
                   CardEffectRegistrationCatalog.TryFind(
                       installation.SourceTrap.SourceCard.CatalogCardId,
                       out CardEffectRegistration registration) &&
                   (handler = registration.Handler as THandler) != null &&
                   runtime.EventLog.Find(installation.PlayedEventId) != null;
        }
    }
}
