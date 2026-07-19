using UnityEngine;

namespace HaveABreak.Cards
{
    [CreateAssetMenu(fileName = "TrapCard", menuName = "Have a Break/Cards/Trap")]
    public sealed class TrapCardData : CardData
    {
        [SerializeField] private bool activatesAtEnemyTurnStart = true;

        public override CardType CardType => CardType.Trap;
        public bool ActivatesAtEnemyTurnStart => activatesAtEnemyTurnStart;
    }
}
