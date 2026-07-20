using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleEncounterStartParameters
    {
        [SerializeField] private string battleInstanceId;
        [SerializeField] private string encounterId;
        [SerializeField] private int shuffleSeed;
        [SerializeField] private int maximumMana;
        [SerializeField] private List<string> startingHandRedrawIds = new();
        [SerializeField] private uint rewardSeed;

        public BattleEncounterStartParameters(
            string battleInstanceId,
            string encounterId,
            int shuffleSeed,
            int maximumMana,
            IEnumerable<string> startingHandRedrawIds,
            uint rewardSeed)
        {
            if (string.IsNullOrWhiteSpace(battleInstanceId))
            {
                throw new ArgumentException(
                    "Battle instance ID is required.",
                    nameof(battleInstanceId));
            }

            if (string.IsNullOrWhiteSpace(encounterId))
            {
                throw new ArgumentException(
                    "Encounter ID is required.",
                    nameof(encounterId));
            }

            this.battleInstanceId = battleInstanceId.Trim();
            this.encounterId = encounterId.Trim();
            this.shuffleSeed = shuffleSeed;
            this.maximumMana = maximumMana;
            this.startingHandRedrawIds = startingHandRedrawIds == null
                ? new List<string>()
                : new List<string>(startingHandRedrawIds);
            this.rewardSeed = rewardSeed;
        }

        public string BattleInstanceId => battleInstanceId;
        public string EncounterId => encounterId;
        public int ShuffleSeed => shuffleSeed;
        public int MaximumMana => maximumMana;
        public IReadOnlyList<string> StartingHandRedrawIds =>
            startingHandRedrawIds;
        public uint RewardSeed => rewardSeed;
    }
}
