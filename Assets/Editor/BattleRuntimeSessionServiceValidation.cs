using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleRuntimeSessionServiceValidation
    {
        [MenuItem("Have a Break/Validate Multi-Round Battle Runtime Session")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            if (valid)
            {
                Debug.Log("Multi-round battle runtime session passed.");
            }
            else
            {
                Debug.LogError("Multi-round battle runtime session failed.");
            }

            EditorUtility.DisplayDialog(
                "Multi-Round Battle Runtime Session Validation",
                valid
                    ? "Multi-round battle runtime session passed."
                    : "Multi-round battle runtime session failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            CardData c01 = FindCard(TestContentIds.C01);
            bool valid = c01 != null;
            valid &= Run(
                "two consecutive rounds",
                () => c01 != null && ValidateTwoRounds(c01));
            valid &= Run(
                "terminal outcome locking",
                ValidateTerminalLocking);
            valid &= Run(
                "session lifecycle rejection",
                ValidateLifecycleRejection);
            return valid;
        }

        private static bool ValidateTwoRounds(CardData card)
        {
            BattleCardInstance ally = Instance(card, "TWO-ROUNDS");
            BattleRuntimeState runtime = new(new[] { ally }, 421, 10);
            if (!runtime.TryAddEnemy(
                    "ENEMY-SESSION-ONGOING",
                    1,
                    10,
                    EnemyFieldPosition.Center,
                    out _))
            {
                return false;
            }

            BattleRuntimeSessionState session = new(runtime);
            if (!BattleRuntimeSessionService.TryBegin(
                    session,
                    Array.Empty<string>(),
                    out List<BattleCardInstance> replacements,
                    out BattleRuntimeSessionFailure beginFailure,
                    out StartingHandRedrawFailure redrawFailure,
                    out BattleTurnFailure beginTurnFailure) ||
                beginFailure != BattleRuntimeSessionFailure.None ||
                redrawFailure != StartingHandRedrawFailure.None ||
                beginTurnFailure != BattleTurnFailure.None ||
                replacements.Count != 0 ||
                !runtime.Deck.Zones.TryMove(
                    ally.Ids.BattleCardId,
                    CardZone.MonsterField,
                    out _) ||
                !runtime.TryRegisterFieldMonster(
                    ally.Ids.BattleCardId,
                    PlayerMonsterFieldPosition.Center,
                    out BattleMonsterState monster))
            {
                return false;
            }

            int initialHealth = monster.CurrentHealth;
            int firstBaseline = session.PlayerTurnEventStartIndex;
            if (!ResolveOngoingRound(
                    session,
                    out BattleRuntimeSessionRoundResult first) ||
                first.CompletedRoundCount != 1 ||
                first.Outcome != BattleOutcome.Ongoing ||
                !first.PlayerTurnStarted ||
                monster.CurrentHealth != initialHealth - 1 ||
                session.PlayerTurnEventStartIndex <= firstBaseline)
            {
                return false;
            }

            int secondBaseline = session.PlayerTurnEventStartIndex;
            return ResolveOngoingRound(
                       session,
                       out BattleRuntimeSessionRoundResult second) &&
                   second.CompletedRoundCount == 2 &&
                   second.Outcome == BattleOutcome.Ongoing &&
                   second.PlayerTurnStarted &&
                   monster.CurrentHealth == initialHealth - 2 &&
                   session.CompletedRoundCount == 2 &&
                   session.PlayerTurnEventStartIndex > secondBaseline &&
                   runtime.Turn.Phase == BattleTurnPhase.PlayerAction &&
                   runtime.Turn.PlayerTurnNumber == 3;
        }

        private static bool ResolveOngoingRound(
            BattleRuntimeSessionState session,
            out BattleRuntimeSessionRoundResult result)
        {
            bool resolved = BattleRuntimeSessionService.TryResolveRound(
                session,
                new[]
                {
                    BattleRuntimeEnemyTurnCommand.CreateAutomaticAttack(
                        "ENEMY-SESSION-ONGOING", 1, new[] { 0 })
                },
                out result,
                out BattleRuntimeSessionFailure sessionFailure,
                out BattleRuntimeRoundFailure roundFailure,
                out BattleTurnFailure playerTurnEndFailure,
                out BattleRuntimeEnemyTurnPipelineFailure pipelineFailure,
                out BattleRuntimeEnemyTurnPlanFailure planFailure,
                out BattleRuntimeEnemyTurnFailure enemyTurnFailure,
                out int failedActionIndex);

            return resolved &&
                   sessionFailure == BattleRuntimeSessionFailure.None &&
                   roundFailure == BattleRuntimeRoundFailure.None &&
                   playerTurnEndFailure == BattleTurnFailure.None &&
                   pipelineFailure ==
                   BattleRuntimeEnemyTurnPipelineFailure.None &&
                   planFailure == BattleRuntimeEnemyTurnPlanFailure.None &&
                   enemyTurnFailure == BattleRuntimeEnemyTurnFailure.None &&
                   failedActionIndex == -1 &&
                   result != null;
        }

        private static bool ValidateTerminalLocking()
        {
            BattleRuntimeState runtime = new(
                Array.Empty<BattleCardInstance>(),
                422,
                10,
                2);
            if (!runtime.TryAddEnemy(
                    "ENEMY-SESSION-DEFEAT",
                    2,
                    10,
                    EnemyFieldPosition.Center,
                    out _))
            {
                return false;
            }

            BattleRuntimeSessionState session = new(runtime);
            if (!BattleRuntimeSessionService.TryBegin(
                    session,
                    Array.Empty<string>(),
                    out _, out _, out _, out _) ||
                !BattleRuntimeSessionService.TryResolveRound(
                    session,
                    new[]
                    {
                        BattleRuntimeEnemyTurnCommand.CreateAutomaticAttack(
                            "ENEMY-SESSION-DEFEAT",
                            3,
                            new[] { 0, 0, 0 })
                    },
                    out BattleRuntimeSessionRoundResult result,
                    out BattleRuntimeSessionFailure sessionFailure,
                    out BattleRuntimeRoundFailure roundFailure,
                    out _, out _, out _, out _, out _))
            {
                return false;
            }

            bool locked = !BattleRuntimeSessionService.TryResolveRound(
                    session,
                    Array.Empty<BattleRuntimeEnemyTurnCommand>(),
                    out _,
                    out BattleRuntimeSessionFailure lockedFailure,
                    out _, out _, out _, out _, out _, out _);
            return sessionFailure == BattleRuntimeSessionFailure.None &&
                   roundFailure == BattleRuntimeRoundFailure.None &&
                   result != null &&
                   result.CompletedRoundCount == 1 &&
                   result.Outcome == BattleOutcome.Defeat &&
                   !result.PlayerTurnStarted &&
                   session.IsFinished &&
                   runtime.Player.IsDefeated &&
                   locked &&
                   lockedFailure == BattleRuntimeSessionFailure.BattleFinished;
        }

        private static bool ValidateLifecycleRejection()
        {
            BattleRuntimeSessionState session = new(
                new BattleRuntimeState(
                    Array.Empty<BattleCardInstance>(), 423));
            bool roundBeforeStartRejected =
                !BattleRuntimeSessionService.TryResolveRound(
                    session,
                    Array.Empty<BattleRuntimeEnemyTurnCommand>(),
                    out _,
                    out BattleRuntimeSessionFailure beforeStartFailure,
                    out _, out _, out _, out _, out _, out _) &&
                beforeStartFailure == BattleRuntimeSessionFailure.NotStarted;

            bool began = BattleRuntimeSessionService.TryBegin(
                session,
                Array.Empty<string>(),
                out _, out _, out _, out _);
            bool duplicateRejected =
                !BattleRuntimeSessionService.TryBegin(
                    session,
                    Array.Empty<string>(),
                    out _,
                    out BattleRuntimeSessionFailure duplicateFailure,
                    out _, out _) &&
                duplicateFailure ==
                BattleRuntimeSessionFailure.AlreadyStarted;

            return roundBeforeStartRejected &&
                   began &&
                   session.Outcome == BattleOutcome.Victory &&
                   session.IsFinished &&
                   duplicateRejected;
        }

        private static bool Run(string label, Func<bool> validation)
        {
            try
            {
                bool passed = validation();
                if (passed)
                {
                    Debug.Log($"Battle session validation passed: {label}.");
                }
                else
                {
                    Debug.LogError($"Battle session validation failed: {label}.");
                }

                return passed;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Battle session validation threw: {label}.\n{exception}");
                return false;
            }
        }

        private static BattleCardInstance Instance(
            CardData card,
            string suffix)
        {
            return new BattleCardInstance(
                card,
                new CardInstanceIds(
                    card.CatalogCardId,
                    $"OWNED-RUNTIME-42-{suffix}",
                    $"BATTLE-RUNTIME-42-{suffix}"),
                1,
                CardZone.DrawPile);
        }

        private static CardData FindCard(string catalogCardId)
        {
            return AssetDatabase.FindAssets("t:CardData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<CardData>)
                .FirstOrDefault(card => card != null && string.Equals(
                    card.CatalogCardId,
                    catalogCardId,
                    StringComparison.OrdinalIgnoreCase));
        }
    }
}
