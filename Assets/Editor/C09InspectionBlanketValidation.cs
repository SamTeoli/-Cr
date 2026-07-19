using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class C09InspectionBlanketValidation
    {
        [MenuItem("Have a Break/Validate C09 Inspection Blanket Effect")]
        private static void ValidateFromMenu()
        {
            CardData trapCard = Find("C09");
            CardData monsterCard = Find("C01");
            bool valid = trapCard != null && monsterCard != null && Validate(trapCard, monsterCard);
            if (!valid) Debug.LogError("C09 Inspection Blanket effect validation failed.");
            EditorUtility.DisplayDialog("C09 Inspection Blanket Effect Validation",
                valid ? "C09 Inspection Blanket effect passed." :
                "C09 Inspection Blanket effect failed. Check the Console.", "OK");
        }

        private static bool Validate(CardData trapCard, CardData monsterCard)
        {
            BattleCardInstance trap = Instance(trapCard, 5, "C09", CardZone.SkillField);
            BattleCardInstance ally = Instance(monsterCard, 1, "C09-ALLY", CardZone.MonsterField);
            BattleMonsterState monster = new(ally);
            BattleEventLog log = new();
            BattleEventRecord attack = log.Record(
                BattleEventType.AttackDeclared, "EnemyAttack",
                "ENEMY-A", "ENEMY-A", ally.Ids.BattleCardId);
            BattleDefenseRetentionState retention = new();
            bool valid = C09InspectionBlanketResolver.TryResolve(
                attack, 2, 2, trap, monster, retention,
                new BattleCardTurnTriggerState(), log, out int defense);
            return valid && defense == 5 && monster.Defense == 5 &&
                   retention.IsMarked(monster.BattleCardId);
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
