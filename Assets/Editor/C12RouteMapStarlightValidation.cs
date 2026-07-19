using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class C12RouteMapStarlightValidation
    {
        [MenuItem("Have a Break/Validate C12 Route Map Starlight Movement Effect")]
        private static void ValidateFromMenu()
        {
            CardData card = FindCard("C12");
            bool valid = card != null && ValidateLevelOne(card) && ValidateLevelFive(card);
            if (!valid)
            {
                Debug.LogError("C12 Route Map Starlight movement effect validation failed.");
            }

            EditorUtility.DisplayDialog(
                "C12 Route Map Starlight Movement Effect Validation",
                valid
                    ? "C12 Route Map Starlight movement effect passed."
                    : "C12 Route Map Starlight movement effect failed. Check the Console.",
                "OK");
        }

        private static bool ValidateLevelOne(CardData card)
        {
            BattleCardInstance source = CreateBarrier(card, 1, "L1");
            BattleEnemyStatusRegistry statuses = new();
            bool valid = statuses.TryAdd("ENEMY-A", out BattleEnemyStatusState enemy);
            BattleEventLog log = new();
            BattleEventRecord moved = Move(log, "ENEMY-A", "L1");
            valid &= C12RouteMapStarlightResolver.TryResolve(
                moved, 1, source, new BattleCardTurnTriggerState(), statuses, null, log,
                out int vulnerable, out int damage);
            return valid && vulnerable == 1 && damage == 0 && enemy.Vulnerable == 1;
        }

        private static bool ValidateLevelFive(CardData card)
        {
            BattleCardInstance source = CreateBarrier(card, 5, "L5");
            BattleEnemyStatusRegistry statuses = new();
            bool valid = statuses.TryAdd("ENEMY-A", out BattleEnemyStatusState enemy);
            BattleEnemyVitalState vital = new("ENEMY-A", 5);
            BattleEventLog log = new();
            valid &= C12RouteMapStarlightResolver.TryResolve(
                Move(log, "ENEMY-A", "L5"), 1, source, new BattleCardTurnTriggerState(),
                statuses, vital, log, out int vulnerable, out int damage);
            return valid && vulnerable == 2 && damage == 1 &&
                   enemy.Vulnerable == 2 && vital.CurrentHealth == 4;
        }

        private static BattleEventRecord Move(BattleEventLog log, string enemyId, string suffix)
        {
            BattleEventRecord command = log.Record(
                BattleEventType.CardPlayed, $"C12-COMMAND-{suffix}",
                "SYSTEM", "SYSTEM", enemyId);
            return log.Record(
                BattleEventType.EnemyMoved, "C12ValidationMove",
                "SYSTEM", "SYSTEM", enemyId, parentEventId: command.EventId,
                beforeValue: 0, afterValue: 1);
        }

        private static BattleCardInstance CreateBarrier(CardData card, int level, string suffix)
        {
            return new BattleCardInstance(
                card,
                new CardInstanceIds(card.CatalogCardId, $"OWNED-C12-{suffix}", $"BATTLE-C12-{suffix}"),
                level,
                CardZone.SkillField);
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
