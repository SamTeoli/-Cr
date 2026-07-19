using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [CreateAssetMenu(fileName = "EnchantData", menuName = "Have a Break/Enchant Data")]
    public sealed class EnchantData : ScriptableObject
    {
        [SerializeField] private string definitionId;
        [SerializeField] private string displayName;
        [SerializeField] private CardRarity rarity;
        [SerializeField] private List<CardType> compatibleCardTypes = new();
        [SerializeField] private bool allowDuplicateOnSameCard;

        public string DefinitionId => definitionId;
        public string DisplayName => displayName;
        public CardRarity Rarity => rarity;
        public IReadOnlyList<CardType> CompatibleCardTypes => compatibleCardTypes;
        public bool AllowDuplicateOnSameCard => allowDuplicateOnSameCard;

        public bool IsCompatible(CardType cardType)
        {
            return compatibleCardTypes.Contains(cardType);
        }

        public bool MatchesDefinition(EnchantData other)
        {
            if (other == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(definitionId) &&
                !string.IsNullOrWhiteSpace(other.definitionId))
            {
                return string.Equals(definitionId, other.definitionId, StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(displayName, other.displayName, StringComparison.OrdinalIgnoreCase);
        }

#if UNITY_EDITOR
        public void EditorInitialize(
            string id,
            string enchantName,
            CardRarity enchantRarity,
            IEnumerable<CardType> compatibleTypes,
            bool allowDuplicate = false)
        {
            definitionId = id?.Trim();
            displayName = enchantName?.Trim();
            rarity = enchantRarity;
            compatibleCardTypes = compatibleTypes == null
                ? new List<CardType>()
                : new List<CardType>(compatibleTypes);
            allowDuplicateOnSameCard = allowDuplicate;
        }
#endif

        private void OnValidate()
        {
            definitionId = definitionId?.Trim();
            displayName = displayName?.Trim();
        }
    }
}
