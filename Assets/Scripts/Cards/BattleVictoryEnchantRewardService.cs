using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleVictoryEnchantRewardService
    {
        [SerializeField] private BattleSettlementService settlement;
        [SerializeField] private BattleVictoryRewardService rewardRules;
        [SerializeField] private RunDeckState runDeck;
        [SerializeField] private List<EnchantData> offeredChoices = new();
        [SerializeField] private bool claimed;
        [SerializeField] private EnchantData claimedEnchant;
        [SerializeField] private string targetOwnedCardId;

        private BattleVictoryEnchantRewardService()
        {
        }

        private BattleVictoryEnchantRewardService(
            BattleSettlementService settlement,
            BattleVictoryRewardService rewardRules,
            RunDeckState runDeck,
            IEnumerable<EnchantData> offeredChoices)
        {
            this.settlement = settlement;
            this.rewardRules = rewardRules;
            this.runDeck = runDeck;
            this.offeredChoices.AddRange(offeredChoices);
        }

        public IReadOnlyList<EnchantData> OfferedChoices => offeredChoices;
        public bool Claimed => claimed;
        public EnchantData ClaimedEnchant => claimedEnchant;
        public string TargetOwnedCardId => targetOwnedCardId;

        public static bool TryCreate(
            BattleRuntimeEncounterContext context,
            RunDeckState runDeck,
            IEnumerable<EnchantData> offeredChoices,
            out BattleVictoryEnchantRewardService service,
            out BattleVictoryEnchantRewardFailure failure)
        {
            service = null;
            if (context?.Settlement == null ||
                context.VictoryRewards == null ||
                context.DeckSnapshot == null)
            {
                failure = BattleVictoryEnchantRewardFailure
                    .InvalidEncounterContext;
                return false;
            }

            if (context.VictoryEnchantRewards != null)
            {
                failure =
                    BattleVictoryEnchantRewardFailure.AlreadyCreated;
                return false;
            }

            if (!MatchesSnapshot(context.DeckSnapshot, runDeck))
            {
                failure = BattleVictoryEnchantRewardFailure.InvalidDeck;
                return false;
            }

            if (!context.Settlement.IsSettled)
            {
                failure = BattleVictoryEnchantRewardFailure
                    .SettlementNotComplete;
                return false;
            }

            if (!context.Settlement.RewardEligible)
            {
                failure = BattleVictoryEnchantRewardFailure.NotVictory;
                return false;
            }

            List<EnchantData> choices = offeredChoices == null
                ? new List<EnchantData>()
                : new List<EnchantData>(offeredChoices);
            if (!ValidateChoices(
                    context.VictoryRewards,
                    runDeck,
                    choices))
            {
                failure =
                    BattleVictoryEnchantRewardFailure.InvalidChoiceSet;
                return false;
            }

            BattleVictoryEnchantRewardService created =
                new BattleVictoryEnchantRewardService(
                    context.Settlement,
                    context.VictoryRewards,
                    runDeck,
                    choices);
            if (!context.TrySetVictoryEnchantRewards(created))
            {
                failure =
                    BattleVictoryEnchantRewardFailure.AlreadyCreated;
                return false;
            }

            service = created;
            failure = BattleVictoryEnchantRewardFailure.None;
            return true;
        }

        public bool TryClaim(
            string enchantDefinitionId,
            string ownedCardId,
            int slotIndex,
            out EnchantAttachmentFailure attachmentFailure,
            out BattleVictoryEnchantRewardFailure failure)
        {
            attachmentFailure = EnchantAttachmentFailure.None;
            if (claimed)
            {
                failure = BattleVictoryEnchantRewardFailure.AlreadyClaimed;
                return false;
            }

            if (!settlement.IsSettled)
            {
                failure = BattleVictoryEnchantRewardFailure
                    .SettlementNotComplete;
                return false;
            }

            if (!settlement.RewardEligible)
            {
                failure = BattleVictoryEnchantRewardFailure.NotVictory;
                return false;
            }

            if (offeredChoices.Count == 0)
            {
                failure =
                    BattleVictoryEnchantRewardFailure.NoEnchantReward;
                return false;
            }

            EnchantData selected = FindChoice(enchantDefinitionId);
            if (selected == null)
            {
                failure =
                    BattleVictoryEnchantRewardFailure.ChoiceNotOffered;
                return false;
            }

            RunCardInstance target = runDeck.Find(ownedCardId);
            if (target == null)
            {
                failure = BattleVictoryEnchantRewardFailure.CardNotFound;
                return false;
            }

            if (!target.Enchants.TryAttach(
                    selected,
                    slotIndex,
                    false,
                    out attachmentFailure))
            {
                failure =
                    BattleVictoryEnchantRewardFailure.AttachmentFailed;
                return false;
            }

            claimed = true;
            claimedEnchant = selected;
            targetOwnedCardId = target.OwnedCardId;
            failure = BattleVictoryEnchantRewardFailure.None;
            return true;
        }

        private EnchantData FindChoice(string definitionId)
        {
            if (string.IsNullOrWhiteSpace(definitionId))
            {
                return null;
            }

            return offeredChoices.Find(enchant => enchant != null &&
                string.Equals(
                    enchant.DefinitionId,
                    definitionId,
                    StringComparison.OrdinalIgnoreCase));
        }

        private static bool ValidateChoices(
            BattleVictoryRewardService rewardRules,
            RunDeckState runDeck,
            IReadOnlyList<EnchantData> choices)
        {
            if (choices.Count != rewardRules.EnchantChoiceCount)
            {
                return false;
            }

            if (choices.Count == 0)
            {
                return true;
            }

            HashSet<string> definitionIds = new(
                StringComparer.OrdinalIgnoreCase);
            bool containsGuaranteedRarity = false;
            foreach (EnchantData choice in choices)
            {
                if (choice == null ||
                    string.IsNullOrWhiteSpace(choice.DefinitionId) ||
                    !Enum.IsDefined(typeof(CardRarity), choice.Rarity) ||
                    !definitionIds.Add(choice.DefinitionId) ||
                    !HasImmediateTarget(runDeck, choice))
                {
                    return false;
                }

                if ((int)choice.Rarity >=
                    (int)rewardRules.MinimumGuaranteedEnchantRarity)
                {
                    containsGuaranteedRarity = true;
                }
            }

            return containsGuaranteedRarity;
        }

        private static bool MatchesSnapshot(
            RunDeckBattleSnapshot snapshot,
            RunDeckState runDeck)
        {
            if (runDeck == null ||
                runDeck.Count != snapshot.Cards.Count)
            {
                return false;
            }

            foreach (BattleCardInstance battleCard in snapshot.Cards)
            {
                if (battleCard == null)
                {
                    return false;
                }

                RunCardInstance runCard = snapshot.FindRunCard(
                    battleCard.Ids.BattleCardId);
                if (runCard == null ||
                    runDeck.Find(runCard.OwnedCardId) != runCard)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasImmediateTarget(
            RunDeckState runDeck,
            EnchantData enchant)
        {
            foreach (RunCardInstance card in runDeck.Cards)
            {
                if (card?.Enchants != null &&
                    card.Enchants.HasImmediateAttachmentTarget(enchant))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
