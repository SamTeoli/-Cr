using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public static class BattleRuntimeEnemyTurnOrderService
    {
        public static bool TryCreateOrderedPlan(
            BattleRuntimeState runtime,
            IEnumerable<BattleRuntimeEnemyTurnCommand> commands,
            out BattleRuntimeEnemyTurnPlan plan,
            out BattleRuntimeEnemyTurnPlanFailure failure,
            out int failedActionIndex)
        {
            plan = null;
            failedActionIndex = -1;
            if (runtime == null)
            {
                failure = BattleRuntimeEnemyTurnPlanFailure.InvalidRuntime;
                return false;
            }

            if (!BattleRuntimeEnemyTurnPlanService.TryCreate(
                    commands,
                    out BattleRuntimeEnemyTurnPlan sourcePlan,
                    out failure,
                    out failedActionIndex))
            {
                return false;
            }

            List<OrderEntry> entries = new();
            for (int i = 0; i < sourcePlan.Commands.Count; i++)
            {
                BattleRuntimeEnemyTurnCommand command =
                    sourcePlan.Commands[i];
                BattleEnemyRuntimeState enemy =
                    runtime.FindEnemy(command.EnemyId);
                if (enemy == null)
                {
                    failure =
                        BattleRuntimeEnemyTurnPlanFailure.EnemyNotFound;
                    failedActionIndex = i;
                    return false;
                }

                if (!enemy.IsAlive ||
                    !runtime.LivingEnemies.Contains(enemy.EnemyId))
                {
                    failure =
                        BattleRuntimeEnemyTurnPlanFailure.EnemyNotActive;
                    failedActionIndex = i;
                    return false;
                }

                EnemyFieldPosition? position =
                    runtime.EnemyPositions.FindPosition(enemy.EnemyId);
                if (!position.HasValue)
                {
                    failure = BattleRuntimeEnemyTurnPlanFailure
                        .EnemyPositionMissing;
                    failedActionIndex = i;
                    return false;
                }

                entries.Add(new OrderEntry(
                    command,
                    i,
                    PositionRank(position.Value),
                    ActionRank(command.ActionType)));
            }

            entries.Sort(CompareEntries);
            List<BattleRuntimeEnemyTurnCommand> orderedCommands = new();
            foreach (OrderEntry entry in entries)
            {
                orderedCommands.Add(entry.Command);
            }

            return BattleRuntimeEnemyTurnPlanService.TryCreate(
                orderedCommands,
                out plan,
                out failure,
                out failedActionIndex);
        }

        private static int CompareEntries(OrderEntry left, OrderEntry right)
        {
            int positionComparison =
                left.PositionRank.CompareTo(right.PositionRank);
            if (positionComparison != 0)
            {
                return positionComparison;
            }

            int actionComparison =
                left.ActionRank.CompareTo(right.ActionRank);
            return actionComparison != 0
                ? actionComparison
                : left.OriginalIndex.CompareTo(right.OriginalIndex);
        }

        private static int PositionRank(EnemyFieldPosition position)
        {
            return position switch
            {
                EnemyFieldPosition.Left => 0,
                EnemyFieldPosition.Center => 1,
                EnemyFieldPosition.Right => 2,
                _ => int.MaxValue
            };
        }

        private static int ActionRank(
            BattleRuntimeEnemyTurnActionType actionType)
        {
            return actionType switch
            {
                BattleRuntimeEnemyTurnActionType.Move => 0,
                BattleRuntimeEnemyTurnActionType.Attack => 1,
                BattleRuntimeEnemyTurnActionType.Ability => 2,
                _ => int.MaxValue
            };
        }

        private readonly struct OrderEntry
        {
            public OrderEntry(
                BattleRuntimeEnemyTurnCommand command,
                int originalIndex,
                int positionRank,
                int actionRank)
            {
                Command = command;
                OriginalIndex = originalIndex;
                PositionRank = positionRank;
                ActionRank = actionRank;
            }

            public BattleRuntimeEnemyTurnCommand Command { get; }
            public int OriginalIndex { get; }
            public int PositionRank { get; }
            public int ActionRank { get; }
        }
    }
}
