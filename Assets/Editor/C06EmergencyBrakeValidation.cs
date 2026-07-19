using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class C06EmergencyBrakeValidation
    {
        [MenuItem("Have a Break/Validate C06 Emergency Brake Effect")]
        private static void ValidateFromMenu()
        {
            CardData card = FindCard("C06");
            bool valid = card != null && ValidateLevelFive(card) && ValidateImmunity(card);
            if (!valid) Debug.LogError("C06 Emergency Brake effect validation failed.");
            EditorUtility.DisplayDialog(
                "C06 Emergency Brake Effect Validation",
                valid ? "C06 Emergency Brake effect passed." :
                    "C06 Emergency Brake effect failed. Check the Console.", "OK");
        }

        private static bool ValidateLevelFive(CardData card)
        {
            BattleCardInstance source = CreateSkill(card, 5, "L5");
            BattleEnemyStatusRegistry statuses = new();
            bool valid = statuses.TryAdd("ENEMY-A", out BattleEnemyStatusState primary);
            valid &= statuses.TryAdd("ENEMY-B", out BattleEnemyStatusState secondary);
            valid &= statuses.TryAdd("ENEMY-C", out BattleEnemyStatusState lower);
            BattleEventLog log = new();
            BattleEventRecord played = Played(log, source, "ENEMY-A");
            BattleEnemyAttackSnapshot[] enemies =
            {
                new("ENEMY-A", 9), new("ENEMY-B", 8), new("ENEMY-C", 3)
            };
            valid &= C06EmergencyBrakeResolver.TryResolve(
                played, source, "ENEMY-A", enemies, statuses, log,
                new BattleEffectResolutionTracker(), out string selected);
            return valid && selected == "ENEMY-B" && primary.Bind == 2 &&
                   primary.Weaken == 1 && secondary.Weaken == 1 && lower.Weaken == 0;
        }

        private static bool ValidateImmunity(CardData card)
        {
            BattleCardInstance source = CreateSkill(card, 2, "IMMUNE");
            BattleEnemyStatusRegistry statuses = new();
            bool valid = statuses.TryAdd("ENEMY-A", out BattleEnemyStatusState target);
            target.SetBindImmune(true);
            BattleEventLog log = new();
            valid &= C06EmergencyBrakeResolver.TryResolve(
                Played(log, source, "ENEMY-A"), source, "ENEMY-A",
                Array.Empty<BattleEnemyAttackSnapshot>(), statuses, log,
                new BattleEffectResolutionTracker(), out _);
            return valid && target.Bind == 0 && target.Weaken == 0;
        }

        private static BattleEventRecord Played(
            BattleEventLog log, BattleCardInstance source, string target)
        {
            return log.Record(
                BattleEventType.CardPlayed, "C06Validation",
                source.Ids.BattleCardId, source.Ids.BattleCardId, target);
        }

        private static BattleCardInstance CreateSkill(CardData card, int level, string suffix)
        {
            return new BattleCardInstance(
                card,
                new CardInstanceIds(card.CatalogCardId, $"OWNED-C06-{suffix}", $"BATTLE-C06-{suffix}"),
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
