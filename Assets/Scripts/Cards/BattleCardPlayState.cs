using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleCardPlayState
    {
        [SerializeField] private BattleDeckState deck;
        [SerializeField] private BattleManaState mana;
        [SerializeField] private BattleCardEnchantRegistry enchants;
        [SerializeField] private BattleNextSkillModifierState nextSkillModifiers;

        private BattleCardPlayState()
        {
        }

        public BattleCardPlayState(BattleDeckState deck, int maximumMana = BattleManaState.DefaultMaximumMana)
            : this(deck, maximumMana, null, null)
        {
        }

        public BattleCardPlayState(
            BattleDeckState deck,
            int maximumMana,
            BattleCardEnchantRegistry enchants)
            : this(deck, maximumMana, enchants, null)
        {
        }

        public BattleCardPlayState(
            BattleDeckState deck,
            int maximumMana,
            BattleCardEnchantRegistry enchants,
            BattleNextSkillModifierState nextSkillModifiers)
        {
            this.deck = deck ?? throw new ArgumentNullException(nameof(deck));
            mana = new BattleManaState(maximumMana);
            this.enchants = enchants;
            this.nextSkillModifiers = nextSkillModifiers;
        }

        public BattleDeckState Deck => deck;
        public BattleManaState Mana => mana;
        public BattleCardEnchantRegistry Enchants => enchants;
        public BattleNextSkillModifierState NextSkillModifiers => nextSkillModifiers;

        public bool TryPreviewPlay(
            string battleCardId,
            out CardPlayPreview preview,
            out CardPlayFailure failure)
        {
            preview = null;
            if (!TryValidateCard(battleCardId, out BattleCardInstance card, out failure))
            {
                return false;
            }

            CardZone destination = GetPlayDestination(card.SourceCard.CardType);
            RunCardEnchantState cardEnchants = enchants?.Find(card.Ids.BattleCardId);
            int manaCost = EnchantManaCostResolver.Resolve(card, cardEnchants);
            manaCost = nextSkillModifiers?.ResolveManaCost(card, manaCost) ?? manaCost;
            if (!mana.CanSpend(manaCost))
            {
                failure = CardPlayFailure.NotEnoughMana;
                return false;
            }

            if (!deck.Zones.HasCapacity(destination))
            {
                failure = CardPlayFailure.DestinationFull;
                return false;
            }

            if (card.SourceCard.CardType == CardType.Barrier && HasSameBarrier(card))
            {
                failure = CardPlayFailure.DuplicateBarrier;
                return false;
            }

            preview = new CardPlayPreview(battleCardId, card.SourceCard.CardType, destination, manaCost);
            failure = CardPlayFailure.None;
            return true;
        }

        public bool TryConfirmPlay(CardPlayPreview preview, out CardPlayFailure failure)
        {
            if (preview == null || string.IsNullOrWhiteSpace(preview.BattleCardId))
            {
                failure = CardPlayFailure.InvalidPreview;
                return false;
            }

            if (!TryPreviewPlay(preview.BattleCardId, out CardPlayPreview current, out failure) ||
                current.CardType != preview.CardType ||
                current.Destination != preview.Destination ||
                current.ManaCost != preview.ManaCost)
            {
                if (failure == CardPlayFailure.None)
                {
                    failure = CardPlayFailure.InvalidPreview;
                }

                return false;
            }

            if (!mana.TrySpend(current.ManaCost))
            {
                failure = CardPlayFailure.NotEnoughMana;
                return false;
            }

            if (!deck.Zones.TryMove(current.BattleCardId, current.Destination, out _))
            {
                mana.Refund(current.ManaCost);
                failure = CardPlayFailure.ZoneMoveFailed;
                return false;
            }

            if (current.CardType == CardType.Skill &&
                !deck.TryResolveGraveyardMove(current.BattleCardId, enchants, true, out _))
            {
                deck.Zones.TryMove(current.BattleCardId, CardZone.Hand, out _);
                mana.Refund(current.ManaCost);
                failure = CardPlayFailure.ZoneMoveFailed;
                return false;
            }

            if (current.CardType == CardType.Skill)
            {
                BattleCardInstance confirmedSkill = deck.Zones.Find(current.BattleCardId);
                nextSkillModifiers?.TryConsumeOnConfirmedSkill(confirmedSkill, out _);
            }

            failure = CardPlayFailure.None;
            return true;
        }

        private bool TryValidateCard(
            string battleCardId,
            out BattleCardInstance card,
            out CardPlayFailure failure)
        {
            card = deck.Zones.Find(battleCardId);
            if (card == null)
            {
                failure = CardPlayFailure.CardNotFound;
                return false;
            }

            if (card.Zone != CardZone.Hand)
            {
                failure = CardPlayFailure.CardNotInHand;
                return false;
            }

            failure = CardPlayFailure.None;
            return true;
        }

        private bool HasSameBarrier(BattleCardInstance candidate)
        {
            List<BattleCardInstance> skillField = deck.Zones.GetCards(CardZone.SkillField);
            for (int i = 0; i < skillField.Count; i++)
            {
                BattleCardInstance installed = skillField[i];
                if (installed.SourceCard.CardType == CardType.Barrier &&
                    string.Equals(
                        installed.SourceCard.CatalogCardId,
                        candidate.SourceCard.CatalogCardId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static CardZone GetPlayDestination(CardType cardType)
        {
            return cardType == CardType.Monster ? CardZone.MonsterField : CardZone.SkillField;
        }
    }
}

