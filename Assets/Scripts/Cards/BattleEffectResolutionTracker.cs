using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleEffectResolutionTracker
    {
        [SerializeField] private List<string> resolvedKeys = new();

        public bool TryBegin(string effectId, string sourceEventId)
        {
            if (string.IsNullOrWhiteSpace(effectId) || string.IsNullOrWhiteSpace(sourceEventId))
            {
                return false;
            }

            string key = $"{effectId.Trim()}::{sourceEventId.Trim()}";
            if (resolvedKeys.Exists(item => string.Equals(
                    item, key, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            resolvedKeys.Add(key);
            return true;
        }
    }
}
