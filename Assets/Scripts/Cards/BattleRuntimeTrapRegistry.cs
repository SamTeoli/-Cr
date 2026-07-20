using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleRuntimeTrapRegistry
    {
        [SerializeField]
        private List<BattleRuntimeTrapInstallation> installations = new();

        public IReadOnlyList<BattleRuntimeTrapInstallation> Installations =>
            installations;

        public int Count => installations.Count;

        public bool TryAdd(BattleRuntimeTrapInstallation installation)
        {
            if (installation == null || installation.SourceTrap == null ||
                installation.SourceTrap.SourceCard == null ||
                installation.SourceTrap.SourceCard.CardType != CardType.Trap ||
                installation.SourceTrap.Zone != CardZone.SkillField ||
                string.IsNullOrWhiteSpace(installation.PlayedEventId) ||
                string.IsNullOrWhiteSpace(
                    installation.SourceTrap.Ids.BattleCardId) ||
                Find(installation.SourceTrap.Ids.BattleCardId) != null)
            {
                return false;
            }

            installations.Add(installation);
            return true;
        }

        public BattleRuntimeTrapInstallation Find(string battleCardId)
        {
            if (string.IsNullOrWhiteSpace(battleCardId))
            {
                return null;
            }

            return installations.Find(item =>
                item != null &&
                item.SourceTrap != null &&
                string.Equals(
                    item.SourceTrap.Ids.BattleCardId,
                    battleCardId,
                    StringComparison.OrdinalIgnoreCase));
        }

        public bool TryRemove(BattleRuntimeTrapInstallation installation)
        {
            return installation != null && installations.Remove(installation);
        }

        public int PruneInactive()
        {
            int removed = 0;
            for (int i = installations.Count - 1; i >= 0; i--)
            {
                BattleRuntimeTrapInstallation installation = installations[i];
                if (installation != null &&
                    installation.SourceTrap != null &&
                    installation.SourceTrap.Zone == CardZone.SkillField)
                {
                    continue;
                }

                installations.RemoveAt(i);
                removed++;
            }

            return removed;
        }
    }
}
