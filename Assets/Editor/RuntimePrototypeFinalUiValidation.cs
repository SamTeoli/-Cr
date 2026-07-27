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
                        "battle command routing, reward completion, " +
                        "run result routing, confirmation, and restart.");
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
                           root.BattleHandCardList.childCount > 0 &&
                           root.BattleHandCardList.GetChild(0)
                               .GetComponent<RuntimeCardView>() != null &&
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
            bool battleCommand = hadCommand &&
                                 root.CurrentScreen ==
                                 RuntimeGameScreen.Battle &&
                                 root.BattleCommandList.childCount ==
                                 commandsBefore;
            RuntimeCardView playableCard = Enumerable.Range(
                    0,
                    root.BattleHandCardList.childCount)
                .Select(index => root.BattleHandCardList.GetChild(index)
                    .GetComponent<RuntimeCardView>())
                .FirstOrDefault(card =>
                    card != null && card.ClickButton.interactable);
            for (int attempt = 0;
                 playableCard == null && attempt < 2;
                 attempt++)
            {
                Button endTurn = Enumerable.Range(
                        0,
                        root.BattleCommandList.childCount)
                    .Select(index => root.BattleCommandList.GetChild(index)
                        .GetComponent<Button>())
                    .FirstOrDefault(button =>
                        button != null &&
                        button.interactable &&
                        button.GetComponentInChildren<Text>().text ==
                        "턴 종료");
                endTurn?.onClick.Invoke();
                playableCard = Enumerable.Range(
                        0,
                        root.BattleHandCardList.childCount)
                    .Select(index => root.BattleHandCardList.GetChild(index)
                        .GetComponent<RuntimeCardView>())
                    .FirstOrDefault(card =>
                        card != null && card.ClickButton.interactable);
            }
            string playedCommand = playableCard?.Presentation.CommandId;
            bool hadPlayableCard = playableCard != null;
            playableCard?.ClickButton.onClick.Invoke();
            bool cardCommand = hadPlayableCard &&
                               root.CurrentScreen ==
                               RuntimeGameScreen.Battle &&
                               !Enumerable.Range(
                                       0,
                                       root.BattleHandCardList.childCount)
                                   .Select(index =>
                                       root.BattleHandCardList.GetChild(index)
                                           .GetComponent<RuntimeCardView>())
                                   .Any(card =>
                                       card?.Presentation.CommandId ==
                                       playedCommand);
            bool rewardBridge = battleCommand && cardCommand &&
                                ValidateRewardBridge(
                                    prototype,
                                    root,
                                    campaign,
                                    progress);
            if (!rewardBridge)
            {
                Debug.LogError(
                    "Final UI battle card bridge detail: " +
                    $"initial={initial}, hadCommand={hadCommand}, " +
                    $"battleCommand={battleCommand}, " +
                    $"playableCard={hadPlayableCard}, " +
                    $"playedCommand={playedCommand}, " +
                    $"cardCommand={cardCommand}, rewardBridge={rewardBridge}");
            }
            return rewardBridge;
        }

        private static bool ValidateRewardBridge(
            RuntimePrototypeScreen prototype,
            RuntimeGameUiRoot root,
            RunCampaignState campaign,
            RunEncounterProgressState progress)
        {
            BattleRuntimeEncounterContext context = progress.ActiveEncounter;
            BattleEnemyRuntimeState[] living = context?.Runtime?.Enemies
                .Where(enemy => enemy != null && enemy.IsAlive)
                .ToArray();
            if (context?.Session == null || living == null ||
                living.Length == 0)
            {
                return false;
            }

            foreach (BattleEnemyRuntimeState enemy in living)
            {
                int health = enemy.Vital.CurrentHealth;
                if (health <= 0 || enemy.Vital.ApplyDamage(health) <= 0 ||
                    !context.Runtime.LivingEnemies.TryRemove(enemy.EnemyId))
                {
                    return false;
                }
            }

            if (!BattleRuntimeSessionService.TryFinalizeTerminalOutcome(
                    context.Session,
                    out BattleOutcome outcome,
                    out _) ||
                outcome != BattleOutcome.Victory ||
                !new BattleSettlementViewModel().TrySettle(
                    campaign,
                    progress).Succeeded)
            {
                return false;
            }

            const BindingFlags fields =
                BindingFlags.Instance | BindingFlags.NonPublic;
            MethodInfo showFinalUi =
                typeof(RuntimePrototypeScreen).GetMethod(
                    "TryShowFinalUi",
                    fields);
            bool shown = showFinalUi?.Invoke(prototype, null) as bool? == true;
            if (!shown || root.CurrentScreen != RuntimeGameScreen.Reward ||
                root.RewardCommandList.childCount == 0 ||
                string.IsNullOrWhiteSpace(root.RewardSummaryText.text))
            {
                return false;
            }

            for (int pass = 0; pass < 8; pass++)
            {
                Button complete = null;
                Button claim = null;
                for (int index = 0;
                     index < root.RewardCommandList.childCount;
                     index++)
                {
                    Button button = root.RewardCommandList.GetChild(index)
                        .GetComponent<Button>();
                    Text label = button?.GetComponentInChildren<Text>();
                    if (label?.text.StartsWith("보상 완료") == true)
                    {
                        complete = button;
                    }
                    else if (button?.interactable == true && claim == null)
                    {
                        claim = button;
                    }
                }

                if (complete?.interactable == true)
                {
                    complete.onClick.Invoke();
                    bool advanced = showFinalUi?.Invoke(
                        prototype,
                        null) as bool? == true;
                    return campaign.Phase != RunCampaignPhase.Reward &&
                           !progress.HasActiveEncounter &&
                           advanced &&
                           root.CurrentScreen ==
                           RuntimeGameScreen.NodeSelection &&
                           ValidateRunEndBridge(
                               prototype,
                               root,
                               campaign,
                               showFinalUi);
                }

                if (claim == null)
                {
                    return false;
                }
                claim.onClick.Invoke();
            }

            return false;
        }

        private static bool ValidateRunEndBridge(
            RuntimePrototypeScreen prototype,
            RuntimeGameUiRoot root,
            RunCampaignState campaign,
            MethodInfo showFinalUi)
        {
            const BindingFlags fields =
                BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo phase = typeof(RunCampaignState).GetField(
                "phase",
                fields);
            if (phase == null)
            {
                return false;
            }

            phase.SetValue(campaign, RunCampaignPhase.Completed);
            bool completedShown =
                showFinalUi?.Invoke(prototype, null) as bool? == true &&
                root.CurrentScreen == RuntimeGameScreen.Completed &&
                root.RunResultTitleText.text == "런 완료" &&
                !string.IsNullOrWhiteSpace(root.RunResultSummaryText.text);
            if (!completedShown)
            {
                return false;
            }

            root.ReturnToStartButton.onClick.Invoke();
            bool returned = root.CurrentScreen == RuntimeGameScreen.Start;
            root.NewRunButton.onClick.Invoke();
            bool confirmation =
                root.CurrentScreen == RuntimeGameScreen.Confirmation &&
                root.ConfirmationTitleText.text == "새 런을 시작할까요?";
            root.ConfirmActionButton.onClick.Invoke();
            bool preparation =
                root.CurrentScreen == RuntimeGameScreen.RunPreparation &&
                root.RunPreparationCardList.childCount > 0;
            root.CancelRunPreparationButton.onClick.Invoke();

            phase.SetValue(campaign, RunCampaignPhase.Defeated);
            bool defeatedShown =
                showFinalUi?.Invoke(prototype, null) as bool? == true &&
                root.CurrentScreen == RuntimeGameScreen.Defeated &&
                root.RunResultTitleText.text == "런 패배";
            root.ReturnToStartButton.onClick.Invoke();
            bool defeatedReturned =
                root.CurrentScreen == RuntimeGameScreen.Start;
            return returned && confirmation && preparation &&
                   defeatedShown && defeatedReturned;
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
