using UnityEngine;

namespace HaveABreak.Cards
{
    public sealed partial class RuntimePrototypeScreen : MonoBehaviour
    {
        private readonly BattleStartViewModel battleStart = new();

        private void BeginSelectedBattle()
        {
            BattleStartCommandResult result = battleStart.TryStart(
                campaign,
                progress,
                config);
            message = result.Message;
            if (result.BattleStarted)
            {
                battleScreen.Reset();
            }
        }
    }
}
