using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    public enum BattleChainParticipant
    {
        Player = 0,
        Enemy = 1
    }

    public enum BattleChainPhase
    {
        Idle = 0,
        Building = 1,
        Resolving = 2
    }

    public enum BattleChainLinkStatus
    {
        Pending = 0,
        Resolving = 1,
        Resolved = 2,
        Negated = 3,
        Failed = 4
    }

    [Serializable]
    public sealed class BattleEffectTarget
    {
        [SerializeField] private string targetId;
        [SerializeField] private string targetType;

        public BattleEffectTarget(string id, string type)
        {
            targetId = id ?? string.Empty;
            targetType = type ?? string.Empty;
        }

        public string TargetId => targetId;
        public string TargetType => targetType;
    }

    [Serializable]
    public sealed class BattleActivationContext
    {
        [SerializeField] private string sourceId;
        [SerializeField] private string effectId;
        [SerializeField] private BattleChainParticipant controller;
        [SerializeField] private string timing;
        [SerializeField] private int paidMana;
        [SerializeField] private List<BattleEffectTarget> targets;

        public BattleActivationContext(
            string source,
            string effect,
            BattleChainParticipant owner,
            string activationTiming,
            int manaCost,
            IEnumerable<BattleEffectTarget> selectedTargets)
        {
            sourceId = source ?? string.Empty;
            effectId = effect ?? string.Empty;
            controller = owner;
            timing = activationTiming ?? string.Empty;
            paidMana = Mathf.Max(0, manaCost);
            targets = new List<BattleEffectTarget>(
                selectedTargets ?? Array.Empty<BattleEffectTarget>());
        }

        public string SourceId => sourceId;
        public string EffectId => effectId;
        public BattleChainParticipant Controller => controller;
        public string Timing => timing;
        public int PaidMana => paidMana;
        public IReadOnlyList<BattleEffectTarget> Targets => targets;
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(sourceId) &&
            !string.IsNullOrWhiteSpace(effectId);
    }

    [Serializable]
    public sealed class BattleChainLink
    {
        [SerializeField] private int linkIndex;
        [SerializeField] private BattleActivationContext activation;
        [SerializeField] private BattleChainLinkStatus status;

        internal BattleChainLink(
            int index,
            BattleActivationContext context)
        {
            linkIndex = index;
            activation = context;
            status = BattleChainLinkStatus.Pending;
        }

        public int LinkIndex => linkIndex;
        public BattleActivationContext Activation => activation;
        public BattleChainLinkStatus Status => status;

        internal void BeginResolving()
        {
            status = BattleChainLinkStatus.Resolving;
        }

        internal void Complete(BattleChainLinkStatus finalStatus)
        {
            status = finalStatus == BattleChainLinkStatus.Negated ||
                     finalStatus == BattleChainLinkStatus.Failed
                ? finalStatus
                : BattleChainLinkStatus.Resolved;
        }
    }

    [Serializable]
    public sealed class BattleRuntimeChainState
    {
        [SerializeField] private BattleChainPhase phase;
        [SerializeField] private List<BattleChainLink> links = new();
        [SerializeField] private BattleChainParticipant nextParticipant;
        [SerializeField] private int consecutivePasses;

        public BattleChainPhase Phase => phase;
        public IReadOnlyList<BattleChainLink> Links => links;
        public BattleChainParticipant NextParticipant => nextParticipant;
        public bool IsInputLocked => phase == BattleChainPhase.Resolving;
        public bool HasChain => links.Count > 0;

        public bool TryBegin(
            BattleActivationContext firstActivation,
            out BattleChainLink firstLink)
        {
            firstLink = null;
            if (phase != BattleChainPhase.Idle ||
                firstActivation?.IsValid != true)
            {
                return false;
            }

            links.Clear();
            firstLink = new BattleChainLink(1, firstActivation);
            links.Add(firstLink);
            phase = BattleChainPhase.Building;
            nextParticipant = OpponentOf(firstActivation.Controller);
            consecutivePasses = 0;
            return true;
        }

        public bool TryAdd(
            BattleActivationContext activation,
            out BattleChainLink link)
        {
            link = null;
            if (phase != BattleChainPhase.Building ||
                activation?.IsValid != true ||
                activation.Controller != nextParticipant)
            {
                return false;
            }

            link = new BattleChainLink(links.Count + 1, activation);
            links.Add(link);
            nextParticipant = OpponentOf(activation.Controller);
            consecutivePasses = 0;
            return true;
        }

        public bool TryPass(BattleChainParticipant participant)
        {
            if (phase != BattleChainPhase.Building ||
                participant != nextParticipant)
            {
                return false;
            }

            consecutivePasses++;
            nextParticipant = OpponentOf(participant);
            if (consecutivePasses >= 2)
            {
                phase = BattleChainPhase.Resolving;
            }

            return true;
        }

        public bool TryGetNextResolvingLink(out BattleChainLink link)
        {
            link = null;
            if (phase != BattleChainPhase.Resolving)
            {
                return false;
            }

            for (int index = links.Count - 1; index >= 0; index--)
            {
                BattleChainLink candidate = links[index];
                if (candidate.Status != BattleChainLinkStatus.Pending)
                {
                    continue;
                }

                candidate.BeginResolving();
                link = candidate;
                return true;
            }

            return false;
        }

        public bool TryCompleteResolvingLink(
            BattleChainLink link,
            BattleChainLinkStatus finalStatus)
        {
            if (phase != BattleChainPhase.Resolving ||
                link == null ||
                link.Status != BattleChainLinkStatus.Resolving ||
                !links.Contains(link))
            {
                return false;
            }

            link.Complete(finalStatus);
            bool pending = links.Exists(value =>
                value.Status == BattleChainLinkStatus.Pending ||
                value.Status == BattleChainLinkStatus.Resolving);
            if (!pending)
            {
                phase = BattleChainPhase.Idle;
                consecutivePasses = 0;
            }

            return true;
        }

        public void ClearCompleted()
        {
            if (phase == BattleChainPhase.Idle)
            {
                links.Clear();
            }
        }

        private static BattleChainParticipant OpponentOf(
            BattleChainParticipant participant)
        {
            return participant == BattleChainParticipant.Player
                ? BattleChainParticipant.Enemy
                : BattleChainParticipant.Player;
        }
    }
}
