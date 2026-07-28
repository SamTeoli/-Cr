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

            try
            {
                RuntimeBattleFieldView view =
                    host.GetComponent<RuntimeBattleFieldView>();
                view.Initialize(value => commandId = value);
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
                            "attack:MONSTER-01", string.Empty),
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
                            "빈 스킬존", "스킬 카드를 놓아 설치",
                            false, false, false,
                            string.Empty, "field:skill:0")
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
                RuntimeBattleFieldSlotView skillDrop = view.SkillSlots[0];

                enemy.Button.onClick.Invoke();
                bool enemyCommand = commandId == "enemy:ENEMY-01" &&
                                    enemy.Presentation.Selected &&
                                    enemy.GetComponent<Outline>().enabled;
                commandId = null;
                occupiedMonster.Button.onClick.Invoke();
                bool monsterCommand = commandId == "attack:MONSTER-01" &&
                                      !occupiedMonster.DropZone.AcceptsCards &&
                                      centerDrop.DropZone.AcceptsCards &&
                                      rightDrop.DropZone.AcceptsCards;

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
                    !centerDrop.DropZone.Accepts(skillCard) &&
                    skillDrop.DropZone.Accepts(skillCard) &&
                    skillDrop.DropZone.Accepts(trapCard) &&
                    skillDrop.DropZone.Accepts(barrierCard) &&
                    !skillDrop.DropZone.Accepts(monsterCard);

                RuntimeCardDropZone.SetActivePresentation(monsterCard);
                bool allEmptyMonsterZonesHighlighted =
                    !occupiedMonster.DropZone.IsAvailableHighlighted &&
                    centerDrop.DropZone.IsAvailableHighlighted &&
                    rightDrop.DropZone.IsAvailableHighlighted &&
                    !skillDrop.DropZone.IsAvailableHighlighted;
                RuntimeCardDropZone.SetActivePresentation(skillCard);
                bool skillOnlyHighlight =
                    !centerDrop.DropZone.IsAvailableHighlighted &&
                    !rightDrop.DropZone.IsAvailableHighlighted &&
                    skillDrop.DropZone.IsAvailableHighlighted;
                RuntimeCardDropZone.SetActivePresentation(null);

                bool labels = enemy.LabelText.text.Contains("검증 적") &&
                              occupiedMonster.LabelText.text.Contains("검증 몬스터") &&
                              centerDrop.LabelText.text.Contains("빈 몬스터존") &&
                              rightDrop.LabelText.text.Contains("빈 몬스터존");

                view.Bind(RuntimeBattleFieldPresentation.Empty);
                bool emptyRebind = view.EnemySlots.All(slot =>
                        !slot.Presentation.Occupied &&
                        !slot.Button.interactable &&
                        !slot.DropZone.AcceptsCards) &&
                    view.MonsterSlots.All(slot => !slot.Presentation.Occupied) &&
                    view.SkillSlots.All(slot => !slot.Presentation.Occupied);

                bool valid = slotCounts && enemyCommand && monsterCommand &&
                             cardTypeRouting && allEmptyMonsterZonesHighlighted &&
                             skillOnlyHighlight && labels && emptyRebind;
                if (valid)
                {
                    Debug.Log(
                        "Runtime battle field validation passed: all empty " +
                        "monster zones accept and highlight for monster cards, " +
                        "while occupied and wrong-type zones remain blocked.");
                }
                else
                {
                    Debug.LogError(
                        "Runtime battle field validation failed. " +
                        $"slotCounts={slotCounts}, enemyCommand={enemyCommand}, " +
                        $"monsterCommand={monsterCommand}, " +
                        $"cardTypeRouting={cardTypeRouting}, " +
                        $"allEmptyHighlights={allEmptyMonsterZonesHighlighted}, " +
                        $"skillOnlyHighlight={skillOnlyHighlight}, " +
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
