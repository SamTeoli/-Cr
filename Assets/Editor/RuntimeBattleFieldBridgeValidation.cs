using System;
using System.Linq;
using System.Reflection;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HaveABreak.Editor
{
    internal static class RuntimeBattleFieldBridgeValidation
    {
        [MenuItem("Have a Break/Tests/Validate Runtime Battle Field Bridge")]
        private static void RunFromMenu()
        {
            Validate();
        }

        internal static bool Validate()
        {
            RuntimePrototypeConfig config =
                Resources.Load<RuntimePrototypeConfig>(
                    "GameData/RuntimePrototypeConfig");
            EventSystem previousEventSystem = EventSystem.current;
            GameObject host = new("Runtime Battle Field Bridge Validation");

            try
            {
                RunEncounterProgressState progress =
                    CreateProgress(config?.CardDatabase);
                RunCampaignState campaign = new(20260728);
                RunNodeChoice battleNode = RunCampaignService
                    .GetChoices(campaign)
                    .FirstOrDefault(option => option.IsBattle);
                if (config == null || !config.IsReady || progress == null ||
                    battleNode == null ||
                    !RunCampaignService.TrySelectNode(
                        campaign,
                        battleNode.NodeId,
                        out _) ||
                    !new BattleStartViewModel().TryStart(
                        campaign,
                        progress,
                        config).BattleStarted)
                {
                    Debug.LogError(
                        "Runtime battle field bridge validation failed: " +
                        "could not create validation battle.");
                    return false;
                }

                RuntimePrototypeScreen prototype =
                    host.AddComponent<RuntimePrototypeScreen>();
                prototype.Initialize(config);
                const BindingFlags members =
                    BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(RuntimePrototypeScreen).GetField("campaign", members)
                    ?.SetValue(prototype, campaign);
                typeof(RuntimePrototypeScreen).GetField("progress", members)
                    ?.SetValue(prototype, progress);

                MethodInfo showFinalUi =
                    typeof(RuntimePrototypeScreen).GetMethod(
                        "TryShowFinalUi",
                        members);
                MethodInfo refreshField =
                    typeof(RuntimePrototypeScreen).GetMethod(
                        "RefreshFinalBattleFieldOverlay",
                        members);
                bool shown = showFinalUi?.Invoke(prototype, null) as bool? == true;
                refreshField?.Invoke(prototype, null);

                RuntimeGameUiRoot root = prototype.FinalUiRoot;
                RuntimeBattleFieldView field =
                    root?.GetComponentInChildren<RuntimeBattleFieldView>(true);
                bool structure = shown && root != null &&
                                 root.CurrentScreen == RuntimeGameScreen.Battle &&
                                 field != null &&
                                 field.EnemySlots.Count == 3 &&
                                 field.MonsterSlots.Count == 3 &&
                                 field.SkillSlots.Count == 3;
                bool enemyState = field != null && field.EnemySlots.Any(slot =>
                    slot.Presentation?.Occupied == true &&
                    slot.Presentation.Selected &&
                    slot.Button.interactable &&
                    slot.DropZone.AcceptsCards);
                int monsterDropCount = field?.MonsterSlots.Count(slot =>
                    slot.DropZone.AcceptsCards &&
                    slot.DropZone.TargetCommandId.StartsWith(
                        "field:monster:",
                        StringComparison.Ordinal)) ?? 0;
                int skillDropCount = field?.SkillSlots.Count(slot =>
                    slot.DropZone.AcceptsCards &&
                    slot.DropZone.TargetCommandId.StartsWith(
                        "field:skill:",
                        StringComparison.Ordinal)) ?? 0;
                bool fieldDrops = monsterDropCount == 3 &&
                                  skillDropCount == 1;
                bool legacyHidden = root?.BattleCommandList != null &&
                                    !Enumerable.Range(
                                            0,
                                            root.BattleCommandList.childCount)
                                        .Select(index =>
                                            root.BattleCommandList.GetChild(index)
                                                .gameObject)
                                        .Where(value => value.activeSelf)
                                        .Select(value =>
                                            value.GetComponentInChildren<Text>(true)
                                                ?.text ?? string.Empty)
                                        .Any(text =>
                                            text.StartsWith(
                                                "[적 대상]",
                                                StringComparison.Ordinal) ||
                                            text.StartsWith(
                                                "[공격]",
                                                StringComparison.Ordinal));

                bool valid = structure && enemyState && fieldDrops &&
                             legacyHidden;
                if (valid)
                {
                    Debug.Log(
                        "Runtime battle field bridge validation passed: final " +
                        "UGUI exposes three enemy, monster, and skill slots with " +
                        "selection and all-empty monster-zone card-drop routing.");
                }
                else
                {
                    Debug.LogError(
                        "Runtime battle field bridge validation failed. " +
                        $"structure={structure}, enemyState={enemyState}, " +
                        $"monsterDrops={monsterDropCount}, " +
                        $"skillDrops={skillDropCount}, " +
                        $"legacyHidden={legacyHidden}");
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
                            $"OWNED-FIELD-UI-{catalogCardId}"),
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
