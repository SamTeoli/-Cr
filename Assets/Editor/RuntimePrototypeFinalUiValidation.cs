using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HaveABreak.Cards;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HaveABreak.Editor
{
    internal static class RuntimePrototypeFinalUiValidation
    {
        public static void RunBatchMode()
        {
            if (!Validate())
            {
                throw new InvalidOperationException(
                    "Final UI prototype bridge validation failed.");
            }
        }

        internal static bool Validate()
        {
            RuntimePrototypeConfig config =
                Resources.Load<RuntimePrototypeConfig>(
                    "GameData/RuntimePrototypeConfig");
            EventSystem previousEventSystem = EventSystem.current;
            GameObject host = new("Final UI Prototype Validation");

            try
            {
                RuntimePrototypeScreen prototype =
                    host.AddComponent<RuntimePrototypeScreen>();
                prototype.Initialize(config);
                RuntimeGameUiRoot root = prototype.FinalUiRoot;
                bool start = config != null && config.IsReady &&
                             root != null &&
                             root.CurrentScreen == RuntimeGameScreen.Start &&
                             root.RootCanvas.gameObject.activeSelf;

                MethodInfo beginPreparation =
                    typeof(RuntimePrototypeScreen).GetMethod(
                        "BeginRunPreparation",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                beginPreparation?.Invoke(prototype, null);

                bool preparation =
                    root.CurrentScreen == RuntimeGameScreen.RunPreparation &&
                    root.RunPreparationCardList.childCount > 0 &&
                    root.ConfirmRunPreparationButton.interactable;
                int selectedBefore = ParseSelectedCount(
                    root.RunPreparationSelectedCountText.text);
                Button firstCard = preparation
                    ? root.RunPreparationCardList.GetChild(0)
                        .GetComponent<Button>()
                    : null;
                bool hadCardButton = firstCard != null;
                firstCard?.onClick.Invoke();
                int selectedAfter = ParseSelectedCount(
                    root.RunPreparationSelectedCountText.text);
                bool toggle = hadCardButton &&
                              selectedAfter == selectedBefore - 1;

                root.CancelRunPreparationButton.onClick.Invoke();
                bool cancel = root.CurrentScreen == RuntimeGameScreen.Start &&
                              root.RootCanvas.gameObject.activeSelf;
                bool battle = ValidateBattleBridge(
                    prototype,
                    root,
                    config);
                bool valid = start && preparation && toggle && cancel &&
                             battle;
                if (valid)
                {
                    Debug.Log(
                        "Final UI prototype bridge validation passed: " +
                        "start, preparation, card toggle, cancellation, " +
                        "and battle command routing.");
                }
                else
                {
                    Debug.LogError(
                        "Final UI prototype bridge validation failed. " +
                        $"start={start}, preparation={preparation}, " +
                        $"toggle={toggle} ({selectedBefore}->{selectedAfter}), " +
                        $"cancel={cancel}, battle={battle}");
                }

                return valid;
            }
            finally
            {
                Object.DestroyImmediate(host);
                if (previousEventSystem != null)
                {
                    EventSystem.current = previousEventSystem;
                }
            }
        }

        private static int ParseSelectedCount(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return -1;
            }

            string[] parts = value.Split(' ');
            return parts.Length > 1 &&
                   int.TryParse(parts[1].TrimEnd('장'), out int count)
                ? count
                : -1;
        }

        private static bool ValidateBattleBridge(
            RuntimePrototypeScreen prototype,
            RuntimeGameUiRoot root,
            RuntimePrototypeConfig config)
        {
            RunEncounterProgressState progress =
                CreateProgress(config?.CardDatabase);
            RunCampaignState campaign = new(20260727);
            RunNodeChoice battleNode = RunCampaignService
                .GetChoices(campaign)
                .FirstOrDefault(option => option.IsBattle);
            if (progress == null || battleNode == null ||
                !RunCampaignService.TrySelectNode(
                    campaign,
                    battleNode.NodeId,
                    out _) ||
                !new BattleStartViewModel().TryStart(
                    campaign,
                    progress,
                    config).BattleStarted)
            {
                return false;
            }

            const BindingFlags fields =
                BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(RuntimePrototypeScreen).GetField("campaign", fields)
                ?.SetValue(prototype, campaign);
            typeof(RuntimePrototypeScreen).GetField("progress", fields)
                ?.SetValue(prototype, progress);
            MethodInfo showFinalUi =
                typeof(RuntimePrototypeScreen).GetMethod(
                    "TryShowFinalUi",
                    fields);
            bool shown = showFinalUi?.Invoke(prototype, null) as bool? == true;
            bool initial = shown &&
                           root.CurrentScreen == RuntimeGameScreen.Battle &&
                           root.BattleCommandList.childCount > 0 &&
                           !string.IsNullOrWhiteSpace(
                               root.BattleSummaryText.text);
            if (!initial)
            {
                return false;
            }

            int commandsBefore = root.BattleCommandList.childCount;
            Button firstCommand = root.BattleCommandList.GetChild(0)
                .GetComponent<Button>();
            bool hadCommand = firstCommand != null &&
                              firstCommand.interactable;
            firstCommand?.onClick.Invoke();
            return hadCommand &&
                   root.CurrentScreen == RuntimeGameScreen.Battle &&
                   root.BattleCommandList.childCount == commandsBefore;
        }

        private static RunEncounterProgressState CreateProgress(
            CardDatabase database)
        {
            if (database == null)
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
                if (data == null ||
                    !deck.TryAdd(
                        new RunCardInstance(
                            data,
                            $"OWNED-FINAL-UI-{catalogCardId}"),
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
                    Array.Empty<string>()),
                deck);
        }
    }
}
