using UnityEngine;

namespace HaveABreak.Cards
{
    [CreateAssetMenu(fileName = "SkillCard", menuName = "Have a Break/Cards/Skill")]
    public sealed class SkillCardData : CardData
    {
        public override CardType CardType => CardType.Skill;
    }
}
