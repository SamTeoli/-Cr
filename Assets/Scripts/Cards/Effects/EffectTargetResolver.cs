using System;
using System.Collections.Generic;
using System.Linq;

namespace HaveABreak.Cards
{
    public sealed class EffectTargetCandidate
    {
        internal EffectTargetCandidate(
            string targetId,
            EffectTargetSide side,
            EffectTargetKind kind)
        {
            TargetId = targetId;
            Side = side;
            Kind = kind;
        }

        public string TargetId { get; }
        public EffectTargetSide Side { get; }
        public EffectTargetKind Kind { get; }
    }

    public static class EffectTargetResolver
    {
        public static IReadOnlyList<EffectTargetCandidate> GetLegalTargets(
            BattleRuntimeState runtime,
            EffectTargetSpec spec,
            string sourceCardId = null)
        {
            if (runtime == null || spec == null)
            {
                return Array.Empty<EffectTargetCandidate>();
            }

            List<EffectTargetCandidate> candidates = new();
            if (spec.Kind == EffectTargetKind.Monster &&
                (spec.Side == EffectTargetSide.Enemy ||
                 spec.Side == EffectTargetSide.Any))
            {
                AddEnemyMonsters(runtime, spec, candidates);
            }

            if (spec.Kind == EffectTargetKind.Monster &&
                (spec.Side == EffectTargetSide.Ally ||
                 spec.Side == EffectTargetSide.Any))
            {
                AddAllyMonsters(runtime, spec, candidates);
            }

            if (spec.Kind == EffectTargetKind.HandCard &&
                (spec.Side == EffectTargetSide.Self ||
                 spec.Side == EffectTargetSide.Ally ||
                 spec.Side == EffectTargetSide.Any))
            {
                AddHandCards(runtime, sourceCardId, candidates);
            }

            return candidates;
        }

        public static bool TryResolveSingleTarget(
            BattleRuntimeState runtime,
            EffectTargetSpec spec,
            string targetId,
            string sourceCardId,
            out EffectTargetCandidate candidate)
        {
            candidate = null;
            if (runtime == null ||
                spec == null ||
                spec.MinimumCount > 1 ||
                spec.MaximumCount != 1 ||
                string.IsNullOrWhiteSpace(targetId))
            {
                return false;
            }

            if (!TryResolveTargets(
                    runtime,
                    spec,
                    new[] { targetId },
                    sourceCardId,
                    out IReadOnlyList<EffectTargetCandidate> resolved))
            {
                return false;
            }

            candidate = resolved[0];
            return true;
        }

        public static bool TryResolveSingleTarget(
            BattleRuntimeState runtime,
            EffectTargetSpec spec,
            string targetId,
            out EffectTargetCandidate candidate)
        {
            return TryResolveSingleTarget(
                runtime,
                spec,
                targetId,
                null,
                out candidate);
        }

        public static bool TryResolveTargets(
            BattleRuntimeState runtime,
            EffectTargetSpec spec,
            IEnumerable<string> targetIds,
            string sourceCardId,
            out IReadOnlyList<EffectTargetCandidate> candidates)
        {
            candidates = Array.Empty<EffectTargetCandidate>();
            if (runtime == null || spec == null)
            {
                return false;
            }

            string[] requested = targetIds?
                .Select(targetId => targetId?.Trim())
                .ToArray() ?? Array.Empty<string>();
            int minimum = spec.Optional ? 0 : spec.MinimumCount;
            if (requested.Length < minimum ||
                requested.Length > spec.MaximumCount ||
                requested.Any(string.IsNullOrWhiteSpace) ||
                !spec.AllowDuplicate &&
                requested.Distinct(
                    StringComparer.OrdinalIgnoreCase).Count() !=
                requested.Length)
            {
                return false;
            }

            Dictionary<string, EffectTargetCandidate> legal =
                GetLegalTargets(runtime, spec, sourceCardId)
                    .ToDictionary(
                        candidate => candidate.TargetId,
                        StringComparer.OrdinalIgnoreCase);
            List<EffectTargetCandidate> resolved = new(requested.Length);
            foreach (string targetId in requested)
            {
                if (!legal.TryGetValue(
                        targetId,
                        out EffectTargetCandidate candidate))
                {
                    return false;
                }

                resolved.Add(candidate);
            }

            candidates = resolved;
            return true;
        }

        public static bool TryResolveTargets(
            BattleRuntimeState runtime,
            EffectTargetSpec spec,
            IEnumerable<string> targetIds,
            out IReadOnlyList<EffectTargetCandidate> candidates)
        {
            return TryResolveTargets(
                runtime,
                spec,
                targetIds,
                null,
                out candidates);
        }

        public static bool TryResolveCandidate(
            BattleRuntimeState runtime,
            EffectTargetSpec spec,
            string targetId,
            string sourceCardId,
            out EffectTargetCandidate candidate)
        {
            candidate = null;
            if (runtime == null ||
                spec == null ||
                string.IsNullOrWhiteSpace(targetId))
            {
                return false;
            }

            foreach (EffectTargetCandidate legal in
                     GetLegalTargets(runtime, spec, sourceCardId))
            {
                if (string.Equals(
                        legal.TargetId,
                        targetId.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    candidate = legal;
                    return true;
                }
            }

            return false;
        }

        private static void AddEnemyMonsters(
            BattleRuntimeState runtime,
            EffectTargetSpec spec,
            ICollection<EffectTargetCandidate> candidates)
        {
            foreach (EnemyFieldPosition position in
                     Enum.GetValues(typeof(EnemyFieldPosition)))
            {
                string enemyId =
                    runtime.EnemyPositions.GetOccupant(position);
                BattleEnemyRuntimeState enemy =
                    runtime.FindEnemy(enemyId);
                bool alive = enemy?.IsAlive == true &&
                             runtime.LivingEnemies.Contains(enemyId);
                if (enemy == null ||
                    spec.RequireAlive && !alive ||
                    runtime.EnemyStatuses.Find(enemyId) == null)
                {
                    continue;
                }

                candidates.Add(new EffectTargetCandidate(
                    enemy.EnemyId,
                    EffectTargetSide.Enemy,
                    EffectTargetKind.Monster));
            }
        }

        private static void AddAllyMonsters(
            BattleRuntimeState runtime,
            EffectTargetSpec spec,
            ICollection<EffectTargetCandidate> candidates)
        {
            foreach (PlayerMonsterFieldPosition position in
                     Enum.GetValues(typeof(PlayerMonsterFieldPosition)))
            {
                string battleCardId =
                    runtime.PlayerMonsterPositions.GetOccupant(position);
                BattleMonsterState monster =
                    runtime.Monsters.Find(battleCardId);
                if (monster == null ||
                    spec.RequireAlive && monster.CurrentHealth <= 0)
                {
                    continue;
                }

                candidates.Add(new EffectTargetCandidate(
                    monster.BattleCardId,
                    EffectTargetSide.Ally,
                    EffectTargetKind.Monster));
            }
        }

        private static void AddHandCards(
            BattleRuntimeState runtime,
            string sourceCardId,
            ICollection<EffectTargetCandidate> candidates)
        {
            foreach (BattleCardInstance card in
                     runtime.Deck.Zones.GetCards(CardZone.Hand))
            {
                if (card == null ||
                    string.Equals(
                        card.Ids.BattleCardId,
                        sourceCardId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                candidates.Add(new EffectTargetCandidate(
                    card.Ids.BattleCardId,
                    EffectTargetSide.Self,
                    EffectTargetKind.HandCard));
            }
        }
    }
}
