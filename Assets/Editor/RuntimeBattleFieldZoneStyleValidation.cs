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
                field.Bind(CreateDropEnabledPresentation());
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
                    slot.DropZone.AcceptsCards &&
                    slot.DropZone.IsAvailableHighlighted) &&
                    field.SkillSlots.All(slot =>
                        slot.DropZone.AcceptsCards &&
                        !slot.DropZone.IsAvailableHighlighted) &&
                    field.EnemySlots.All(slot =>
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
