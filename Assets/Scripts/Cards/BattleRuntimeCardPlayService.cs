using System;

namespace HaveABreak.Cards
{
    public static class BattleRuntimeCardPlayService
    {
        public static bool TryPlay(
            BattleRuntimeState runtime,
            string battleCardId,
            out BattleRuntimeCardPlayResult result,
            out BattleRuntimeCardPlayFailure failure,
            out CardPlayFailure cardPlayFailure)
        {
            result = null;
            cardPlayFailure = CardPlayFailure.None;
            if (runtime == null || string.IsNullOrWhiteSpace(battleCardId))
            {
                failure = BattleRuntimeCardPlayFailure.InvalidRuntime;
                return false;
            }

            if (!runtime.Turn.CanAcceptPlayerAction)
            {
                failure = BattleRuntimeCardPlayFailure.InvalidTurnPhase;
                return false;
            }

            BattleCardInstance card = runtime.Deck.Zones.Find(battleCardId);
            if (!runtime.CardPlay.TryPreviewPlay(
                    battleCardId, out CardPlayPreview preview, out cardPlayFailure))
            {
                failure = BattleRuntimeCardPlayFailure.PreviewFailed;
                return false;
            }

            if (!runtime.Turn.TryBeginPlayerAction(out _))
            {
                failure = BattleRuntimeCardPlayFailure.BeginActionFailed;
                return false;
            }

            int manaBefore = runtime.CardPlay.Mana.CurrentMana;
            if (!runtime.CardPlay.TryConfirmPlay(preview, out cardPlayFailure))
            {
                runtime.Turn.TryCompletePlayerAction(out _);
                failure = BattleRuntimeCardPlayFailure.ConfirmFailed;
                return false;
            }

            BattleEventRecord playedEvent = runtime.EventLog.Record(
                BattleEventType.CardPlayed,
                "PlayerCardPlayConfirmed",
                card.Ids.BattleCardId,
                card.Ids.BattleCardId,
                card.Ids.BattleCardId,
                beforeValue: manaBefore,
                afterValue: runtime.CardPlay.Mana.CurrentMana);

            BattleEventRecord summonedEvent = null;
            BattleMonsterState summonedMonster = null;
            if (card.SourceCard.CardType == CardType.Monster)
            {
                if (!runtime.TryRegisterFieldMonster(
                        card.Ids.BattleCardId, out summonedMonster))
                {
                    runtime.Turn.TryCompletePlayerAction(out _);
                    failure = BattleRuntimeCardPlayFailure.MonsterRegistrationFailed;
                    return false;
                }

                summonedEvent = runtime.EventLog.Record(
                    BattleEventType.MonsterSummoned,
                    "MonsterPlayResolved",
                    card.Ids.BattleCardId,
                    card.Ids.BattleCardId,
                    card.Ids.BattleCardId,
                    parentEventId: playedEvent.EventId);
            }

            if (!runtime.Turn.TryCompletePlayerAction(out _))
            {
                failure = BattleRuntimeCardPlayFailure.CompleteActionFailed;
                return false;
            }

            result = new BattleRuntimeCardPlayResult(
                card, preview, playedEvent, summonedEvent, summonedMonster);
            failure = BattleRuntimeCardPlayFailure.None;
            return true;
        }
    }
}
