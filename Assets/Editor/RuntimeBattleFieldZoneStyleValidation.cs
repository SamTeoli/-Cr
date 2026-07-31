using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HaveABreak.Editor
{
    internal static class RuntimeBattleFieldZoneStyleValidation
    {
        [MenuItem("Have a Break/Tests/Validate Runtime Battle Field Zone Style")]
        private static void RunFromMenu()
        {
            Validate();
        }

        internal static bool Validate()
        {
            GameObject host = new(
                "Runtime Battle Field Zone Style Validation",
                typeof(RectTransform),
                typeof(RuntimeBattleFieldView));

            try
            {
                RuntimeBattleFieldView field =
                    host.GetComponent<RuntimeBattleFieldView>();
                field.Initialize(_ => { });
                field.Bind(RuntimeBattleFieldPresentation.Empty);
                RuntimeBattleFieldZoneStyleBootstrap.ApplyToLoadedFields();

                RuntimeBattleFieldSlotView[] slots = field.EnemySlots
                    .Concat(field.MonsterSlots)
                    .Concat(field.SkillSlots)
                    .ToArray();
                bool allDecorated = slots.Length == 9 && slots.All(slot =>
                {
                    RuntimeBattleFieldZonePlate plate =
                        slot.GetComponent<RuntimeBattleFieldZonePlate>();
                    Image background = slot.GetComponent<Image>();
                    return plate != null && plate.IsInitialized &&
                           plate.OuterFrame != null &&
                           plate.InnerFrame != null &&
                           plate.CenterGlyph != null &&
                           background?.sprite != null &&
                           !plate.OuterFrame.raycastTarget &&
                           !plate.InnerFrame.raycastTarget &&
                           !plate.CenterGlyph.raycastTarget;
                });

                bool zoneMarks = slots.All(slot =>
                    slot.transform.Find("ZoneTypeMark") != null) &&
                    field.EnemySlots.All(slot =>
                        slot.transform.Find("ZoneTypeMark")
                            .GetComponent<Text>().text == "ENEMY") &&
                    field.MonsterSlots.All(slot =>
                        slot.transform.Find("ZoneTypeMark")
                            .GetComponent<Text>().text == "MONSTER") &&
                    field.SkillSlots.All(slot =>
                        slot.transform.Find("ZoneTypeMark")
                            .GetComponent<Text>().text == "SKILL");

                RuntimeCardPresentation monster = new(
                    "play:style-test",
                    "스타일 검증 카드",
                    "STYLE-TEST",
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
                    "스타일 검증 카드");
                RuntimeCardDropZone.SetActivePresentation(monster);
                bool highlightPreserved = field.MonsterSlots.All(slot =>
                    slot.DropZone.IsAvailableHighlighted) &&
                    field.SkillSlots.All(slot =>
                        !slot.DropZone.IsAvailableHighlighted);
                RuntimeCardDropZone.SetActivePresentation(null);

                bool valid = allDecorated && zoneMarks && highlightPreserved;
                if (valid)
                {
                    Debug.Log(
                        "Runtime battle field zone style validation passed: " +
                        "all nine slots use chamfered dual-frame plates while " +
                        "card-drop highlighting remains active.");
                }
                else
                {
                    Debug.LogError(
                        "Runtime battle field zone style validation failed. " +
                        $"decorated={allDecorated}, zoneMarks={zoneMarks}, " +
                        $"highlight={highlightPreserved}");
                }
                return valid;
            }
            finally
            {
                RuntimeCardDropZone.SetActivePresentation(null);
                Object.DestroyImmediate(host);
            }
        }
    }
}
