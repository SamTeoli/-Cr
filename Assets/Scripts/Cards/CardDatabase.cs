using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [CreateAssetMenu(fileName = "CardDatabase", menuName = "Have a Break/Cards/Database")]
    public sealed class CardDatabase : ScriptableObject
    {
        [SerializeField] private List<CardData> cards = new();

        public IReadOnlyList<CardData> Cards => cards;

        public bool TryGetCard(string catalogCardId, out CardData card)
        {
            card = cards.Find(item => item != null &&
                string.Equals(item.CatalogCardId, catalogCardId, StringComparison.OrdinalIgnoreCase));
            return card != null;
        }

#if UNITY_EDITOR
        public void EditorSetCards(List<CardData> value)
        {
            cards = value ?? new List<CardData>();
        }
#endif
    }
}
