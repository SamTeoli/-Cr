using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HaveABreak.Cards
{
    public static class RuntimeConsumableIconCatalog
    {
        private const string ResourceRoot = "UI/ConsumableIcons/";

        private static readonly IReadOnlyDictionary<string, string> Paths =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [PrototypeConsumableCatalog.HealingPotion] =
                    ResourceRoot + "healing_potion",
                [PrototypeConsumableCatalog.CleanseScroll] =
                    ResourceRoot + "cleanse_scroll",
                [PrototypeConsumableCatalog.ManaBattery] =
                    ResourceRoot + "mana_battery",
                [PrototypeConsumableCatalog.EnchantHammer] =
                    ResourceRoot + "enchant_hammer",
                [PrototypeConsumableCatalog.MutationScroll] =
                    ResourceRoot + "mutation_scroll"
            };

        public static Sprite Load(string itemId)
        {
            return !string.IsNullOrWhiteSpace(itemId) &&
                   Paths.TryGetValue(itemId.Trim(), out string path)
                ? Resources.Load<Sprite>(path)
                : null;
        }
    }

    [DisallowMultipleComponent]
    public sealed class RuntimeConsumableTooltipTrigger : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        ISelectHandler,
        IDeselectHandler
    {
        private Action entered;
        private Action exited;

        public bool IsTooltipVisible { get; private set; }

        public void Configure(Action onEntered, Action onExited)
        {
            entered = onEntered;
            exited = onExited;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Show();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Hide();
        }

        public void OnSelect(BaseEventData eventData)
        {
            Show();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            Hide();
        }

        private void OnDisable()
        {
            Hide();
        }

        private void Show()
        {
            IsTooltipVisible = true;
            entered?.Invoke();
        }

        private void Hide()
        {
            IsTooltipVisible = false;
            exited?.Invoke();
        }
    }
}
