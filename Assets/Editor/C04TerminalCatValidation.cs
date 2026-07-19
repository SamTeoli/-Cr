using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class C04TerminalCatValidation
    {
        [MenuItem("Have a Break/Validate C04 Terminal Cat Movement Effect")]
        private static void ValidateFromMenu()
        {
            CardData card = FindCard("C04");
            bool valid = card != null && ValidateLevelThree(card) && ValidateLevelFive(card);
            if (!valid)
            {
                Debug.LogError("C04 Terminal Cat movement effect validation failed.");
            }

            EditorUtility.DisplayDialog(
                "C04 Terminal Cat Movement Effect Validation",
                valid
                    ? "C04 Terminal Cat movement effect passed."
                    : "C04 Terminal Cat movement effect failed. Check the Console.",
                "OK");
        }

        private static bool ValidateLevelThree(CardData card)
        {
            BattleMonsterState monster = CreateMonster(card, 3, "L3");
            BattleEventLog log = new();
            BattleEventRecord command = Command(log, "COMMAND-L3");
            BattleCardTurnTriggerState triggers = new();
            bool valid = C04TerminalCatResolver.TryResolve(
                Move(log, command, "ENEMY-A"), 1, monster, triggers, log, out int gained);
            valid &= gained == 2 && monster.AttackEnhancement == 2;
            valid &= !C04TerminalCatResolver.TryResolve(
                Move(log, command, "ENEMY-B"), 1, monster, triggers, log, out _);
            return valid;
        }

        private static bool ValidateLevelFive(CardData card)
        {
            BattleMonsterState monster = CreateMonster(card, 5, "L5");
            BattleEventLog log = new();
            BattleCardTurnTriggerState triggers = new();
            bool valid = ResolveNewCommand(log, triggers, monster, 1, "A");
            valid &= ResolveNewCommand(log, triggers, monster, 1, "B");
            valid &= !ResolveNewCommand(log, triggers, monster, 1, "C");
            return valid && monster.AttackEnhancement == 4;
        }

        private static bool ResolveNewCommand(
            BattleEventLog log, BattleCardTurnTriggerState triggers,
            BattleMonsterState monster, int turn, string suffix)
        {
            BattleEventRecord command = Command(log, $"COMMAND-{suffix}");
            return C04TerminalCatResolver.TryResolve(
                Move(log, command, $"ENEMY-{suffix}"), turn, monster, triggers, log, out _);
        }

        private static BattleEventRecord Command(BattleEventLog log, string cause)
        {
            return log.Record(BattleEventType.CardPlayed, cause, "SYSTEM", "SYSTEM", "ENEMY");
        }

        private static BattleEventRecord Move(
            BattleEventLog log, BattleEventRecord command, string enemyId)
        {
            return log.Record(
                BattleEventType.EnemyMoved, "C04ValidationMove",
                "SYSTEM", "SYSTEM", enemyId, parentEventId: command.EventId,
                beforeValue: 0, afterValue: 1);
        }

        private static BattleMonsterState CreateMonster(CardData card, int level, string suffix)
        {
            BattleCardInstance instance = new(
                card,
                new CardInstanceIds(card.CatalogCardId, $"OWNED-C04-{suffix}", $"BATTLE-C04-{suffix}"),
                level,
                CardZone.MonsterField);
            return new BattleMonsterState(instance);
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
