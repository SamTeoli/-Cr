using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HaveABreak.Cards
{
    [CreateAssetMenu(fileName = "RunStartProgressionConfig",
        menuName = "Have a Break/Run/Start And Act Progression Config")]
    public sealed class RunStartProgressionConfig : ScriptableObject
    {
        [SerializeField, Min(1)] private int startingMaximumHealth = 30;
        [SerializeField, Min(0)] private int startingGold = 60;
        [SerializeField, Range(0, BattleManaState.MaximumManaLimit)]
        private int battleMaximumMana = BattleManaState.DefaultMaximumMana;
        [SerializeField, Min(1)] private int nodesPerAct = 4;
        [SerializeField, Min(1)] private int maximumActCount = 3;
        [SerializeField] private List<string> startingConsumableItemIds = new();

        public int StartingMaximumHealth => startingMaximumHealth;
        public int StartingGold => startingGold;
        public int BattleMaximumMana => battleMaximumMana;
        public int NodesPerAct => nodesPerAct;
        public int MaximumActCount => maximumActCount;
        public int TotalNodeCount => nodesPerAct * maximumActCount;
        public IReadOnlyList<string> StartingConsumableItemIds =>
            startingConsumableItemIds ??= new List<string>();

        public RunBattleState CreateInitialRunState()
        {
            return new RunBattleState(startingMaximumHealth,
                startingMaximumHealth, startingGold, StartingConsumableItemIds);
        }

        public int GetAct(int completedNodeCount)
        {
            return Mathf.Clamp(Mathf.Max(0, completedNodeCount) / nodesPerAct + 1,
                1, maximumActCount);
        }

        internal void EditorInitialize(int maximumHealth, int gold, int mana,
            int actNodeCount, int actCount, IEnumerable<string> consumableIds)
        {
            startingMaximumHealth = maximumHealth;
            startingGold = gold;
            battleMaximumMana = mana;
            nodesPerAct = actNodeCount;
            maximumActCount = actCount;
            startingConsumableItemIds = consumableIds == null
                ? new List<string>() : new List<string>(consumableIds);
        }

        public IReadOnlyList<string> GetValidationErrors()
        {
            List<string> errors = new();
            if (startingMaximumHealth < 1)
                errors.Add("Starting maximum health must be positive.");
            if (startingGold < 0)
                errors.Add("Starting gold cannot be negative.");
            if (battleMaximumMana < 0 ||
                battleMaximumMana > BattleManaState.MaximumManaLimit)
                errors.Add("Battle maximum mana is outside the supported range.");
            if (nodesPerAct < 1)
                errors.Add("Nodes per act must be positive.");
            if (maximumActCount < 1)
                errors.Add("Maximum act count must be positive.");
            if (StartingConsumableItemIds.Any(string.IsNullOrWhiteSpace))
                errors.Add("Starting consumable IDs cannot be empty.");
            if (StartingConsumableItemIds.Where(id => !string.IsNullOrWhiteSpace(id))
                .GroupBy(id => id.Trim(), StringComparer.OrdinalIgnoreCase)
                .Any(group => group.Count() > 1))
                errors.Add("Starting consumable IDs must be unique.");
            return errors;
        }
    }
}
