using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public static class BattleRuntimeEnemyAttackTargetService
    {
        public static bool TrySelect(
            BattleRuntimeState runtime,
            string attackerEnemyId,
            int tieBreakerValue,
            out BattleRuntimeEnemyAttackTargetResult result,
            out BattleRuntimeEnemyAttackTargetFailure failure)
        {
            result = null;
            if (runtime == null)
            {
                failure =
                    BattleRuntimeEnemyAttackTargetFailure.InvalidRuntime;
                return false;
            }

            BattleEnemyRuntimeState attacker =
                runtime.FindEnemy(attackerEnemyId);
            if (attacker == null || !attacker.IsAlive ||
                !runtime.LivingEnemies.Contains(attackerEnemyId))
            {
                failure =
                    BattleRuntimeEnemyAttackTargetFailure.InvalidAttacker;
                return false;
            }

            EnemyFieldPosition? attackerPosition =
                runtime.EnemyPositions.FindPosition(attackerEnemyId);
            if (!attackerPosition.HasValue)
            {
                failure = BattleRuntimeEnemyAttackTargetFailure
                    .AttackerPositionMissing;
                return false;
            }

            List<Candidate> nearest = new();
            int nearestDistance = int.MaxValue;
            foreach (PlayerMonsterFieldPosition position in
                     System.Enum.GetValues(
                         typeof(PlayerMonsterFieldPosition)))
            {
                string battleCardId =
                    runtime.PlayerMonsterPositions.GetOccupant(position);
                BattleMonsterState monster =
                    runtime.Monsters.Find(battleCardId);
                if (monster == null ||
                    monster.Card.Zone != CardZone.MonsterField ||
                    monster.IsDestructionCandidate)
                {
                    continue;
                }

                int distance = System.Math.Abs(
                    PositionRank(attackerPosition.Value) -
                    PositionRank(position));
                if (distance < nearestDistance)
                {
                    nearest.Clear();
                    nearestDistance = distance;
                }

                if (distance == nearestDistance)
                {
                    nearest.Add(new Candidate(monster, position));
                }
            }

            if (nearest.Count == 0)
            {
                result = new BattleRuntimeEnemyAttackTargetResult(
                    attacker.EnemyId,
                    attackerPosition.Value,
                    BattleRuntimeEnemyAttackTargetType.Player,
                    null,
                    null,
                    0);
                failure = BattleRuntimeEnemyAttackTargetFailure.None;
                return true;
            }

            int selectedIndex = nearest.Count == 1
                ? 0
                : (int)((uint)tieBreakerValue % (uint)nearest.Count);
            Candidate selected = nearest[selectedIndex];
            result = new BattleRuntimeEnemyAttackTargetResult(
                attacker.EnemyId,
                attackerPosition.Value,
                BattleRuntimeEnemyAttackTargetType.Monster,
                selected.Monster,
                selected.Position,
                nearest.Count);
            failure = BattleRuntimeEnemyAttackTargetFailure.None;
            return true;
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

        private static int PositionRank(
            PlayerMonsterFieldPosition position)
        {
            return position switch
            {
                PlayerMonsterFieldPosition.Left => 0,
                PlayerMonsterFieldPosition.Center => 1,
                PlayerMonsterFieldPosition.Right => 2,
                _ => int.MaxValue
            };
        }

        private readonly struct Candidate
        {
            public Candidate(
                BattleMonsterState monster,
                PlayerMonsterFieldPosition position)
            {
                Monster = monster;
                Position = position;
            }

            public BattleMonsterState Monster { get; }
            public PlayerMonsterFieldPosition Position { get; }
        }
    }
}
