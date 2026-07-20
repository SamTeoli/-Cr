using System;
using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public static class BattleRuntimeBootstrapService
    {
        public static bool TryCreate(
            IEnumerable<BattleCardInstance> deckCards,
            RunBattleState run,
            EncounterData encounter,
            int shuffleSeed,
            int maximumMana,
            out BattleRuntimeBootstrapResult result,
            out BattleRuntimeBootstrapFailure failure,
            out List<string> validationErrors)
        {
            result = null;
            List<BattleCardInstance> deckSnapshot = deckCards == null
                ? null
                : new List<BattleCardInstance>(deckCards);
            validationErrors = ValidateDeck(deckSnapshot);
            if (validationErrors.Count > 0)
            {
                failure = BattleRuntimeBootstrapFailure.InvalidDeck;
                return false;
            }

            if (run == null || run.RunEnded || run.CurrentHealth <= 0)
            {
                failure = BattleRuntimeBootstrapFailure.InvalidRunState;
                return false;
            }

            validationErrors =
                EncounterDataValidationService.ValidateEncounter(encounter);
            if (validationErrors.Count > 0)
            {
                failure = BattleRuntimeBootstrapFailure.InvalidEncounter;
                return false;
            }

            BattleRuntimeState runtime = new(
                deckSnapshot,
                shuffleSeed,
                maximumMana,
                run.MaximumHealth);
            runtime.Player.ApplyDamage(
                run.MaximumHealth - run.CurrentHealth);

            foreach (EncounterEnemySlot slot in encounter.EnemySlots)
            {
                if (!runtime.TryAddEnemy(
                        slot.EnemyInstanceId,
                        slot.Enemy.Attack,
                        slot.Enemy.MaximumHealth,
                        slot.Position,
                        out _))
                {
                    failure =
                        BattleRuntimeBootstrapFailure.EnemyRegistrationFailed;
                    return false;
                }
            }

            BattleRuntimeSessionState session = new(runtime);
            result = new BattleRuntimeBootstrapResult(
                runtime, session, encounter);
            validationErrors.Clear();
            failure = BattleRuntimeBootstrapFailure.None;
            return true;
        }

        private static List<string> ValidateDeck(
            IEnumerable<BattleCardInstance> deckCards)
        {
            List<string> errors = new();
            if (deckCards == null)
            {
                errors.Add("Battle deck is required.");
                return errors;
            }

            List<BattleCardInstance> snapshot = new(deckCards);
            if (snapshot.Count == 0)
            {
                errors.Add("Battle deck requires at least one card.");
                return errors;
            }

            HashSet<string> battleCardIds = new(
                StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < snapshot.Count; i++)
            {
                BattleCardInstance card = snapshot[i];
                if (card == null)
                {
                    errors.Add($"Battle deck card {i} is null.");
                    continue;
                }

                if (!battleCardIds.Add(card.Ids.BattleCardId))
                {
                    errors.Add(
                        $"Battle deck has duplicate battle card ID '{card.Ids.BattleCardId}'.");
                }
            }

            return errors;
        }
    }
}
