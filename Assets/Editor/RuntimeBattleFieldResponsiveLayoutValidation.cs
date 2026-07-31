using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HaveABreak.Editor
{
    internal static class RuntimeBattleFieldResponsiveLayoutValidation
    {
        [MenuItem("Have a Break/Tests/Validate Runtime Battle Field Responsive Layout")]
        private static void RunFromMenu()
        {
            Validate();
        }

        internal static bool Validate()
        {
            GameObject panelObject = new(
                "Responsive Battle Panel",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup));
            GameObject fieldObject = new(
                "BattleField",
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement),
                typeof(RuntimeBattleFieldView));
            fieldObject.transform.SetParent(panelObject.transform, false);

            try
            {
                RectTransform panel =
                    panelObject.GetComponent<RectTransform>();
                panel.anchorMin = Vector2.zero;
                panel.anchorMax = Vector2.one;
                panel.offsetMin = new Vector2(16f, 16f);
                panel.offsetMax = new Vector2(-16f, -82f);
                panel.sizeDelta = new Vector2(1600f, 802f);

                RuntimeBattleFieldView field =
                    fieldObject.GetComponent<RuntimeBattleFieldView>();
                field.Initialize(_ => { });
                field.Bind(CreateDropEnabledPresentation());
                RuntimeBattleFieldZoneStyleBootstrap.ApplyToLoadedFields();
                RuntimeBattleFieldResponsiveLayoutBootstrap
                    .ApplyToLoadedFields();

                RuntimeBattleFieldResponsiveLayout responsive =
                    fieldObject.GetComponent<
                        RuntimeBattleFieldResponsiveLayout>();
                RuntimeCardPresentation monster = new(
                    "play:responsive-test",
                    "확대 검증 카드",
                    "RESPONSIVE-TEST",
                    CardType.Monster,
                    CardRarity.Common,
                    1,
                    2,
                    3,
                    "검증 효과",
                    false,
                    0,
                    true,
                    null,
                    "확대 검증 카드");
                RuntimeCardDropZone.SetActivePresentation(monster);
                responsive?.ApplyNow(1f);

                RectTransform fieldRect =
                    fieldObject.GetComponent<RectTransform>();
                LayoutElement fieldElement =
                    fieldObject.GetComponent<LayoutElement>();
                bool fieldRegion = responsive != null &&
                                   fieldElement.ignoreLayout &&
                                   fieldRect.anchorMin == Vector2.zero &&
                                   fieldRect.anchorMax == Vector2.one &&
                                   fieldRect.offsetMin.y >=
                                   RuntimeBattleFieldResponsiveLayout
                                       .ReservedBottomHeight &&
                                   fieldRect.offsetMax.y <=
                                   -RuntimeBattleFieldResponsiveLayout
                                       .TopInset;

                RuntimeBattleFieldSlotView[] allSlots = field.EnemySlots
                    .Concat(field.MonsterSlots)
                    .Concat(field.SkillSlots)
                    .ToArray();
                bool slotsExpanded = allSlots.Length == 9 &&
                                     allSlots.All(slot =>
                                     {
                                         LayoutElement element =
                                             slot.GetComponent<LayoutElement>();
                                         return element != null &&
                                                element.preferredWidth >=
                                                RuntimeBattleFieldResponsiveLayout
                                                    .SlotWidth;
                                     });
                bool cardsReadable = field.MonsterSlots
                    .Concat(field.SkillSlots)
                    .All(slot =>
                    {
                        RectTransform card =
                            slot.CardView?.transform as RectTransform;
                        return card != null &&
                               card.localScale.x >=
                               RuntimeBattleFieldResponsiveLayout
                                   .FieldCardScale - 0.001f;
                    });
                bool pulse = field.MonsterSlots.All(slot =>
                {
                    RuntimeBattleFieldZonePlate plate =
                        slot.GetComponent<RuntimeBattleFieldZonePlate>();
                    return slot.DropZone.IsAvailableHighlighted &&
                           plate?.OuterFrame != null &&
                           plate.OuterFrame.color.a >= 0.95f &&
                           plate.CenterGlyph != null &&
                           plate.CenterGlyph.transform.localScale.x > 1f;
                }) && field.SkillSlots.All(slot =>
                    !slot.DropZone.IsAvailableHighlighted);
                bool handSeparated = fieldRect.offsetMin.y >= 270f;

                bool valid = fieldRegion && slotsExpanded && cardsReadable &&
                             pulse && handSeparated;
                if (valid)
                {
                    Debug.Log(
                        "Runtime battle field responsive layout validation " +
                        "passed: the field fills the HUD-to-hand region, " +
                        "cards are enlarged, and available slots pulse.");
                }
                else
                {
                    Debug.LogError(
                        "Runtime battle field responsive layout validation " +
                        $"failed. fieldRegion={fieldRegion}, " +
                        $"slotsExpanded={slotsExpanded}, " +
                        $"cardsReadable={cardsReadable}, pulse={pulse}, " +
                        $"handSeparated={handSeparated}");
                }
                return valid;
            }
            finally
            {
                RuntimeCardDropZone.SetActivePresentation(null);
                Object.DestroyImmediate(panelObject);
            }
        }

        private static RuntimeBattleFieldPresentation
            CreateDropEnabledPresentation()
        {
            RuntimeBattleFieldSlotPresentation[] enemies =
                new RuntimeBattleFieldSlotPresentation[
                    RuntimeBattleFieldPresentation.SlotCount];
            RuntimeBattleFieldSlotPresentation[] monsters =
                new RuntimeBattleFieldSlotPresentation[
                    RuntimeBattleFieldPresentation.SlotCount];
            RuntimeBattleFieldSlotPresentation[] skills =
                new RuntimeBattleFieldSlotPresentation[
                    RuntimeBattleFieldPresentation.SlotCount];

            for (int index = 0;
                 index < RuntimeBattleFieldPresentation.SlotCount;
                 index++)
            {
                enemies[index] = new RuntimeBattleFieldSlotPresentation(
                    RuntimeBattleFieldZone.Enemy,
                    index,
                    "빈 적 칸",
                    string.Empty,
                    false,
                    false,
                    false,
                    string.Empty,
                    string.Empty);
                monsters[index] = new RuntimeBattleFieldSlotPresentation(
                    RuntimeBattleFieldZone.PlayerMonster,
                    index,
                    "빈 몬스터존",
                    "몬스터 카드를 놓아 소환",
                    false,
                    false,
                    false,
                    string.Empty,
                    $"field:monster:{index}");
                skills[index] = new RuntimeBattleFieldSlotPresentation(
                    RuntimeBattleFieldZone.PlayerSkill,
                    index,
                    "빈 스킬존",
                    "스킬·트랩·결계 카드를 놓아 설치",
                    false,
                    false,
                    false,
                    string.Empty,
                    $"field:skill:{index}");
            }

            return new RuntimeBattleFieldPresentation(
                enemies,
                monsters,
                skills);
        }
    }
}
