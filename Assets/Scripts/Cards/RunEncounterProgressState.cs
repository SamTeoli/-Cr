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
        [SerializeField] private List<string> usedBattleInstanceIds = new();
        [SerializeField] private int completedEncounterCount;
        [NonSerialized] private BattleRuntimeEncounterContext activeEncounter;

        private RunEncounterProgressState()
        {
        }

        public RunEncounterProgressState(
            RunBattleState runState,
            RunDeckState runDeck)
        {
            this.runState = runState ??
                throw new ArgumentNullException(nameof(runState));
            this.runDeck = runDeck ??
                throw new ArgumentNullException(nameof(runDeck));
        }

        public RunBattleState RunState => runState;
        public RunDeckState RunDeck => runDeck;
        public BattleRuntimeEncounterContext ActiveEncounter =>
            activeEncounter;
        public bool HasActiveEncounter => activeEncounter != null;
        public int CompletedEncounterCount => completedEncounterCount;
        public IReadOnlyList<string> UsedBattleInstanceIds =>
            usedBattleInstanceIds;

        internal bool HasUsedBattleInstanceId(string battleInstanceId)
        {
            if (string.IsNullOrWhiteSpace(battleInstanceId))
            {
                return false;
            }

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
            usedBattleInstanceIds.Add(battleInstanceId.Trim());
        }

        internal void CompleteActiveEncounter()
        {
            activeEncounter = null;
            completedEncounterCount++;
        }
    }
}
