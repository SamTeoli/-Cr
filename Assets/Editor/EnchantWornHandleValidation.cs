using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class EnchantWornHandleValidation
    {
        [MenuItem("Have a Break/Validate E02 Worn Handle Attack Completion")]
        private static void ValidateFromMenu()
        {
            Validate(true);
        }

        internal static bool Validate(bool showDialog)
        {
            CardData card = FindCard("C01");
            EnchantData enchant = FindEnchant("E02");
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
                         firstCounter.SourceEffectId == "E02" &&
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
}
