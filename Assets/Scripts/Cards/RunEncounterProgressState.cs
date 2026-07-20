using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class RunEncounterProgressState
    {
        [SerializeField] private RunBattleState runState;
        [SerializeField] private RunDeckState runDeck;
        [SerializeField] private PlayerPermanentRewardState permanentRewards;
        [SerializeField] private List<string> usedBattleInstanceIds = new();
        [SerializeField] private int completedEncounterCount;
        [NonSerialized] private BattleRuntimeEncounterContext activeEncounter;

        private RunEncounterProgressState()
        {
        }

        public RunEncounterProgressState(
            RunBattleState runState,
            RunDeckState runDeck)
            : this(runState, runDeck, new PlayerPermanentRewardState())
        {
        }

        public RunEncounterProgressState(
            RunBattleState runState,
            RunDeckState runDeck,
            PlayerPermanentRewardState permanentRewards)
            : this(
                runState,
                runDeck,
                permanentRewards,
                Array.Empty<string>(),
                0)
        {
        }

        public RunEncounterProgressState(
            RunBattleState runState,
            RunDeckState runDeck,
            PlayerPermanentRewardState permanentRewards,
            IEnumerable<string> completedBattleInstanceIds,
            int completedEncounterCount)
        {
            this.runState = runState ??
                throw new ArgumentNullException(nameof(runState));
            this.runDeck = runDeck ??
                throw new ArgumentNullException(nameof(runDeck));
            this.permanentRewards = permanentRewards ??
                throw new ArgumentNullException(nameof(permanentRewards));
            if (completedEncounterCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(completedEncounterCount));
            }

            if (completedBattleInstanceIds == null)
            {
                throw new ArgumentNullException(
                    nameof(completedBattleInstanceIds));
            }

            foreach (string battleInstanceId in completedBattleInstanceIds)
            {
                if (string.IsNullOrWhiteSpace(battleInstanceId) ||
                    HasUsedBattleInstanceId(battleInstanceId))
                {
                    throw new ArgumentException(
                        "Completed battle instance IDs must be non-empty and unique.",
                        nameof(completedBattleInstanceIds));
                }

                usedBattleInstanceIds.Add(battleInstanceId.Trim());
            }

            if (usedBattleInstanceIds.Count != completedEncounterCount)
            {
                throw new ArgumentException(
                    "Completed encounter count must match completed battle IDs.",
                    nameof(completedEncounterCount));
            }

            this.completedEncounterCount = completedEncounterCount;
        }

        public RunBattleState RunState => runState;
        public RunDeckState RunDeck => runDeck;
        public PlayerPermanentRewardState PermanentRewards =>
            permanentRewards ??= new PlayerPermanentRewardState();
        public BattleRuntimeEncounterContext ActiveEncounter =>
            activeEncounter;
        public bool HasActiveEncounter => activeEncounter != null;
        public int CompletedEncounterCount => completedEncounterCount;
        public IReadOnlyList<string> UsedBattleInstanceIds =>
            usedBattleInstanceIds ??= new List<string>();

        internal bool HasUsedBattleInstanceId(string battleInstanceId)
        {
            if (string.IsNullOrWhiteSpace(battleInstanceId))
            {
                return false;
            }

            usedBattleInstanceIds ??= new List<string>();
            return usedBattleInstanceIds.Exists(id => string.Equals(
                id,
                battleInstanceId.Trim(),
                StringComparison.OrdinalIgnoreCase));
        }

        internal void SetActiveEncounter(
            string battleInstanceId,
            BattleRuntimeEncounterContext context)
        {
            activeEncounter = context ??
                throw new ArgumentNullException(nameof(context));
            usedBattleInstanceIds ??= new List<string>();
            usedBattleInstanceIds.Add(battleInstanceId.Trim());
        }

        internal void CompleteActiveEncounter()
        {
            activeEncounter = null;
            completedEncounterCount++;
        }
    }
}
