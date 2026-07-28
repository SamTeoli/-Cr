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
                            RuntimeBattleFieldZone.Enemy,
                            0,
                            "검증 적",
                            "공격 3 · HP 8/10",
                            true,
                            true,
                            true,
                            "enemy:ENEMY-01",
                            "enemy:ENEMY-01")
                    },
                    new[]
                    {
                        new RuntimeBattleFieldSlotPresentation(
                            RuntimeBattleFieldZone.PlayerMonster,
                            0,
                            "검증 몬스터",
                            "공격 가능",
                            true,
                            false,
                            true,
                            "attack:MONSTER-01",
                            string.Empty),
                        new RuntimeBattleFieldSlotPresentation(
                            RuntimeBattleFieldZone.PlayerMonster,
                            1,
                            "빈 몬스터존",
                            "몬스터 카드를 놓아 소환",
                            false,
                            false,
                            false,
                            string.Empty,
                            "field:monster:1")
                    },
                    new[]
                    {
                        new RuntimeBattleFieldSlotPresentation(
                            RuntimeBattleFieldZone.PlayerSkill,
                            0,
                            "빈 스킬존",
                            "스킬 카드를 놓아 설치",
                            false,
                            false,
                            false,
                            string.Empty,
                            "field:skill:0")
                    });

                view.Bind(presentation);

                bool slotCounts =
                    view.EnemySlots.Count == RuntimeBattleFieldPresentation.SlotCount &&
                    view.MonsterSlots.Count == RuntimeBattleFieldPresentation.SlotCount &&
                    view.SkillSlots.Count == RuntimeBattleFieldPresentation.SlotCount;

                RuntimeBattleFieldSlotView enemy = view.EnemySlots[0];
                RuntimeBattleFieldSlotView monster = view.MonsterSlots[0];
                RuntimeBattleFieldSlotView monsterDrop = view.MonsterSlots[1];
                RuntimeBattleFieldSlotView skillDrop = view.SkillSlots[0];

                enemy.Button.onClick.Invoke();
                bool enemyCommand =
                    commandId == "enemy:ENEMY-01" &&
                    enemy.Presentation.Selected &&
                    enemy.GetComponent<Outline>().enabled &&
                    enemy.DropZone.AcceptsCards &&
                    enemy.DropZone.TargetCommandId == "enemy:ENEMY-01";

                commandId = null;
                monster.Button.onClick.Invoke();
                bool monsterCommand =
                    commandId == "attack:MONSTER-01" &&
                    !monster.DropZone.AcceptsCards &&
                    monsterDrop.DropZone.AcceptsCards &&
                    monsterDrop.DropZone.TargetCommandId == "field:monster:1";

                RuntimeCardPresentation monsterCard =
                    CreateCard(CardType.Monster, "검증 몬스터 카드");
                RuntimeCardPresentation skillCard =
                    CreateCard(CardType.Skill, "검증 스킬 카드");
                RuntimeCardPresentation trapCard =
                    CreateCard(CardType.Trap, "검증 트랩 카드");
                RuntimeCardPresentation barrierCard =
                    CreateCard(CardType.Barrier, "검증 결계 카드");
                bool cardTypeRouting =
                    monsterDrop.DropZone.Accepts(monsterCard) &&
                    !monsterDrop.DropZone.Accepts(skillCard) &&
                    skillDrop.DropZone.Accepts(skillCard) &&
                    skillDrop.DropZone.Accepts(trapCard) &&
                    skillDrop.DropZone.Accepts(barrierCard) &&
                    !skillDrop.DropZone.Accepts(monsterCard);

                bool labels =
                    enemy.LabelText.text.Contains("검증 적") &&
                    monster.LabelText.text.Contains("검증 몬스터") &&
                    monsterDrop.LabelText.text.Contains("빈 몬스터존") &&
                    skillDrop.LabelText.text.Contains("빈 스킬존");

                view.Bind(RuntimeBattleFieldPresentation.Empty);
                bool emptyRebind =
                    view.EnemySlots.All(slot =>
                        slot.Presentation != null &&
                        !slot.Presentation.Occupied &&
                        !slot.Button.interactable &&
                        !slot.DropZone.AcceptsCards) &&
                    view.MonsterSlots.All(slot =>
                        slot.Presentation != null &&
                        !slot.Presentation.Occupied) &&
                    view.SkillSlots.All(slot =>
                        slot.Presentation != null &&
                        !slot.Presentation.Occupied);

                bool valid = slotCounts && enemyCommand && monsterCommand &&
                             cardTypeRouting && labels && emptyRebind;
                if (valid)
                {
                    Debug.Log(
                        "Runtime battle field validation passed: enemy, " +
                        "monster, and skill zones each expose three slots with " +
                        "selection, attack, first-empty placement, and card-type " +
                        "drop rules.");
                }
                else
                {
                    Debug.LogError(
                        "Runtime battle field validation failed. " +
                        $"slotCounts={slotCounts}, enemyCommand={enemyCommand}, " +
                        $"monsterCommand={monsterCommand}, " +
                        $"cardTypeRouting={cardTypeRouting}, labels={labels}, " +
                        $"emptyRebind={emptyRebind}");
                }

                return valid;
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        private static RuntimeCardPresentation CreateCard(
            CardType cardType,
            string displayName)
        {
            bool monster = cardType == CardType.Monster;
            return new RuntimeCardPresentation(
                $"play:{cardType}",
                displayName,
                $"VALIDATION-{cardType}",
                cardType,
                CardRarity.Common,
                1,
                monster ? 2 : null,
                monster ? 3 : null,
                "검증 효과",
                false,
                0,
                true,
                null,
                displayName);
        }
    }
}
