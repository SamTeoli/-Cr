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

            installation = new BattleRuntimeTrapInstallation(
                playResult.Card,
                playResult.PlayedEvent.EventId,
                runtime.Turn.PlayerTurnNumber);
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
            if (!CanRespond(runtime, installation, "C08"))
            {
                return false;
            }

            return C08ClosingDoorResolver.TryReplace(
                moveAttemptEvent,
                runtime.Turn.PlayerTurnNumber,
                installation.EligibleEnemyTurn,
                requestedSteps,
                movingEnemyId,
                installation.SourceTrap,
                runtime.EnemyMovementLocks,
                runtime.EnemyStatuses,
                runtime.CardTurnTriggers,
                runtime.EventLog,
                out replacementSteps);
        }

        public static bool TryResolveIncomingAttack(
            BattleRuntimeState runtime,
            BattleRuntimeTrapInstallation installation,
            BattleEventRecord declaredAttack,
            string targetBattleCardId,
            out int defenseGained)
        {
            defenseGained = 0;
            if (!CanRespond(runtime, installation, "C09"))
            {
                return false;
            }

            BattleMonsterState target = runtime.Monsters.Find(targetBattleCardId);
            return C09InspectionBlanketResolver.TryResolve(
                declaredAttack,
                runtime.Turn.PlayerTurnNumber,
                installation.EligibleEnemyTurn,
                installation.SourceTrap,
                target,
                runtime.DefenseRetention,
                runtime.CardTurnTriggers,
                runtime.EventLog,
                out defenseGained);
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
            if (!CanRespond(runtime, installation, "C10"))
            {
                return false;
            }

            return C10BrokenCallLineResolver.TryCancel(
                abilityEvent,
                ability,
                installation.SourceTrap,
                runtime.Deck.Zones,
                runtime.EnemyStatuses,
                runtime.EventLog,
                runtime.EffectResolutions,
                out cancelled,
                out returnedToHand);
        }

        private static bool CanRespond(
            BattleRuntimeState runtime,
            BattleRuntimeTrapInstallation installation,
            string catalogCardId)
        {
            return runtime != null &&
                   installation != null &&
                   installation.SourceTrap != null &&
                   runtime.Turn.Phase == BattleTurnPhase.EnemyTurn &&
                   runtime.Turn.PlayerTurnNumber >= installation.EligibleEnemyTurn &&
                   installation.SourceTrap.Zone == CardZone.SkillField &&
                   string.Equals(
                       installation.SourceTrap.SourceCard.CatalogCardId,
                       catalogCardId,
                       StringComparison.OrdinalIgnoreCase) &&
                   runtime.EventLog.Find(installation.PlayedEventId) != null;
        }
    }
}
