using System;
using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public static class BattleRuntimePlayerTurnStartEffectService
    {
        public static bool TryCompleteEnemyTurnAndResolve(
            BattleRuntimeState runtime,
            out BattleRuntimePlayerTurnStartEffectResult result,
            out BattleTurnFailure turnFailure)
        {
            result = null;
            turnFailure = BattleTurnFailure.None;
            if (runtime == null ||
                !runtime.Turn.TryCompleteEnemyTurn(out turnFailure))
            {
                return false;
            }

            int resolvedC11Count = 0;
            int drawnCount = 0;
            List<string> defendedMonsterIds = new();
            foreach (BattleCardInstance card in
                     runtime.Deck.Zones.GetCards(CardZone.SkillField))
            {
                if (card == null ||
                    card.SourceCard.CardType != CardType.Barrier ||
                    !string.Equals(
                        card.SourceCard.CatalogCardId,
                        "C11",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!C11LateNightWaitingRoomResolver.TryResolve(
                        card,
                        runtime.Turn.PlayerTurnNumber,
                        runtime.Deck,
                        runtime.Monsters,
                        runtime.EventLog,
                        runtime.EffectResolutions,
                        out int drawn,
                        out string defendedMonsterId))
                {
                    return false;
                }

                resolvedC11Count++;
                drawnCount += drawn;
                if (!string.IsNullOrWhiteSpace(defendedMonsterId))
                {
                    defendedMonsterIds.Add(defendedMonsterId);
                }
            }

            result = new BattleRuntimePlayerTurnStartEffectResult(
                resolvedC11Count, drawnCount, defendedMonsterIds);
            return true;
        }
    }
}
