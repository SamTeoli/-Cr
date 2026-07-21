using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    // C01-C12 and E01-E08 are disposable validation fixtures. Their builders,
    // focused checks, and aggregate regression stay together so the complete
    // prototype content set can be removed or replaced as one module.
    internal static class AllCardAndEnchantRegressionValidation
    {
        private const BindingFlags PrivateStatic =
            BindingFlags.NonPublic | BindingFlags.Static;

        [MenuItem("Have a Break/Validate All Cards C01-C12 And Enchants E01-E08")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            EditorUtility.DisplayDialog(
                "All Cards And Enchants Regression Validation",
                valid
                    ? "All cards C01-C12 and enchants E01-E08 passed."
                    : "Full regression failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            CardData c01 = FindCard(TestContentIds.C01);
            CardData c04 = FindCard(TestContentIds.C04);
            CardData c05 = FindCard(TestContentIds.C05);
            CardData c06 = FindCard(TestContentIds.C06);
            CardData c07 = FindCard(TestContentIds.C07);
            CardData c08 = FindCard(TestContentIds.C08);
            CardData c09 = FindCard(TestContentIds.C09);
            CardData c10 = FindCard(TestContentIds.C10);
            CardData c11 = FindCard(TestContentIds.C11);
            CardData c12 = FindCard(TestContentIds.C12);

            bool valid = true;
            valid &= Run("C01 Sleeper Keeper", () => C01SleeperKeeperValidation.Validate(false));
            valid &= Run("C02 Lantern Bearer", () => C02LanternBearerValidation.Validate(false));
            valid &= Run("C03 Seat Repairer", () => C03SeatRepairerValidation.Validate(false));
            valid &= Run("C04 Terminal Cat", () =>
                Invoke(typeof(C04TerminalCatValidation), "ValidateLevelThree", c04) &&
                Invoke(typeof(C04TerminalCatValidation), "ValidateLevelFive", c04));
            valid &= Run("C05 Platform Push", () =>
                Invoke(typeof(C05PlatformPushValidation), "Validate", c05, 1, false) &&
                Invoke(typeof(C05PlatformPushValidation), "Validate", c05, 5, true));
            valid &= Run("C06 Emergency Brake", () =>
                Invoke(typeof(C06EmergencyBrakeValidation), "ValidateLevelFive", c06) &&
                Invoke(typeof(C06EmergencyBrakeValidation), "ValidateImmunity", c06));
            valid &= Run("C07 Lost Ticket", () =>
                Invoke(typeof(C07LostTicketValidation), "Validate", c07, c01));
            valid &= Run("C08 Closing Door", () =>
                Invoke(typeof(C08ClosingDoorValidation), "Validate", c08));
            valid &= Run("C09 Inspection Blanket", () =>
                Invoke(typeof(C09InspectionBlanketValidation), "Validate", c09, c01));
            valid &= Run("C10 Broken Call Line", () =>
                Invoke(typeof(C10BrokenCallLineValidation), "Validate", c10));
            valid &= Run("C11 Late Night Waiting Room", () =>
                Invoke(typeof(C11LateNightWaitingRoomValidation), "Validate", c11, c01));
            valid &= Run("C12 Route Map Starlight", () =>
                Invoke(typeof(C12RouteMapStarlightValidation), "ValidateLevelOne", c12) &&
                Invoke(typeof(C12RouteMapStarlightValidation), "ValidateLevelFive", c12));
            valid &= Run("E01-E08 test enchants", () =>
                AllTestEnchantBattleEffectsValidation.Validate(false));

            if (valid)
            {
                Debug.Log("Full regression C01-C12 and E01-E08 passed.");
            }
            else
            {
                Debug.LogError("Full regression C01-C12 and E01-E08 failed.");
            }

            return valid;
        }

        private static bool Invoke(Type validationType, string methodName, params object[] arguments)
        {
            MethodInfo method = validationType.GetMethod(methodName, PrivateStatic);
            if (method == null)
            {
                throw new MissingMethodException(validationType.FullName, methodName);
            }

            object result = method.Invoke(null, arguments);
            return result is bool passed && passed;
        }

        private static bool Run(string label, Func<bool> validation)
        {
            try
            {
                bool passed = validation();
                if (!passed)
                {
                    Debug.LogError($"Regression failed: {label}.");
                }

                return passed;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Regression threw an exception: {label}.\n{exception}");
                return false;
            }
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

    internal static class AllTestEnchantBattleEffectsValidation
    {
        [MenuItem("Have a Break/Validate All Test Enchant Battle Effects E01-E08")]
        private static void ValidateFromMenu()
        {
            Validate(true);
        }

        internal static bool Validate(bool showDialog)
        {
            bool valid = true;
            valid &= Run("E01-E08 data", () => TestEnchantDataBuilder.ValidateTestEnchants(false));
            valid &= Run("E01-E08 compatibility", () => TestEnchantCompatibilityBuilder.Validate(false));
            valid &= Run("E01 Warm Seat", () => EnchantWarmSeatValidation.Validate(false));
            valid &= Run("E02 Worn Handle", () => EnchantWornHandleValidation.Validate(false));
            valid &= Run("E03 Round Trip Ticket", () => EnchantRoundTripTicketValidation.Validate(false));
            valid &= Run("E04 Backup Power", () => EnchantBackupPowerValidation.Validate(false));
            valid &= Run("E05 Rusty Announcement", () => EnchantRustyAnnouncementValidation.Validate(false));
            valid &= Run("E06 Starlight Engraving", () => EnchantStarlightEngravingValidation.Validate(false));
            valid &= Run("E07 Transfer Stamp", () => EnchantTransferStampValidation.Validate(false));
            valid &= Run("E08 Route Pin", () => EnchantRoutePinValidation.Validate(false));

            if (!valid)
            {
                Debug.LogError("All test enchant battle effects validation failed.");
            }
            else
            {
                Debug.Log("All test enchant battle effects E01-E08 passed.");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "All Test Enchant Battle Effects Validation",
                    valid
                        ? "All test enchant battle effects E01-E08 passed."
                        : "All test enchant battle effects failed. Check the Console.",
                    "OK");
            }

            return valid;
        }

        private static bool Run(string label, Func<bool> validation)
        {
            try
            {
                bool passed = validation();
                if (!passed)
                {
                    Debug.LogError($"Enchant regression failed: {label}.");
                }

                return passed;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Enchant regression threw an exception: {label}.\n{exception}");
                return false;
            }
        }
    }

    internal static class C01SleeperKeeperValidation
    {
        [MenuItem("Have a Break/Validate C01 Sleeper Keeper Summon Effect")]
        private static void ValidateFromMenu()
        {
            Validate(true);
        }

        internal static bool Validate(bool showDialog)
        {
            CardData card = FindCard(TestContentIds.C01);
            EnchantData routePin = FindEnchant(TestContentIds.E08);
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
            BattleMonsterState monster = CreateMonster(card, 1, TestContentIds.E08, out BattleCardInstance source);
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

    internal static class C02LanternBearerValidation
    {
        [MenuItem("Have a Break/Validate C02 Lantern Bearer Summon Effect")]
        private static void ValidateFromMenu()
        {
            Validate(true);
        }

        internal static bool Validate(bool showDialog)
        {
            CardData c02 = FindCard(TestContentIds.C02);
            CardData skill = FindCard(TestContentIds.C05);
            bool valid = c02 != null && skill != null;
            if (valid)
            {
                valid &= ValidateLevelOne(c02, skill);
                valid &= ValidateTurnExpiry(c02);
                valid &= ValidateLevelFour(c02);
                valid &= ValidateLevelFive(c02, skill);
            }
            else
            {
                Debug.LogError("C02 validation requires C02 and C05.");
            }

            if (!valid)
            {
                Debug.LogError("C02 Lantern Bearer summon effect validation failed.");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "C02 Lantern Bearer Summon Effect Validation",
                    valid
                        ? "C02 Lantern Bearer summon effect passed."
                        : "C02 Lantern Bearer summon effect failed. Check the Console.",
                    "OK");
            }

            return valid;
        }

        private static bool ValidateLevelOne(CardData c02, CardData skillData)
        {
            BattleNextSkillModifierState modifiers = new();
            BattleMonsterState monster = CreateMonster(c02, 1, "L1", out BattleCardInstance source);
            bool valid = ResolveSummon(monster, source, modifiers, out C02LanternBearerResult result) &&
                         result.CostReduction == 1 && result.FirstNumericEffectBonus == 0 &&
                         result.DefenseGained == 0 && modifiers.PendingCount == 1;

            BattleCardInstance skill = CreateCard(skillData, 1, "SKILL-L1", CardZone.DrawPile);
            BattleDeckState deck = new(new[] { skill }, 29);
            valid &= deck.Zones.TryMove(skill.Ids.BattleCardId, CardZone.Hand, out _);
            BattleCardPlayState play = new(deck, 5, null, modifiers);
            play.Mana.StartPlayerTurn();
            int expectedCost = Mathf.Max(0, skill.Resolved.ManaCost - 1);
            valid &= play.TryPreviewPlay(skill.Ids.BattleCardId, out CardPlayPreview preview, out _) &&
                     preview.ManaCost == expectedCost && modifiers.PendingCount == 1;
            valid &= play.TryConfirmPlay(preview, out _) && modifiers.PendingCount == 0 &&
                     play.Mana.CurrentMana == play.Mana.MaximumMana - expectedCost;
            return valid;
        }

        private static bool ValidateTurnExpiry(CardData c02)
        {
            BattleNextSkillModifierState modifiers = new();
            BattleMonsterState monster = CreateMonster(c02, 1, "EXPIRY", out BattleCardInstance source);
            bool valid = ResolveSummon(monster, source, modifiers, out _) && modifiers.PendingCount == 1;
            modifiers.EndPlayerTurn();
            return valid && modifiers.PendingCount == 0;
        }

        private static bool ValidateLevelFour(CardData c02)
        {
            BattleNextSkillModifierState modifiers = new();
            BattleMonsterState monster = CreateMonster(c02, 4, "L4", out BattleCardInstance source);
            return ResolveSummon(monster, source, modifiers, out C02LanternBearerResult result) &&
                   result.DefenseGained == 1 && monster.Defense == 1;
        }

        private static bool ValidateLevelFive(CardData c02, CardData skillData)
        {
            BattleNextSkillModifierState modifiers = new();
            BattleMonsterState monster = CreateMonster(c02, 5, "L5", out BattleCardInstance source);
            bool valid = ResolveSummon(monster, source, modifiers, out C02LanternBearerResult result) &&
                         result.FirstNumericEffectBonus == 1 && result.DefenseGained == 1;

            BattleCardInstance skill = CreateCard(skillData, 1, "SKILL-L5", CardZone.DrawPile);
            BattleDeckState deck = new(new[] { skill }, 30);
            valid &= deck.Zones.TryMove(skill.Ids.BattleCardId, CardZone.Hand, out _);
            BattleCardPlayState play = new(deck, 5, null, modifiers);
            play.Mana.StartPlayerTurn();
            valid &= play.TryPreviewPlay(skill.Ids.BattleCardId, out CardPlayPreview preview, out _) &&
                     play.TryConfirmPlay(preview, out _);
            valid &= modifiers.TryTakeFirstNumericEffectBonus(skill.Ids.BattleCardId, out int bonus) &&
                     bonus == 1;
            valid &= !modifiers.TryTakeFirstNumericEffectBonus(skill.Ids.BattleCardId, out _);
            return valid;
        }

        private static bool ResolveSummon(
            BattleMonsterState monster,
            BattleCardInstance source,
            BattleNextSkillModifierState modifiers,
            out C02LanternBearerResult result)
        {
            BattleEventLog log = new();
            BattleEventRecord summoned = log.Record(
                BattleEventType.MonsterSummoned, "C02ValidationSummon",
                source.Ids.BattleCardId, source.Ids.BattleCardId, source.Ids.BattleCardId);
            BattleEffectResolutionTracker tracker = new();
            bool resolved = C02LanternBearerResolver.TryResolve(
                summoned, monster, modifiers, log, tracker, out result);
            return resolved && !C02LanternBearerResolver.TryResolve(
                summoned, monster, modifiers, log, tracker, out _);
        }

        private static BattleMonsterState CreateMonster(
            CardData card, int level, string suffix, out BattleCardInstance battleCard)
        {
            battleCard = CreateCard(card, level, suffix, CardZone.MonsterField);
            return new BattleMonsterState(battleCard);
        }

        private static BattleCardInstance CreateCard(
            CardData card, int level, string suffix, CardZone zone)
        {
            return new BattleCardInstance(
                card,
                new CardInstanceIds(card.CatalogCardId, $"OWNED-{suffix}", $"BATTLE-{suffix}"),
                level,
                zone);
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

    internal static class C03SeatRepairerValidation
    {
        [MenuItem("Have a Break/Validate C03 Seat Repairer Turn End Effect")]
        private static void ValidateFromMenu()
        {
            Validate(true);
        }

        internal static bool Validate(bool showDialog)
        {
            CardData card = FindCard(TestContentIds.C03);
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

    internal static class C04TerminalCatValidation
    {
        [MenuItem("Have a Break/Validate C04 Terminal Cat Movement Effect")]
        private static void ValidateFromMenu()
        {
            CardData card = FindCard(TestContentIds.C04);
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

    internal static class C05PlatformPushValidation
    {
        [MenuItem("Have a Break/Validate C05 Platform Push Effect")]
        private static void ValidateFromMenu()
        {
            CardData card = FindCard(TestContentIds.C05);
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

    internal static class C06EmergencyBrakeValidation
    {
        [MenuItem("Have a Break/Validate C06 Emergency Brake Effect")]
        private static void ValidateFromMenu()
        {
            CardData card = FindCard(TestContentIds.C06);
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

    internal static class C07LostTicketValidation
    {
        [MenuItem("Have a Break/Validate C07 Lost Ticket Effect")]
        private static void ValidateFromMenu()
        {
            CardData c07 = FindCard(TestContentIds.C07);
            CardData monsterCard = FindCard(TestContentIds.C01);
            bool valid = c07 != null && monsterCard != null && Validate(c07, monsterCard);
            if (!valid) Debug.LogError("C07 Lost Ticket effect validation failed.");
            EditorUtility.DisplayDialog(
                "C07 Lost Ticket Effect Validation",
                valid ? "C07 Lost Ticket effect passed." :
                    "C07 Lost Ticket effect failed. Check the Console.", "OK");
        }

        private static bool Validate(CardData c07, CardData monsterCard)
        {
            BattleCardInstance source = Instance(c07, 5, "SOURCE", CardZone.Graveyard);
            List<BattleCardInstance> deckCards = new();
            for (int i = 0; i < 5; i++)
            {
                deckCards.Add(Instance(monsterCard, 1, $"DECK-{i}", CardZone.DrawPile));
            }

            BattleDeckState deck = new(deckCards, 32);
            BattleCardInstance selected = deckCards[0];
            bool valid = deck.Zones.TryMove(selected.Ids.BattleCardId, CardZone.Hand, out _);
            BattleMonsterRegistry monsters = new();
            BattleCardInstance ally = Instance(monsterCard, 1, "ALLY", CardZone.MonsterField);
            valid &= monsters.TryAdd(ally, out BattleMonsterState allyState);
            BattleEventLog log = new();
            BattleEventRecord played = log.Record(
                BattleEventType.CardPlayed, "C07Validation",
                source.Ids.BattleCardId, source.Ids.BattleCardId, selected.Ids.BattleCardId);
            valid &= C07LostTicketResolver.TryResolve(
                played, source, selected.Ids.BattleCardId, deck, monsters, log,
                new BattleEffectResolutionTracker(),
                out int drawn, out bool banished, out int defended);
            return valid && drawn == 3 && banished && defended == 1 &&
                   selected.Zone == CardZone.Banished && allyState.Defense == 2;
        }

        private static BattleCardInstance Instance(
            CardData card, int level, string suffix, CardZone zone)
        {
            return new BattleCardInstance(
                card,
                new CardInstanceIds(card.CatalogCardId, $"OWNED-C07-{suffix}", $"BATTLE-C07-{suffix}"),
                level, zone);
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

    internal static class C08ClosingDoorValidation
    {
        [MenuItem("Have a Break/Validate C08 Closing Door Effect")]
        private static void ValidateFromMenu()
        {
            CardData card = Find(TestContentIds.C08);
            bool valid = card != null && Validate(card);
            if (!valid) Debug.LogError("C08 Closing Door effect validation failed.");
            EditorUtility.DisplayDialog("C08 Closing Door Effect Validation",
                valid ? "C08 Closing Door effect passed." :
                "C08 Closing Door effect failed. Check the Console.", "OK");
        }

        private static bool Validate(CardData card)
        {
            BattleCardInstance trap = Instance(card, 5, TestContentIds.C08, CardZone.SkillField);
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

    internal static class C09InspectionBlanketValidation
    {
        [MenuItem("Have a Break/Validate C09 Inspection Blanket Effect")]
        private static void ValidateFromMenu()
        {
            CardData trapCard = Find(TestContentIds.C09);
            CardData monsterCard = Find(TestContentIds.C01);
            bool valid = trapCard != null && monsterCard != null && Validate(trapCard, monsterCard);
            if (!valid) Debug.LogError("C09 Inspection Blanket effect validation failed.");
            EditorUtility.DisplayDialog("C09 Inspection Blanket Effect Validation",
                valid ? "C09 Inspection Blanket effect passed." :
                "C09 Inspection Blanket effect failed. Check the Console.", "OK");
        }

        private static bool Validate(CardData trapCard, CardData monsterCard)
        {
            BattleCardInstance trap = Instance(trapCard, 5, TestContentIds.C09, CardZone.SkillField);
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

    internal static class C10BrokenCallLineValidation
    {
        [MenuItem("Have a Break/Validate C10 Broken Call Line Effect")]
        private static void ValidateFromMenu()
        {
            CardData card = Find(TestContentIds.C10);
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

    internal static class C11LateNightWaitingRoomValidation
    {
        [MenuItem("Have a Break/Validate C11 Late Night Waiting Room Effect")]
        private static void ValidateFromMenu()
        {
            CardData c11 = FindCard(TestContentIds.C11);
            CardData monsterCard = FindCard(TestContentIds.C01);
            bool valid = c11 != null && monsterCard != null && Validate(c11, monsterCard);
            if (!valid) Debug.LogError("C11 Late Night Waiting Room effect validation failed.");
            EditorUtility.DisplayDialog(
                "C11 Late Night Waiting Room Effect Validation",
                valid ? "C11 Late Night Waiting Room effect passed." :
                    "C11 Late Night Waiting Room effect failed. Check the Console.", "OK");
        }

        private static bool Validate(CardData c11, CardData monsterCard)
        {
            List<BattleCardInstance> deckCards = new();
            for (int i = 0; i < 3; i++)
            {
                deckCards.Add(Instance(monsterCard, 1, $"DECK-{i}", CardZone.DrawPile));
            }

            BattleDeckState deck = new(deckCards, 33);
            BattleCardInstance source = Instance(c11, 5, "SOURCE", CardZone.SkillField);
            BattleMonsterRegistry monsters = new();
            BattleCardInstance ally = Instance(monsterCard, 1, "ALLY", CardZone.MonsterField);
            bool valid = monsters.TryAdd(ally, out BattleMonsterState allyState);
            allyState.ApplyDamage(2);
            valid &= C11LateNightWaitingRoomResolver.TryResolve(
                source, 1, deck, monsters, new BattleEventLog(),
                new BattleEffectResolutionTracker(),
                out int drawn, out string defended);
            return valid && drawn == 2 && defended == ally.Ids.BattleCardId &&
                   deck.Zones.Count(CardZone.Hand) == 2 && allyState.Defense == 1;
        }

        private static BattleCardInstance Instance(
            CardData card, int level, string suffix, CardZone zone)
        {
            return new BattleCardInstance(
                card,
                new CardInstanceIds(card.CatalogCardId, $"OWNED-C11-{suffix}", $"BATTLE-C11-{suffix}"),
                level, zone);
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

    internal static class C12RouteMapStarlightValidation
    {
        [MenuItem("Have a Break/Validate C12 Route Map Starlight Movement Effect")]
        private static void ValidateFromMenu()
        {
            CardData card = FindCard(TestContentIds.C12);
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

    internal static class EnchantBackupPowerValidation
    {
        [MenuItem("Have a Break/Validate E04 Backup Power Battle Cost")]
        private static void ValidateFromMenu()
        {
            Validate(true);
        }

        internal static bool Validate(bool showDialog)
        {
            CardData card = FindCard(TestContentIds.C06);
            EnchantData enchant = FindEnchant(TestContentIds.E04);
            bool valid = card != null && enchant != null && card.CardType == CardType.Skill;

            if (valid)
            {
                BattleCardInstance battleCard = new(
                    card,
                    new CardInstanceIds(card.CatalogCardId, "OWNED-E04-TEST", "BATTLE-E04-TEST"),
                    1,
                    CardZone.DrawPile);
                BattleDeckState deck = new(new[] { battleCard }, 1904);
                valid &= deck.DrawStartingHand() == 1 && battleCard.Zone == CardZone.Hand;

                RunCardEnchantState runEnchants = new(card);
                valid &= runEnchants.TryAttach(enchant, 0, false, out EnchantAttachmentFailure attachFailure) &&
                         attachFailure == EnchantAttachmentFailure.None;

                BattleCardEnchantRegistry registry = new();
                valid &= registry.TryRegister(battleCard, runEnchants) &&
                         !registry.TryRegister(battleCard, runEnchants) &&
                         registry.Find(battleCard.Ids.BattleCardId) == runEnchants;

                int baseCost = battleCard.Resolved.ManaCost;
                int expectedCost = Mathf.Max(1, baseCost - 1);
                BattleCardPlayState play = new(deck, BattleManaState.DefaultMaximumMana, registry);
                valid &= play.TryPreviewPlay(
                             battleCard.Ids.BattleCardId,
                             out CardPlayPreview activePreview,
                             out CardPlayFailure previewFailure) &&
                         previewFailure == CardPlayFailure.None &&
                         activePreview.ManaCost == expectedCost;

                runEnchants.RefreshCompatibility(CardType.Monster);
                valid &= EnchantManaCostResolver.Resolve(battleCard, runEnchants) == baseCost &&
                         !play.TryConfirmPlay(activePreview, out CardPlayFailure staleFailure) &&
                         staleFailure == CardPlayFailure.InvalidPreview &&
                         play.Mana.CurrentMana == BattleManaState.DefaultMaximumMana &&
                         battleCard.Zone == CardZone.Hand;

                runEnchants.RefreshCompatibility(CardType.Skill);
                valid &= play.TryConfirmPlay(activePreview, out CardPlayFailure confirmFailure) &&
                         confirmFailure == CardPlayFailure.None &&
                         play.Mana.CurrentMana == BattleManaState.DefaultMaximumMana - expectedCost &&
                         battleCard.Zone == CardZone.Graveyard;
            }
            else
            {
                Debug.LogError("E04 battle cost validation requires C06 and E04.");
            }

            if (!valid)
            {
                Debug.LogError("E04 Backup Power battle cost validation failed.");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "E04 Backup Power Battle Cost Validation",
                    valid
                        ? "E04 Backup Power battle cost passed."
                        : "E04 Backup Power battle cost failed. Check the Console.",
                    "OK");
            }

            return valid;
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

    internal static class EnchantRoundTripTicketValidation
    {
        [MenuItem("Have a Break/Validate E03 Round Trip Ticket Main Effect Draw")]
        private static void ValidateFromMenu()
        {
            Validate(true);
        }

        internal static bool Validate(bool showDialog)
        {
            CardData sourceCard = FindCard(TestContentIds.C05);
            CardData fillerCard = FindCard(TestContentIds.C01);
            EnchantData enchant = FindEnchant(TestContentIds.E03);
            bool valid = sourceCard != null && fillerCard != null && enchant != null;

            if (valid)
            {
                valid &= ValidateSuccessfulDraws(sourceCard, fillerCard, enchant);
                valid &= ValidateFullHand(sourceCard, fillerCard, enchant);
            }
            else
            {
                Debug.LogError("E03 draw validation requires C05, C01 and E03.");
            }

            if (!valid)
            {
                Debug.LogError("E03 Round Trip Ticket main effect draw validation failed.");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "E03 Round Trip Ticket Main Effect Draw Validation",
                    valid
                        ? "E03 Round Trip Ticket main effect draw passed."
                        : "E03 Round Trip Ticket main effect draw failed. Check the Console.",
                    "OK");
            }

            return valid;
        }

        private static bool ValidateSuccessfulDraws(
            CardData sourceCard,
            CardData fillerCard,
            EnchantData enchant)
        {
            BattleCardInstance source = CreateCard(sourceCard, "SOURCE", CardZone.Graveyard);
            BattleCardEnchantRegistry registry = CreateRegistry(source, enchant, out bool valid);
            BattleDeckState deck = new(new[]
            {
                CreateCard(fillerCard, "DRAW-1", CardZone.DrawPile),
                CreateCard(fillerCard, "DRAW-2", CardZone.DrawPile)
            }, 2403);
            BattleManaState zeroMana = new(0);
            BattleEventLog log = new();
            EnchantTurnUsageTracker usage = new();

            BattleEventRecord firstRoot = RecordRoot(log, source, "ROOT-1");
            valid &= BattleMainEffectEventService.TryRecordCompleted(
                log, firstRoot, source.Ids.BattleCardId, out BattleEventRecord firstCompleted);
            valid &= !BattleMainEffectEventService.TryRecordCompleted(
                log, firstRoot, source.Ids.BattleCardId, out _);
            valid &= EnchantRoundTripTicketResolver.TryResolve(
                         firstCompleted, 1, zeroMana, deck, registry, usage, log,
                         out BattleCardInstance firstDrawn,
                         out BattleEventRecord firstDrawEvent,
                         out CardDrawFailure firstFailure) &&
                     firstFailure == CardDrawFailure.None &&
                     firstDrawn != null && firstDrawn.Zone == CardZone.Hand &&
                     firstDrawEvent.EventType == BattleEventType.CardMoved &&
                     firstDrawEvent.ParentEventId == firstCompleted.EventId &&
                     firstDrawEvent.SourceEffectId == TestContentIds.E03 &&
                     firstDrawEvent.FromZone == CardZone.DrawPile &&
                     firstDrawEvent.ToZone == CardZone.Hand;
            valid &= !EnchantRoundTripTicketResolver.TryResolve(
                firstCompleted, 2, zeroMana, deck, registry, usage, log, out _, out _, out _);

            BattleEventRecord sameTurnRoot = RecordRoot(log, source, "ROOT-SAME-TURN");
            valid &= BattleMainEffectEventService.TryRecordCompleted(
                log, sameTurnRoot, source.Ids.BattleCardId, out BattleEventRecord sameTurnCompleted);
            valid &= !EnchantRoundTripTicketResolver.TryResolve(
                sameTurnCompleted, 1, zeroMana, deck, registry, usage, log, out _, out _, out _);

            BattleEventRecord nextTurnRoot = RecordRoot(log, source, "ROOT-2");
            valid &= BattleMainEffectEventService.TryRecordCompleted(
                log, nextTurnRoot, source.Ids.BattleCardId, out BattleEventRecord nextTurnCompleted);
            valid &= EnchantRoundTripTicketResolver.TryResolve(
                         nextTurnCompleted, 2, zeroMana, deck, registry, usage, log,
                         out BattleCardInstance secondDrawn, out _, out CardDrawFailure secondFailure) &&
                     secondFailure == CardDrawFailure.None && secondDrawn != null &&
                     deck.DrawPileOrder.Count == 0;

            BattleManaState remainingMana = new(1);
            BattleEventRecord manaRoot = RecordRoot(log, source, "ROOT-MANA");
            valid &= BattleMainEffectEventService.TryRecordCompleted(
                log, manaRoot, source.Ids.BattleCardId, out BattleEventRecord manaCompleted);
            valid &= !EnchantRoundTripTicketResolver.TryResolve(
                manaCompleted, 3, remainingMana, deck, registry, usage, log, out _, out _, out _);
            return valid;
        }

        private static bool ValidateFullHand(
            CardData sourceCard,
            CardData fillerCard,
            EnchantData enchant)
        {
            BattleCardInstance source = CreateCard(sourceCard, "FULL-SOURCE", CardZone.Graveyard);
            BattleCardEnchantRegistry registry = CreateRegistry(source, enchant, out bool valid);
            List<BattleCardInstance> fillers = new();
            for (int i = 0; i < 11; i++)
            {
                fillers.Add(CreateCard(fillerCard, $"FULL-{i}", CardZone.DrawPile));
            }

            BattleDeckState deck = new(fillers, 2413);
            valid &= deck.DrawStartingHand() == 5;
            for (int i = 0; i < 5; i++)
            {
                valid &= deck.TryDraw(out _, out _);
            }

            valid &= deck.Zones.GetCards(CardZone.Hand).Count == 10 && deck.DrawPileOrder.Count == 1;
            string topBefore = deck.DrawPileOrder[0];
            BattleEventLog log = new();
            BattleEventRecord root = RecordRoot(log, source, "ROOT-FULL");
            valid &= BattleMainEffectEventService.TryRecordCompleted(
                log, root, source.Ids.BattleCardId, out BattleEventRecord completed);
            EnchantTurnUsageTracker usage = new();
            valid &= EnchantRoundTripTicketResolver.TryResolve(
                         completed, 1, new BattleManaState(0), deck, registry, usage, log,
                         out BattleCardInstance drawn, out BattleEventRecord drawEvent,
                         out CardDrawFailure failure) &&
                     failure == CardDrawFailure.HandFull && drawn == null && drawEvent == null &&
                     deck.DrawPileOrder.Count == 1 && deck.DrawPileOrder[0] == topBefore;
            valid &= !EnchantRoundTripTicketResolver.TryResolve(
                completed, 2, new BattleManaState(0), deck, registry, usage, log,
                out _, out _, out _);
            return valid;
        }

        private static BattleCardEnchantRegistry CreateRegistry(
            BattleCardInstance source,
            EnchantData enchant,
            out bool valid)
        {
            RunCardEnchantState state = new(source.SourceCard);
            valid = state.TryAttach(enchant, 0, false, out EnchantAttachmentFailure failure) &&
                    failure == EnchantAttachmentFailure.None;
            BattleCardEnchantRegistry registry = new();
            valid &= registry.TryRegister(source, state);
            return registry;
        }

        private static BattleEventRecord RecordRoot(
            BattleEventLog log,
            BattleCardInstance source,
            string cause)
        {
            return log.Record(
                BattleEventType.CardPlayed,
                cause,
                source.Ids.BattleCardId,
                source.Ids.BattleCardId,
                source.Ids.BattleCardId);
        }

        private static BattleCardInstance CreateCard(CardData card, string suffix, CardZone zone)
        {
            return new BattleCardInstance(
                card,
                new CardInstanceIds(
                    card.CatalogCardId,
                    $"OWNED-E03-{suffix}",
                    $"BATTLE-E03-{suffix}"),
                1,
                zone);
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

    internal static class EnchantRoutePinValidation
    {
        [MenuItem("Have a Break/Validate E08 Route Pin Position Target")]
        private static void ValidateFromMenu()
        {
            Validate(true);
        }

        internal static bool Validate(bool showDialog)
        {
            CardData card = FindCard(TestContentIds.C01);
            EnchantData enchant = FindEnchant(TestContentIds.E08);
            bool valid = card != null && enchant != null;

            if (valid)
            {
                BattleCardInstance source = new(
                    card,
                    new CardInstanceIds(card.CatalogCardId, "OWNED-E08", "BATTLE-E08"),
                    1,
                    CardZone.MonsterField);
                RunCardEnchantState runEnchants = new(card);
                valid &= runEnchants.TryAttach(enchant, 0, false, out EnchantAttachmentFailure failure) &&
                         failure == EnchantAttachmentFailure.None;
                BattleCardEnchantRegistry registry = new();
                valid &= registry.TryRegister(source, runEnchants);

                BattleEnemyPositionState positions = new();
                valid &= positions.TryPlace("ENEMY-A", EnemyFieldPosition.Left) &&
                         positions.TryPlace("ENEMY-B", EnemyFieldPosition.Center) &&
                         !positions.TryPlace("ENEMY-C", EnemyFieldPosition.Left);

                valid &= EnchantFixedTargetResolver.TryDeclare(
                             source.Ids.BattleCardId,
                             "ENEMY-A",
                             positions,
                             registry,
                             out EnchantFixedTargetDeclaration positionTarget) &&
                         positionTarget.TargetsPosition &&
                         positionTarget.Position == EnemyFieldPosition.Left;

                valid &= positions.TryMove("ENEMY-A", EnemyFieldPosition.Right) &&
                         positions.TryMove("ENEMY-B", EnemyFieldPosition.Left) &&
                         EnchantFixedTargetResolver.Resolve(positionTarget, positions) == "ENEMY-B";

                valid &= positions.TryMove("ENEMY-B", EnemyFieldPosition.Center) &&
                         EnchantFixedTargetResolver.Resolve(positionTarget, positions) == null;

                valid &= EnchantFixedTargetResolver.TryDeclare(
                             source.Ids.BattleCardId,
                             "ENEMY-A",
                             positions,
                             null,
                             out EnchantFixedTargetDeclaration fixedTarget) &&
                         !fixedTarget.TargetsPosition;
                valid &= positions.TryMove("ENEMY-A", EnemyFieldPosition.Left) &&
                         EnchantFixedTargetResolver.Resolve(fixedTarget, positions) == "ENEMY-A";

                valid &= !EnchantFixedTargetResolver.TryDeclare(
                    source.Ids.BattleCardId,
                    "ENEMY-NOT-PLACED",
                    positions,
                    registry,
                    out _);
            }
            else
            {
                Debug.LogError("E08 position target validation requires C01 and E08.");
            }

            if (!valid)
            {
                Debug.LogError("E08 Route Pin position target validation failed.");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "E08 Route Pin Position Target Validation",
                    valid
                        ? "E08 Route Pin position target passed."
                        : "E08 Route Pin position target failed. Check the Console.",
                    "OK");
            }

            return valid;
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

    internal static class EnchantRustyAnnouncementValidation
    {
        [MenuItem("Have a Break/Validate E05 Rusty Announcement Affected Enemies")]
        private static void ValidateFromMenu()
        {
            Validate(true);
        }

        internal static bool Validate(bool showDialog)
        {
            CardData card = FindCard(TestContentIds.C08);
            EnchantData enchant = FindEnchant(TestContentIds.E05);
            bool valid = card != null && enchant != null && card.CardType == CardType.Trap;

            if (valid)
            {
                BattleCardInstance source = new(
                    card,
                    new CardInstanceIds(card.CatalogCardId, "OWNED-E05", "BATTLE-E05"),
                    1,
                    CardZone.SkillField);
                RunCardEnchantState runEnchants = new(card);
                valid &= runEnchants.TryAttach(enchant, 0, false, out EnchantAttachmentFailure failure) &&
                         failure == EnchantAttachmentFailure.None;
                BattleCardEnchantRegistry enchantRegistry = new();
                valid &= enchantRegistry.TryRegister(source, runEnchants);

                BattleEnemyTracker living = new();
                valid &= living.TryAdd("ENEMY-A") && living.TryAdd("ENEMY-B") && living.TryAdd("ENEMY-C");
                BattleEnemyStatusRegistry statuses = new();
                bool addedA = statuses.TryAdd("ENEMY-A", out BattleEnemyStatusState enemyA);
                bool addedB = statuses.TryAdd("ENEMY-B", out BattleEnemyStatusState enemyB);
                bool addedC = statuses.TryAdd("ENEMY-C", out BattleEnemyStatusState enemyC);
                valid &= addedA && addedB && addedC;

                BattleEventLog log = new();
                BattleEventRecord root = RecordRoot(log, source, "E05-ROOT");
                valid &= BattleMainEffectEventService.TryRecordCompleted(
                    log, root, source.Ids.BattleCardId, out BattleEventRecord completed);
                valid &= BattleEffectImpactService.TryCreate(
                    completed,
                    source.Ids.BattleCardId,
                    new[] { "ENEMY-A", "ENEMY-B" },
                    living,
                    out BattleEffectImpactRecord impact);
                valid &= EnchantRustyAnnouncementResolver.TryResolve(
                             completed, impact, enchantRegistry, statuses, log, out var weakenEvents) &&
                         weakenEvents.Count == 2 &&
                         enemyA.Weaken == 1 && enemyB.Weaken == 1 && enemyC.Weaken == 0 &&
                         weakenEvents.All(item =>
                             item.EventType == BattleEventType.StatusApplied &&
                             item.ParentEventId == completed.EventId &&
                             item.SourceEffectId == TestContentIds.E05 &&
                             item.BeforeValue == 0 && item.AfterValue == 1);

                int eventCountAfterFirstResolution = log.Events.Count;
                valid &= !EnchantRustyAnnouncementResolver.TryResolve(
                             completed, impact, enchantRegistry, statuses, log, out _) &&
                         log.Events.Count == eventCountAfterFirstResolution &&
                         enemyA.Weaken == 1 && enemyB.Weaken == 1;

                valid &= !BattleEffectImpactService.TryCreate(
                    completed,
                    source.Ids.BattleCardId,
                    new[] { "ENEMY-NOT-LIVING" },
                    living,
                    out _);

                BattleEventRecord emptyRoot = RecordRoot(log, source, "E05-EMPTY");
                valid &= BattleMainEffectEventService.TryRecordCompleted(
                    log, emptyRoot, source.Ids.BattleCardId, out BattleEventRecord emptyCompleted);
                valid &= BattleEffectImpactService.TryCreate(
                    emptyCompleted,
                    source.Ids.BattleCardId,
                    Array.Empty<string>(),
                    living,
                    out BattleEffectImpactRecord emptyImpact);
                valid &= !EnchantRustyAnnouncementResolver.TryResolve(
                    emptyCompleted, emptyImpact, enchantRegistry, statuses, log, out _);

                runEnchants.RefreshCompatibility(CardType.Skill);
                BattleEventRecord inactiveRoot = RecordRoot(log, source, "E05-INACTIVE");
                valid &= BattleMainEffectEventService.TryRecordCompleted(
                    log, inactiveRoot, source.Ids.BattleCardId, out BattleEventRecord inactiveCompleted);
                valid &= BattleEffectImpactService.TryCreate(
                    inactiveCompleted,
                    source.Ids.BattleCardId,
                    new[] { "ENEMY-C" },
                    living,
                    out BattleEffectImpactRecord inactiveImpact);
                valid &= !EnchantRustyAnnouncementResolver.TryResolve(
                             inactiveCompleted, inactiveImpact, enchantRegistry, statuses, log, out _) &&
                         enemyC.Weaken == 0;
            }
            else
            {
                Debug.LogError("E05 affected enemy validation requires C08 and E05.");
            }

            if (!valid)
            {
                Debug.LogError("E05 Rusty Announcement affected enemy validation failed.");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "E05 Rusty Announcement Affected Enemies Validation",
                    valid
                        ? "E05 Rusty Announcement affected enemies passed."
                        : "E05 Rusty Announcement affected enemies failed. Check the Console.",
                    "OK");
            }

            return valid;
        }

        private static BattleEventRecord RecordRoot(
            BattleEventLog log,
            BattleCardInstance source,
            string cause)
        {
            return log.Record(
                BattleEventType.CardPlayed,
                cause,
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

    internal static class EnchantStarlightEngravingValidation
    {
        [MenuItem("Have a Break/Validate E06 Starlight Engraving Repeated Value")]
        private static void ValidateFromMenu()
        {
            Validate(true);
        }

        internal static bool Validate(bool showDialog)
        {
            EnchantData enchant = FindEnchant(TestContentIds.E06);
            CardData c11 = FindCard(TestContentIds.C11);
            CardData c12 = FindCard(TestContentIds.C12);
            bool valid = enchant != null && c11 != null && c12 != null;

            if (valid)
            {
                valid &= ValidateCard(c11, enchant, 2011);
                valid &= ValidateCard(c12, enchant, 2012);
            }
            else
            {
                Debug.LogError("E06 repeated value validation requires C11, C12 and E06.");
            }

            if (!valid)
            {
                Debug.LogError("E06 Starlight Engraving repeated value validation failed.");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "E06 Starlight Engraving Repeated Value Validation",
                    valid
                        ? "E06 Starlight Engraving repeated value passed."
                        : "E06 Starlight Engraving repeated value failed. Check the Console.",
                    "OK");
            }

            return valid;
        }

        private static bool ValidateCard(CardData card, EnchantData enchant, int suffix)
        {
            BattleCardInstance battleCard = new(
                card,
                new CardInstanceIds(
                    card.CatalogCardId,
                    $"OWNED-E06-{suffix}",
                    $"BATTLE-E06-{suffix}"),
                1,
                CardZone.SkillField);
            RunCardEnchantState runEnchants = new(card);
            bool valid = runEnchants.TryAttach(
                enchant, 0, false, out EnchantAttachmentFailure attachFailure) &&
                         attachFailure == EnchantAttachmentFailure.None;

            BattleCardEnchantRegistry registry = new();
            valid &= registry.TryRegister(battleCard, runEnchants);

            RepeatedEffectParameters original = new(7, 3, 2, 4);
            RepeatedEffectParameters active = EnchantRepeatedEffectResolver.Resolve(
                battleCard, registry, original);
            valid &= active.FirstValue == 8 &&
                     active.TargetCount == original.TargetCount &&
                     active.ActivationCount == original.ActivationCount &&
                     active.ConditionThreshold == original.ConditionThreshold;

            runEnchants.RefreshCompatibility(CardType.Skill);
            RepeatedEffectParameters inactive = EnchantRepeatedEffectResolver.Resolve(
                battleCard, registry, original);
            valid &= inactive.FirstValue == original.FirstValue &&
                     inactive.TargetCount == original.TargetCount &&
                     inactive.ActivationCount == original.ActivationCount &&
                     inactive.ConditionThreshold == original.ConditionThreshold;

            runEnchants.RefreshCompatibility(CardType.Barrier);
            RepeatedEffectParameters restored = EnchantRepeatedEffectResolver.Resolve(
                battleCard, registry, original);
            valid &= restored.FirstValue == original.FirstValue + 1;

            RepeatedEffectParameters unregistered = EnchantRepeatedEffectResolver.Resolve(
                battleCard, null, original);
            valid &= unregistered.FirstValue == original.FirstValue;
            return valid;
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

    internal static class EnchantTransferStampValidation
    {
        [MenuItem("Have a Break/Validate E07 Transfer Stamp Graveyard Replacement")]
        private static void ValidateFromMenu()
        {
            Validate(true);
        }

        internal static bool Validate(bool showDialog)
        {
            CardData card = FindCard(TestContentIds.C05);
            EnchantData enchant = FindEnchant(TestContentIds.E07);
            bool valid = card != null && enchant != null && card.CardType == CardType.Skill;

            if (valid)
            {
                valid &= ValidatePlayedSkill(card, enchant);
                valid &= ValidateNonResolutionMove(card, enchant);
            }
            else
            {
                Debug.LogError("E07 graveyard replacement validation requires C05 and E07.");
            }

            if (!valid)
            {
                Debug.LogError("E07 Transfer Stamp graveyard replacement validation failed.");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "E07 Transfer Stamp Graveyard Replacement Validation",
                    valid
                        ? "E07 Transfer Stamp graveyard replacement passed."
                        : "E07 Transfer Stamp graveyard replacement failed. Check the Console.",
                    "OK");
            }

            return valid;
        }

        private static bool ValidatePlayedSkill(CardData card, EnchantData enchant)
        {
            BattleCardInstance battleCard = CreateBattleCard(card, "PLAY");
            BattleDeckState deck = new(new[] { battleCard }, 2107);
            bool valid = deck.DrawStartingHand() == 1;
            RunCardEnchantState runEnchants = CreateEnchantState(card, enchant, ref valid);
            BattleCardEnchantRegistry registry = new();
            valid &= registry.TryRegister(battleCard, runEnchants);

            BattleCardPlayState play = new(deck, BattleManaState.DefaultMaximumMana, registry);
            valid &= play.TryPreviewPlay(battleCard.Ids.BattleCardId, out CardPlayPreview firstPreview, out _) &&
                     play.TryConfirmPlay(firstPreview, out _) &&
                     battleCard.Zone == CardZone.DrawPile &&
                     deck.DrawPileOrder.Count == 1 &&
                     deck.DrawPileOrder[deck.DrawPileOrder.Count - 1] == battleCard.Ids.BattleCardId &&
                     !registry.HasAvailableTransferStamp(battleCard.Ids.BattleCardId);

            valid &= deck.TryDraw(out BattleCardInstance redrawn, out _) && redrawn == battleCard;
            play.Mana.StartPlayerTurn();
            valid &= play.TryPreviewPlay(battleCard.Ids.BattleCardId, out CardPlayPreview secondPreview, out _) &&
                     play.TryConfirmPlay(secondPreview, out _) &&
                     battleCard.Zone == CardZone.Graveyard &&
                     deck.DrawPileOrder.Count == 0;
            return valid;
        }

        private static bool ValidateNonResolutionMove(CardData card, EnchantData enchant)
        {
            BattleCardInstance battleCard = CreateBattleCard(card, "FORCED");
            BattleDeckState deck = new(new[] { battleCard }, 2117);
            bool valid = deck.DrawStartingHand() == 1;
            RunCardEnchantState runEnchants = CreateEnchantState(card, enchant, ref valid);
            BattleCardEnchantRegistry registry = new();
            valid &= registry.TryRegister(battleCard, runEnchants);

            BattleEventLog log = new();
            BattleEventRecord root = log.Record(
                BattleEventType.CardPlayed,
                "E07Validation",
                battleCard.Ids.BattleCardId,
                battleCard.Ids.BattleCardId,
                battleCard.Ids.BattleCardId);
            BattleEffectCommand forcedMove = new(
                "E07-FORCED-MOVE",
                battleCard.Ids.BattleCardId,
                root.EventId,
                EffectProcessingStage.MainEffect,
                true,
                BattleEventType.CardPlayed,
                operation: EffectOperation.Move,
                targetBattleCardId: battleCard.Ids.BattleCardId,
                destinationZone: CardZone.Graveyard,
                hasDestinationZone: true,
                normalResolutionGraveyardMove: false);
            BattleEffectQueue queue = new();
            valid &= queue.TryRegister(forcedMove, root, out _);
            BattleEffectExecutor executor = new(deck, log, queue, enchants: registry);
            valid &= executor.TryExecuteNext(out _, out BattleEventRecord moved, out _) &&
                     moved.ToZone == CardZone.Graveyard &&
                     battleCard.Zone == CardZone.Graveyard &&
                     registry.HasAvailableTransferStamp(battleCard.Ids.BattleCardId);
            return valid;
        }

        private static BattleCardInstance CreateBattleCard(CardData card, string suffix)
        {
            return new BattleCardInstance(
                card,
                new CardInstanceIds(
                    card.CatalogCardId,
                    $"OWNED-E07-{suffix}",
                    $"BATTLE-E07-{suffix}"),
                1,
                CardZone.DrawPile);
        }

        private static RunCardEnchantState CreateEnchantState(
            CardData card,
            EnchantData enchant,
            ref bool valid)
        {
            RunCardEnchantState state = new(card);
            valid &= state.TryAttach(enchant, 0, false, out EnchantAttachmentFailure failure) &&
                     failure == EnchantAttachmentFailure.None;
            return state;
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

    internal static class EnchantWarmSeatValidation
    {
        [MenuItem("Have a Break/Validate E01 Warm Seat Battle Effect")]
        private static void ValidateFromMenu()
        {
            Validate(true);
        }

        internal static bool Validate(bool showDialog)
        {
            CardData card = FindCard(TestContentIds.C01);
            EnchantData enchant = FindEnchant(TestContentIds.E01);
            bool valid = card != null && enchant != null;

            if (valid)
            {
                BattleCardInstance battleCard = new(
                    card,
                    new CardInstanceIds(card.CatalogCardId, "OWNED-E01-TEST", "BATTLE-E01-TEST"),
                    1,
                    CardZone.MonsterField);
                int baseHealth = battleCard.Resolved.Health;
                RunCardEnchantState runEnchants = new(card);

                valid &= runEnchants.TryAttach(enchant, 0, false, out EnchantAttachmentFailure failure) &&
                         failure == EnchantAttachmentFailure.None;

                BattleMonsterRegistry registry = new();
                valid &= registry.TryAdd(battleCard, runEnchants, out BattleMonsterState monster) &&
                         monster.BaseMaximumHealth == baseHealth &&
                         monster.MaximumHealth == baseHealth + 2 &&
                         monster.CurrentHealth == baseHealth + 2;

                monster.ApplyDamage(1);
                runEnchants.RefreshCompatibility(CardType.Skill);
                valid &= monster.ApplyEnchantState(runEnchants) &&
                         monster.MaximumHealth == baseHealth &&
                         monster.CurrentHealth == baseHealth;

                runEnchants.RefreshCompatibility(CardType.Monster);
                valid &= monster.ApplyEnchantState(runEnchants) &&
                         monster.MaximumHealth == baseHealth + 2 &&
                         monster.CurrentHealth == baseHealth + 2;

                valid &= monster.ApplyEnchantState(runEnchants) &&
                         monster.MaximumHealth == baseHealth + 2 &&
                         monster.CurrentHealth == baseHealth + 2;

                RunCardEnchantState wrongCardState = new(FindCard(TestContentIds.C02));
                valid &= !monster.ApplyEnchantState(wrongCardState) &&
                         monster.MaximumHealth == baseHealth + 2;
            }
            else
            {
                Debug.LogError("E01 battle effect validation requires C01 and E01.");
            }

            if (!valid)
            {
                Debug.LogError("E01 Warm Seat battle effect validation failed.");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "E01 Warm Seat Battle Effect Validation",
                    valid
                        ? "E01 Warm Seat battle effect passed."
                        : "E01 Warm Seat battle effect failed. Check the Console.",
                    "OK");
            }

            return valid;
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

    internal static class EnchantWornHandleValidation
    {
        [MenuItem("Have a Break/Validate E02 Worn Handle Attack Completion")]
        private static void ValidateFromMenu()
        {
            Validate(true);
        }

        internal static bool Validate(bool showDialog)
        {
            CardData card = FindCard(TestContentIds.C01);
            EnchantData enchant = FindEnchant(TestContentIds.E02);
            bool valid = card != null && enchant != null;

            if (valid)
            {
                BattleCardInstance battleCard = new(
                    card,
                    new CardInstanceIds(card.CatalogCardId, "OWNED-E02", "BATTLE-E02"),
                    1,
                    CardZone.MonsterField);
                RunCardEnchantState runEnchants = new(card);
                valid &= runEnchants.TryAttach(enchant, 0, false, out EnchantAttachmentFailure failure) &&
                         failure == EnchantAttachmentFailure.None;
                BattleCardEnchantRegistry enchantRegistry = new();
                valid &= enchantRegistry.TryRegister(battleCard, runEnchants);
                BattleMonsterRegistry monsters = new();
                valid &= monsters.TryAdd(battleCard, runEnchants, out BattleMonsterState monster);

                BattleEventLog log = new();
                EnchantTurnUsageTracker usage = new();
                BattleEventRecord cancelledDeclaration = DeclareAttack(log, battleCard, "ENEMY-CANCELLED");
                valid &= !EnchantWornHandleResolver.TryResolve(
                             cancelledDeclaration, 1, enchantRegistry, monsters, usage, log, out _) &&
                         monster.Counter == 0;

                BattleEventRecord firstDeclaration = DeclareAttack(log, battleCard, "ENEMY-FIRST");
                valid &= BattleAttackEventService.TryRecordCompleted(
                    log, firstDeclaration, out BattleEventRecord firstCompleted);
                valid &= !BattleAttackEventService.TryRecordCompleted(log, firstDeclaration, out _);
                valid &= EnchantWornHandleResolver.TryResolve(
                             firstCompleted, 1, enchantRegistry, monsters, usage, log,
                             out BattleEventRecord firstCounter) &&
                         monster.Counter == 1 &&
                         firstCounter.EventType == BattleEventType.StatusApplied &&
                         firstCounter.ParentEventId == firstCompleted.EventId &&
                         firstCounter.SourceEffectId == TestContentIds.E02 &&
                         firstCounter.BeforeValue == 0 && firstCounter.AfterValue == 1;

                valid &= !EnchantWornHandleResolver.TryResolve(
                             firstCompleted, 2, enchantRegistry, monsters, usage, log, out _) &&
                         monster.Counter == 1;

                BattleEventRecord sameTurnDeclaration = DeclareAttack(log, battleCard, "ENEMY-SAME-TURN");
                valid &= BattleAttackEventService.TryRecordCompleted(
                    log, sameTurnDeclaration, out BattleEventRecord sameTurnCompleted);
                valid &= !EnchantWornHandleResolver.TryResolve(
                             sameTurnCompleted, 1, enchantRegistry, monsters, usage, log, out _) &&
                         monster.Counter == 1;

                BattleEventRecord nextTurnDeclaration = DeclareAttack(log, battleCard, "ENEMY-NEXT-TURN");
                valid &= BattleAttackEventService.TryRecordCompleted(
                    log, nextTurnDeclaration, out BattleEventRecord nextTurnCompleted);
                valid &= EnchantWornHandleResolver.TryResolve(
                             nextTurnCompleted, 2, enchantRegistry, monsters, usage, log, out _) &&
                         monster.Counter == 2;

                runEnchants.RefreshCompatibility(CardType.Skill);
                BattleEventRecord inactiveDeclaration = DeclareAttack(log, battleCard, "ENEMY-INACTIVE");
                valid &= BattleAttackEventService.TryRecordCompleted(
                    log, inactiveDeclaration, out BattleEventRecord inactiveCompleted);
                valid &= !EnchantWornHandleResolver.TryResolve(
                             inactiveCompleted, 3, enchantRegistry, monsters, usage, log, out _) &&
                         monster.Counter == 2;
            }
            else
            {
                Debug.LogError("E02 attack completion validation requires C01 and E02.");
            }

            if (!valid)
            {
                Debug.LogError("E02 Worn Handle attack completion validation failed.");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "E02 Worn Handle Attack Completion Validation",
                    valid
                        ? "E02 Worn Handle attack completion passed."
                        : "E02 Worn Handle attack completion failed. Check the Console.",
                    "OK");
            }

            return valid;
        }

        private static BattleEventRecord DeclareAttack(
            BattleEventLog log,
            BattleCardInstance attacker,
            string targetId)
        {
            return log.Record(
                BattleEventType.AttackDeclared,
                "E02ValidationAttack",
                attacker.Ids.BattleCardId,
                attacker.Ids.BattleCardId,
                targetId);
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

    internal static class TestEnchantCompatibilityBuilder
    {
        private static readonly Dictionary<string, EnchantCompatibilityTag[]> Tags = new(
            StringComparer.OrdinalIgnoreCase)
        {
            [TestContentIds.C01] = new[]
            {
                EnchantCompatibilityTag.MainEffectCompletion,
                EnchantCompatibilityTag.FixedSingleEnemyTarget
            },
            [TestContentIds.C02] = new[] { EnchantCompatibilityTag.MainEffectCompletion },
            [TestContentIds.C03] = new[] { EnchantCompatibilityTag.MainEffectCompletion },
            [TestContentIds.C04] = new[] { EnchantCompatibilityTag.MainEffectCompletion },
            [TestContentIds.C05] = new[]
            {
                EnchantCompatibilityTag.MainEffectCompletion,
                EnchantCompatibilityTag.NormalGraveyardAfterResolution,
                EnchantCompatibilityTag.FixedSingleEnemyTarget
            },
            [TestContentIds.C06] = new[]
            {
                EnchantCompatibilityTag.MainEffectCompletion,
                EnchantCompatibilityTag.NormalGraveyardAfterResolution,
                EnchantCompatibilityTag.FixedSingleEnemyTarget
            },
            [TestContentIds.C07] = new[]
            {
                EnchantCompatibilityTag.MainEffectCompletion,
                EnchantCompatibilityTag.NormalGraveyardAfterResolution
            },
            [TestContentIds.C08] = new[]
            {
                EnchantCompatibilityTag.MainEffectCompletion,
                EnchantCompatibilityTag.EnemyAffectingEffect,
                EnchantCompatibilityTag.NormalGraveyardAfterResolution
            },
            [TestContentIds.C09] = new[]
            {
                EnchantCompatibilityTag.MainEffectCompletion,
                EnchantCompatibilityTag.NormalGraveyardAfterResolution
            },
            [TestContentIds.C10] = new[]
            {
                EnchantCompatibilityTag.MainEffectCompletion,
                EnchantCompatibilityTag.EnemyAffectingEffect,
                EnchantCompatibilityTag.NormalGraveyardAfterResolution
            },
            [TestContentIds.C11] = new[]
            {
                EnchantCompatibilityTag.MainEffectCompletion,
                EnchantCompatibilityTag.NumericRepeatingEffect
            },
            [TestContentIds.C12] = new[]
            {
                EnchantCompatibilityTag.MainEffectCompletion,
                EnchantCompatibilityTag.EnemyAffectingEffect,
                EnchantCompatibilityTag.NumericRepeatingEffect
            }
        };

        private static readonly Dictionary<string, string[]> ExpectedCompatibleCards = new(
            StringComparer.OrdinalIgnoreCase)
        {
            [TestContentIds.E01] = new[] { TestContentIds.C01, TestContentIds.C02, TestContentIds.C03, TestContentIds.C04 },
            [TestContentIds.E02] = new[] { TestContentIds.C01, TestContentIds.C02, TestContentIds.C03, TestContentIds.C04 },
            [TestContentIds.E03] = new[]
                { TestContentIds.C01, TestContentIds.C02, TestContentIds.C03, TestContentIds.C04, TestContentIds.C05, TestContentIds.C06, TestContentIds.C07, TestContentIds.C08, TestContentIds.C09, TestContentIds.C10, TestContentIds.C11, TestContentIds.C12 },
            [TestContentIds.E04] = new[] { TestContentIds.C06 },
            [TestContentIds.E05] = new[] { TestContentIds.C08, TestContentIds.C10 },
            [TestContentIds.E06] = new[] { TestContentIds.C11, TestContentIds.C12 },
            [TestContentIds.E07] = new[] { TestContentIds.C05, TestContentIds.C06, TestContentIds.C07, TestContentIds.C08, TestContentIds.C09, TestContentIds.C10 },
            [TestContentIds.E08] = new[] { TestContentIds.C01, TestContentIds.C05, TestContentIds.C06 }
        };

        public static void ApplyTags(bool showDialog)
        {
            Dictionary<string, CardData> cards = FindCards();
            bool complete = true;
            foreach (KeyValuePair<string, EnchantCompatibilityTag[]> pair in Tags)
            {
                string cardId = pair.Key;
                EnchantCompatibilityTag[] tags = pair.Value;
                if (!cards.TryGetValue(cardId, out CardData card))
                {
                    complete = false;
                    Debug.LogError($"Missing card for enchant compatibility tags: {cardId}");
                    continue;
                }

                card.EditorSetEnchantCompatibilityTags(tags);
                EditorUtility.SetDirty(card);
            }

            AssetDatabase.SaveAssets();
            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Enchant Compatibility Tags",
                    complete
                        ? "Applied enchant compatibility tags to C01-C12."
                        : "Some cards were missing. Check the Console.",
                    "OK");
            }
        }

        public static bool Validate(bool showDialog)
        {
            Dictionary<string, CardData> cards = FindCards();
            EnchantDatabase database = AssetDatabase.LoadAssetAtPath<EnchantDatabase>(
                "Assets/GameData/EnchantDatabase.asset");
            bool valid = database != null && Tags.Keys.All(cards.ContainsKey);

            foreach (KeyValuePair<string, EnchantCompatibilityTag[]> pair in Tags)
            {
                string cardId = pair.Key;
                EnchantCompatibilityTag[] expectedTags = pair.Value;
                if (!cards.TryGetValue(cardId, out CardData card))
                {
                    valid = false;
                    continue;
                }

                bool tagsMatch = card.EnchantCompatibilityTags.SequenceEqual(expectedTags);
                valid &= tagsMatch;
                if (!tagsMatch)
                {
                    Debug.LogError($"Enchant compatibility tag mismatch: {cardId}", card);
                }
            }

            if (database != null)
            {
                foreach (KeyValuePair<string, string[]> pair in ExpectedCompatibleCards)
                {
                    string enchantId = pair.Key;
                    string[] expectedCardIds = pair.Value;
                    EnchantData enchant = database.Find(enchantId);
                    if (enchant == null)
                    {
                        valid = false;
                        Debug.LogError($"Missing enchant for compatibility validation: {enchantId}");
                        continue;
                    }

                    string[] actualCardIds = cards.Values
                        .Where(card => EnchantCompatibilityEvaluator.IsCompatible(enchant, card))
                        .Select(card => card.CatalogCardId)
                        .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                    bool matrixMatches = actualCardIds.SequenceEqual(expectedCardIds);
                    valid &= matrixMatches;
                    if (!matrixMatches)
                    {
                        Debug.LogError(
                            $"Compatibility mismatch for {enchantId}. " +
                            $"Expected [{string.Join(", ", expectedCardIds)}], " +
                            $"actual [{string.Join(", ", actualCardIds)}].");
                    }
                }
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Enchant Compatibility Validation",
                    valid
                        ? "Enchant compatibility passed."
                        : "Enchant compatibility failed. Check the Console.",
                    "OK");
            }

            return valid;
        }

        private static Dictionary<string, CardData> FindCards()
        {
            return AssetDatabase.FindAssets("t:CardData")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => AssetDatabase.LoadAssetAtPath<CardData>(path))
                .Where(card => card != null && !string.IsNullOrWhiteSpace(card.CatalogCardId))
                .ToDictionary(card => card.CatalogCardId, StringComparer.OrdinalIgnoreCase);
        }
    }

    internal static class TestEnchantDataBuilder
    {
        private const string EnchantFolder = "Assets/GameData/Enchants";
        private const string DatabasePath = "Assets/GameData/EnchantDatabase.asset";
        private const string SourceVersion = "시험 콘텐츠 / v0.4 / 2026-07-18";

        private sealed class Definition
        {
            public string Id;
            public string Name;
            public CardRarity Rarity;
            public EnchantApplicationType Type;
            public string Role;
            public CardType[] CompatibleTypes;
            public string CompatibilityRule;
            public string RulesText;
            public string SourceDocument;
            public EnchantEffectData[] Effects;
        }

        public static void RebuildTestEnchants(bool showDialog)
        {
            EnsureFolder(EnchantFolder);
            List<EnchantData> assets = new();
            foreach (Definition definition in CreateDefinitions())
            {
                string path = $"{EnchantFolder}/{definition.Id}_{definition.Name}.asset";
                EnchantData asset = AssetDatabase.LoadAssetAtPath<EnchantData>(path);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<EnchantData>();
                    AssetDatabase.CreateAsset(asset, path);
                }

                asset.EditorInitialize(
                    definition.Id,
                    definition.Name,
                    definition.Rarity,
                    definition.CompatibleTypes,
                    false,
                    definition.Type,
                    definition.Role,
                    definition.CompatibilityRule,
                    definition.RulesText,
                    SourceVersion,
                    definition.SourceDocument,
                    definition.Effects);
                EditorUtility.SetDirty(asset);
                assets.Add(asset);
            }

            EnchantDatabase database = AssetDatabase.LoadAssetAtPath<EnchantDatabase>(DatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<EnchantDatabase>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }

            database.EditorSetEnchants(assets.OrderBy(asset => asset.DefinitionId));
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Test Enchant Data",
                    "Created or rebuilt E01-E08 and EnchantDatabase.",
                    "OK");
            }
        }

        public static bool ValidateTestEnchants(bool showDialog)
        {
            Definition[] definitions = CreateDefinitions();
            List<EnchantData> all = AssetDatabase.FindAssets("t:EnchantData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<EnchantData>(path))
                .Where(asset => asset != null)
                .ToList();
            bool valid = true;

            foreach (Definition expected in definitions)
            {
                List<EnchantData> matches = all.Where(asset => string.Equals(
                    asset.DefinitionId, expected.Id, StringComparison.OrdinalIgnoreCase)).ToList();
                valid &= matches.Count == 1;
                if (matches.Count != 1)
                {
                    Debug.LogError($"Expected exactly one enchant '{expected.Id}', found {matches.Count}.");
                    continue;
                }

                EnchantData actual = matches[0];
                bool entryValid = actual.DisplayName == expected.Name &&
                                  actual.Rarity == expected.Rarity &&
                                  actual.ApplicationType == expected.Type &&
                                  actual.Role == expected.Role &&
                                  actual.AdditionalCompatibilityRule == expected.CompatibilityRule &&
                                  actual.RulesText == expected.RulesText &&
                                  actual.SourceVersion == SourceVersion &&
                                  actual.SourceDocument == expected.SourceDocument &&
                                  !actual.AllowDuplicateOnSameCard &&
                                  actual.CompatibleCardTypes.SequenceEqual(expected.CompatibleTypes) &&
                                  EffectsMatch(actual.Effects, expected.Effects);
                valid &= entryValid;
                if (!entryValid)
                {
                    Debug.LogError($"Enchant data mismatch: {expected.Id} {expected.Name}", actual);
                }
            }

            bool idsUnique = all
                .Where(asset => definitions.Any(definition => string.Equals(
                    definition.Id, asset.DefinitionId, StringComparison.OrdinalIgnoreCase)))
                .GroupBy(asset => asset.DefinitionId, StringComparer.OrdinalIgnoreCase)
                .All(group => group.Count() == 1);
            valid &= idsUnique;

            EnchantDatabase database = AssetDatabase.LoadAssetAtPath<EnchantDatabase>(DatabasePath);
            bool databaseValid = database != null && database.Enchants.Count == definitions.Length &&
                                 definitions.All(definition => database.Find(definition.Id) != null);
            valid &= databaseValid;
            if (!databaseValid)
            {
                Debug.LogError("EnchantDatabase must contain E01-E08 exactly once.");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Test Enchant Validation",
                    valid ? "Test enchants E01-E08 passed." : "Test enchant validation failed. Check the Console.",
                    "OK");
            }

            return valid;
        }

        private static bool EffectsMatch(
            IReadOnlyList<EnchantEffectData> actual,
            IReadOnlyList<EnchantEffectData> expected)
        {
            if (actual.Count != expected.Count)
            {
                return false;
            }

            for (int i = 0; i < actual.Count; i++)
            {
                if (actual[i] == null || expected[i] == null ||
                    actual[i].Trigger != expected[i].Trigger ||
                    actual[i].ConditionAndTarget != expected[i].ConditionAndTarget ||
                    actual[i].Resolution != expected[i].Resolution ||
                    actual[i].Limitation != expected[i].Limitation)
                {
                    return false;
                }
            }

            return true;
        }

        private static Definition[] CreateDefinitions()
        {
            CardType[] allTypes =
                { CardType.Monster, CardType.Skill, CardType.Trap, CardType.Barrier };
            return new[]
            {
                Define(
                    TestContentIds.E01, "따뜻한 좌석", CardRarity.Common,
                    EnchantApplicationType.StaticModifier, "생존",
                    new[] { CardType.Monster }, "기본 생명력이 존재",
                    "최대 생명력 +2.",
                    "https://docs.google.com/document/d/1kn9eWeo6TRRtRzyjoq5DTPChUGqVTXLu67qKeWB6KW8/edit",
                    Effect("정적 적용", "장착 카드가 몬스터이고 인첸트가 활성",
                        "최대 생명력 +2. 장착 시 현재 생명력도 +2.",
                        "제거·비활성 시 최대 생명력 -2 후 현재 생명력 상한 정리")),
                Define(
                    TestContentIds.E02, "닳은 손잡이", CardRarity.Common,
                    EnchantApplicationType.PostTrigger, "공격 / 반격",
                    new[] { CardType.Monster }, "기본 공격 능력 존재",
                    "공격을 완료한 뒤 반격 1을 얻는다. 플레이어 턴당 1회.",
                    "https://docs.google.com/document/d/1JBdtUGW3HOGgY6H9sBqHswpIY185WVvuCZewgaHUOh4/edit",
                    Effect("공격 완료 사건", "장착 몬스터가 공격을 실제 완료 / 플레이어 턴당 1회",
                        "장착 몬스터에게 반격 1을 부여합니다.",
                        "공격이 취소·무효·대상 상실로 완료되지 않으면 미발동")),
                Define(
                    TestContentIds.E03, "구겨진 왕복 승차권", CardRarity.Rare,
                    EnchantApplicationType.PostTrigger, "자원 / 드로우",
                    allTypes, "본 효과 완료 사건 존재",
                    "장착 카드의 본 효과 뒤 현재 마력이 0이면 카드 1장을 드로우한다. 턴당 1회.",
                    "https://docs.google.com/document/d/1icMWm_AT1lqImoEMU1FApTjQpE4h0iVGlpRiHWxZbFQ/edit",
                    Effect("본 효과 완료 사건", "장착 카드 본 효과 완료 / 현재 마력 0 / 플레이어 턴당 1회",
                        "카드 1장을 드로우합니다.",
                        "패 10장이면 드로우 실패하고 카드는 덱 맨 위에 남음")),
                Define(
                    TestContentIds.E04, "예비 전원", CardRarity.Common,
                    EnchantApplicationType.StaticModifier, "자원 / 비용 감소",
                    new[] { CardType.Skill }, "수록 기본 비용 2 이상",
                    "비용이 1 감소한다. 이 효과로는 1 미만이 되지 않는다.",
                    "https://docs.google.com/document/d/1QXaRyXQHN0lbaosr86JN08mlePuDNsI-Pi6Yq33l774/edit",
                    Effect("카드 비용 계산", "활성 상태의 장착 스킬",
                        "마력 비용을 1 낮추고 E04 적용 직후 최소 1로 제한합니다.",
                        "카드 자체의 비용 설정 효과가 있으면 설정 뒤에 적용합니다.")),
                Define(
                    TestContentIds.E05, "녹슨 안내방송", CardRarity.Common,
                    EnchantApplicationType.PostTrigger, "상태 / 트랩 특화",
                    new[] { CardType.Trap }, "적 대상·영향 효과 존재",
                    "장착 트랩이 적에게 영향을 준 뒤 영향을 받은 각 적에게 약화 1을 부여한다.",
                    "https://docs.google.com/document/d/1P8QU173hx_PV3AJi741zU1bNKVhpUEvT0QMA9NXJcg0/edit",
                    Effect("트랩 본 효과 완료 사건", "장착 트랩이 적 1개 이상에게 실제 영향",
                        "영향을 받은 각 적에게 약화 1을 부여합니다.",
                        "트랩 효과가 무효화되거나 모든 대상이 사라지면 미발동합니다.")),
                Define(
                    TestContentIds.E06, "별빛 각인", CardRarity.Rare,
                    EnchantApplicationType.StaticModifier, "결계 특화 / 수치 강화",
                    new[] { CardType.Barrier }, "수치형 반복 효과 존재",
                    "반복 효과의 첫 수치가 1 증가한다.",
                    "https://docs.google.com/document/d/1eSWNDwZRF41ryU0JS0QxO43i5OqLeqaBQQPgNpXCYtw/edit",
                    Effect("반복 효과 명령 생성", "활성 상태의 장착 결계 / 수치형 반복 효과",
                        "해당 반복 효과의 첫 수치를 1 증가시킵니다.",
                        "대상 수·발동 횟수·조건 기준값은 증가하지 않습니다.")),
                Define(
                    TestContentIds.E07, "환승 도장", CardRarity.Rare,
                    EnchantApplicationType.PreReplacement, "묘지·순환",
                    new[] { CardType.Skill, CardType.Trap }, "정상 처리 후 묘지 이동",
                    "전투당 처음 묘지로 갈 때 대신 드로우 더미 맨 아래로 간다.",
                    "https://docs.google.com/document/d/1veq88O0iS2-4FS_PJQypJMXbGj3FICYs0I5nLdmwaPk/edit",
                    Effect("장착 카드 묘지 이동 직전", "정상 처리 후 묘지 이동 / 전투당 첫 1회",
                        "묘지 대신 드로우 더미 맨 아래로 이동시킵니다.",
                        "묘지 이동 사건은 발생하지 않습니다."),
                    Effect("전투 시작 사건", "장착 카드의 새 전투카드",
                        "전투당 사용 표식을 미사용으로 초기화합니다.",
                        "전투 종료 시 표식을 폐기합니다.")),
                Define(
                    TestContentIds.E08, "노선 고정핀", CardRarity.Legendary,
                    EnchantApplicationType.RuleChange, "대상 방식 / 위치",
                    allTypes, "적 1개 고정 대상 효과",
                    "적 고정 대상 효과가 위치 대상 효과로 바뀐다.",
                    "https://docs.google.com/document/d/1NjzwzmYLOkf9A1vEPp18Ycq_e5MuQ628HOS7RU1BD4Q/edit",
                    Effect("대상 선언 사건", "장착 카드의 적 1개 고정 대상 효과",
                        "선택한 적의 현재 위치를 기록하고 위치 대상으로 변경합니다.",
                        "선언 당시 빈 위치를 선택할 수 없습니다."),
                    Effect("효과 해결 사건", "기록한 위치에 적 존재",
                        "현재 그 위치를 점유한 적에게 효과를 적용합니다.",
                        "빈 위치면 실패하며 다른 위치로 재탐색하지 않습니다."))
            };
        }

        private static Definition Define(
            string id,
            string name,
            CardRarity rarity,
            EnchantApplicationType type,
            string role,
            CardType[] compatibleTypes,
            string compatibilityRule,
            string rulesText,
            string sourceDocument,
            params EnchantEffectData[] effects)
        {
            return new Definition
            {
                Id = id,
                Name = name,
                Rarity = rarity,
                Type = type,
                Role = role,
                CompatibleTypes = compatibleTypes,
                CompatibilityRule = compatibilityRule,
                RulesText = rulesText,
                SourceDocument = sourceDocument,
                Effects = effects
            };
        }

        private static EnchantEffectData Effect(
            string trigger,
            string condition,
            string resolution,
            string limitation)
        {
            return new EnchantEffectData(trigger, condition, resolution, limitation);
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] segments = folderPath.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = $"{current}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }
        }
    }
}
