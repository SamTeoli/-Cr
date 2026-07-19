using System;

namespace HaveABreak.Cards
{
    public static class EnchantWornHandleResolver
    {
        public static bool TryResolve(
            BattleEventRecord completedAttack,
            int playerTurn,
            BattleCardEnchantRegistry enchants,
            BattleMonsterRegistry monsters,
            EnchantTurnUsageTracker usage,
            BattleEventLog eventLog,
            out BattleEventRecord counterEvent)
        {
            counterEvent = null;
            if (completedAttack == null ||
                completedAttack.EventType != BattleEventType.AttackCompleted ||
                enchants == null || monsters == null || usage == null || eventLog == null ||
                eventLog.Find(completedAttack.EventId) != completedAttack)
            {
                return false;
            }

            string attackerId = completedAttack.ActorId;
            BattleMonsterState monster = monsters.Find(attackerId);
            RunCardEnchantState cardEnchants = enchants.Find(attackerId);
            if (monster == null || !HasActiveWornHandle(cardEnchants) ||
                !usage.TryUseOncePerPlayerTurn(
                    "E02", attackerId, completedAttack.EventId, playerTurn))
            {
                return false;
            }

            int beforeCounter = monster.Counter;
            monster.ApplyCounter(1);
            counterEvent = eventLog.Record(
                BattleEventType.StatusApplied,
                "E02WornHandle",
                attackerId,
                attackerId,
                attackerId,
                parentEventId: completedAttack.EventId,
                sourceEffectId: "E02",
                beforeValue: beforeCounter,
                afterValue: monster.Counter);
            return true;
        }

        private static bool HasActiveWornHandle(RunCardEnchantState enchants)
        {
            if (enchants == null)
            {
                return false;
            }

            foreach (RunEnchantSlot slot in enchants.Slots)
            {
                if (!slot.IsEmpty && slot.Active && string.Equals(
                        slot.Enchant.DefinitionId,
                        "E02",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
