using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public sealed class BattleRuntimeEnemyTurnPlan
    {
        internal BattleRuntimeEnemyTurnPlan(
            List<BattleRuntimeEnemyTurnCommand> commands,
            List<BattleRuntimeEnemyTurnIntent> intents)
        {
            Commands = commands.AsReadOnly();
            Intents = intents.AsReadOnly();
        }

        public IReadOnlyList<BattleRuntimeEnemyTurnCommand> Commands { get; }
        public IReadOnlyList<BattleRuntimeEnemyTurnIntent> Intents { get; }
        public int ActionCount => Commands.Count;
    }
}
