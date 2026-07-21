using System;

namespace HaveABreak.Cards
{
    public static class BattleRuntimeC07EffectService
    {
        public static bool TryResolve(
            BattleRuntimeState runtime,
            BattleRuntimeCardPlayResult playResult,
            string selectedBanishBattleCardId,
            out BattleRuntimeC07EffectResult result)
        {
            result = null;
            if (runtime == null || playResult == null || playResult.Card == null ||
                playResult.PlayedEvent == null ||
                runtime.EventLog.Find(playResult.PlayedEvent.EventId) !=
                playResult.PlayedEvent ||
                !string.Equals(
                    playResult.Card.SourceCard.CatalogCardId,
                    TestContentIds.C07,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!C07LostTicketResolver.TryResolve(
                    playResult.PlayedEvent,
                    playResult.Card,
                    selectedBanishBattleCardId,
                    runtime.Deck,
                    runtime.Monsters,
                    runtime.EventLog,
                    runtime.EffectResolutions,
                    out int drawnCount,
                    out bool banished,
                    out int defendedMonsterCount))
            {
                return false;
            }

            result = new BattleRuntimeC07EffectResult(
                drawnCount, banished, defendedMonsterCount);
            return true;
        }
    }
}
