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
    internal static class RuntimeConsumableBarValidation
    {
        [MenuItem("Have a Break/Tests/Validate Runtime Consumable Bar")]
        private static void RunFromMenu()
        {
            Validate();
        }

        internal static bool Validate()
        {
            EventSystem previousEventSystem = EventSystem.current;
            GameObject host = new("Runtime Consumable Bar Validation");
            ConsumableData healing = null;
            ConsumableData cleanse = null;
            ConsumableData mana = null;

            try
            {
                healing = CreateItem(
                    PrototypeConsumableCatalog.HealingPotion,
                    "회복 포션",
                    "플레이어 HP를 5 회복한다.",
                    ConsumableEffect.HealPlayer,
                    5);
                cleanse = CreateItem(
                    PrototypeConsumableCatalog.CleanseScroll,
                    "해제 주문서",
                    "플레이어의 상태이상을 모두 해제한다.",
                    ConsumableEffect.ClearPlayerStatuses,
                    0);
                mana = CreateItem(
                    PrototypeConsumableCatalog.ManaBattery,
                    "비상 전지",
                    "현재 마력을 2 회복한다.",
                    ConsumableEffect.RestoreMana,
                    2);

                RuntimeGameUiRoot root =
                    host.AddComponent<RuntimeGameUiRoot>();
                string requestedCommand = null;
                root.BattleCommandRequested +=
                    commandId => requestedCommand = commandId;
                root.Initialize();
                root.BindBattle(
                    "검증 전투",
                    new[]
                    {
                        new RuntimeGameCommandOption("end-turn", "턴 종료")
                    },
                    "전투 상태",
                    "검증 메시지");
                root.BindBattleConsumables(
                    new[]
                    {
                        CreateOption(healing, 2, 0),
                        CreateOption(cleanse, 1, 1),
                        CreateOption(mana, 1, 0)
                    });
                root.ShowScreen(RuntimeGameScreen.Battle);

                bool resources =
                    RuntimeConsumableIconCatalog.Load(
                        PrototypeConsumableCatalog.HealingPotion) != null &&
                    RuntimeConsumableIconCatalog.Load(
                        PrototypeConsumableCatalog.CleanseScroll) != null &&
                    RuntimeConsumableIconCatalog.Load(
                        PrototypeConsumableCatalog.ManaBattery) != null &&
                    RuntimeConsumableIconCatalog.Load(
                        PrototypeConsumableCatalog.EnchantHammer) != null &&
                    RuntimeConsumableIconCatalog.Load(
                        PrototypeConsumableCatalog.MutationScroll) != null &&
                    RuntimeConsumableIconCatalog.Load("INVALID") == null;

                bool structure =
                    root.BattleConsumableBar != null &&
                    root.BattleConsumableIconList != null &&
                    root.BattleConsumableTooltipText != null &&
                    root.BattleConsumableIconList.childCount == 3 &&
                    root.BattleConsumableBar.GetSiblingIndex() ==
                    root.BattleRelicBar.GetSiblingIndex() + 1 &&
                    !root.GetComponentsInChildren<Text>(true).Any(text =>
                        text.text == "카드를 이곳에 놓아 사용") &&
                    root.BattleCommandList.childCount == 0 &&
                    root.BattleEndTurnButton != null &&
                    !root.BattleCommandList.GetComponentsInChildren<Text>(true)
                        .Any(text => text.text.StartsWith(
                            "[소모품]",
                            StringComparison.Ordinal));

                Transform healingIcon =
                    root.BattleConsumableIconList.GetChild(0);
                Button healingButton = healingIcon.GetComponent<Button>();
                Image healingImage = healingIcon.GetComponent<Image>();
                RuntimeConsumableTooltipTrigger healingTooltip =
                    healingIcon.GetComponent<RuntimeConsumableTooltipTrigger>();
                Text healingCount =
                    healingIcon.Find("Count")?.GetComponent<Text>();
                healingTooltip.OnPointerEnter(null);
                bool hover =
                    healingTooltip.IsTooltipVisible &&
                    root.BattleConsumableTooltipText.gameObject.activeSelf &&
                    root.BattleConsumableTooltipText.text.Contains("회복 포션") &&
                    root.BattleConsumableTooltipText.text.Contains("HP를 5") &&
                    healingCount != null &&
                    healingCount.text == "×2";
                healingTooltip.OnPointerExit(null);
                bool hoverCleared =
                    !healingTooltip.IsTooltipVisible &&
                    !root.BattleConsumableTooltipText.gameObject.activeSelf;

                healingButton.onClick.Invoke();
                bool click =
                    healingButton.interactable &&
                    healingImage.sprite != null &&
                    requestedCommand ==
                    $"consumable:{PrototypeConsumableCatalog.HealingPotion}";

                Transform cleanseIcon =
                    root.BattleConsumableIconList.GetChild(1);
                Button cleanseButton = cleanseIcon.GetComponent<Button>();
                RuntimeConsumableTooltipTrigger cleanseTooltip =
                    cleanseIcon.GetComponent<RuntimeConsumableTooltipTrigger>();
                requestedCommand = null;
                cleanseButton.OnPointerClick(
                    new PointerEventData(EventSystem.current));
                cleanseTooltip.OnPointerEnter(null);
                bool disabled =
                    !cleanseButton.interactable &&
                    requestedCommand == null &&
                    root.BattleConsumableTooltipText.text.Contains(
                        "남은 수량 없음");
                cleanseTooltip.OnPointerExit(null);

                bool valid = resources && structure && hover &&
                             hoverCleared && click && disabled;
                if (valid)
                {
                    Debug.Log(
                        "Runtime consumable bar validation passed: five icon " +
                        "resources load, battle items render in the top bar, " +
                        "hover tooltips show item details, clicks emit the " +
                        "existing consumable command, and empty items stay disabled.");
                }
                else
                {
                    Debug.LogError(
                        "Runtime consumable bar validation failed. " +
                        $"resources={resources}, structure={structure}, " +
                        $"hover={hover}, hoverCleared={hoverCleared}, " +
                        $"click={click}, disabled={disabled}");
                }

                return valid;
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(healing);
                Object.DestroyImmediate(cleanse);
                Object.DestroyImmediate(mana);
                if (previousEventSystem != null)
                {
                    EventSystem.current = previousEventSystem;
                }
            }
        }

        private static ConsumableData CreateItem(
            string itemId,
            string displayName,
            string rulesText,
            ConsumableEffect effect,
            int amount)
        {
            ConsumableData item =
                ScriptableObject.CreateInstance<ConsumableData>();
            item.EditorInitialize(
                itemId,
                displayName,
                rulesText,
                effect,
                amount,
                0);
            return item;
        }

        private static BattleConsumableActionOption CreateOption(
            ConsumableData item,
            int owned,
            int consumed)
        {
            return Activator.CreateInstance(
                typeof(BattleConsumableActionOption),
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new object[] { item, owned, consumed, false },
                null) as BattleConsumableActionOption;
        }
    }
}
