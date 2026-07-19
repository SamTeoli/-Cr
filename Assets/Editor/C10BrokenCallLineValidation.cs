using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class C10BrokenCallLineValidation
    {
        [MenuItem("Have a Break/Validate C10 Broken Call Line Effect")]
        private static void ValidateFromMenu()
        {
            CardData card = Find("C10");
            bool valid = card != null && Validate(card);
            if (!valid) Debug.LogError("C10 Broken Call Line effect validation failed.");
            EditorUtility.DisplayDialog("C10 Broken Call Line Effect Validation",
                valid ? "C10 Broken Call Line effect passed." :
                "C10 Broken Call Line effect failed. Check the Console.", "OK");
        }

        private static bool Validate(CardData card)
        {
            BattleCardInstance trap = new(card,
                new CardInstanceIds(card.CatalogCardId, "OWNED-C10", "BATTLE-C10"),
                5, CardZone.SkillField);
            BattleCardZoneState zones = new();
            bool valid = zones.TryAdd(trap, out _);
            BattleEnemyStatusRegistry statuses = new();
            valid &= statuses.TryAdd("ENEMY-A", out BattleEnemyStatusState enemy);
            BattleEventLog log = new();
            BattleEventRecord abilityEvent = log.Record(
                BattleEventType.CardPlayed, "EnemyAbility",
                "ENEMY-A", "ENEMY-A", "PLAYER");
            EnemyAbilityResolutionContext context = new(
                "ABILITY-A", "ENEMY-A", false, true, true);
            valid &= C10BrokenCallLineResolver.TryCancel(
                abilityEvent, context, trap, zones, statuses, log,
                new BattleEffectResolutionTracker(),
                out bool cancelled, out bool returned);
            return valid && cancelled && returned && trap.Zone == CardZone.Hand &&
                   enemy.Weaken == 1;
        }

        private static CardData Find(string id) => AssetDatabase.FindAssets("t:CardData")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(path => AssetDatabase.LoadAssetAtPath<CardData>(path))
            .FirstOrDefault(card => card != null &&
                string.Equals(card.CatalogCardId, id, StringComparison.OrdinalIgnoreCase));
    }
}
