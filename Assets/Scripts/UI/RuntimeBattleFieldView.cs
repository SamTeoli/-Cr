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
            new(0.28f, 0.09f, 0.1f, 0.96f);
        private static readonly Color MonsterColor =
            new(0.07f, 0.2f, 0.32f, 0.96f);
        private static readonly Color SkillColor =
            new(0.18f, 0.11f, 0.3f, 0.96f);
        private static readonly Color EmptyColor =
            new(0.055f, 0.07f, 0.1f, 0.86f);
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
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateRow(root, "EnemyRow", "적 필드", RuntimeBattleFieldZone.Enemy,
                EnemyColor, enemySlots);
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
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = true;
            rowObject.GetComponent<LayoutElement>().preferredHeight = 124f;

            Text rowLabel = CreateText(
                "ZoneLabel",
                rowObject.transform,
                label,
                17,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);
            LayoutElement labelLayout = rowLabel.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 104f;
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
                slotLayout.preferredWidth = 250f;
                slotLayout.flexibleWidth = 1f;
                slotLayout.preferredHeight = 116f;

                Text text = CreateText(
                    "Label",
                    slotObject.transform,
                    string.Empty,
                    14,
                    FontStyle.Normal,
                    TextAnchor.MiddleCenter);
                RectTransform textRect = text.rectTransform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(8f, 3f);
                textRect.offsetMax = new Vector2(-8f, -3f);

                RuntimeBattleFieldSlotView slot =
                    slotObject.GetComponent<RuntimeBattleFieldSlotView>();
                slot.Initialize(text, image, slotObject.GetComponent<Button>(),
                    slotObject.GetComponent<Outline>());
                slots.Add(slot);
            }
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

        internal static bool AcceptsCardType(
            RuntimeBattleFieldZone zone,
            CardType cardType)
        {
            return zone switch
            {
                RuntimeBattleFieldZone.Enemy => true,
                RuntimeBattleFieldZone.PlayerMonster =>
                    cardType == CardType.Monster,
                RuntimeBattleFieldZone.PlayerSkill =>
                    cardType == CardType.Skill ||
                    cardType == CardType.Trap ||
                    cardType == CardType.Barrier,
                _ => false
            };
        }
    }

    [DisallowMultipleComponent]
    public sealed class RuntimeBattleFieldSlotView : MonoBehaviour
    {
        private Text labelText;
        private Image background;
        private Button button;
        private Outline selectionOutline;
        private RuntimeCardDropZone dropZone;
        private RuntimeBattleFieldSlotPresentation presentation;

        public RuntimeBattleFieldSlotPresentation Presentation => presentation;
        public Text LabelText => labelText;
        public Button Button => button;
        public RuntimeCardDropZone DropZone => dropZone;

        public void Initialize(
            Text label,
            Image image,
            Button slotButton,
            Outline outline)
        {
            labelText = label;
            background = image;
            button = slotButton;
            selectionOutline = outline;
            dropZone = gameObject.GetComponent<RuntimeCardDropZone>();
            if (dropZone == null)
            {
                dropZone = gameObject.AddComponent<RuntimeCardDropZone>();
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
            }
            if (selectionOutline != null)
            {
                selectionOutline.effectColor = new Color(0.95f, 0.76f, 0.2f, 1f);
                selectionOutline.effectDistance = new Vector2(3f, -3f);
                selectionOutline.enabled = value?.Selected == true;
            }
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.interactable = value?.Interactable == true &&
                                      !string.IsNullOrWhiteSpace(
                                          value.ClickCommandId);
                string clickCommand = value?.ClickCommandId;
                if (button.interactable)
                {
                    button.onClick.AddListener(() =>
                    {
                        if (inspect != null)
                        {
                            inspect(value);
                        }
                        else
                        {
                            command?.Invoke(clickCommand);
                        }
                    });
                }
            }

            RuntimeBattleFieldZone zone =
                value?.Zone ?? RuntimeBattleFieldZone.Enemy;
            dropZone?.Configure(
                value?.DropCommandId,
                background,
                idleColor,
                hoverColor,
                card => RuntimeBattleFieldView.AcceptsCardType(
                    zone,
                    card.CardType));
        }
    }
}
