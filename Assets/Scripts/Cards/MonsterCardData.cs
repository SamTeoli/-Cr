using UnityEngine;

namespace HaveABreak.Cards
{
    [CreateAssetMenu(fileName = "MonsterCard", menuName = "Have a Break/Cards/Monster")]
    public sealed class MonsterCardData : CardData
    {
        [Header("Monster Stats")]
        [SerializeField, Min(0)] private int attack;
        [SerializeField, Min(1)] private int health = 1;

        public override CardType CardType => CardType.Monster;
        public int Attack => attack;
        public int Health => health;
    }
}
