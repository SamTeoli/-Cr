using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [CreateAssetMenu(
        fileName = "ConsumableDatabase",
        menuName = "Have a Break/Consumables/Database")]
    public sealed class ConsumableDatabase : ScriptableObject
    {
        [SerializeField] private List<ConsumableData> consumables = new();

        public IReadOnlyList<ConsumableData> Consumables => consumables;

        public ConsumableData Find(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return null;
            }

            string normalizedId = itemId.Trim();
            return consumables.Find(item => item != null && string.Equals(
                item.ItemId, normalizedId, StringComparison.OrdinalIgnoreCase));
        }

        public IReadOnlyList<string> GetValidationErrors()
        {
            List<string> errors = new();
            HashSet<string> ids = new(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < consumables.Count; index++)
            {
                ConsumableData item = consumables[index];
                if (item == null)
                {
                    errors.Add($"Consumable entry {index} is empty.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(item.ItemId))
                {
                    errors.Add($"Consumable entry {index} has an empty ID.");
                }
                else if (!ids.Add(item.ItemId.Trim()))
                {
                    errors.Add($"Duplicate consumable ID: {item.ItemId}");
                }
            }

            return errors;
        }

#if UNITY_EDITOR
        public void EditorSetConsumables(IEnumerable<ConsumableData> values)
        {
            consumables = values == null
                ? new List<ConsumableData>()
                : new List<ConsumableData>(values);
        }
#endif
    }
}
