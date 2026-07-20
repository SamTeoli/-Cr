using System;
using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public static class RunDeckBattleSnapshotService
    {
        public static bool TryCreate(
            RunDeckState runDeck,
            string battleInstanceId,
            out RunDeckBattleSnapshot snapshot,
            out RunDeckFailure failure)
        {
            snapshot = null;
            if (runDeck == null || runDeck.Count == 0)
            {
                failure = RunDeckFailure.InvalidDeck;
                return false;
            }

            if (string.IsNullOrWhiteSpace(battleInstanceId))
            {
                failure = RunDeckFailure.InvalidBattleInstanceId;
                return false;
            }

            string trimmedBattleInstanceId = battleInstanceId.Trim();
            HashSet<string> ownedCardIds = new(
                StringComparer.OrdinalIgnoreCase);
            HashSet<string> battleCardIds = new(
                StringComparer.OrdinalIgnoreCase);
            foreach (RunCardInstance card in runDeck.Cards)
            {
                if (card?.Card == null || card.Enchants == null ||
                    card.Enchants.Card != card.Card)
                {
                    failure = RunDeckFailure.InvalidCard;
                    return false;
                }

                if (string.IsNullOrWhiteSpace(card.OwnedCardId))
                {
                    failure = RunDeckFailure.InvalidOwnedCardId;
                    return false;
                }

                if (!ownedCardIds.Add(card.OwnedCardId))
                {
                    failure = RunDeckFailure.DuplicateOwnedCardId;
                    return false;
                }

                string battleCardId =
                    RunDeckBattleSnapshot.BuildBattleCardId(
                        trimmedBattleInstanceId,
                        card.OwnedCardId);
                if (!battleCardIds.Add(battleCardId))
                {
                    failure = RunDeckFailure.DuplicateOwnedCardId;
                    return false;
                }
            }

            snapshot = new RunDeckBattleSnapshot(
                trimmedBattleInstanceId,
                runDeck.Cards);
            failure = RunDeckFailure.None;
            return true;
        }
    }
}
