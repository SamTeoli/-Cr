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
                        "C03",
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
            if (runtime == null || movedEvent == null ||
                runtime.Turn.Phase != BattleTurnPhase.EnemyTurn ||
                movedEvent.EventType != BattleEventType.EnemyMoved ||
                runtime.EventLog.Find(movedEvent.EventId) != movedEvent)
            {
                return false;
            }

            int resolvedCount = 0;
            int attackEnhancement = 0;
            foreach (BattleMonsterState monster in runtime.Monsters.Monsters)
            {
                if (monster == null ||
                    monster.Card.Zone != CardZone.MonsterField ||
                    !string.Equals(
                        monster.Card.SourceCard.CatalogCardId,
                        "C04",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (C04TerminalCatResolver.TryResolve(
                        movedEvent,
                        runtime.Turn.PlayerTurnNumber,
                        monster,
                        runtime.CardTurnTriggers,
                        runtime.EventLog,
                        out int gained))
                {
                    resolvedCount++;
                    attackEnhancement += gained;
                }
            }

            result = new BattleRuntimeTurnEffectResult(
                0, resolvedCount, 0, attackEnhancement);
            return true;
        }
    }
}
