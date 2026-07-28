using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattlePlayerActionViewModelValidation
    {
        [MenuItem("Have a Break/Validate Battle Player Action ViewModel")]
        private static void ValidateFromMenu()
        {
            Debug.Log(Validate()
                ? "Battle player action ViewModel passed."
                : "Battle player action ViewModel failed.");
        }

        internal static bool Validate()
        {
            CardDatabase cards = AssetDatabase.LoadAssetAtPath<CardDatabase>(
                "Assets/GameData/CardDatabase.asset");
            ConsumableData[] battleItems = PrototypeConsumableCatalog.All
                .Where(item => item != null &&
                    item.Effect != ConsumableEffect.IncreaseEnchantSlot &&
                    item.Effect != ConsumableEffect.ReplaceEnchant)
                .ToArray();
            RunEncounterProgressState progress = CreateProgress(
                cards,
                battleItems);
            EnemyDefinitionData enemy =
                ScriptableObject.CreateInstance<EnemyDefinitionData>();
            EncounterData encounter =
                ScriptableObject.CreateInstance<EncounterData>();
            try
            {
                if (cards == null || battleItems.Length == 0 ||
                    progress == null)
                {
                    return false;
                }

                enemy.EditorInitialize(
                    "TEST-ENEMY-PLAYER-ACTION-VM",
                    "Test Player Action ViewModel Enemy",
                    1,
                    50);
                encounter.EditorInitialize(
                    "TEST-ENCOUNTER-PLAYER-ACTION-VM",
                    "Test Player Action ViewModel Encounter",
                    BattleEncounterGrade.Normal,
                    new[]
                    {
                        new EncounterEnemySlot(
                            "TEST-ENEMY-PLAYER-ACTION-VM-A",
                            enemy,
                            EnemyFieldPosition.Center)
                    });
                if (!TryBegin(
                        progress,
                        encounter,
                        out BattleRuntimeEncounterContext context))
                {
                    return false;
                }

                BattleRuntimeState runtime = context.Runtime;
                BattleCardInstance c01 = FindBattleCard(
                    runtime,
                    TestContentIds.C01);
                BattleCardInstance c07 = FindBattleCard(
                    runtime,
                    TestContentIds.C07);
                if (c01 == null || c07 == null ||
                    !EnsureInHand(runtime, c01) ||
                    !EnsureInHand(runtime, c07))
                {
                    return false;
                }

                BattlePlayerActionViewModel viewModel = new();
                viewModel.Refresh(context);
                BattleEnemyTargetOption[] enemies =
                    viewModel.CreateEnemyTargets(context);
                BattleEnemyTargetOption selectedEnemy = enemies
                    .FirstOrDefault(option => option.IsSelected);
                if (enemies.Length != Enum.GetValues(typeof(EnemyFieldPosition)).Length ||
                    selectedEnemy == null || !selectedEnemy.IsOccupied ||
                    selectedEnemy.EnemyId !=
                        "TEST-ENEMY-PLAYER-ACTION-VM-A" ||
                    viewModel.SelectedEnemyId != selectedEnemy.EnemyId ||
                    viewModel.SelectEnemy(context, "MISSING-ENEMY") ||
                    viewModel.SelectedEnemyId != selectedEnemy.EnemyId ||
                    !viewModel.SelectEnemy(context, selectedEnemy.EnemyId))
                {
                    return false;
                }

                BattleHandCardActionOption[] handOptions =
                    viewModel.CreateHandOptions(context);
                BattleHandCardActionOption c07Option = handOptions
                    .FirstOrDefault(option => option.BattleCardId ==
                        c07.Ids.BattleCardId);
                if (c07Option == null ||
                    c07Option.BanishTargets.Length == 0 ||
                    c07Option.SelectedBanishTarget == null ||
                    string.IsNullOrWhiteSpace(c07Option.DisplayText) ||
                    viewModel.SelectBanishTarget(
                        context,
                        c07.Ids.BattleCardId,
                        "MISSING-BANISH-TARGET"))
                {
                    return false;
                }

                string selectedBanishId =
                    c07Option.SelectedBanishTarget.BattleCardId;
                BattleBanishTargetOption cycled =
                    viewModel.CycleBanishTarget(
                        context,
                        c07.Ids.BattleCardId);
                if (cycled == null ||
                    string.IsNullOrWhiteSpace(cycled.BattleCardId) ||
                    !viewModel.SelectBanishTarget(
                        context,
                        c07.Ids.BattleCardId,
                        selectedBanishId))
                {
                    return false;
                }

                int handCountBeforeInvalid = runtime.Deck.Zones
                    .Count(CardZone.Hand);
                BattleCardPlayCommandResult invalidPlay =
                    viewModel.TryPlayCard(context, "MISSING-HAND-CARD");
                if (invalidPlay.Succeeded ||
                    string.IsNullOrWhiteSpace(invalidPlay.Message) ||
                    runtime.Deck.Zones.Count(CardZone.Hand) !=
                        handCountBeforeInvalid)
                {
                    return false;
                }

                BattleHandCardActionOption c01Option = viewModel
                    .CreateHandOptions(context)
                    .FirstOrDefault(option => option.BattleCardId ==
                        c01.Ids.BattleCardId);
                BattleCardPlayCommandResult play =
                    viewModel.TryPlayCard(
                        context,
                        c01.Ids.BattleCardId,
                        PlayerMonsterFieldPosition.Right);
                if (c01Option == null || !c01Option.CanPlay ||
                    !play.Succeeded || play.Result == null ||
                    string.IsNullOrWhiteSpace(play.Message) ||
                    runtime.PlayerMonsterPositions.GetOccupant(
                PlayerMonsterFieldPosition.Right) !=
            c01.Ids.BattleCardId ||
            !string.IsNullOrWhiteSpace(
                runtime.PlayerMonsterPositions.GetOccupant(
                    PlayerMonsterFieldPosition.Left)) ||
            !string.IsNullOrWhiteSpace(
                runtime.PlayerMonsterPositions.GetOccupant(
                    PlayerMonsterFieldPosition.Center)) ||
                    runtime.Deck.Zones.Count(CardZone.Hand) !=
                        handCountBeforeInvalid - 1 ||
                    viewModel.TryPlayCard(
                        context,
                        c01.Ids.BattleCardId).Succeeded)
                {
                    return false;
                }

                BattleMonsterAttackActionOption monster = viewModel
                    .CreateMonsterAttackOptions(context)
                    .FirstOrDefault(option => option.BattleCardId ==
                        c01.Ids.BattleCardId);
                BattleMonsterAttackCommandResult invalidAttack =
                    viewModel.TryAttack(context, "MISSING-MONSTER");
                BattleMonsterAttackCommandResult attack =
                    viewModel.TryAttack(
                        context,
                        c01.Ids.BattleCardId);
                BattleChainCommandResult attackChain =
                    viewModel.TryPassAndResolveChain(context);
                if (monster == null || !monster.CanAttack ||
                    invalidAttack.Succeeded ||
                    string.IsNullOrWhiteSpace(invalidAttack.Message) ||
                    !attack.Succeeded || attack.Result != null ||
                    attackChain?.Succeeded != true ||
                    attackChain.AttackResult == null ||
                    attackChain.AttackResult.DamageApplied <= 0 ||
                    string.IsNullOrWhiteSpace(attack.Message))
                {
                    return false;
                }

                BattleConsumableActionOption[] consumables =
                    viewModel.CreateConsumableOptions(progress);
                BattleConsumableCommandResult invalidItem =
                    viewModel.TryUseConsumable(
                        progress,
                        "MISSING-BATTLE-ITEM");
                if (consumables.Length != battleItems.Length ||
                    consumables.Any(option => option == null ||
                        option.RemainingCount != 1 ||
                        string.IsNullOrWhiteSpace(option.DisplayLabel)) ||
                    invalidItem.Succeeded ||
                    string.IsNullOrWhiteSpace(invalidItem.Message))
                {
                    return false;
                }

                BattleConsumableCommandResult usedItem = null;
                foreach (BattleConsumableActionOption option in consumables)
                {
                    BattleConsumableCommandResult attempt =
                        viewModel.TryUseConsumable(progress, option.ItemId);
                    if (attempt.Succeeded)
                    {
                        usedItem = attempt;
                        break;
                    }
                }

                if (usedItem == null || usedItem.Option == null ||
                    string.IsNullOrWhiteSpace(usedItem.Message) ||
                    context.RunChanges.ConsumedItemIds.Count(itemId =>
                        string.Equals(
                            itemId,
                            usedItem.Option.ItemId,
                            StringComparison.OrdinalIgnoreCase)) != 1)
                {
                    return false;
                }

                BattleConsumableActionOption afterItem = viewModel
                    .CreateConsumableOptions(progress)
                    .FirstOrDefault(option => string.Equals(
                        option.ItemId,
                        usedItem.Option.ItemId,
                        StringComparison.OrdinalIgnoreCase));
                if (afterItem == null || afterItem.RemainingCount != 0 ||
                    afterItem.CanUse ||
                    viewModel.TryUseConsumable(
                        progress,
                        usedItem.Option.ItemId).Succeeded)
                {
                    return false;
                }

                int playerTurnBefore = runtime.Turn.PlayerTurnNumber;
                BattleEndTurnCommandResult turn =
                    viewModel.TryEndPlayerTurn(context, 710);
                if (!turn.Succeeded || turn.Result == null ||
                    string.IsNullOrWhiteSpace(turn.Message) ||
                    turn.Result.Outcome == BattleOutcome.Ongoing &&
                    runtime.Turn.PlayerTurnNumber <= playerTurnBefore)
                {
                    return false;
                }

                viewModel.Reset();
                return viewModel.SelectedEnemyId == null &&
                       viewModel.CreateEnemyTargets(null).Length == 0 &&
                       viewModel.CreateHandOptions(null).Length == 0 &&
                       viewModel.CreateConsumableOptions(null).Length == 0;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemy);
                UnityEngine.Object.DestroyImmediate(encounter);
            }
        }

        private static RunEncounterProgressState CreateProgress(
            CardDatabase database,
            IEnumerable<ConsumableData> battleItems)
        {
            if (database == null || battleItems == null)
            {
                return null;
            }

            RunDeckState deck = new();
            for (int number = 1; number <= 12; number++)
            {
                string catalogCardId = $"C{number:00}";
                CardData data = database.Cards.FirstOrDefault(card =>
                    card != null && string.Equals(
                        card.CatalogCardId,
                        catalogCardId,
                        StringComparison.OrdinalIgnoreCase));
                if (data == null || !deck.TryAdd(
                        new RunCardInstance(
                            data,
                            $"OWNED-PLAYER-ACTION-VM-{catalogCardId}"),
                        out RunDeckFailure failure) ||
                    failure != RunDeckFailure.None)
                {
                    return null;
                }
            }

            return new RunEncounterProgressState(
                new RunBattleState(
                    30,
                    20,
                    0,
                    battleItems
                        .Where(item => item != null)
                        .Select(item => item.ItemId)),
                deck);
        }

        private static bool TryBegin(
            RunEncounterProgressState progress,
            EncounterData encounter,
            out BattleRuntimeEncounterContext context)
        {
            bool created = RunEncounterProgressService.TryBegin(
                progress,
                "TEST-BATTLE-PLAYER-ACTION-VM",
                encounter,
                710,
                5,
                Array.Empty<string>(),
                0,
                out context,
                out RunEncounterProgressFailure progressFailure,
                out BattleRuntimeEncounterFlowFailure flowFailure,
                out RunDeckFailure runDeckFailure,
                out BattleRuntimeBootstrapFailure bootstrapFailure,
                out BattleRuntimeSessionFailure sessionFailure,
                out StartingHandRedrawFailure redrawFailure,
                out BattleTurnFailure turnFailure,
                out List<string> validationErrors);
            return created && context != null &&
                   progressFailure == RunEncounterProgressFailure.None &&
                   flowFailure == BattleRuntimeEncounterFlowFailure.None &&
                   runDeckFailure == RunDeckFailure.None &&
                   bootstrapFailure == BattleRuntimeBootstrapFailure.None &&
                   sessionFailure == BattleRuntimeSessionFailure.None &&
                   redrawFailure == StartingHandRedrawFailure.None &&
                   turnFailure == BattleTurnFailure.None &&
                   validationErrors.Count == 0;
        }

        private static BattleCardInstance FindBattleCard(
            BattleRuntimeState runtime,
            string catalogCardId)
        {
            if (runtime == null || string.IsNullOrWhiteSpace(catalogCardId))
            {
                return null;
            }

            return runtime.Deck.Zones.GetCards(CardZone.Hand)
                .Concat(runtime.Deck.Zones.GetCards(CardZone.DrawPile))
                .FirstOrDefault(card => card != null && string.Equals(
                    card.SourceCard.CatalogCardId,
                    catalogCardId,
                    StringComparison.OrdinalIgnoreCase));
        }

        private static bool EnsureInHand(
            BattleRuntimeState runtime,
            BattleCardInstance card)
        {
            if (runtime == null || card == null)
            {
                return false;
            }

            if (runtime.Deck.Zones.GetCards(CardZone.Hand).Any(value =>
                    value != null && string.Equals(
                        value.Ids.BattleCardId,
                        card.Ids.BattleCardId,
                        StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return runtime.Deck.Zones.TryMove(
                card.Ids.BattleCardId,
                CardZone.Hand,
                out _);
        }
    }
}
