using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HaveABreak.Editor
{
    internal static class RuntimeBattleFieldViewValidation
    {
        [MenuItem("Have a Break/Tests/Validate Runtime Battle Field View")]
        private static void RunFromMenu()
        {
            Validate();
        }

        internal static bool Validate()
        {
            GameObject host = new(
                "Runtime Battle Field Validation",
                typeof(RectTransform),
                typeof(RuntimeBattleFieldView));
            string commandId = null;
            RuntimeBattleFieldSlotPresentation inspected = null;

            try
            {
                RuntimeCardPresentation fieldMonsterCard =
                    CreateCard(CardType.Monster, "검증 몬스터 카드");
                RuntimeCardPresentation fieldSkillCard =
                    CreateCard(CardType.Skill, "검증 스킬 카드");

                RuntimeBattleFieldView view =
                    host.GetComponent<RuntimeBattleFieldView>();
                view.Initialize(
                    value => commandId = value,
                    value => inspected = value);
                RuntimeBattleFieldPresentation presentation = new(
                    new[]
                    {
                        new RuntimeBattleFieldSlotPresentation(
                            RuntimeBattleFieldZone.Enemy, 0,
                            "검증 적", "공격 3 · HP 8/10",
                            true, true, true,
                            "enemy:ENEMY-01", "enemy:ENEMY-01")
                    },
                    new[]
                    {
                        new RuntimeBattleFieldSlotPresentation(
                            RuntimeBattleFieldZone.PlayerMonster, 0,
                            "검증 몬스터", "공격 가능",
                            true, false, true,
                            "attack:MONSTER-01", string.Empty,
                            fieldMonsterCard),
                        new RuntimeBattleFieldSlotPresentation(
                            RuntimeBattleFieldZone.PlayerMonster, 1,
                            "빈 몬스터존", "몬스터 카드를 놓아 소환",
                            false, false, false,
                            string.Empty, "field:monster:1"),
                        new RuntimeBattleFieldSlotPresentation(
                            RuntimeBattleFieldZone.PlayerMonster, 2,
                            "빈 몬스터존", "몬스터 카드를 놓아 소환",
                            false, false, false,
                            string.Empty, "field:monster:2")
                    },
                    new[]
                    {
                        new RuntimeBattleFieldSlotPresentation(
                            RuntimeBattleFieldZone.PlayerSkill, 0,
                            "검증 설치 카드", "설치 상태 확인",
                            true, false, true,
                            string.Empty, string.Empty,
                            fieldSkillCard),
                        new RuntimeBattleFieldSlotPresentation(
                            RuntimeBattleFieldZone.PlayerSkill, 1,
                            "빈 스킬존", "스킬 카드를 놓아 설치",
                            false, false, false,
                            string.Empty, "field:skill:1"),
                        new RuntimeBattleFieldSlotPresentation(
                            RuntimeBattleFieldZone.PlayerSkill, 2,
                            "빈 스킬존", "스킬 카드를 놓아 설치",
                            false, false, false,
                            string.Empty, "field:skill:2")
                    });
                view.Bind(presentation);

                bool slotCounts =
                    view.EnemySlots.Count == 3 &&
                    view.MonsterSlots.Count == 3 &&
                    view.SkillSlots.Count == 3;
                RuntimeBattleFieldSlotView enemy = view.EnemySlots[0];
                RuntimeBattleFieldSlotView occupiedMonster = view.MonsterSlots[0];
                RuntimeBattleFieldSlotView centerDrop = view.MonsterSlots[1];
                RuntimeBattleFieldSlotView rightDrop = view.MonsterSlots[2];
                RuntimeBattleFieldSlotView occupiedSkill = view.SkillSlots[0];
                RuntimeBattleFieldSlotView centerSkillDrop =
                    view.SkillSlots[1];
                RuntimeBattleFieldSlotView rightSkillDrop =
                    view.SkillSlots[2];

                enemy.Button.onClick.Invoke();
                bool enemyCommand = commandId == "enemy:ENEMY-01" &&
                                    enemy.Presentation.Selected &&
                                    enemy.GetComponent<Outline>().enabled;
                commandId = null;
                occupiedMonster.CardView.ClickButton.onClick.Invoke();
                bool monsterCommand = commandId == null &&
                                      inspected == occupiedMonster.Presentation &&
                                      !occupiedMonster.DropZone.AcceptsCards &&
                                      centerDrop.DropZone.AcceptsCards &&
                                      rightDrop.DropZone.AcceptsCards;

                occupiedSkill.CardView.ClickButton.onClick.Invoke();
                bool skillInspect = inspected == occupiedSkill.Presentation;

                RuntimeCardPresentation monsterCard =
                    CreateCard(CardType.Monster, "검증 몬스터 카드");
                RuntimeCardPresentation skillCard =
                    CreateCard(CardType.Skill, "검증 스킬 카드");
                RuntimeCardPresentation trapCard =
                    CreateCard(CardType.Trap, "검증 트랩 카드");
                RuntimeCardPresentation barrierCard =
                    CreateCard(CardType.Barrier, "검증 결계 카드");
                bool cardTypeRouting =
                    centerDrop.DropZone.Accepts(monsterCard) &&
                    rightDrop.DropZone.Accepts(monsterCard) &&
                    !enemy.DropZone.Accepts(monsterCard) &&
                    !centerDrop.DropZone.Accepts(skillCard) &&
                    centerSkillDrop.DropZone.Accepts(skillCard) &&
                    rightSkillDrop.DropZone.Accepts(skillCard) &&
                    centerSkillDrop.DropZone.Accepts(trapCard) &&
                    centerSkillDrop.DropZone.Accepts(barrierCard) &&
                    !centerSkillDrop.DropZone.Accepts(monsterCard) &&
                    !occupiedSkill.DropZone.AcceptsCards;

                RuntimeCardDropZone.SetActivePresentation(monsterCard);
                bool allEmptyMonsterZonesHighlighted =
                    !occupiedMonster.DropZone.IsAvailableHighlighted &&
                    !enemy.DropZone.IsAvailableHighlighted &&
                    centerDrop.DropZone.IsAvailableHighlighted &&
                    rightDrop.DropZone.IsAvailableHighlighted &&
                    !centerSkillDrop.DropZone.IsAvailableHighlighted;
                RuntimeCardDropZone.SetActivePresentation(skillCard);
                bool skillOnlyHighlight =
                    !centerDrop.DropZone.IsAvailableHighlighted &&
                    !rightDrop.DropZone.IsAvailableHighlighted &&
                    !occupiedSkill.DropZone.IsAvailableHighlighted &&
                    centerSkillDrop.DropZone.IsAvailableHighlighted &&
                    rightSkillDrop.DropZone.IsAvailableHighlighted;
                RuntimeCardDropZone.SetActivePresentation(null);

                RectTransform monsterCardRect =
                    occupiedMonster.CardView.transform as RectTransform;
                RectTransform skillCardRect =
                    occupiedSkill.CardView.transform as RectTransform;
                float expectedAspect = RuntimeCardView.ReferenceWidth /
                                       RuntimeCardView.ReferenceHeight;
                bool fieldCardShape =
                    occupiedMonster.IsShowingCard &&
                    occupiedSkill.IsShowingCard &&
                    occupiedMonster.CardView.Presentation.DisplayName ==
                    "검증 몬스터 카드" &&
                    occupiedSkill.CardView.Presentation.DisplayName ==
                    "검증 스킬 카드" &&
                    monsterCardRect != null && skillCardRect != null &&
                    Mathf.Abs(
                        monsterCardRect.rect.width /
                        monsterCardRect.rect.height - expectedAspect) < 0.001f &&
                    Mathf.Abs(
                        skillCardRect.rect.width /
                        skillCardRect.rect.height - expectedAspect) < 0.001f &&
                    !occupiedMonster.LabelText.gameObject.activeSelf &&
                    !occupiedSkill.LabelText.gameObject.activeSelf &&
                    !centerDrop.IsShowingCard &&
                    centerDrop.LabelText.gameObject.activeSelf;

                bool labels = enemy.LabelText.text.Contains("검증 적") &&
                              centerDrop.LabelText.text.Contains("빈 몬스터존") &&
                              rightDrop.LabelText.text.Contains("빈 몬스터존");

                view.Bind(RuntimeBattleFieldPresentation.Empty);
                bool emptyRebind = view.EnemySlots.All(slot =>
                        !slot.Presentation.Occupied &&
                        !slot.Button.interactable &&
                        !slot.DropZone.AcceptsCards) &&
                    view.MonsterSlots.All(slot =>
                        !slot.Presentation.Occupied &&
                        !slot.IsShowingCard &&
                        slot.LabelText.gameObject.activeSelf) &&
                    view.SkillSlots.All(slot =>
                        !slot.Presentation.Occupied &&
                        !slot.IsShowingCard &&
                        slot.LabelText.gameObject.activeSelf);

                bool valid = slotCounts && enemyCommand && monsterCommand &&
                             skillInspect && cardTypeRouting &&
                             allEmptyMonsterZonesHighlighted &&
                             skillOnlyHighlight && fieldCardShape &&
                             labels && emptyRebind;
                if (valid)
                {
                    Debug.Log(
                        "Runtime battle field validation passed: occupied " +
                        "monster and skill slots preserve the complete card " +
                        "shape while empty zones retain drop behavior.");
                }
                else
                {
                    Debug.LogError(
                        "Runtime battle field validation failed. " +
                        $"slotCounts={slotCounts}, enemyCommand={enemyCommand}, " +
                        $"monsterCommand={monsterCommand}, " +
                        $"skillInspect={skillInspect}, " +
                        $"cardTypeRouting={cardTypeRouting}, " +
                        $"allEmptyHighlights={allEmptyMonsterZonesHighlighted}, " +
                        $"skillOnlyHighlight={skillOnlyHighlight}, " +
                        $"fieldCardShape={fieldCardShape}, " +
                        $"labels={labels}, emptyRebind={emptyRebind}");
                }
                return valid;
            }
            finally
            {
                RuntimeCardDropZone.SetActivePresentation(null);
                Object.DestroyImmediate(host);
            }
        }

        private static RuntimeCardPresentation CreateCard(
            CardType cardType,
            string displayName)
        {
            bool monster = cardType == CardType.Monster;
            return new RuntimeCardPresentation(
                $"play:{cardType}", displayName, $"VALIDATION-{cardType}",
                cardType, CardRarity.Common, 1,
                monster ? 2 : null, monster ? 3 : null,
                "검증 효과", false, 0, true, null, displayName);
        }
    }
}
