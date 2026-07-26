using HaveABreak.Cards;
using UnityEditor;

namespace HaveABreak.EditorTools
{
    public sealed partial class IntegratedRunPrototypeWindow : EditorWindow
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
