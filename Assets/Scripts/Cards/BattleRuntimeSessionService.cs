using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public static class BattleRuntimeSessionService
    {
        public static bool TryBegin(
            BattleRuntimeSessionState session,
            IEnumerable<string> selectedStartingHandRedrawIds,
            out List<BattleCardInstance> replacements,
            out BattleRuntimeSessionFailure failure,
            out StartingHandRedrawFailure redrawFailure,
            out BattleTurnFailure turnFailure)
        {
            replacements = new List<BattleCardInstance>();
            redrawFailure = StartingHandRedrawFailure.NotAvailable;
            turnFailure = BattleTurnFailure.None;
            if (session?.Runtime == null)
            {
                failure = BattleRuntimeSessionFailure.InvalidSession;
                return false;
            }

            if (session.Started)
            {
                failure = BattleRuntimeSessionFailure.AlreadyStarted;
                return false;
            }

            if (!session.Runtime.Turn.TryBeginBattle(out turnFailure))
            {
                failure = BattleRuntimeSessionFailure.BeginBattleFailed;
                return false;
            }

            if (!session.Runtime.Turn.TryConfirmStartingHand(
                    selectedStartingHandRedrawIds,
                    out replacements,
                    out redrawFailure,
                    out turnFailure))
            {
                failure = BattleRuntimeSessionFailure
                    .StartingHandConfirmationFailed;
                return false;
            }

            BattleOutcome initialOutcome = new BattleOutcomeEvaluator(
                session.Runtime.Player,
                session.Runtime.LivingEnemies).Evaluate();
            session.MarkStarted(
                session.Runtime.EventLog.Events.Count,
                initialOutcome);
            failure = BattleRuntimeSessionFailure.None;
            return true;
        }

        public static bool TryResolveRound(
            BattleRuntimeSessionState session,
            IEnumerable<BattleRuntimeEnemyTurnCommand> enemyCommands,
            out BattleRuntimeSessionRoundResult result,
            out BattleRuntimeSessionFailure failure,
            out BattleRuntimeRoundFailure roundFailure,
            out BattleTurnFailure playerTurnEndFailure,
            out BattleRuntimeEnemyTurnPipelineFailure pipelineFailure,
            out BattleRuntimeEnemyTurnPlanFailure planFailure,
            out BattleRuntimeEnemyTurnFailure enemyTurnFailure,
            out int failedActionIndex)
        {
            result = null;
            roundFailure = BattleRuntimeRoundFailure.None;
            playerTurnEndFailure = BattleTurnFailure.None;
            pipelineFailure = BattleRuntimeEnemyTurnPipelineFailure.None;
            planFailure = BattleRuntimeEnemyTurnPlanFailure.None;
            enemyTurnFailure = BattleRuntimeEnemyTurnFailure.None;
            failedActionIndex = -1;

            if (session?.Runtime == null)
            {
                failure = BattleRuntimeSessionFailure.InvalidSession;
                return false;
            }

            if (!session.Started)
            {
                failure = BattleRuntimeSessionFailure.NotStarted;
                return false;
            }

            if (session.IsFinished)
            {
                failure = BattleRuntimeSessionFailure.BattleFinished;
                return false;
            }

            if (!BattleRuntimeRoundService.TryResolve(
                    session.Runtime,
                    session.PlayerTurnEventStartIndex,
                    enemyCommands,
                    out BattleRuntimeRoundResult round,
                    out roundFailure,
                    out playerTurnEndFailure,
                    out pipelineFailure,
                    out planFailure,
                    out enemyTurnFailure,
                    out failedActionIndex))
            {
                failure = BattleRuntimeSessionFailure.RoundResolutionFailed;
                return false;
            }

            session.MarkRoundCompleted(round);
            result = new BattleRuntimeSessionRoundResult(round, session);
            failure = BattleRuntimeSessionFailure.None;
            return true;
        }

        public static bool TryFinalizeTerminalOutcome(
            BattleRuntimeSessionState session,
            out BattleOutcome outcome,
            out BattleRuntimeSessionFailure failure)
        {
            outcome = BattleOutcome.Ongoing;
            if (session?.Runtime == null)
            {
                failure = BattleRuntimeSessionFailure.InvalidSession;
                return false;
            }

            if (!session.Started)
            {
                failure = BattleRuntimeSessionFailure.NotStarted;
                return false;
            }

            if (session.IsFinished)
            {
                outcome = session.Outcome;
                failure = BattleRuntimeSessionFailure.BattleFinished;
                return false;
            }

            outcome = new BattleOutcomeEvaluator(
                session.Runtime.Player,
                session.Runtime.LivingEnemies).Evaluate();
            if (outcome == BattleOutcome.Ongoing)
            {
                failure = BattleRuntimeSessionFailure.BattleOngoing;
                return false;
            }

            if (!session.TryMarkTerminalOutcome(outcome))
            {
                failure = BattleRuntimeSessionFailure.InvalidSession;
                return false;
            }

            failure = BattleRuntimeSessionFailure.None;
            return true;
        }
    }

    // Disposable default enemy turn for the prototype play screen. That screen
    // still creates enemies directly rather than through an encounter, so it
    // cannot use the authored pattern service below until encounter data is
    // connected in the next integration step.
    public static class BattleRuntimeTestTurnService
    {
        public static bool TryEndPlayerTurn(
            BattleRuntimeSessionState session,
            int tieBreakerSeed,
            out BattleRuntimeSessionRoundResult result,
            out BattleRuntimeSessionFailure failure,
            out BattleRuntimeRoundFailure roundFailure,
            out BattleTurnFailure playerTurnEndFailure,
            out BattleRuntimeEnemyTurnPipelineFailure pipelineFailure,
            out BattleRuntimeEnemyTurnPlanFailure planFailure,
            out BattleRuntimeEnemyTurnFailure enemyTurnFailure,
            out int failedActionIndex)
        {
            List<BattleRuntimeEnemyTurnCommand> commands = new();
            BattleRuntimeState runtime = session?.Runtime;
            if (runtime?.LivingEnemies != null)
            {
                int activeEnemyIndex = 0;
                foreach (BattleEnemyRuntimeState enemy in runtime.Enemies)
                {
                    if (enemy == null || !enemy.IsAlive ||
                        !runtime.LivingEnemies.Contains(enemy.EnemyId))
                    {
                        continue;
                    }

                    commands.Add(
                        BattleRuntimeEnemyTurnCommand.CreateAutomaticAttack(
                            enemy.EnemyId,
                            1,
                            new[]
                            {
                                unchecked(tieBreakerSeed + activeEnemyIndex)
                            }));
                    activeEnemyIndex++;
                }
            }

            return BattleRuntimeSessionService.TryResolveRound(
                session,
                commands,
                out result,
                out failure,
                out roundFailure,
                out playerTurnEndFailure,
                out pipelineFailure,
                out planFailure,
                out enemyTurnFailure,
                out failedActionIndex);
        }
    }

    public enum BattleRuntimeEnemyPatternFailure
    {
        None,
        InvalidSession,
        InvalidEncounter,
        InvalidTurnNumber,
        EncounterEnemyMissing,
        InvalidPattern
    }

    public static class BattleRuntimeEnemyPatternService
    {
        public static bool TryCreateCommands(
            BattleRuntimeSessionState session,
            EncounterData encounter,
            int tieBreakerSeed,
            out List<BattleRuntimeEnemyTurnCommand> commands,
            out BattleRuntimeEnemyPatternFailure failure)
        {
            commands = new List<BattleRuntimeEnemyTurnCommand>();
            BattleRuntimeState runtime = session?.Runtime;
            if (runtime?.LivingEnemies == null)
            {
                failure = BattleRuntimeEnemyPatternFailure.InvalidSession;
                return false;
            }

            if (encounter?.EnemySlots == null)
            {
                failure = BattleRuntimeEnemyPatternFailure.InvalidEncounter;
                return false;
            }

            int playerTurnNumber = runtime.Turn.PlayerTurnNumber;
            if (playerTurnNumber <= 0)
            {
                failure = BattleRuntimeEnemyPatternFailure.InvalidTurnNumber;
                return false;
            }

            int tieBreakerOffset = 0;
            foreach (EncounterEnemySlot slot in encounter.EnemySlots)
            {
                if (slot?.Enemy == null ||
                    string.IsNullOrWhiteSpace(slot.EnemyInstanceId))
                {
                    failure = BattleRuntimeEnemyPatternFailure.InvalidEncounter;
                    return false;
                }

                BattleEnemyRuntimeState enemy =
                    runtime.FindEnemy(slot.EnemyInstanceId);
                if (enemy == null)
                {
                    failure =
                        BattleRuntimeEnemyPatternFailure.EncounterEnemyMissing;
                    return false;
                }

                if (!enemy.IsAlive ||
                    !runtime.LivingEnemies.Contains(enemy.EnemyId))
                {
                    continue;
                }

                if (!slot.Enemy.ActionPattern.TryGetTurn(
                        playerTurnNumber,
                        out EnemyTurnPatternStep patternTurn))
                {
                    failure = BattleRuntimeEnemyPatternFailure.InvalidPattern;
                    return false;
                }

                if (patternTurn.Moves)
                {
                    commands.Add(
                        BattleRuntimeEnemyTurnCommand.CreateMove(
                            enemy.EnemyId,
                            patternTurn.MoveDirection,
                            patternTurn.MoveSteps));
                }

                if (patternTurn.AttackCount > 0)
                {
                    List<int> tieBreakerValues = new();
                    for (int attackIndex = 0;
                         attackIndex < patternTurn.AttackCount;
                         attackIndex++)
                    {
                        tieBreakerValues.Add(unchecked(
                            tieBreakerSeed + tieBreakerOffset));
                        tieBreakerOffset++;
                    }

                    commands.Add(
                        BattleRuntimeEnemyTurnCommand.CreateAutomaticAttack(
                            enemy.EnemyId,
                            patternTurn.AttackCount,
                            tieBreakerValues));
                }

                foreach (EnemyPatternAbilityData ability in
                         patternTurn.Abilities)
                {
                    if (ability == null ||
                        string.IsNullOrWhiteSpace(ability.AbilityId))
                    {
                        failure =
                            BattleRuntimeEnemyPatternFailure.InvalidPattern;
                        commands.Clear();
                        return false;
                    }

                    commands.Add(
                        BattleRuntimeEnemyTurnCommand.CreateAbility(
                            new EnemyAbilityResolutionContext(
                                ability.AbilityId,
                                enemy.EnemyId,
                                false,
                                ability.AffectsFriendlySide,
                                ability.IsAreaAbility)));
                }
            }

            failure = BattleRuntimeEnemyPatternFailure.None;
            return true;
        }

        public static bool TryEndPlayerTurn(
            BattleRuntimeSessionState session,
            EncounterData encounter,
            int tieBreakerSeed,
            out BattleRuntimeSessionRoundResult result,
            out BattleRuntimeEnemyPatternFailure patternFailure,
            out BattleRuntimeSessionFailure sessionFailure,
            out BattleRuntimeRoundFailure roundFailure,
            out BattleTurnFailure playerTurnEndFailure,
            out BattleRuntimeEnemyTurnPipelineFailure pipelineFailure,
            out BattleRuntimeEnemyTurnPlanFailure planFailure,
            out BattleRuntimeEnemyTurnFailure enemyTurnFailure,
            out int failedActionIndex)
        {
            result = null;
            sessionFailure = BattleRuntimeSessionFailure.None;
            roundFailure = BattleRuntimeRoundFailure.None;
            playerTurnEndFailure = BattleTurnFailure.None;
            pipelineFailure = BattleRuntimeEnemyTurnPipelineFailure.None;
            planFailure = BattleRuntimeEnemyTurnPlanFailure.None;
            enemyTurnFailure = BattleRuntimeEnemyTurnFailure.None;
            failedActionIndex = -1;

            if (!TryCreateCommands(
                    session,
                    encounter,
                    tieBreakerSeed,
                    out List<BattleRuntimeEnemyTurnCommand> commands,
                    out patternFailure))
            {
                return false;
            }

            return BattleRuntimeSessionService.TryResolveRound(
                session,
                commands,
                out result,
                out sessionFailure,
                out roundFailure,
                out playerTurnEndFailure,
                out pipelineFailure,
                out planFailure,
                out enemyTurnFailure,
                out failedActionIndex);
        }
    }
}
