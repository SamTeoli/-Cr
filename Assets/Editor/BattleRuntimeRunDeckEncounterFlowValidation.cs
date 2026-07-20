using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleRuntimeRunDeckEncounterFlowValidation
    {
        [MenuItem("Have a Break/Validate Run Deck Encounter Flow")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            if (valid)
            {
                Debug.Log("Run deck encounter flow passed.");
            }
            else
            {
                Debug.LogError("Run deck encounter flow failed.");
            }

            EditorUtility.DisplayDialog(
                "Run Deck Encounter Flow Validation",
                valid
                    ? "Run deck encounter creation, enchants, and ID mapping passed."
                    : "Run deck encounter flow failed. Check the Console.",
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

            RunCardInstance first = new(c01, "OWNED-46-C01", 3);
            RunCardInstance second = new(c02, "OWNED-46-C02", 5);
            bool attached = first.Enchants.TryAttach(
                e01,
                0,
                false,
                out EnchantAttachmentFailure attachmentFailure);
            if (!attached ||
                attachmentFailure != EnchantAttachmentFailure.None)
            {
                return false;
            }

            RunDeckState runDeck = new();
            bool firstAdded = runDeck.TryAdd(
                first, out RunDeckFailure firstAddFailure);
            bool secondAdded = runDeck.TryAdd(
                second, out RunDeckFailure secondAddFailure);
            if (!firstAdded || !secondAdded ||
                firstAddFailure != RunDeckFailure.None ||
                secondAddFailure != RunDeckFailure.None)
            {
                return false;
            }

            EnemyDefinitionData enemy =
                ScriptableObject.CreateInstance<EnemyDefinitionData>();
            EncounterData encounter =
                ScriptableObject.CreateInstance<EncounterData>();
            try
            {
                enemy.EditorInitialize(
                    "TEST-ENEMY-46",
                    "Test Run Deck Enemy",
                    0,
                    1);
                encounter.EditorInitialize(
                    "TEST-ENCOUNTER-46",
                    "Test Run Deck Encounter",
                    BattleEncounterGrade.Normal,
                    new[]
                    {
                        new EncounterEnemySlot(
                            "TEST-ENEMY-46-A",
                            enemy,
                            EnemyFieldPosition.Center)
                    });

                return ValidateIntegratedStart(
                           runDeck,
                           first,
                           second,
                           encounter) &&
                       ValidateSnapshotFailureMapping(
                           runDeck,
                           encounter);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemy);
                UnityEngine.Object.DestroyImmediate(encounter);
            }
        }

        private static bool ValidateIntegratedStart(
            RunDeckState runDeck,
            RunCardInstance first,
            RunCardInstance second,
            EncounterData encounter)
        {
            bool created = BattleRuntimeEncounterFlowService.TryCreateAndBegin(
                runDeck,
                "TEST-BATTLE-46",
                new RunBattleState(30, 27, 4),
                encounter,
                460,
                5,
                Array.Empty<string>(),
                0,
                out BattleRuntimeEncounterContext context,
                out BattleRuntimeEncounterFlowFailure flowFailure,
                out RunDeckFailure runDeckFailure,
                out BattleRuntimeBootstrapFailure bootstrapFailure,
                out BattleRuntimeSessionFailure sessionFailure,
                out StartingHandRedrawFailure redrawFailure,
                out BattleTurnFailure turnFailure,
                out List<string> validationErrors);
            if (!created || context == null ||
                flowFailure != BattleRuntimeEncounterFlowFailure.None ||
                runDeckFailure != RunDeckFailure.None ||
                bootstrapFailure != BattleRuntimeBootstrapFailure.None ||
                sessionFailure != BattleRuntimeSessionFailure.None ||
                redrawFailure != StartingHandRedrawFailure.None ||
                turnFailure != BattleTurnFailure.None ||
                validationErrors.Count != 0 ||
                context.DeckSnapshot == null ||
                context.DeckSnapshot.Cards.Count != 2)
            {
                return false;
            }

            BattleCardInstance firstBattle =
                context.DeckSnapshot.Cards[0];
            BattleCardInstance secondBattle =
                context.DeckSnapshot.Cards[1];
            return firstBattle.Ids.BattleCardId ==
                   "TEST-BATTLE-46:OWNED-46-C01" &&
                   secondBattle.Ids.BattleCardId ==
                   "TEST-BATTLE-46:OWNED-46-C02" &&
                   firstBattle.Ids.OwnedCardId == first.OwnedCardId &&
                   secondBattle.Ids.OwnedCardId == second.OwnedCardId &&
                   firstBattle.CurrentLevel == 3 &&
                   secondBattle.CurrentLevel == 5 &&
                   context.DeckSnapshot.FindRunCard(
                       firstBattle.Ids.BattleCardId) == first &&
                   context.DeckSnapshot.FindRunCard(
                       secondBattle.Ids.BattleCardId) == second &&
                   context.Runtime.Deck.Zones.Find(
                       firstBattle.Ids.BattleCardId) == firstBattle &&
                   context.Runtime.Deck.Zones.Find(
                       secondBattle.Ids.BattleCardId) == secondBattle &&
                   context.Runtime.Enchants.Find(
                       firstBattle.Ids.BattleCardId) == first.Enchants &&
                   context.Runtime.Enchants.Find(
                       secondBattle.Ids.BattleCardId) == second.Enchants &&
                   runDeck.Find(first.OwnedCardId) == first &&
                   runDeck.Find(second.OwnedCardId) == second;
        }

        private static bool ValidateSnapshotFailureMapping(
            RunDeckState validRunDeck,
            EncounterData encounter)
        {
            bool emptyCreated =
                BattleRuntimeEncounterFlowService.TryCreateAndBegin(
                    new RunDeckState(),
                    "TEST-BATTLE-46-EMPTY",
                    new RunBattleState(30, 30, 0),
                    encounter,
                    461,
                    5,
                    Array.Empty<string>(),
                    0,
                    out BattleRuntimeEncounterContext emptyContext,
                    out BattleRuntimeEncounterFlowFailure emptyFlowFailure,
                    out RunDeckFailure emptyRunDeckFailure,
                    out BattleRuntimeBootstrapFailure emptyBootstrapFailure,
                    out _, out _, out _,
                    out List<string> emptyValidationErrors);

            bool invalidIdCreated =
                BattleRuntimeEncounterFlowService.TryCreateAndBegin(
                    validRunDeck,
                    " ",
                    new RunBattleState(30, 30, 0),
                    encounter,
                    462,
                    5,
                    Array.Empty<string>(),
                    0,
                    out BattleRuntimeEncounterContext invalidIdContext,
                    out BattleRuntimeEncounterFlowFailure invalidIdFlowFailure,
                    out RunDeckFailure invalidIdRunDeckFailure,
                    out BattleRuntimeBootstrapFailure invalidIdBootstrapFailure,
                    out _, out _, out _,
                    out List<string> invalidIdValidationErrors);

            return !emptyCreated && emptyContext == null &&
                   emptyFlowFailure ==
                   BattleRuntimeEncounterFlowFailure.RunDeckSnapshotFailed &&
                   emptyRunDeckFailure == RunDeckFailure.InvalidDeck &&
                   emptyBootstrapFailure ==
                   BattleRuntimeBootstrapFailure.None &&
                   emptyValidationErrors.Count == 0 &&
                   !invalidIdCreated && invalidIdContext == null &&
                   invalidIdFlowFailure ==
                   BattleRuntimeEncounterFlowFailure.RunDeckSnapshotFailed &&
                   invalidIdRunDeckFailure ==
                   RunDeckFailure.InvalidBattleInstanceId &&
                   invalidIdBootstrapFailure ==
                   BattleRuntimeBootstrapFailure.None &&
                   invalidIdValidationErrors.Count == 0;
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
