using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleTurnState
    {
        [SerializeField] private BattleCardPlayState cardPlay;
        [SerializeField] private BattleTurnPhase phase = BattleTurnPhase.BattleSetup;
        [SerializeField] private int playerTurnNumber;
        [SerializeField] private CardDrawFailure lastTurnDrawFailure = CardDrawFailure.None;

        private BattleTurnState()
        {
        }

        public BattleTurnState(BattleCardPlayState cardPlay)
        {
            this.cardPlay = cardPlay ?? throw new ArgumentNullException(nameof(cardPlay));
        }

        public BattleCardPlayState CardPlay => cardPlay;
        public BattleTurnPhase Phase => phase;
        public int PlayerTurnNumber => playerTurnNumber;
        public CardDrawFailure LastTurnDrawFailure => lastTurnDrawFailure;
        public bool CanAcceptPlayerAction => phase == BattleTurnPhase.PlayerAction;

        public bool TryBeginBattle(out BattleTurnFailure failure)
        {
            if (phase != BattleTurnPhase.BattleSetup)
            {
                failure = BattleTurnFailure.InvalidPhase;
                return false;
            }

            cardPlay.Deck.DrawStartingHand();
            phase = BattleTurnPhase.StartingHandRedraw;
            failure = BattleTurnFailure.None;
            return true;
        }

        public bool TryConfirmStartingHand(
            IEnumerable<string> selectedBattleCardIds,
            out List<BattleCardInstance> replacements,
            out StartingHandRedrawFailure redrawFailure,
            out BattleTurnFailure failure)
        {
            replacements = new List<BattleCardInstance>();
            redrawFailure = StartingHandRedrawFailure.NotAvailable;
            if (phase != BattleTurnPhase.StartingHandRedraw)
            {
                failure = BattleTurnFailure.InvalidPhase;
                return false;
            }

            if (!cardPlay.Deck.TryRedrawStartingHand(
                    selectedBattleCardIds,
                    out replacements,
                    out redrawFailure))
            {
                failure = BattleTurnFailure.StartingHandRedrawFailed;
                return false;
            }

            playerTurnNumber = 1;
            cardPlay.Mana.StartPlayerTurn();
            cardPlay.Deck.TryDrawAtPlayerTurnStart(out _, out lastTurnDrawFailure);
            phase = BattleTurnPhase.PlayerAction;
            failure = BattleTurnFailure.None;
            return true;
        }

        public bool TryBeginPlayerAction(out BattleTurnFailure failure)
        {
            if (phase != BattleTurnPhase.PlayerAction)
            {
                failure = BattleTurnFailure.InvalidPhase;
                return false;
            }

            phase = BattleTurnPhase.PlayerActionResolving;
            failure = BattleTurnFailure.None;
            return true;
        }

        public bool TryCompletePlayerAction(out BattleTurnFailure failure)
        {
            if (phase != BattleTurnPhase.PlayerActionResolving)
            {
                failure = BattleTurnFailure.InvalidPhase;
                return false;
            }

            phase = BattleTurnPhase.PlayerAction;
            failure = BattleTurnFailure.None;
            return true;
        }

        public bool TryEndPlayerTurn(out BattleTurnFailure failure)
        {
            if (phase != BattleTurnPhase.PlayerAction)
            {
                failure = BattleTurnFailure.InvalidPhase;
                return false;
            }

            cardPlay.Mana.EndPlayerTurn();
            phase = BattleTurnPhase.EnemyTurn;
            failure = BattleTurnFailure.None;
            return true;
        }

        public bool TryCompleteEnemyTurn(out BattleTurnFailure failure)
        {
            if (phase != BattleTurnPhase.EnemyTurn)
            {
                failure = BattleTurnFailure.InvalidPhase;
                return false;
            }

            playerTurnNumber++;
            cardPlay.Mana.StartPlayerTurn();
            bool drewCard = cardPlay.Deck.TryDrawAtPlayerTurnStart(out _, out CardDrawFailure drawFailure);
            lastTurnDrawFailure = drewCard ? CardDrawFailure.None : drawFailure;
            phase = BattleTurnPhase.PlayerAction;
            failure = BattleTurnFailure.None;
            return true;
        }
    }
}
