using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleScreenViewModelValidation
    {
        [MenuItem("Have a Break/Validate Battle Screen ViewModel")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            if (valid)
            {
                Debug.Log("Battle screen ViewModel passed.");
            }
            else
            {
                Debug.LogError("Battle screen ViewModel failed.");
            }
        }

        internal static bool Validate()
        {
            BattleScreenViewModel viewModel = new();
            BattleScreenSnapshot empty = viewModel.CreateSnapshot(null, null);
            if (empty.Available ||
                string.IsNullOrWhiteSpace(empty.ErrorText) ||
                empty.CanEndTurn || empty.CanSettle ||
                empty.Enemies.Length != 0 || empty.Hand.Length != 0)
            {
                return false;
            }

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
                if (cards == null || progress == null || battleItems.Length == 0)
                {
                    return false;
                }

                enemy.EditorInitialize(
                    "TEST-ENEMY-SCREEN-VM",
                    "Test Battle Screen Enemy",
                    1,
                    50);
                encounter.EditorInitialize(
                    "TEST-ENCOUNTER-SCREEN-VM",
                    "Test Battle Screen Encounter",
                    BattleEncounterGrade.Normal,
                    new[]
                    {
                        new EncounterEnemySlot(
                            "TEST-ENEMY-SCREEN-VM-A",
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

                RunCampaignState campaign = new(20260726);
                BattleScreenSnapshot first =
                    viewModel.CreateSnapshot(progress, campaign);
                BattleEnemyDisplayOption selectedEnemy = first.Enemies
                    .FirstOrDefault(option => option.IsSelected);
                BattleHandCardActionOption c07Option = first.Hand
                    .FirstOrDefault(option => option.BattleCardId ==
                        c07.Ids.BattleCardId);
                if (!first.Available || !first.CanEndTurn || first.CanSettle ||
                    string.IsNullOrWhiteSpace(first.TitleText) ||
                    string.IsNullOrWhiteSpace(first.PlayerSummaryText) ||
                    string.IsNullOrWhiteSpace(first.ZoneSummaryText) ||
                    string.IsNullOrWhiteSpace(first.CheckpointNoticeText) ||
                    first.Enemies.Length !=
                        Enum.GetValues(typeof(EnemyFieldPosition)).Length ||
                    first.Monsters.Length !=
                        Enum.GetValues(typeof(PlayerMonsterFieldPosition)).Length ||
                    first.Hand.Length != runtime.Deck.Zones.Count(CardZone.Hand) ||
                    first.InstalledCards.Length != 0 ||
                    first.Consumables.Length != battleItems.Length ||
                    selectedEnemy == null || !selectedEnemy.IsOccupied ||
                    string.IsNullOrWhiteSpace(selectedEnemy.DisplayText) ||
                    string.IsNullOrWhiteSpace(selectedEnemy.IntentText) ||
                    c07Option == null || c07Option.BanishTargets.Length == 0 ||
                    c07Option.SelectedBanishTarget == null ||
                    viewModel.SelectEnemy(progress, "MISSING-ENEMY") ||
                    !viewModel.SelectEnemy(progress, selectedEnemy.EnemyId))
                {
                    return false;
                }

                string firstBanishId =
                    c07Option.SelectedBanishTarget.BattleCardId;
                BattleBanishTargetOption cycled =
                    viewModel.CycleBanishTarget(
                        progress,
                        c07.Ids.BattleCardId);
                if (cycled == null ||
                    !viewModel.SelectBanishTarget(
                        progress,
                        c07.Ids.BattleCardId,
                        firstBanishId))
                {
                    return false;
                }

                int handBefore = first.Hand.Length;
                BattleCardPlayCommandResult play = viewModel.TryPlayCard(
                    progress,
                    c01.Ids.BattleCardId);
                BattleScreenSnapshot afterPlay =
                    viewModel.CreateSnapshot(progress, campaign);
                BattleMonsterDisplayOption summoned = afterPlay.Monsters
                    .FirstOrDefault(option => option.BattleCardId ==
                        c01.Ids.BattleCardId);
                if (!play.Succeeded || play.Result == null ||
                    afterPlay.Hand.Length != handBefore - 1 ||
                    summoned == null || !summoned.IsOccupied ||
                    string.IsNullOrWhiteSpace(summoned.DisplayText) ||
                    afterPlay.RecentEvents.Length == 0)
                {
                    return false;
                }

                int enemyHealthBefore = context.Runtime
                    .FindEnemy(selectedEnemy.EnemyId)
                    .Vital.CurrentHealth;
                BattleMonsterAttackCommandResult attack = viewModel.TryAttack(
                    progress,
                    c01.Ids.BattleCardId);
                BattleScreenSnapshot afterAttack =
                    viewModel.CreateSnapshot(progress, campaign);
                BattleEnemyDisplayOption attackedEnemy = afterAttack.Enemies
                    .FirstOrDefault(option => option.EnemyId ==
                        selectedEnemy.EnemyId);
                if (!attack.Succeeded || attack.Result == null ||
                    attackedEnemy?.Target?.Enemy == null ||
                    attackedEnemy.Target.Enemy.Vital.CurrentHealth >=
                        enemyHealthBefore ||
                    afterAttack.RecentEvents.Length == 0 ||
                    afterAttack.RecentEvents.Length > 6 ||
                    afterAttack.RecentEvents.Any(option =>
                        string.IsNullOrWhiteSpace(option.DisplayText)))
                {
                    return false;
                }

                BattleConsumableActionOption item = afterAttack.Consumables
                    .FirstOrDefault(option => option.CanUse);
                if (item == null)
                {
                    return false;
                }

                BattleConsumableCommandResult itemResult =
                    viewModel.TryUseConsumable(progress, item.ItemId);
                BattleScreenSnapshot afterItem =
                    viewModel.CreateSnapshot(progress, campaign);
                BattleConsumableActionOption consumed = afterItem.Consumables
                    .FirstOrDefault(option => string.Equals(
                        option.ItemId,
                        item.ItemId,
                        StringComparison.OrdinalIgnoreCase));
                if (!itemResult.Succeeded || consumed == null ||
                    consumed.RemainingCount != 0 || consumed.CanUse)
                {
                    return false;
                }

                int playerTurnBefore = runtime.Turn.PlayerTurnNumber;
                BattleEndTurnCommandResult turn =
                    viewModel.TryEndPlayerTurn(progress, campaign);
                BattleScreenSnapshot afterTurn =
                    viewModel.CreateSnapshot(progress, campaign);
                if (!turn.Succeeded || turn.Result == null ||
                    !afterTurn.Available ||
                    turn.Result.Outcome == BattleOutcome.Ongoing &&
                    runtime.Turn.PlayerTurnNumber <= playerTurnBefore)
                {
                    return false;
                }

                viewModel.Reset();
                BattleScreenSnapshot afterReset =
                    viewModel.CreateSnapshot(progress, campaign);
                return afterReset.Available &&
                       afterReset.Enemies.Count(option => option.IsSelected) <= 1 &&
                       afterReset.RecentEvents.Length <= 6;
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
                            $"OWNED-SCREEN-VM-{catalogCardId}"),
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
                "TEST-BATTLE-SCREEN-VM",
                encounter,
                720,
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
