using System;

namespace HaveABreak.Cards
{
    [Serializable]
    public readonly struct CardInstanceIds
    {
        public CardInstanceIds(string catalogCardId, string ownedCardId, string battleCardId, string instantId = null)
        {
            CatalogCardId = catalogCardId;
            OwnedCardId = ownedCardId;
            BattleCardId = battleCardId;
            InstantId = instantId;
        }

        public string CatalogCardId { get; }
        public string OwnedCardId { get; }
        public string BattleCardId { get; }
        public string InstantId { get; }
        public bool IsTemporary => !string.IsNullOrWhiteSpace(InstantId);
    }
}
