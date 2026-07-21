using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    // Disposable E01-E08 test-content module. It intentionally keeps the
    // prototype enchant behavior together so a new content set can replace
    // it without restructuring the generic enchant data model.
    public readonly struct EnchantFixedTargetDeclaration
    {
        public EnchantFixedTargetDeclaration(
            string sourceBattleCardId,
            string declaredEnemyId,
            bool targetsPosition,
            EnemyFieldPosition position)
        {
            SourceBattleCardId = sourceBattleCardId;
            DeclaredEnemyId = declaredEnemyId;
            TargetsPosition = targetsPosition;
            Position = position;
        }

        public string SourceBattleCardId { get; }
        public string DeclaredEnemyId { get; }
        public bool TargetsPosition { get; }
        public EnemyFieldPosition Position { get; }
    }

    public static class EnchantFixedTargetResolver
    {
        public static bool TryDeclare(
            string sourceBattleCardId,
            string targetEnemyId,
            BattleEnemyPositionState positions,
            BattleCardEnchantRegistry enchants,
            out EnchantFixedTargetDeclaration declaration)
        {
            declaration = default;
            if (string.IsNullOrWhiteSpace(sourceBattleCardId) ||
                string.IsNullOrWhiteSpace(targetEnemyId) || positions == null)
            {
                return false;
            }

            EnemyFieldPosition? position = positions.FindPosition(targetEnemyId);
            if (!position.HasValue)
            {
                return false;
            }

            bool targetsPosition = HasActiveRoutePin(
                enchants?.Find(sourceBattleCardId));
            declaration = new EnchantFixedTargetDeclaration(
                sourceBattleCardId.Trim(),
                targetEnemyId.Trim(),
                targetsPosition,
                position.Value);
            return true;
        }

        public static string Resolve(
            EnchantFixedTargetDeclaration declaration,
            BattleEnemyPositionState positions)
        {
            if (positions == null)
            {
                return null;
            }

            if (declaration.TargetsPosition)
            {
                return positions.GetOccupant(declaration.Position);
            }

            return positions.FindPosition(declaration.DeclaredEnemyId).HasValue
                ? declaration.DeclaredEnemyId
                : null;
        }

        private static bool HasActiveRoutePin(RunCardEnchantState enchants)
        {
            if (enchants == null)
            {
                return false;
            }

            foreach (RunEnchantSlot slot in enchants.Slots)
            {
                if (!slot.IsEmpty && slot.Active && string.Equals(
                        slot.Enchant.DefinitionId,
                        TestContentIds.E08,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public static class EnchantManaCostResolver
    {
        public static int Resolve(BattleCardInstance card, RunCardEnchantState enchants)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            int cost = Mathf.Max(0, card.Resolved.ManaCost);
            if (enchants == null || enchants.Card != card.SourceCard)
            {
                return cost;
            }

            foreach (RunEnchantSlot slot in enchants.Slots)
            {
                if (!slot.IsEmpty && slot.Active &&
                    string.Equals(slot.Enchant.DefinitionId, TestContentIds.E04, StringComparison.OrdinalIgnoreCase))
                {
                    cost = Mathf.Max(1, cost - 1);
                }
            }

            return cost;
        }
    }

    public static class EnchantRepeatedEffectResolver
    {
        public static RepeatedEffectParameters Resolve(
            BattleCardInstance sourceCard,
            BattleCardEnchantRegistry enchants,
            RepeatedEffectParameters original)
        {
            if (sourceCard == null)
            {
                throw new ArgumentNullException(nameof(sourceCard));
            }

            RunCardEnchantState cardEnchants = enchants?.Find(sourceCard.Ids.BattleCardId);
            if (sourceCard.SourceCard.CardType != CardType.Barrier ||
                !sourceCard.SourceCard.HasEnchantCompatibilityTag(
                    EnchantCompatibilityTag.NumericRepeatingEffect) ||
                !HasActiveStarlightEngraving(cardEnchants))
            {
                return original;
            }

            return new RepeatedEffectParameters(
                original.FirstValue + 1,
                original.TargetCount,
                original.ActivationCount,
                original.ConditionThreshold);
        }

        private static bool HasActiveStarlightEngraving(RunCardEnchantState enchants)
        {
            if (enchants == null)
            {
                return false;
            }

            foreach (RunEnchantSlot slot in enchants.Slots)
            {
                if (!slot.IsEmpty && slot.Active && string.Equals(
                        slot.Enchant.DefinitionId,
                        TestContentIds.E06,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }

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
                    TestContentIds.E03, sourceCardId, completedMainEffect.EventId, playerTurn))
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
                sourceEffectId: TestContentIds.E03,
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
                        TestContentIds.E03,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public static class EnchantRustyAnnouncementResolver
    {
        public static bool TryResolve(
            BattleEventRecord completedMainEffect,
            BattleEffectImpactRecord impact,
            BattleCardEnchantRegistry enchants,
            BattleEnemyStatusRegistry enemyStatuses,
            BattleEventLog eventLog,
            out List<BattleEventRecord> weakenEvents)
        {
            weakenEvents = new List<BattleEventRecord>();
            if (completedMainEffect == null || impact == null || enchants == null ||
                enemyStatuses == null || eventLog == null ||
                completedMainEffect.EventType != BattleEventType.MainEffectCompleted ||
                eventLog.Find(completedMainEffect.EventId) != completedMainEffect ||
                !string.Equals(
                    impact.CompletedEventId,
                    completedMainEffect.EventId,
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    impact.SourceBattleCardId,
                    completedMainEffect.ActorId,
                    StringComparison.OrdinalIgnoreCase) ||
                impact.AffectedEnemyIds.Count == 0 ||
                !HasActiveRustyAnnouncement(enchants.Find(impact.SourceBattleCardId)) ||
                WasAlreadyResolved(completedMainEffect.EventId, eventLog))
            {
                return false;
            }

            foreach (string enemyId in impact.AffectedEnemyIds)
            {
                BattleEnemyStatusState enemy = enemyStatuses.Find(enemyId);
                if (enemy == null)
                {
                    continue;
                }

                int beforeWeaken = enemy.Weaken;
                enemy.ApplyWeaken(1);
                weakenEvents.Add(eventLog.Record(
                    BattleEventType.StatusApplied,
                    "E05RustyAnnouncement",
                    impact.SourceBattleCardId,
                    impact.SourceBattleCardId,
                    enemy.EnemyId,
                    parentEventId: completedMainEffect.EventId,
                    sourceEffectId: TestContentIds.E05,
                    beforeValue: beforeWeaken,
                    afterValue: enemy.Weaken));
            }

            return weakenEvents.Count > 0;
        }

        private static bool WasAlreadyResolved(string completedEventId, BattleEventLog eventLog)
        {
            foreach (BattleEventRecord record in eventLog.Events)
            {
                if (record != null && record.SourceEffectId == TestContentIds.E05 && string.Equals(
                        record.ParentEventId,
                        completedEventId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasActiveRustyAnnouncement(RunCardEnchantState enchants)
        {
            if (enchants == null)
            {
                return false;
            }

            foreach (RunEnchantSlot slot in enchants.Slots)
            {
                if (!slot.IsEmpty && slot.Active && string.Equals(
                        slot.Enchant.DefinitionId,
                        TestContentIds.E05,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }

    [Serializable]
    public sealed class EnchantTurnUsageTracker
    {
        [SerializeField] private List<Entry> entries = new();
        [SerializeField] private List<string> processedEventIds = new();

        [Serializable]
        private sealed class Entry
        {
            [SerializeField] private string effectId;
            [SerializeField] private string battleCardId;
            [SerializeField] private int lastPlayerTurn;

            public Entry(string effectId, string battleCardId, int playerTurn)
            {
                this.effectId = effectId;
                this.battleCardId = battleCardId;
                lastPlayerTurn = playerTurn;
            }

            public string EffectId => effectId;
            public string BattleCardId => battleCardId;
            public int LastPlayerTurn => lastPlayerTurn;

            public void MarkTurn(int playerTurn)
            {
                lastPlayerTurn = playerTurn;
            }
        }

        public bool TryUseOncePerPlayerTurn(
            string effectId,
            string battleCardId,
            string sourceEventId,
            int playerTurn)
        {
            if (string.IsNullOrWhiteSpace(effectId) || string.IsNullOrWhiteSpace(battleCardId) ||
                string.IsNullOrWhiteSpace(sourceEventId) || playerTurn < 1 ||
                processedEventIds.Exists(id => string.Equals(
                    id, sourceEventId, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            Entry entry = entries.Find(item => item != null &&
                string.Equals(item.EffectId, effectId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.BattleCardId, battleCardId, StringComparison.OrdinalIgnoreCase));
            if (entry != null && entry.LastPlayerTurn == playerTurn)
            {
                processedEventIds.Add(sourceEventId.Trim());
                return false;
            }

            if (entry == null)
            {
                entries.Add(new Entry(effectId.Trim(), battleCardId.Trim(), playerTurn));
            }
            else
            {
                entry.MarkTurn(playerTurn);
            }

            processedEventIds.Add(sourceEventId.Trim());
            return true;
        }
    }

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
                    TestContentIds.E02, attackerId, completedAttack.EventId, playerTurn))
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
                sourceEffectId: TestContentIds.E02,
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
                        TestContentIds.E02,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public readonly struct RepeatedEffectParameters
    {
        public RepeatedEffectParameters(
            int firstValue,
            int targetCount,
            int activationCount,
            int conditionThreshold)
        {
            FirstValue = firstValue;
            TargetCount = targetCount;
            ActivationCount = activationCount;
            ConditionThreshold = conditionThreshold;
        }

        public int FirstValue { get; }
        public int TargetCount { get; }
        public int ActivationCount { get; }
        public int ConditionThreshold { get; }
    }
}
