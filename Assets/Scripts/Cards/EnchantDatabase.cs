using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [CreateAssetMenu(fileName = "EnchantDatabase", menuName = "Have a Break/Enchant Database")]
    public sealed class EnchantDatabase : ScriptableObject
    {
        [SerializeField] private List<EnchantData> enchants = new();

        public IReadOnlyList<EnchantData> Enchants => enchants;

        public EnchantData Find(string definitionId)
        {
            if (string.IsNullOrWhiteSpace(definitionId))
            {
                return null;
            }

            return enchants.Find(enchant => enchant != null && string.Equals(
                enchant.DefinitionId, definitionId, StringComparison.OrdinalIgnoreCase));
        }

#if UNITY_EDITOR
        public void EditorSetEnchants(IEnumerable<EnchantData> values)
        {
            enchants = values == null ? new List<EnchantData>() : new List<EnchantData>(values);
        }
#endif
    }
}
