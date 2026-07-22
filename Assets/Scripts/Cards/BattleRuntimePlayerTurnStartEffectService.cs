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
                    !CardEffectRegistrationCatalog.TryFind(
                        card.SourceCard.CatalogCardId, out CardEffectRegistration registration) ||
                    registration.Handler is not IPlayerTurnStartCardEffectHandler handler)
                {
                    continue;
                }

                if (!handler.TryResolve(runtime, card, out int drawn,
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
