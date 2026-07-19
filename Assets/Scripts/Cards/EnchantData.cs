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
        [SerializeField] private EnchantApplicationType applicationType;
        [SerializeField] private string role;
        [SerializeField] private List<CardType> compatibleCardTypes = new();
        [SerializeField, TextArea(2, 5)] private string additionalCompatibilityRule;
        [SerializeField] private bool allowDuplicateOnSameCard;
        [SerializeField, TextArea(2, 6)] private string rulesText;
        [SerializeField] private string sourceVersion;
        [SerializeField] private string sourceDocument;
        [SerializeField] private List<EnchantEffectData> effects = new();

        public string DefinitionId => definitionId;
        public string DisplayName => displayName;
        public CardRarity Rarity => rarity;
        public EnchantApplicationType ApplicationType => applicationType;
        public string Role => role;
        public IReadOnlyList<CardType> CompatibleCardTypes => compatibleCardTypes;
        public string AdditionalCompatibilityRule => additionalCompatibilityRule;
        public bool AllowDuplicateOnSameCard => allowDuplicateOnSameCard;
        public string RulesText => rulesText;
        public string SourceVersion => sourceVersion;
        public string SourceDocument => sourceDocument;
        public IReadOnlyList<EnchantEffectData> Effects => effects;

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
            bool allowDuplicate = false,
            EnchantApplicationType type = EnchantApplicationType.StaticModifier,
            string enchantRole = null,
            string compatibilityRule = null,
            string displayRules = null,
            string version = null,
            string document = null,
            IEnumerable<EnchantEffectData> effectList = null)
        {
            definitionId = id?.Trim();
            displayName = enchantName?.Trim();
            rarity = enchantRarity;
            applicationType = type;
            role = enchantRole;
            compatibleCardTypes = compatibleTypes == null
                ? new List<CardType>()
                : new List<CardType>(compatibleTypes);
            additionalCompatibilityRule = compatibilityRule;
            allowDuplicateOnSameCard = allowDuplicate;
            rulesText = displayRules;
            sourceVersion = version;
            sourceDocument = document;
            effects = effectList == null
                ? new List<EnchantEffectData>()
                : new List<EnchantEffectData>(effectList);
        }
#endif

        private void OnValidate()
        {
            definitionId = definitionId?.Trim();
            displayName = displayName?.Trim();
            role = role?.Trim();
            additionalCompatibilityRule = additionalCompatibilityRule?.Trim();
            rulesText = rulesText?.Trim();
            sourceVersion = sourceVersion?.Trim();
            sourceDocument = sourceDocument?.Trim();
        }
    }
}
