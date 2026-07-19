using System;

namespace HaveABreak.Cards
{
    public static class EnchantRoundTripTicketResolver
    {
        public static bool TryResolve(
            BattleEventRecord completedMainEffect,
            int playerTurn,
            BattleManaState mana,
            BattleDeckState deck,
            BattleCardEnchantRegistry enchants,
            EnchantTurnUsageTracker usage,
            BattleEventLog eventLog,
            out BattleCardInstance drawnCard,
            out BattleEventRecord drawEvent,
            out CardDrawFailure drawFailure)
        {
            drawnCard = null;
            drawEvent = null;
            drawFailure = CardDrawFailure.None;
            if (completedMainEffect == null ||
                completedMainEffect.EventType != BattleEventType.MainEffectCompleted ||
                mana == null || deck == null || enchants == null || usage == null || eventLog == null ||
                eventLog.Find(completedMainEffect.EventId) != completedMainEffect ||
                mana.CurrentMana != 0)
            {
                return false;
            }

            string sourceCardId = completedMainEffect.ActorId;
            if (!HasActiveRoundTripTicket(enchants.Find(sourceCardId)) ||
                !usage.TryUseOncePerPlayerTurn(
                    "E03", sourceCardId, completedMainEffect.EventId, playerTurn))
            {
                return false;
            }

            if (!deck.TryDraw(out drawnCard, out drawFailure))
            {
                return true;
            }

            drawEvent = eventLog.Record(
                BattleEventType.CardMoved,
                "E03RoundTripTicketDraw",
                sourceCardId,
                sourceCardId,
                drawnCard.Ids.BattleCardId,
                parentEventId: completedMainEffect.EventId,
                sourceEffectId: "E03",
                hasZoneChange: true,
                fromZone: CardZone.DrawPile,
                toZone: CardZone.Hand);
            drawFailure = CardDrawFailure.None;
            return true;
        }

        private static bool HasActiveRoundTripTicket(RunCardEnchantState enchants)
        {
            if (enchants == null)
            {
                return false;
            }

            foreach (RunEnchantSlot slot in enchants.Slots)
            {
                if (!slot.IsEmpty && slot.Active && string.Equals(
                        slot.Enchant.DefinitionId,
                        "E03",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
