using HaveABreak.Cards;
using UnityEditor;

namespace HaveABreak.EditorTools
{
    public sealed partial class IntegratedRunPrototypeWindow : EditorWindow
    {
        private readonly BattleStartViewModel battleStart = new();

        private void BeginSelectedBattle()
        {
            BattleStartCommandResult result = battleStart.TryStart(
                campaign,
                progress,
                prototypeConfig);
            message = result.Message;
            if (result.BattleStarted)
            {
                battleScreen.Reset();
            }
        }
    }
}
