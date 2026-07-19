using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class C01SleeperKeeperValidation
    {
        [MenuItem("Have a Break/Validate C01 Sleeper Keeper Summon Effect")]
        private static void ValidateFromMenu()
        {
            Validate(true);
        }

        internal static bool Validate(bool showDialog)
        {
            CardData card = FindCard("C01");
            EnchantData routePin = FindEnchant("E08");
            bool valid = card != null && routePin != null;

            if (valid)
            {
                valid &= ValidateNormalMove(card);
                valid &= ValidateLockedFailure(card, 1, 2, "LOCK-L1");
                valid &= ValidateLockedFailure(card, 4, 3, "LOCK-L4");
                valid &= ValidateLevelFiveSuccess(card);
                valid &= ValidateRoutePin(card, routePin);
                valid &= ValidateEmptyRoutePinPosition(card, routePin);
            }
            else
            {
                Debug.LogError("C01 validation requires C01 and E08.");
            }

            if (!valid)
            {
                Debug.LogError("C01 Sleeper Keeper summon effect validation failed.");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "C01 Sleeper Keeper Summon Effect Validation",
                    valid
                        ? "C01 Sleeper Keeper summon effect passed."
                        : "C01 Sleeper Keeper summon effect failed. Check the Console.",
                    "OK");
            }

            return valid;
        }

        private static bool ValidateNormalMove(CardData card)
        {
            BattleMonsterState monster = CreateMonster(card, 1, "NORMAL", out BattleCardInstance source);
            BattleEnemyPositionState positions = new();
            bool valid = positions.TryPlace("ENEMY-A", EnemyFieldPosition.Center) &&
                         positions.TryPlace("ENEMY-B", EnemyFieldPosition.Left);
            valid &= EnchantFixedTargetResolver.TryDeclare(
                source.Ids.BattleCardId, "ENEMY-A", positions, null, out var target);
            BattleEventLog log = new();
            BattleEventRecord summoned = RecordSummoned(log, source);
            BattleEffectResolutionTracker tracker = new();
            valid &= C01SleeperKeeperResolver.TryResolve(
                         summoned, monster, target, positions, null, log, tracker, out var result) &&
                     result.MovementSucceeded && result.DefenseGained == 0 &&
                     result.Moves.Count == 2 && monster.Defense == 0 &&
                     positions.GetOccupant(EnemyFieldPosition.Left) == "ENEMY-A" &&
                     positions.GetOccupant(EnemyFieldPosition.Right) == "ENEMY-B";
            valid &= !C01SleeperKeeperResolver.TryResolve(
                summoned, monster, target, positions, null, log, tracker, out _);
            return valid;
        }

        private static bool ValidateLockedFailure(
            CardData card,
            int level,
            int expectedDefense,
            string suffix)
        {
            BattleMonsterState monster = CreateMonster(card, level, suffix, out BattleCardInstance source);
            BattleEnemyPositionState positions = new();
            bool valid = positions.TryPlace("ENEMY-A", EnemyFieldPosition.Center) &&
                         positions.TryPlace("ENEMY-B", EnemyFieldPosition.Left);
            BattleEnemyMovementLockState locks = new();
            valid &= locks.TryLock("ENEMY-B");
            valid &= EnchantFixedTargetResolver.TryDeclare(
                source.Ids.BattleCardId, "ENEMY-A", positions, null, out var target);
            BattleEventLog log = new();
            valid &= C01SleeperKeeperResolver.TryResolve(
                         RecordSummoned(log, source), monster, target, positions, locks, log,
                         new BattleEffectResolutionTracker(), out var result) &&
                     !result.MovementSucceeded &&
                     result.MovementFailure == EnemyPositionMoveFailure.MovementLocked &&
                     result.DefenseGained == expectedDefense && monster.Defense == expectedDefense &&
                     positions.GetOccupant(EnemyFieldPosition.Center) == "ENEMY-A" &&
                     positions.GetOccupant(EnemyFieldPosition.Left) == "ENEMY-B";
            return valid;
        }

        private static bool ValidateLevelFiveSuccess(CardData card)
        {
            BattleMonsterState monster = CreateMonster(card, 5, "LEVEL-5", out BattleCardInstance source);
            BattleEnemyPositionState positions = new();
            bool valid = positions.TryPlace("ENEMY-A", EnemyFieldPosition.Center);
            valid &= EnchantFixedTargetResolver.TryDeclare(
                source.Ids.BattleCardId, "ENEMY-A", positions, null, out var target);
            BattleEventLog log = new();
            valid &= C01SleeperKeeperResolver.TryResolve(
                         RecordSummoned(log, source), monster, target, positions, null, log,
                         new BattleEffectResolutionTracker(), out var result) &&
                     result.MovementSucceeded && result.DefenseGained == 1 && monster.Defense == 1;
            return valid;
        }

        private static bool ValidateRoutePin(CardData card, EnchantData routePin)
        {
            BattleMonsterState monster = CreateMonster(card, 1, "E08", out BattleCardInstance source);
            BattleCardEnchantRegistry enchants = CreateRoutePinRegistry(source, routePin, out bool valid);
            BattleEnemyPositionState positions = new();
            valid &= positions.TryPlace("ENEMY-A", EnemyFieldPosition.Left) &&
                     positions.TryPlace("ENEMY-B", EnemyFieldPosition.Center);
            valid &= EnchantFixedTargetResolver.TryDeclare(
                         source.Ids.BattleCardId, "ENEMY-A", positions, enchants, out var target) &&
                     target.TargetsPosition;
            valid &= positions.TryMove("ENEMY-A", EnemyFieldPosition.Right) &&
                     positions.TryMove("ENEMY-B", EnemyFieldPosition.Left);
            BattleEventLog log = new();
            valid &= C01SleeperKeeperResolver.TryResolve(
                         RecordSummoned(log, source), monster, target, positions, null, log,
                         new BattleEffectResolutionTracker(), out var result) &&
                     result.ResolvedTargetEnemyId == "ENEMY-B" && result.MovementSucceeded;
            return valid;
        }

        private static bool ValidateEmptyRoutePinPosition(CardData card, EnchantData routePin)
        {
            BattleMonsterState monster = CreateMonster(card, 1, "E08-EMPTY", out BattleCardInstance source);
            BattleCardEnchantRegistry enchants = CreateRoutePinRegistry(source, routePin, out bool valid);
            BattleEnemyPositionState positions = new();
            valid &= positions.TryPlace("ENEMY-A", EnemyFieldPosition.Left);
            valid &= EnchantFixedTargetResolver.TryDeclare(
                source.Ids.BattleCardId, "ENEMY-A", positions, enchants, out var target);
            valid &= positions.TryMove("ENEMY-A", EnemyFieldPosition.Right);
            BattleEventLog log = new();
            valid &= C01SleeperKeeperResolver.TryResolve(
                         RecordSummoned(log, source), monster, target, positions, null, log,
                         new BattleEffectResolutionTracker(), out var result) &&
                     !result.MovementSucceeded &&
                     result.MovementFailure == EnemyPositionMoveFailure.EnemyNotFound &&
                     result.DefenseGained == 0 && monster.Defense == 0;
            return valid;
        }

        private static BattleCardEnchantRegistry CreateRoutePinRegistry(
            BattleCardInstance source,
            EnchantData routePin,
            out bool valid)
        {
            RunCardEnchantState state = new(source.SourceCard);
            valid = state.TryAttach(routePin, 0, false, out EnchantAttachmentFailure failure) &&
                    failure == EnchantAttachmentFailure.None;
            BattleCardEnchantRegistry registry = new();
            valid &= registry.TryRegister(source, state);
            return registry;
        }

        private static BattleMonsterState CreateMonster(
            CardData card,
            int level,
            string suffix,
            out BattleCardInstance battleCard)
        {
            battleCard = new BattleCardInstance(
                card,
                new CardInstanceIds(
                    card.CatalogCardId,
                    $"OWNED-C01-{suffix}",
                    $"BATTLE-C01-{suffix}"),
                level,
                CardZone.MonsterField);
            return new BattleMonsterState(battleCard);
        }

        private static BattleEventRecord RecordSummoned(
            BattleEventLog log,
            BattleCardInstance source)
        {
            return log.Record(
                BattleEventType.MonsterSummoned,
                "C01ValidationSummon",
                source.Ids.BattleCardId,
                source.Ids.BattleCardId,
                source.Ids.BattleCardId);
        }

        private static CardData FindCard(string id)
        {
            return AssetDatabase.FindAssets("t:CardData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<CardData>(path))
                .FirstOrDefault(card => card != null && string.Equals(
                    card.CatalogCardId, id, StringComparison.OrdinalIgnoreCase));
        }

        private static EnchantData FindEnchant(string id)
        {
            return AssetDatabase.FindAssets("t:EnchantData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<EnchantData>(path))
                .FirstOrDefault(enchant => enchant != null && string.Equals(
                    enchant.DefinitionId, id, StringComparison.OrdinalIgnoreCase));
        }
    }
}
