using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HaveABreak.Cards
{
    [DisallowMultipleComponent]
    public sealed class RuntimeBattleFieldView : MonoBehaviour
    {
        private static readonly Color EnemyColor =
            new(0.22f, 0.07f, 0.085f, 0.94f);
        private static readonly Color MonsterColor =
            new(0.055f, 0.16f, 0.25f, 0.94f);
        private static readonly Color SkillColor =
            new(0.12f, 0.085f, 0.2f, 0.94f);
        private static readonly Color EmptyColor =
            new(0.025f, 0.045f, 0.065f, 0.72f);
        private static readonly Color HoverColor =
            new(0.23f, 0.55f, 0.78f, 1f);

        private readonly List<RuntimeBattleFieldSlotView> enemySlots = new();
        private readonly List<RuntimeBattleFieldSlotView> monsterSlots = new();
        private readonly List<RuntimeBattleFieldSlotView> skillSlots = new();
        private Action<string> commandRequested;
        private Action<RuntimeBattleFieldSlotPresentation> detailRequested;
        private bool initialized;

        public IReadOnlyList<RuntimeBattleFieldSlotView> EnemySlots => enemySlots;
        public IReadOnlyList<RuntimeBattleFieldSlotView> MonsterSlots => monsterSlots;
        public IReadOnlyList<RuntimeBattleFieldSlotView> SkillSlots => skillSlots;

        public void Initialize(
            Action<string> command,
            Action<RuntimeBattleFieldSlotPresentation> inspect = null)
        {
            commandRequested = command;
            detailRequested = inspect;
            if (initialized)
            {
                return;
            }

            initialized = true;
            RectTransform root = transform as RectTransform;
            VerticalLayoutGroup layout = gameObject.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = gameObject.AddComponent<VerticalLayoutGroup>();
            }
            layout.padding = new RectOffset(18, 18, 10, 10);
            layout.spacing = 5f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateRow(root, "EnemyRow", "적 필드", RuntimeBattleFieldZone.Enemy,
                EnemyColor, enemySlots);
            CreateFieldDivider(root);
            CreateRow(root, "MonsterRow", "몬스터존", RuntimeBattleFieldZone.PlayerMonster,
                MonsterColor, monsterSlots);
            CreateRow(root, "SkillRow", "스킬존", RuntimeBattleFieldZone.PlayerSkill,
                SkillColor, skillSlots);
        }

        public void Bind(RuntimeBattleFieldPresentation presentation)
        {
            if (!initialized)
            {
                Initialize(commandRequested);
            }

            RuntimeBattleFieldPresentation source =
                presentation ?? RuntimeBattleFieldPresentation.Empty;
            BindRow(enemySlots, source.Enemies, EnemyColor);
            BindRow(monsterSlots, source.Monsters, MonsterColor);
            BindRow(skillSlots, source.Skills, SkillColor);
        }

        private void CreateRow(
            Transform parent,
            string name,
            string label,
            RuntimeBattleFieldZone zone,
            Color zoneColor,
            ICollection<RuntimeBattleFieldSlotView> slots)
        {
            GameObject rowObject = new(
                name,
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            rowObject.transform.SetParent(parent, false);
            HorizontalLayoutGroup row = rowObject.GetComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(4, 4, 2, 2);
            row.spacing = 8f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            bool cardZone = zone != RuntimeBattleFieldZone.Enemy;
            rowObject.GetComponent<LayoutElement>().preferredHeight =
                cardZone ? 174f : 148f;

            Text rowLabel = CreateText(
                "ZoneLabel",
                rowObject.transform,
                label,
                17,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);
            LayoutElement labelLayout = rowLabel.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 110f;
            labelLayout.flexibleWidth = 0f;

            for (int index = 0;
                 index < RuntimeBattleFieldPresentation.SlotCount;
                 index++)
            {
                GameObject slotObject = new(
                    $"{zone}_{index}",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button),
                    typeof(LayoutElement),
                    typeof(Outline),
                    typeof(RuntimeBattleFieldSlotView));
                slotObject.transform.SetParent(rowObject.transform, false);
                Image image = slotObject.GetComponent<Image>();
                image.color = EmptyColor;
                image.raycastTarget = true;
                LayoutElement slotLayout = slotObject.GetComponent<LayoutElement>();
                slotLayout.preferredWidth = 170f;
                slotLayout.flexibleWidth = 0f;
                slotLayout.preferredHeight = cardZone ? 168f : 142f;

                Text text = CreateText(
                    "Label",
                    slotObject.transform,
                    string.Empty,
                    13,
                    FontStyle.Normal,
                    TextAnchor.MiddleCenter);
                RectTransform textRect = text.rectTransform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(8f, 3f);
                textRect.offsetMax = new Vector2(-8f, -3f);

                RuntimeBattleFieldSlotView slot =
                    slotObject.GetComponent<RuntimeBattleFieldSlotView>();
                slot.Initialize(
                    text,
                    image,
                    slotObject.GetComponent<Button>(),
                    slotObject.GetComponent<Outline>(),
                    cardZone);
                slots.Add(slot);
            }
        }

        private static void CreateFieldDivider(Transform parent)
        {
            GameObject divider = new(
                "FieldCenterLine",
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement));
            divider.transform.SetParent(parent, false);
            Image image = divider.GetComponent<Image>();
            image.color = new Color(0.18f, 0.55f, 0.72f, 0.46f);
            image.raycastTarget = false;
            divider.GetComponent<LayoutElement>().preferredHeight = 3f;
        }

        private void BindRow(
            IReadOnlyList<RuntimeBattleFieldSlotView> views,
            IReadOnlyList<RuntimeBattleFieldSlotPresentation> presentations,
            Color zoneColor)
        {
            for (int index = 0; index < views.Count; index++)
            {
                RuntimeBattleFieldSlotPresentation presentation =
                    presentations != null && index < presentations.Count
                        ? presentations[index]
                        : null;
                views[index].Bind(
                    presentation,
                    zoneColor,
                    EmptyColor,
                    HoverColor,
                    commandId => commandRequested?.Invoke(commandId),
                    value => detailRequested?.Invoke(value));
            }
        }

        private static Text CreateText(
            string name,
            Transform parent,
            string text,
            int fontSize,
            FontStyle style,
            TextAnchor alignment)
        {
            GameObject textObject = new(
                name,
                typeof(RectTransform),
                typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text result = textObject.GetComponent<Text>();
            result.text = text ?? string.Empty;
            result.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            result.fontSize = fontSize;
            result.fontStyle = style;
            result.alignment = alignment;
            result.color = Color.white;
            result.horizontalOverflow = HorizontalWrapMode.Wrap;
            result.verticalOverflow = VerticalWrapMode.Truncate;
            result.raycastTarget = false;
            return result;
        }

        internal static bool AcceptsCard(
            RuntimeBattleFieldZone zone,
            RuntimeCardPresentation card)
        {
            if (card == null)
            {
                return false;
            }

            return zone switch
            {
                RuntimeBattleFieldZone.Enemy =>
                    card.RequiresEnemyTarget,
                RuntimeBattleFieldZone.PlayerMonster =>
                    card.CardType == CardType.Monster,
                RuntimeBattleFieldZone.PlayerSkill =>
                    card.CardType == CardType.Skill ||
                    card.CardType == CardType.Trap ||
                    card.CardType == CardType.Barrier,
                _ => false
            };
        }
    }

    [DisallowMultipleComponent]
    public sealed class RuntimeBattleFieldSlotView : MonoBehaviour
    {
        private const float FieldCardScale = 0.45f;

        private Text labelText;
        private Image background;
        private Button button;
        private Outline selectionOutline;
        private RuntimeCardDropZone dropZone;
        private RuntimeBattleFieldSlotPresentation presentation;
        private RuntimeCardView cardView;
        private bool supportsCardView;

        public RuntimeBattleFieldSlotPresentation Presentation => presentation;
        public Text LabelText => labelText;
        public Button Button => button;
        public RuntimeCardDropZone DropZone => dropZone;
        public RuntimeCardView CardView => cardView;
        public bool IsShowingCard =>
            cardView != null && cardView.gameObject.activeSelf;

        public void Initialize(
            Text label,
            Image image,
            Button slotButton,
            Outline outline,
            bool createCardView = false)
        {
            labelText = label;
            background = image;
            button = slotButton;
            selectionOutline = outline;
            supportsCardView = createCardView;
            dropZone = gameObject.GetComponent<RuntimeCardDropZone>();
            if (dropZone == null)
            {
                dropZone = gameObject.AddComponent<RuntimeCardDropZone>();
            }
            if (supportsCardView)
            {
                CreateCardView();
            }
        }

        public void Bind(
            RuntimeBattleFieldSlotPresentation value,
            Color occupiedColor,
            Color emptyColor,
            Color hoverColor,
            Action<string> command,
            Action<RuntimeBattleFieldSlotPresentation> inspect = null)
        {
            presentation = value;
            bool occupied = value?.Occupied == true;
            bool showCard = supportsCardView && value?.ShowsCard == true;
            Color idleColor = occupied ? occupiedColor : emptyColor;
            if (background != null)
            {
                background.color = idleColor;
            }
            if (labelText != null)
            {
                string title = value?.Title ?? string.Empty;
                string detail = value?.Detail ?? string.Empty;
                labelText.text = string.IsNullOrWhiteSpace(detail)
                    ? title
                    : $"{title}\n{detail}";
                labelText.fontStyle = occupied ? FontStyle.Bold : FontStyle.Italic;
                labelText.gameObject.SetActive(!showCard);
            }

            BindCardView(value, showCard, command, inspect);

            if (selectionOutline != null)
            {
                bool targetableEnemy =
                    value?.Zone == RuntimeBattleFieldZone.Enemy &&
                    !string.IsNullOrWhiteSpace(value.ClickCommandId);
                selectionOutline.effectColor = value?.Selected == true
                    ? new Color(0.95f, 0.76f, 0.2f, 1f)
                    : new Color(0.25f, 0.75f, 1f, 1f);
                selectionOutline.effectDistance = new Vector2(3f, -3f);
                selectionOutline.enabled =
                    value?.Selected == true || targetableEnemy;
            }
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.interactable = value?.Interactable == true;
                if (button.interactable)
                {
                    button.onClick.AddListener(() =>
                        InvokePrimaryAction(value, command, inspect));
                }
            }

            RuntimeBattleFieldZone zone =
                value?.Zone ?? RuntimeBattleFieldZone.Enemy;
            dropZone?.Configure(
                value?.DropCommandId,
                background,
                idleColor,
                hoverColor,
                card => RuntimeBattleFieldView.AcceptsCard(
                    zone,
                    card));
        }

        private void CreateCardView()
        {
            GameObject cardObject = new(
                "FieldCard",
                typeof(RectTransform),
                typeof(RuntimeCardView));
            RectTransform cardRect = cardObject.GetComponent<RectTransform>();
            cardRect.SetParent(transform, false);
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(
                RuntimeCardView.ReferenceWidth,
                RuntimeCardView.ReferenceHeight);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.localRotation = Quaternion.identity;
            cardRect.localScale = Vector3.one * FieldCardScale;

            cardView = cardObject.GetComponent<RuntimeCardView>();
            cardView.Initialize();
            LayoutElement layout = cardObject.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.ignoreLayout = true;
            }
            cardObject.SetActive(false);
        }

        private void BindCardView(
            RuntimeBattleFieldSlotPresentation value,
            bool showCard,
            Action<string> command,
            Action<RuntimeBattleFieldSlotPresentation> inspect)
        {
            if (cardView == null)
            {
                return;
            }

            cardView.gameObject.SetActive(showCard);
            if (!showCard)
            {
                return;
            }

            RuntimeCardPresentation source = value.CardPresentation;
            RuntimeCardPresentation fieldCard = source.WithInteraction(
                value.ClickCommandId,
                value.Selected,
                true,
                null,
                value.Detail);
            cardView.Bind(
                fieldCard,
                _ => InvokePrimaryAction(value, command, inspect));

            RectTransform cardRect = cardView.transform as RectTransform;
            if (cardRect != null)
            {
                cardRect.anchoredPosition = Vector2.zero;
                cardRect.localRotation = Quaternion.identity;
                cardRect.localScale = Vector3.one * FieldCardScale;
                cardRect.SetAsLastSibling();
            }
        }

        private static void InvokePrimaryAction(
            RuntimeBattleFieldSlotPresentation value,
            Action<string> command,
            Action<RuntimeBattleFieldSlotPresentation> inspect)
        {
            if (value == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(value.ClickCommandId))
            {
                command?.Invoke(value.ClickCommandId);
                return;
            }

            inspect?.Invoke(value);
        }
    }
}
