using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class C05PlatformPushValidation
    {
        [MenuItem("Have a Break/Validate C05 Platform Push Effect")]
        private static void ValidateFromMenu()
        {
            CardData card = FindCard("C05");
            bool valid = card != null && Validate(card, 1, false) && Validate(card, 5, true);
            if (!valid) Debug.LogError("C05 Platform Push effect validation failed.");
            EditorUtility.DisplayDialog(
                "C05 Platform Push Effect Validation",
                valid ? "C05 Platform Push effect passed." :
                    "C05 Platform Push effect failed. Check the Console.", "OK");
        }

        private static bool Validate(CardData card, int level, bool levelFive)
        {
            BattleCardInstance source = CreateSkill(card, level, $"L{level}");
            BattleEnemyPositionState positions = new();
            bool valid = positions.TryPlace("ENEMY-A", EnemyFieldPosition.Left);
            BattleEnemyStatusRegistry statuses = new();
            valid &= statuses.TryAdd("ENEMY-A", out BattleEnemyStatusState target);
            BattleEventLog log = new();
            BattleEventRecord played = log.Record(
                BattleEventType.CardPlayed, "C05Validation",
                source.Ids.BattleCardId, source.Ids.BattleCardId, "ENEMY-A");
            valid &= C05PlatformPushResolver.TryResolve(
                played, source, "ENEMY-A", positions, null, statuses, log,
                new BattleEffectResolutionTracker(),
                out int moved, out int weaken, out int vulnerable);
            return valid && moved == (levelFive ? 2 : 1) &&
                   weaken == (levelFive ? 2 : 1) &&
                   vulnerable == (levelFive ? 1 : 0) &&
                   target.Weaken == weaken && target.Vulnerable == vulnerable;
        }

        private static BattleCardInstance CreateSkill(CardData card, int level, string suffix)
        {
            return new BattleCardInstance(
                card,
                new CardInstanceIds(card.CatalogCardId, $"OWNED-C05-{suffix}", $"BATTLE-C05-{suffix}"),
                level, CardZone.Graveyard);
        }

        private static CardData FindCard(string id)
        {
            return AssetDatabase.FindAssets("t:CardData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<CardData>(path))
                .FirstOrDefault(card => card != null &&
                    string.Equals(card.CatalogCardId, id, StringComparison.OrdinalIgnoreCase));
        }
    }
}
