using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class EnchantRustyAnnouncementValidation
    {
        [MenuItem("Have a Break/Validate E05 Rusty Announcement Affected Enemies")]
        private static void ValidateFromMenu()
        {
            Validate(true);
        }

        internal static bool Validate(bool showDialog)
        {
            CardData card = FindCard("C08");
            EnchantData enchant = FindEnchant("E05");
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
                             item.SourceEffectId == "E05" &&
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
}
