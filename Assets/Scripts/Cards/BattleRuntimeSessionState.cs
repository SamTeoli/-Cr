using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleRuntimeSessionState
    {
        [SerializeField] private BattleRuntimeState runtime;
        [SerializeField] private bool started;
        [SerializeField] private int completedRoundCount;
        [SerializeField] private int playerTurnEventStartIndex;
        [SerializeField] private BattleOutcome outcome = BattleOutcome.Ongoing;

        private BattleRuntimeSessionState()
        {
        }

        public BattleRuntimeSessionState(BattleRuntimeState runtime)
        {
            this.runtime = runtime ??
                throw new ArgumentNullException(nameof(runtime));
        }

        public BattleRuntimeState Runtime => runtime;
        public bool Started => started;
        public int CompletedRoundCount => completedRoundCount;
        public int PlayerTurnEventStartIndex => playerTurnEventStartIndex;
        public BattleOutcome Outcome => outcome;
        public bool IsFinished => started && outcome != BattleOutcome.Ongoing;

        internal void MarkStarted(
            int firstPlayerTurnEventIndex,
            BattleOutcome initialOutcome)
        {
            started = true;
            playerTurnEventStartIndex = firstPlayerTurnEventIndex;
            outcome = initialOutcome;
        }

        internal void MarkRoundCompleted(BattleRuntimeRoundResult round)
        {
            completedRoundCount++;
            outcome = round.Outcome;
            if (round.PlayerTurnStarted)
            {
                playerTurnEventStartIndex = runtime.EventLog.Events.Count;
            }
        }
    }
}
