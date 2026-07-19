using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public struct CardInstanceIds
    {
        public CardInstanceIds(string catalogCardId, string ownedCardId, string battleCardId, string instantId = null)
        {
            this.catalogCardId = catalogCardId?.Trim();
            this.ownedCardId = ownedCardId?.Trim();
            this.battleCardId = battleCardId?.Trim();
            this.instantId = instantId?.Trim();
        }

        [SerializeField] private string catalogCardId;
        [SerializeField] private string ownedCardId;
        [SerializeField] private string battleCardId;
        [SerializeField] private string instantId;

        public string CatalogCardId => catalogCardId;
        public string OwnedCardId => ownedCardId;
        public string BattleCardId => battleCardId;
        public string InstantId => instantId;
        public bool IsTemporary => !string.IsNullOrWhiteSpace(instantId);
        public bool IsValid => !string.IsNullOrWhiteSpace(catalogCardId) &&
                               !string.IsNullOrWhiteSpace(battleCardId) &&
                               (IsTemporary || !string.IsNullOrWhiteSpace(ownedCardId));
    }
}
