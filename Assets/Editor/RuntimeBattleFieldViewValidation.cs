using System;
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
                            "field:monster:0")
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
                RuntimeBattleFieldSlotView skill = view.SkillSlots[0];

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
                    monster.DropZone.AcceptsCards &&
                    monster.DropZone.TargetCommandId == "field:monster:0";

                bool skillDrop =
                    !skill.Button.interactable &&
                    skill.DropZone.AcceptsCards &&
                    skill.DropZone.TargetCommandId == "field:skill:0";

                bool labels =
                    enemy.LabelText.text.Contains("검증 적") &&
                    monster.LabelText.text.Contains("검증 몬스터") &&
                    skill.LabelText.text.Contains("빈 스킬존");

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
                             skillDrop && labels && emptyRebind;
                if (valid)
                {
                    Debug.Log(
                        "Runtime battle field validation passed: enemy, " +
                        "monster, and skill zones each expose three slots with " +
                        "selection, attack, and card-drop commands.");
                }
                else
                {
                    Debug.LogError(
                        "Runtime battle field validation failed. " +
                        $"slotCounts={slotCounts}, enemyCommand={enemyCommand}, " +
                        $"monsterCommand={monsterCommand}, skillDrop={skillDrop}, " +
                        $"labels={labels}, emptyRebind={emptyRebind}");
                }

                return valid;
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }
}
