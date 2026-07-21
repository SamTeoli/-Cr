using System;

namespace HaveABreak.Cards
{
    public static class BattleRuntimeTurnEffectService
    {
        public static bool TryEndPlayerTurn(
            BattleRuntimeState runtime,
            int firstPlayerTurnEventIndex,
            out BattleRuntimeTurnEffectResult result,
            out BattleTurnFailure turnFailure)
        {
            result = null;
            turnFailure = BattleTurnFailure.None;
            if (runtime == null ||
                runtime.Turn.Phase != BattleTurnPhase.PlayerAction ||
                firstPlayerTurnEventIndex < 0 ||
                firstPlayerTurnEventIndex > runtime.EventLog.Events.Count)
            {
                turnFailure = BattleTurnFailure.InvalidPhase;
                return false;
            }

            int resolvedCount = 0;
            int defenseGained = 0;
            foreach (BattleMonsterState monster in runtime.Monsters.Monsters)
            {
                if (monster == null ||
                    monster.Card.Zone != CardZone.MonsterField ||
                    !string.Equals(
                        monster.Card.SourceCard.CatalogCardId,
                        TestContentIds.C03,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!C03SeatRepairerTurnEndResolver.TryResolve(
                        monster,
                        runtime.Turn.PlayerTurnNumber,
                        firstPlayerTurnEventIndex,
                        runtime.EventLog,
                        runtime.EffectResolutions,
                        out C03SeatRepairerResult c03Result))
                {
                    return false;
                }

                resolvedCount++;
                defenseGained += c03Result.DefenseGained;
            }

            runtime.DefenseRetention.EndPlayerTurn();
            if (!runtime.Turn.TryEndPlayerTurn(out turnFailure))
            {
                return false;
            }

            result = new BattleRuntimeTurnEffectResult(
                resolvedCount, 0, defenseGained, 0);
            return true;
        }

        public static bool TryResolveEnemyMoved(
            BattleRuntimeState runtime,
            BattleEventRecord movedEvent,
            out BattleRuntimeTurnEffectResult result)
        {
            result = null;
            if (!BattleRuntimeMovementReactionService.TryResolve(
                    runtime,
                    movedEvent,
                    out BattleRuntimeMovementReactionResult movementResult))
            {
                return false;
            }

            result = new BattleRuntimeTurnEffectResult(
                0,
                movementResult.ResolvedC04Count,
                0,
                movementResult.AttackEnhancementGained);
            return true;
        }
    }
}
