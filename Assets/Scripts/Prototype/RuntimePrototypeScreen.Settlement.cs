using UnityEngine;

namespace HaveABreak.Cards
{
    public sealed partial class RuntimePrototypeScreen : MonoBehaviour
    {
        private readonly BattleSettlementViewModel battleSettlement = new();

        private void SettleBattle()
        {
            BattleSettlementCommandResult command =
                battleSettlement.TrySettle(campaign, progress);
            message = command.Message;
            if (!command.Succeeded)
            {
                return;
            }

            battleScreen.Reset();
            SaveRun(null);
        }
    }
}
