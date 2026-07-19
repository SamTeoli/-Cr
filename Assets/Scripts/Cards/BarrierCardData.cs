using UnityEngine;

namespace HaveABreak.Cards
{
    [CreateAssetMenu(fileName = "BarrierCard", menuName = "Have a Break/Cards/Barrier")]
    public sealed class BarrierCardData : CardData
    {
        [SerializeField, Min(0)] private int duration;

        public override CardType CardType => CardType.Barrier;
        public int Duration => duration;
    }
}
