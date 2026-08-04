using System;
using System.Collections.Generic;

namespace HaveABreak.Cards
{
    public enum PendingEffectActivationPhase
    {
        Targeting = 0,
        Declared = 1
    }

    public sealed class PendingEffectActivation
    {
        private readonly List<string> selectedTargetIds = new();

        public PendingEffectActivation(
            string sourceCardId,
            string effectDefinitionId,
            EffectTargetSpec targetSpec,
            BattleRuntimeCardPlayResult placementResult = null)
        {
            SourceCardId = sourceCardId?.Trim();
            EffectDefinitionId = effectDefinitionId?.Trim();
            TargetSpec = targetSpec;
            PlacementResult = placementResult;
            Phase = PendingEffectActivationPhase.Targeting;
        }

        public string SourceCardId { get; }
        public string EffectDefinitionId { get; }
        public EffectTargetSpec TargetSpec { get; }
        public BattleRuntimeCardPlayResult PlacementResult { get; }
        public PendingEffectActivationPhase Phase { get; private set; }
        public IReadOnlyList<string> SelectedTargetIds =>
            selectedTargetIds;
        public string SingleTargetId =>
            selectedTargetIds.Count == 1
                ? selectedTargetIds[0]
                : null;
        public bool IsAwaitingTarget =>
            Phase == PendingEffectActivationPhase.Targeting;
        public bool HasPlacementResult => PlacementResult != null;

        public bool TrySelectSingleTarget(
            BattleRuntimeState runtime,
            string targetId,
            out EffectTargetCandidate candidate)
        {
            candidate = null;
            if (!IsAwaitingTarget ||
                !EffectTargetResolver.TryResolveSingleTarget(
                    runtime,
                    TargetSpec,
                    targetId,
                    SourceCardId,
                    out candidate))
            {
                return false;
            }

            selectedTargetIds.Clear();
            selectedTargetIds.Add(candidate.TargetId);
            return true;
        }

        public bool TrySetTargets(
            BattleRuntimeState runtime,
            IEnumerable<string> targetIds,
            out IReadOnlyList<EffectTargetCandidate> candidates)
        {
            candidates = Array.Empty<EffectTargetCandidate>();
            if (!IsAwaitingTarget ||
                !EffectTargetResolver.TryResolveTargets(
                    runtime,
                    TargetSpec,
                    targetIds,
                    SourceCardId,
                    out candidates))
            {
                return false;
            }

            selectedTargetIds.Clear();
            foreach (EffectTargetCandidate candidate in candidates)
            {
                selectedTargetIds.Add(candidate.TargetId);
            }

            return true;
        }

        public bool TryAddTarget(
            BattleRuntimeState runtime,
            string targetId,
            out EffectTargetCandidate candidate)
        {
            candidate = null;
            if (!IsAwaitingTarget ||
                TargetSpec == null ||
                selectedTargetIds.Count >= TargetSpec.MaximumCount ||
                !EffectTargetResolver.TryResolveCandidate(
                    runtime,
                    TargetSpec,
                    targetId,
                    SourceCardId,
                    out candidate))
            {
                return false;
            }

            string candidateId = candidate.TargetId;
            if (!TargetSpec.AllowDuplicate &&
                selectedTargetIds.Exists(selected =>
                    string.Equals(
                        selected,
                        candidateId,
                        StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            selectedTargetIds.Add(candidateId);
            return true;
        }

        public bool TryRemoveTarget(string targetId)
        {
            if (!IsAwaitingTarget ||
                string.IsNullOrWhiteSpace(targetId))
            {
                return false;
            }

            int index = selectedTargetIds.FindIndex(selected =>
                string.Equals(
                    selected,
                    targetId.Trim(),
                    StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                return false;
            }

            selectedTargetIds.RemoveAt(index);
            return true;
        }

        public bool TryDeclare(BattleRuntimeState runtime)
        {
            if (!IsAwaitingTarget ||
                TargetSpec == null ||
                !EffectTargetResolver.TryResolveTargets(
                    runtime,
                    TargetSpec,
                    selectedTargetIds,
                    SourceCardId,
                    out IReadOnlyList<EffectTargetCandidate> resolved))
            {
                return false;
            }

            selectedTargetIds.Clear();
            foreach (EffectTargetCandidate candidate in resolved)
            {
                selectedTargetIds.Add(candidate.TargetId);
            }
            Phase = PendingEffectActivationPhase.Declared;
            return true;
        }

        public bool TryDeclare()
        {
            if (!IsAwaitingTarget ||
                TargetSpec == null ||
                selectedTargetIds.Count <
                    (TargetSpec.Optional
                        ? 0
                        : TargetSpec.MinimumCount) ||
                selectedTargetIds.Count > TargetSpec.MaximumCount)
            {
                return false;
            }

            Phase = PendingEffectActivationPhase.Declared;
            return true;
        }

        public bool MatchesSource(string sourceCardId)
        {
            return !string.IsNullOrWhiteSpace(sourceCardId) &&
                   string.Equals(
                       SourceCardId,
                       sourceCardId.Trim(),
                       StringComparison.OrdinalIgnoreCase);
        }
    }
}
