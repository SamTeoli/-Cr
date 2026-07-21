using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleRuntimeCommonEnemyStatusValidation
    {
        [MenuItem("Have a Break/Validate Common Enemy Status Turns")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            if (valid)
            {
                Debug.Log("Common enemy status turn flow passed.");
            }
            else
            {
                Debug.LogError("Common enemy status turn flow failed.");
            }

            EditorUtility.DisplayDialog(
                "Common Enemy Status Turn Validation",
                valid
                    ? "Common enemy status turn flow passed."
                    : "Common enemy status turn flow failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            return ValidateLowLevelGuards() &&
                   ValidateTurnFlow() &&
                   ValidateInjuryVictoryStopsPlayerTurn();
        }

        private static bool ValidateLowLevelGuards()
        {
            BattleRuntimeState runtime = new(
                Array.Empty<BattleCardInstance>(),
                380);
            if (!runtime.TryAddEnemy(
                    "STATUS-GUARD",
                    4,
                    10,
                    EnemyFieldPosition.Center,
                    out BattleEnemyRuntimeState enemy) ||
                !runtime.Turn.TryBeginBattle(out _) ||
                !runtime.Turn.TryConfirmStartingHand(
                    Array.Empty<string>(), out _, out _, out _) ||
                !runtime.Turn.TryEndPlayerTurn(out _))
            {
                return false;
            }

            BattleEnemyStatusState status =
                runtime.EnemyStatuses.Find(enemy.EnemyId);
            if (status == null || status.ApplyBind(1) != 1 ||
                BattleRuntimeEnemyDirectAttackService.TryResolve(
                    runtime,
                    enemy.EnemyId,
                    out _,
                    out BattleRuntimeEnemyDirectAttackFailure attackFailure) ||
                attackFailure != BattleRuntimeEnemyDirectAttackFailure
                    .ActionBlockedByStatus ||
                status.ApplyStun(1) != 1 ||
                BattleRuntimeEnemyAbilityService.TryResolve(
                    runtime,
                    new EnemyAbilityResolutionContext(
                        "STATUS-GUARD-ABILITY",
                        enemy.EnemyId,
                        false,
                        false,
                        false),
                    out _,
                    out BattleRuntimeEnemyAbilityFailure abilityFailure) ||
                abilityFailure != BattleRuntimeEnemyAbilityFailure
                    .ActionBlockedByStatus)
            {
                return false;
            }

            return true;
        }

        private static bool ValidateTurnFlow()
        {
            BattleRuntimeState runtime = new(
                Array.Empty<BattleCardInstance>(),
                381);
            if (!runtime.TryAddEnemy(
                    "STATUS-BIND",
                    4,
                    10,
                    EnemyFieldPosition.Left,
                    out BattleEnemyRuntimeState bindEnemy) ||
                !runtime.TryAddEnemy(
                    "STATUS-STUN",
                    4,
                    2,
                    EnemyFieldPosition.Right,
                    out BattleEnemyRuntimeState stunEnemy) ||
                !runtime.Turn.TryBeginBattle(out _) ||
                !runtime.Turn.TryConfirmStartingHand(
                    Array.Empty<string>(), out _, out _, out _))
            {
                return false;
            }

            BattleEnemyStatusState bind =
                runtime.EnemyStatuses.Find(bindEnemy.EnemyId);
            BattleEnemyStatusState stun =
                runtime.EnemyStatuses.Find(stunEnemy.EnemyId);
            if (bind == null || stun == null ||
                bind.ApplyInjury(2) != 2 ||
                bind.ApplyBind(2) != 2 ||
                bind.ApplyWeaken(2) != 2 ||
                bind.ApplyVulnerable(3) != 3 ||
                stun.ApplyInjury(3) != 3 ||
                stun.ApplyStun(1) != 1 ||
                !runtime.Turn.TryEndPlayerTurn(out _))
            {
                return false;
            }

            BattleRuntimeEnemyTurnCommand[] commands =
            {
                BattleRuntimeEnemyTurnCommand.CreateAutomaticAttack(
                    bindEnemy.EnemyId, 1, new[] { 0 }),
                BattleRuntimeEnemyTurnCommand.CreateAbility(
                    new EnemyAbilityResolutionContext(
                        "STATUS-BIND-ABILITY",
                        bindEnemy.EnemyId,
                        false,
                        false,
                        false)),
                BattleRuntimeEnemyTurnCommand.CreateAutomaticAttack(
                    stunEnemy.EnemyId, 1, new[] { 0 }),
                BattleRuntimeEnemyTurnCommand.CreateAbility(
                    new EnemyAbilityResolutionContext(
                        "STATUS-STUN-ABILITY",
                        stunEnemy.EnemyId,
                        false,
                        false,
                        false))
            };

            bool resolved = BattleRuntimeEnemyTurnService.TryResolve(
                runtime,
                commands,
                out BattleRuntimeEnemyTurnResult result,
                out BattleRuntimeEnemyTurnFailure failure,
                out int failedActionIndex);
            BattleRuntimeEnemyStatusTurnEntryResult bindTurn =
                result?.StatusTurnEnd?.Entries.FirstOrDefault(
                    item => item.EnemyId == bindEnemy.EnemyId);
            BattleRuntimeEnemyStatusTurnEntryResult stunTurn =
                result?.StatusTurnEnd?.Entries.FirstOrDefault(
                    item => item.EnemyId == stunEnemy.EnemyId);

            return resolved &&
                   failure == BattleRuntimeEnemyTurnFailure.None &&
                   failedActionIndex == -1 &&
                   result != null &&
                   result.ProcessedActionCount == 4 &&
                   result.ActionResults[0].BlockedByStatus ==
                   StatusKeyword.Bind &&
                   result.ActionResults[1].AbilityResult != null &&
                   !result.ActionResults[1].AbilityResult.Cancelled &&
                   result.ActionResults[2].BlockedByStatus ==
                   StatusKeyword.Stun &&
                   result.ActionResults[3].BlockedByStatus ==
                   StatusKeyword.Stun &&
                   result.ActionResults[0].StatusBlockEvent?.EventType ==
                   BattleEventType.EnemyActionBlocked &&
                   result.ActionResults[2].StatusBlockEvent?.EventType ==
                   BattleEventType.EnemyActionBlocked &&
                   result.StatusTurnEnd != null &&
                   result.StatusTurnEnd.TotalInjuryDamage == 4 &&
                   result.StatusTurnEnd.DefeatedEnemyCount == 1 &&
                   bindTurn != null && bindTurn.InjuryDamage == 2 &&
                   bindTurn.InjuryBefore == 2 && bindTurn.InjuryAfter == 1 &&
                   bindTurn.BindBefore == 2 && bindTurn.BindAfter == 1 &&
                   bindTurn.WeakenBefore == 2 && bindTurn.WeakenAfter == 1 &&
                   stunTurn != null && stunTurn.InjuryDamage == 2 &&
                   stunTurn.StunBefore == 1 && stunTurn.StunAfter == 0 &&
                   bindEnemy.Vital.CurrentHealth == 8 &&
                   bind.Injury == 1 && bind.Bind == 1 && bind.Stun == 0 &&
                   bind.Weaken == 1 && bind.Vulnerable == 3 &&
                   !stunEnemy.IsAlive && stun.Injury == 2 && stun.Stun == 0 &&
                   runtime.LivingEnemies.Contains(bindEnemy.EnemyId) &&
                   !runtime.LivingEnemies.Contains(stunEnemy.EnemyId) &&
                   !runtime.EnemyPositions.FindPosition(
                       stunEnemy.EnemyId).HasValue &&
                   result.Outcome == BattleOutcome.Ongoing &&
                   result.PlayerTurnStarted &&
                   runtime.Turn.Phase == BattleTurnPhase.PlayerAction &&
                   runtime.Player.CurrentHealth ==
                   BattlePlayerState.DefaultMaximumHealth &&
                   runtime.EventLog.Events.Any(item =>
                       item.Cause == "InjuryTurnEndDamage") &&
                   runtime.EventLog.Events.Any(item =>
                       item.Cause == "StunTurnEndClear");
        }

        private static bool ValidateInjuryVictoryStopsPlayerTurn()
        {
            BattleRuntimeState runtime = new(
                Array.Empty<BattleCardInstance>(),
                382);
            if (!runtime.TryAddEnemy(
                    "STATUS-INJURY-VICTORY",
                    0,
                    1,
                    EnemyFieldPosition.Center,
                    out BattleEnemyRuntimeState enemy) ||
                !runtime.Turn.TryBeginBattle(out _) ||
                !runtime.Turn.TryConfirmStartingHand(
                    Array.Empty<string>(), out _, out _, out _) ||
                runtime.EnemyStatuses.Find(enemy.EnemyId)?.ApplyInjury(1) != 1 ||
                !runtime.Turn.TryEndPlayerTurn(out _))
            {
                return false;
            }

            bool resolved = BattleRuntimeEnemyTurnService.TryResolve(
                runtime,
                Array.Empty<BattleRuntimeEnemyTurnCommand>(),
                out BattleRuntimeEnemyTurnResult result,
                out BattleRuntimeEnemyTurnFailure failure,
                out int failedActionIndex);

            return resolved &&
                   failure == BattleRuntimeEnemyTurnFailure.None &&
                   failedActionIndex == -1 &&
                   result != null &&
                   result.ProcessedActionCount == 0 &&
                   result.StatusTurnEnd != null &&
                   result.StatusTurnEnd.TotalInjuryDamage == 1 &&
                   result.StatusTurnEnd.DefeatedEnemyCount == 1 &&
                   result.Outcome == BattleOutcome.Victory &&
                   !result.PlayerTurnStarted &&
                   result.PlayerTurnStartEffects == null &&
                   !enemy.IsAlive &&
                   runtime.LivingEnemies.Count == 0 &&
                   runtime.Turn.Phase == BattleTurnPhase.EnemyTurn;
        }
    }
}
