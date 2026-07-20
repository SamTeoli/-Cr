using System;

namespace HaveABreak.Cards
{
    public sealed class BattleRuntimeEncounterContext
    {
        internal BattleRuntimeEncounterContext(
            BattleRuntimeBootstrapResult bootstrap,
            RunBattleState runState,
            BattleRunChanges runChanges,
            BattleEffectQueue pendingSettlementEffects,
            BattleSettlementService settlement,
            BattleVictoryRewardService victoryRewards)
            : this(
                bootstrap,
                runState,
                runChanges,
                pendingSettlementEffects,
                settlement,
                victoryRewards,
                null)
        {
        }

        internal BattleRuntimeEncounterContext(
            BattleRuntimeBootstrapResult bootstrap,
            RunBattleState runState,
            BattleRunChanges runChanges,
            BattleEffectQueue pendingSettlementEffects,
            BattleSettlementService settlement,
            BattleVictoryRewardService victoryRewards,
            RunDeckBattleSnapshot deckSnapshot)
        {
            Bootstrap = bootstrap ??
                throw new ArgumentNullException(nameof(bootstrap));
            RunState = runState ??
                throw new ArgumentNullException(nameof(runState));
            RunChanges = runChanges ??
                throw new ArgumentNullException(nameof(runChanges));
            PendingSettlementEffects = pendingSettlementEffects ??
                throw new ArgumentNullException(
                    nameof(pendingSettlementEffects));
            Settlement = settlement ??
                throw new ArgumentNullException(nameof(settlement));
            VictoryRewards = victoryRewards ??
                throw new ArgumentNullException(nameof(victoryRewards));
            DeckSnapshot = deckSnapshot;
        }

        public BattleRuntimeBootstrapResult Bootstrap { get; }
        public BattleRuntimeState Runtime => Bootstrap.Runtime;
        public BattleRuntimeSessionState Session => Bootstrap.Session;
        public EncounterData Encounter => Bootstrap.Encounter;
        public RunBattleState RunState { get; }
        public BattleRunChanges RunChanges { get; }
        public BattleEffectQueue PendingSettlementEffects { get; }
        public BattleSettlementService Settlement { get; }
        public BattleVictoryRewardService VictoryRewards { get; }
        public RunDeckBattleSnapshot DeckSnapshot { get; }
        public BattleEncounterStartParameters StartParameters
        {
            get;
            private set;
        }

        public BattleVictoryEnchantRewardService VictoryEnchantRewards
        {
            get;
            private set;
        }

        public BattleVictoryConsumableRewardService VictoryConsumableRewards
        {
            get;
            private set;
        }

        public BattleVictoryPermanentRewardService VictoryPermanentRewards
        {
            get;
            private set;
        }

        internal bool TrySetVictoryEnchantRewards(
            BattleVictoryEnchantRewardService rewards)
        {
            if (rewards == null || VictoryEnchantRewards != null)
            {
                return false;
            }

            VictoryEnchantRewards = rewards;
            return true;
        }

        internal bool TrySetStartParameters(
            BattleEncounterStartParameters parameters)
        {
            if (parameters == null || StartParameters != null)
            {
                return false;
            }

            StartParameters = parameters;
            return true;
        }

        internal bool TrySetVictoryConsumableRewards(
            BattleVictoryConsumableRewardService rewards)
        {
            if (rewards == null || VictoryConsumableRewards != null)
            {
                return false;
            }

            VictoryConsumableRewards = rewards;
            return true;
        }

        internal bool TrySetVictoryPermanentRewards(
            BattleVictoryPermanentRewardService rewards)
        {
            if (rewards == null || VictoryPermanentRewards != null)
            {
                return false;
            }

            VictoryPermanentRewards = rewards;
            return true;
        }
    }
}
