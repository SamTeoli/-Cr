using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class C03SeatRepairerValidation
    {
        [MenuItem("Have a Break/Validate C03 Seat Repairer Turn End Effect")]
        private static void ValidateFromMenu()
        {
            Validate(true);
        }

        internal static bool Validate(bool showDialog)
        {
            CardData card = FindCard("C03");
            bool valid = card != null;
            if (valid)
            {
                valid &= ValidateNoAttack(card, 1, 3, 0, "L1");
                valid &= ValidateNoAttack(card, 3, 4, 0, "L3");
                valid &= ValidateNoAttack(card, 5, 4, 1, "L5");
                valid &= ValidateCompletedAttackBlocksEffect(card);
                valid &= ValidateDeclarationDoesNotBlockEffect(card);
                valid &= ValidateEarlierTurnCompletionIsIgnored(card);
            }
            else
            {
                Debug.LogError("C03 validation requires C03.");
            }

            if (!valid)
            {
                Debug.LogError("C03 Seat Repairer turn end effect validation failed.");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "C03 Seat Repairer Turn End Effect Validation",
                    valid
                        ? "C03 Seat Repairer turn end effect passed."
                        : "C03 Seat Repairer turn end effect failed. Check the Console.",
                    "OK");
            }

            return valid;
        }

        private static bool ValidateNoAttack(
            CardData card, int level, int expectedDefense, int expectedCounter, string suffix)
        {
            BattleMonsterState monster = CreateMonster(card, level, suffix, out _);
            BattleEventLog log = new();
            BattleEffectResolutionTracker tracker = new();
            bool valid = C03SeatRepairerTurnEndResolver.TryResolve(
                             monster, 1, 0, log, tracker, out C03SeatRepairerResult result) &&
                         !result.AttackedThisTurn &&
                         result.DefenseGained == expectedDefense &&
                         result.CounterGained == expectedCounter &&
                         monster.Defense == expectedDefense &&
                         monster.Counter == expectedCounter;
            valid &= !C03SeatRepairerTurnEndResolver.TryResolve(
                monster, 1, 0, log, tracker, out _);
            return valid;
        }

        private static bool ValidateCompletedAttackBlocksEffect(CardData card)
        {
            BattleMonsterState monster = CreateMonster(card, 5, "COMPLETED", out BattleCardInstance source);
            BattleEventLog log = new();
            BattleEventRecord declaration = DeclareAttack(log, source, "ENEMY-A");
            bool valid = BattleAttackEventService.TryRecordCompleted(log, declaration, out _);
            valid &= C03SeatRepairerTurnEndResolver.TryResolve(
                         monster, 1, 0, log, new BattleEffectResolutionTracker(), out var result) &&
                     result.AttackedThisTurn && result.DefenseGained == 0 &&
                     result.CounterGained == 0 && monster.Defense == 0 && monster.Counter == 0;
            return valid;
        }

        private static bool ValidateDeclarationDoesNotBlockEffect(CardData card)
        {
            BattleMonsterState monster = CreateMonster(card, 1, "DECLARED", out BattleCardInstance source);
            BattleEventLog log = new();
            DeclareAttack(log, source, "ENEMY-CANCELLED");
            return C03SeatRepairerTurnEndResolver.TryResolve(
                       monster, 1, 0, log, new BattleEffectResolutionTracker(), out var result) &&
                   !result.AttackedThisTurn && result.DefenseGained == 3 && monster.Defense == 3;
        }

        private static bool ValidateEarlierTurnCompletionIsIgnored(CardData card)
        {
            BattleMonsterState monster = CreateMonster(card, 1, "EARLIER", out BattleCardInstance source);
            BattleEventLog log = new();
            BattleEventRecord declaration = DeclareAttack(log, source, "ENEMY-OLD");
            bool valid = BattleAttackEventService.TryRecordCompleted(log, declaration, out _);
            int currentTurnStart = log.Events.Count;
            valid &= C03SeatRepairerTurnEndResolver.TryResolve(
                         monster, 2, currentTurnStart, log,
                         new BattleEffectResolutionTracker(), out var result) &&
                     !result.AttackedThisTurn && result.DefenseGained == 3;
            return valid;
        }

        private static BattleEventRecord DeclareAttack(
            BattleEventLog log, BattleCardInstance attacker, string targetId)
        {
            return log.Record(
                BattleEventType.AttackDeclared,
                "C03ValidationAttack",
                attacker.Ids.BattleCardId,
                attacker.Ids.BattleCardId,
                targetId);
        }

        private static BattleMonsterState CreateMonster(
            CardData card, int level, string suffix, out BattleCardInstance battleCard)
        {
            battleCard = new BattleCardInstance(
                card,
                new CardInstanceIds(
                    card.CatalogCardId,
                    $"OWNED-C03-{suffix}",
                    $"BATTLE-C03-{suffix}"),
                level,
                CardZone.MonsterField);
            return new BattleMonsterState(battleCard);
        }

        private static CardData FindCard(string id)
        {
            return AssetDatabase.FindAssets("t:CardData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<CardData>(path))
                .FirstOrDefault(card => card != null && string.Equals(
                    card.CatalogCardId, id, StringComparison.OrdinalIgnoreCase));
        }
    }
}
