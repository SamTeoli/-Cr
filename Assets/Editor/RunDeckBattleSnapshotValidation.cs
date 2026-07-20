using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class RunDeckBattleSnapshotValidation
    {
        [MenuItem("Have a Break/Validate Run Deck Battle Snapshot")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            if (valid)
            {
                Debug.Log("Run deck battle snapshot passed.");
            }
            else
            {
                Debug.LogError("Run deck battle snapshot failed.");
            }

            EditorUtility.DisplayDialog(
                "Run Deck Battle Snapshot Validation",
                valid
                    ? "Run deck IDs, levels, and enchants passed."
                    : "Run deck battle snapshot failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            CardData c01 = FindCard("C01");
            CardData c02 = FindCard("C02");
            EnchantData e01 = FindEnchant("E01");
            if (c01 == null || c02 == null || e01 == null)
            {
                return false;
            }

            RunCardInstance first = new(c01, "OWNED-45-C01", 3);
            RunCardInstance second = new(c02, "OWNED-45-C02", 99);
            if (!first.Enchants.TryAttach(
                    e01,
                    0,
                    false,
                    out EnchantAttachmentFailure attachmentFailure) ||
                attachmentFailure != EnchantAttachmentFailure.None)
            {
                return false;
            }

            RunDeckState runDeck = new();
            bool firstAdded = runDeck.TryAdd(
                first, out RunDeckFailure firstAddFailure);
            bool secondAdded = runDeck.TryAdd(
                second, out RunDeckFailure secondAddFailure);
            bool duplicateRejected = !runDeck.TryAdd(
                    new RunCardInstance(c01, "owned-45-c01", 1),
                    out RunDeckFailure duplicateFailure) &&
                duplicateFailure == RunDeckFailure.DuplicateOwnedCardId;
            if (!firstAdded || !secondAdded ||
                firstAddFailure != RunDeckFailure.None ||
                secondAddFailure != RunDeckFailure.None ||
                !duplicateRejected || runDeck.Count != 2 ||
                runDeck.Find("owned-45-c02") != second)
            {
                return false;
            }

            return ValidateSnapshot(runDeck, first, second) &&
                   ValidateDeterminism(runDeck) &&
                   ValidateRejectedSnapshots();
        }

        private static bool ValidateSnapshot(
            RunDeckState runDeck,
            RunCardInstance first,
            RunCardInstance second)
        {
            if (!RunDeckBattleSnapshotService.TryCreate(
                    runDeck,
                    "TEST-BATTLE-45-A",
                    out RunDeckBattleSnapshot snapshot,
                    out RunDeckFailure snapshotFailure) ||
                snapshotFailure != RunDeckFailure.None ||
                snapshot.Cards.Count != 2)
            {
                return false;
            }

            BattleCardInstance firstBattle = snapshot.Cards[0];
            BattleCardInstance secondBattle = snapshot.Cards[1];
            if (firstBattle.SourceCard != first.Card ||
                firstBattle.Ids.CatalogCardId != first.CatalogCardId ||
                firstBattle.Ids.OwnedCardId != first.OwnedCardId ||
                firstBattle.Ids.BattleCardId !=
                "TEST-BATTLE-45-A:OWNED-45-C01" ||
                firstBattle.CurrentLevel != 3 ||
                firstBattle.Zone != CardZone.DrawPile ||
                firstBattle.IsTemporary ||
                secondBattle.Ids.BattleCardId !=
                "TEST-BATTLE-45-A:OWNED-45-C02" ||
                secondBattle.CurrentLevel != CardData.MaximumLevel ||
                snapshot.FindRunCard(
                    firstBattle.Ids.BattleCardId) != first ||
                snapshot.FindRunCard(
                    secondBattle.Ids.BattleCardId) != second)
            {
                return false;
            }

            BattleRuntimeState runtime = new(snapshot.Cards, 451);
            bool registered = snapshot.TryRegisterEnchants(
                runtime, out RunDeckFailure registrationFailure);
            bool duplicateRegistrationRejected =
                !snapshot.TryRegisterEnchants(
                    runtime,
                    out RunDeckFailure duplicateRegistrationFailure);
            return registered &&
                   registrationFailure == RunDeckFailure.None &&
                   duplicateRegistrationRejected &&
                   duplicateRegistrationFailure ==
                   RunDeckFailure.EnchantRegistrationFailed &&
                   runtime.Enchants.Find(
                       firstBattle.Ids.BattleCardId) == first.Enchants &&
                   runtime.Enchants.Find(
                       secondBattle.Ids.BattleCardId) == second.Enchants;
        }

        private static bool ValidateDeterminism(RunDeckState runDeck)
        {
            if (!RunDeckBattleSnapshotService.TryCreate(
                    runDeck,
                    "TEST-BATTLE-45-B",
                    out RunDeckBattleSnapshot first,
                    out _) ||
                !RunDeckBattleSnapshotService.TryCreate(
                    runDeck,
                    "TEST-BATTLE-45-B",
                    out RunDeckBattleSnapshot repeated,
                    out _) ||
                !RunDeckBattleSnapshotService.TryCreate(
                    runDeck,
                    "TEST-BATTLE-45-C",
                    out RunDeckBattleSnapshot nextBattle,
                    out _))
            {
                return false;
            }

            for (int i = 0; i < first.Cards.Count; i++)
            {
                if (!string.Equals(
                        first.Cards[i].Ids.BattleCardId,
                        repeated.Cards[i].Ids.BattleCardId,
                        StringComparison.Ordinal) ||
                    string.Equals(
                        first.Cards[i].Ids.BattleCardId,
                        nextBattle.Cards[i].Ids.BattleCardId,
                        StringComparison.Ordinal) ||
                    first.Cards[i].Ids.OwnedCardId !=
                    nextBattle.Cards[i].Ids.OwnedCardId)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateRejectedSnapshots()
        {
            bool emptyRejected =
                !RunDeckBattleSnapshotService.TryCreate(
                    new RunDeckState(),
                    "TEST-BATTLE-45-EMPTY",
                    out _,
                    out RunDeckFailure emptyFailure) &&
                emptyFailure == RunDeckFailure.InvalidDeck;

            RunDeckState runDeck = new();
            CardData c01 = FindCard("C01");
            if (c01 == null ||
                !runDeck.TryAdd(
                    new RunCardInstance(c01, "OWNED-45-INVALID-ID"),
                    out _))
            {
                return false;
            }

            bool idRejected =
                !RunDeckBattleSnapshotService.TryCreate(
                    runDeck,
                    " ",
                    out _,
                    out RunDeckFailure idFailure) &&
                idFailure == RunDeckFailure.InvalidBattleInstanceId;
            return emptyRejected && idRejected;
        }

        private static CardData FindCard(string catalogCardId)
        {
            return AssetDatabase.FindAssets("t:CardData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<CardData>)
                .FirstOrDefault(card => card != null && string.Equals(
                    card.CatalogCardId,
                    catalogCardId,
                    StringComparison.OrdinalIgnoreCase));
        }

        private static EnchantData FindEnchant(string definitionId)
        {
            return AssetDatabase.FindAssets("t:EnchantData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<EnchantData>)
                .FirstOrDefault(enchant => enchant != null && string.Equals(
                    enchant.DefinitionId,
                    definitionId,
                    StringComparison.OrdinalIgnoreCase));
        }
    }
}
