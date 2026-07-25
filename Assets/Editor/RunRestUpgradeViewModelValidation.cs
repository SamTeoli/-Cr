using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class RunRestUpgradeViewModelValidation
    {
        [MenuItem("Have a Break/Validate Run Rest Upgrade ViewModel")]
        private static void ValidateFromMenu()
        {
            Debug.Log(Validate()
                ? "Run rest upgrade ViewModel passed."
                : "Run rest upgrade ViewModel failed.");
        }

        internal static bool Validate()
        {
            CardDatabase cards = AssetDatabase.LoadAssetAtPath<CardDatabase>(
                "Assets/GameData/CardDatabase.asset");
            RestUpgradeConfig rules = AssetDatabase.LoadAssetAtPath<RestUpgradeConfig>(
                "Assets/GameData/RestUpgradeConfig.asset");
            CardData[] cardData = cards?.Cards
                .Where(card => card != null)
                .Take(2)
                .ToArray();
            if (rules == null || cardData == null || cardData.Length < 2)
            {
                return false;
            }

            RunRestUpgradeViewModel viewModel = new();
            if (viewModel.CreateCardOptions(null, null).Length != 0 ||
                viewModel.RestButtonLabel(null) != "회복" ||
                viewModel.UpgradeButtonLabel(null) != "선택 카드 강화")
            {
                return false;
            }

            RunCampaignState upgradeCampaign = CampaignAtRestNode();
            RunEncounterProgressState upgradeProgress = CreateProgress(cardData);
            if (upgradeCampaign == null || upgradeProgress == null)
            {
                return false;
            }

            RunCardInstance first = upgradeProgress.OwnedCards.Cards[0];
            RunCardInstance second = upgradeProgress.OwnedCards.Cards[1];
            RunRestUpgradeCardOption[] options = viewModel.CreateCardOptions(
                upgradeCampaign,
                upgradeProgress,
                second.OwnedCardId);
            if (options.Length != 2 ||
                options[0].IsSelected ||
                !options[1].IsSelected ||
                options[0].DisplayLabel !=
                    $"{first.Card.DisplayName} · 레벨 {first.CurrentLevel}" ||
                options[1].DisplayLabel !=
                    $"{second.Card.DisplayName} · 레벨 {second.CurrentLevel}" ||
                viewModel.SelectedOwnedCardId != second.OwnedCardId ||
                viewModel.SelectCard(
                    upgradeCampaign,
                    upgradeProgress,
                    "MISSING-REST-UPGRADE-CARD") ||
                viewModel.SelectedOwnedCardId != second.OwnedCardId)
            {
                return false;
            }

            RunRestUpgradeCardOption cycled = viewModel.CycleCard(
                upgradeCampaign,
                upgradeProgress,
                second.OwnedCardId);
            if (cycled == null ||
                cycled.OwnedCardId != first.OwnedCardId ||
                viewModel.SelectedOwnedCardId != first.OwnedCardId)
            {
                return false;
            }

            RunRestUpgradeCardOption[] preferredOverride =
                viewModel.CreateCardOptions(
                    upgradeCampaign,
                    upgradeProgress,
                    second.OwnedCardId);
            if (preferredOverride.Length != 2 ||
                preferredOverride[0].IsSelected ||
                !preferredOverride[1].IsSelected ||
                viewModel.SelectedOwnedCardId != second.OwnedCardId ||
                !viewModel.SelectCard(
                    upgradeCampaign,
                    upgradeProgress,
                    first.OwnedCardId) ||
                viewModel.SelectedOwnedCardId != first.OwnedCardId)
            {
                return false;
            }

            int previousLevel = second.CurrentLevel;
            if (!viewModel.TryUpgrade(
                    upgradeCampaign,
                    upgradeProgress,
                    rules,
                    second.OwnedCardId,
                    out RunRestUpgradeCardOption upgraded,
                    out string upgradeResult,
                    out RunCampaignFailure upgradeFailure) ||
                upgradeFailure != RunCampaignFailure.None ||
                upgraded == null ||
                upgraded.OwnedCardId != second.OwnedCardId ||
                first.CurrentLevel != 1 ||
                second.CurrentLevel != previousLevel + rules.UpgradeLevelIncrease ||
                string.IsNullOrWhiteSpace(upgradeResult) ||
                upgradeCampaign.CompletedNodeCount != 1 ||
                upgradeCampaign.Phase != RunCampaignPhase.NodeSelection ||
                upgradeCampaign.ActiveNode != null ||
                viewModel.SelectedOwnedCardId != null ||
                viewModel.CreateCardOptions(
                    upgradeCampaign,
                    upgradeProgress).Length != 0)
            {
                return false;
            }

            RunCampaignState restCampaign = CampaignAtRestNode();
            RunBattleState restRun = new(30, 10, 0);
            if (restCampaign == null ||
                !viewModel.TryRest(
                    restCampaign,
                    restRun,
                    rules,
                    out int healed,
                    out string restResult,
                    out RunCampaignFailure restFailure) ||
                restFailure != RunCampaignFailure.None ||
                healed <= 0 ||
                restRun.CurrentHealth != Math.Min(30, 10 + healed) ||
                string.IsNullOrWhiteSpace(restResult) ||
                restCampaign.CompletedNodeCount != 1 ||
                restCampaign.Phase != RunCampaignPhase.NodeSelection ||
                restCampaign.ActiveNode != null ||
                viewModel.SelectedOwnedCardId != null)
            {
                return false;
            }

            RunCampaignState unavailable = new(20260725);
            return viewModel.CreateCardOptions(
                       unavailable,
                       upgradeProgress).Length == 0 &&
                   !viewModel.TryRest(
                       unavailable,
                       restRun,
                       rules,
                       out _,
                       out _,
                       out _);
        }

        private static RunEncounterProgressState CreateProgress(
            CardData[] cardData)
        {
            RunOwnedCardState owned = new();
            RunDeckState deck = new();
            for (int index = 0; index < cardData.Length; index++)
            {
                RunCardInstance card = new(
                    cardData[index],
                    $"REST-UPGRADE-OWNED-{index + 1:00}",
                    index + 1);
                if (!owned.TryAdd(card, out _) || !deck.TryAdd(card, out _))
                {
                    return null;
                }
            }

            return new RunEncounterProgressState(
                new RunBattleState(30, 30, 0),
                owned,
                deck,
                new PlayerPermanentRewardState(),
                Array.Empty<string>(),
                0);
        }

        private static RunCampaignState CampaignAtRestNode()
        {
            for (int seed = 0; seed < 1000; seed++)
            {
                RunCampaignState campaign = new(seed);
                RunNodeChoice choice = RunCampaignService.GetChoices(campaign)
                    .FirstOrDefault(value =>
                        value.NodeType == RunNodeType.RestOrUpgrade);
                if (choice != null && RunCampaignService.TrySelectNode(
                        campaign,
                        choice.NodeId,
                        out _))
                {
                    return campaign;
                }
            }

            return null;
        }
    }
}
