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
        public BattleVictoryEnchantRewardService VictoryEnchantRewards
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
    }
}
