using System;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleRuntimeChainStateValidation
    {
        [MenuItem("Have a Break/Validate Battle Chain Foundation")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            EditorUtility.DisplayDialog(
                "Battle Chain Foundation",
                valid
                    ? "Chain building, passing, and reverse resolution passed."
                    : "Chain foundation failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            BattleRuntimeChainState chain = new();
            BattleActivationContext first = new(
                "PLAYER-CARD",
                "PLAYER-EFFECT",
                BattleChainParticipant.Player,
                "AttackDeclared",
                1,
                new[]
                {
                    new BattleEffectTarget("ENEMY-A", "EnemyMonster")
                });
            BattleActivationContext response = new(
                "ENEMY-CARD",
                "ENEMY-RESPONSE",
                BattleChainParticipant.Enemy,
                "AttackDeclared",
                0,
                new[]
                {
                    new BattleEffectTarget("PLAYER-CARD", "AllyMonster")
                });

            bool valid =
                chain.TryBegin(first, out BattleChainLink firstLink) &&
                firstLink.LinkIndex == 1 &&
                chain.Phase == BattleChainPhase.Building &&
                chain.NextParticipant == BattleChainParticipant.Enemy &&
                chain.TryAdd(response, out BattleChainLink secondLink) &&
                secondLink.LinkIndex == 2 &&
                chain.NextParticipant == BattleChainParticipant.Player &&
                chain.TryPass(BattleChainParticipant.Player) &&
                chain.TryPass(BattleChainParticipant.Enemy) &&
                chain.Phase == BattleChainPhase.Resolving &&
                chain.IsInputLocked &&
                chain.TryGetNextResolvingLink(out BattleChainLink resolving2) &&
                resolving2 == secondLink &&
                chain.TryCompleteResolvingLink(
                    resolving2,
                    BattleChainLinkStatus.Resolved) &&
                chain.TryGetNextResolvingLink(out BattleChainLink resolving1) &&
                resolving1 == firstLink &&
                chain.TryCompleteResolvingLink(
                    resolving1,
                    BattleChainLinkStatus.Negated) &&
                chain.Phase == BattleChainPhase.Idle &&
                secondLink.Status == BattleChainLinkStatus.Resolved &&
                firstLink.Status == BattleChainLinkStatus.Negated;

            chain.ClearCompleted();
            valid &= chain.Links.Count == 0 && !chain.HasChain;

            if (valid)
            {
                Debug.Log("Battle chain foundation passed.");
            }
            else
            {
                Debug.LogError("Battle chain foundation failed.");
            }

            return valid;
        }
    }
}
