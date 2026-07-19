using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class C08ClosingDoorValidation
    {
        [MenuItem("Have a Break/Validate C08 Closing Door Effect")]
        private static void ValidateFromMenu()
        {
            CardData card = Find("C08");
            bool valid = card != null && Validate(card);
            if (!valid) Debug.LogError("C08 Closing Door effect validation failed.");
            EditorUtility.DisplayDialog("C08 Closing Door Effect Validation",
                valid ? "C08 Closing Door effect passed." :
                "C08 Closing Door effect failed. Check the Console.", "OK");
        }

        private static bool Validate(CardData card)
        {
            BattleCardInstance trap = Instance(card, 5, "C08", CardZone.SkillField);
            BattleEnemyStatusRegistry statuses = new();
            bool valid = statuses.TryAdd("ENEMY-A", out BattleEnemyStatusState enemy);
            BattleEventLog log = new();
            BattleEventRecord attempt = log.Record(
                BattleEventType.CardPlayed, "EnemyMoveAttempt",
                "ENEMY-A", "ENEMY-A", "ENEMY-A", beforeValue: 2);
            valid &= C08ClosingDoorResolver.TryReplace(
                attempt, 2, 2, 2, "ENEMY-A", trap, null, statuses,
                new BattleCardTurnTriggerState(), log, out int replacement);
            return valid && replacement == 0 && enemy.Bind == 2 && enemy.Weaken == 1;
        }

        private static BattleCardInstance Instance(
            CardData card, int level, string suffix, CardZone zone)
        {
            return new BattleCardInstance(card,
                new CardInstanceIds(card.CatalogCardId, $"OWNED-{suffix}", $"BATTLE-{suffix}"),
                level, zone);
        }

        private static CardData Find(string id) => AssetDatabase.FindAssets("t:CardData")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(path => AssetDatabase.LoadAssetAtPath<CardData>(path))
            .FirstOrDefault(card => card != null &&
                string.Equals(card.CatalogCardId, id, StringComparison.OrdinalIgnoreCase));
    }
}
