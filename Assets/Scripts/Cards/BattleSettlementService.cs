using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleSettlementService
    {
        [SerializeField] private BattlePlayerState battlePlayer;
        [SerializeField] private BattleOutcomeEvaluator outcomeEvaluator;
        [SerializeField] private BattleEffectQueue effectQueue;
        [SerializeField] private RunBattleState runState;
        [SerializeField] private BattleRunChanges runChanges;
        [SerializeField] private bool settled;
        [SerializeField] private bool battleStateDiscarded;
        [SerializeField] private BattleOutcome settledOutcome;

        private BattleSettlementService()
        {
        }

        public BattleSettlementService(
            BattlePlayerState battlePlayer,
            BattleOutcomeEvaluator outcomeEvaluator,
            BattleEffectQueue effectQueue,
            RunBattleState runState,
            BattleRunChanges runChanges)
        {
            this.battlePlayer = battlePlayer ?? throw new ArgumentNullException(nameof(battlePlayer));
            this.outcomeEvaluator = outcomeEvaluator ?? throw new ArgumentNullException(nameof(outcomeEvaluator));
            this.effectQueue = effectQueue ?? throw new ArgumentNullException(nameof(effectQueue));
            this.runState = runState ?? throw new ArgumentNullException(nameof(runState));
            this.runChanges = runChanges ?? throw new ArgumentNullException(nameof(runChanges));
        }

        public bool IsSettled => settled;
        public bool BattleStateDiscarded => battleStateDiscarded;
        public BattleOutcome SettledOutcome => settledOutcome;
        public bool RewardEligible => settled && settledOutcome == BattleOutcome.Victory;

        public bool TrySettle(out BattleSettlementFailure failure)
        {
            if (settled)
            {
                failure = BattleSettlementFailure.AlreadySettled;
                return false;
            }

            if (effectQueue.Count > 0)
            {
                failure = BattleSettlementFailure.PendingEffects;
                return false;
            }

            BattleOutcome outcome = outcomeEvaluator.Evaluate();
            if (outcome == BattleOutcome.Ongoing)
            {
                failure = BattleSettlementFailure.BattleOngoing;
                return false;
            }

            int finalHealth = battlePlayer.CurrentHealth;
            if (!runState.CanApplySettlement(
                    finalHealth, runChanges.GoldDelta, runChanges.ConsumedItemIds))
            {
                failure = BattleSettlementFailure.InvalidRunState;
                return false;
            }

            runState.ApplySettlement(
                finalHealth,
                runChanges.GoldDelta,
                runChanges.ConsumedItemIds,
                outcome == BattleOutcome.Defeat);
            settledOutcome = outcome;
            settled = true;
            battleStateDiscarded = true;
            failure = BattleSettlementFailure.None;
            return true;
        }
    }
}
