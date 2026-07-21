using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleRuntimeCommonFriendlyStatusValidation
    {
        [MenuItem("Have a Break/Validate Common Player And Ally Status Turns")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            if (valid)
            {
                Debug.Log(
                    "Common player and ally status turn flow passed.");
            }
            else
            {
                Debug.LogError(
                    "Common player and ally status turn flow failed.");
            }

            EditorUtility.DisplayDialog(
                "Common Player And Ally Status Turn Validation",
                valid
                    ? "Common player and ally status turn flow passed."
                    : "Common player and ally status turn flow failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            CardData c03 = FindCard(TestContentIds.C03);
            return c03 != null &&
                   ValidatePlayerMonsterAttackStatuses(c03) &&
                   ValidateIncomingVulnerable(c03) &&
                   ValidateFriendlyTurnEnd(c03) &&
                   ValidateInjuryDefeatStopsEnemyTurn();
        }

        private static bool ValidatePlayerMonsterAttackStatuses(
            CardData card)
        {
            BattleRuntimeState runtime = StartWithMonster(
                card,
                "FRIENDLY-ATTACK",
                "FRIENDLY-ATTACK-ENEMY",
                20,
                out BattleMonsterState attacker,
                out _);
            BattleEnemyRuntimeState enemy = runtime?.FindEnemy(
                "FRIENDLY-ATTACK-ENEMY");
            BattleEnemyStatusState enemyStatus =
                runtime?.EnemyStatuses.Find("FRIENDLY-ATTACK-ENEMY");
            if (runtime == null || attacker == null || enemy == null ||
                enemyStatus == null || attacker.Status.ApplyBind(1) != 1)
            {
                return false;
            }

            int healthBefore = enemy.Vital.CurrentHealth;
            if (BattleRuntimePlayerAttackService.TryResolve(
                    runtime,
                    attacker.BattleCardId,
                    enemy.EnemyId,
                    out _,
                    out BattleRuntimePlayerAttackFailure bindFailure) ||
                bindFailure !=
                BattleRuntimePlayerAttackFailure.ActionBlockedByStatus ||
                enemy.Vital.CurrentHealth != healthBefore ||
                attacker.Status.ReduceBindAtTurnEnd() != 1 ||
                attacker.Status.ApplyStun(1) != 1 ||
                BattleRuntimePlayerAttackService.TryResolve(
                    runtime,
                    attacker.BattleCardId,
                    enemy.EnemyId,
                    out _,
                    out BattleRuntimePlayerAttackFailure stunFailure) ||
                stunFailure !=
                BattleRuntimePlayerAttackFailure.ActionBlockedByStatus ||
                attacker.Status.ClearStunAtTurnEnd() != 1 ||
                attacker.ApplyAttackEnhancement(4) != 4 ||
                attacker.Status.ApplyWeaken(2) != 2 ||
                enemyStatus.ApplyVulnerable(2) != 2)
            {
                return false;
            }

            int expectedAdjusted = Mathf.Max(0, attacker.Attack - 2);
            bool resolved = BattleRuntimePlayerAttackService.TryResolve(
                runtime,
                attacker.BattleCardId,
                enemy.EnemyId,
                out BattleRuntimePlayerAttackResult result,
                out BattleRuntimePlayerAttackFailure failure);
            return resolved &&
                   failure == BattleRuntimePlayerAttackFailure.None &&
                   result != null &&
                   result.BaseAttack == attacker.Attack &&
                   result.WeakenReduction == 2 &&
                   result.AdjustedAttack == expectedAdjusted &&
                   result.VulnerableBonus == 2 &&
                   result.FinalDamage == expectedAdjusted + 2 &&
                   result.DamageApplied == expectedAdjusted + 2 &&
                   enemyStatus.Vulnerable == 0 &&
                   enemy.Vital.CurrentHealth ==
                   healthBefore - expectedAdjusted - 2;
        }

        private static bool ValidateIncomingVulnerable(CardData card)
        {
            BattleRuntimeState monsterRuntime = StartWithMonster(
                card,
                "FRIENDLY-INCOMING",
                "FRIENDLY-INCOMING-ENEMY",
                20,
                out BattleMonsterState monster,
                out _);
            if (monsterRuntime == null || monster == null ||
                monster.Status.ApplyVulnerable(2) != 2 ||
                !monsterRuntime.Turn.TryEndPlayerTurn(out _) ||
                !BattleRuntimeEnemyAttackService.TryDeclare(
                    monsterRuntime,
                    "FRIENDLY-INCOMING-ENEMY",
                    monster.BattleCardId,
                    out BattleRuntimeEnemyAttackDeclarationResult declaration,
                    out _) ||
                !BattleRuntimeEnemyAttackService.TryResolveDamage(
                    monsterRuntime,
                    declaration,
                    out BattleRuntimeEnemyAttackResolutionResult resolution,
                    out _))
            {
                return false;
            }

            BattleRuntimeState playerRuntime = new(
                Array.Empty<BattleCardInstance>(),
                412);
            if (!playerRuntime.TryAddEnemy(
                    "FRIENDLY-PLAYER-INCOMING",
                    3,
                    20,
                    EnemyFieldPosition.Center,
                    out _) ||
                !BeginPlayerTurn(playerRuntime) ||
                !playerRuntime.Turn.TryEndPlayerTurn(out _) ||
                playerRuntime.Player.Status.ApplyVulnerable(2) != 2 ||
                !BattleRuntimeEnemyDirectAttackService.TryResolve(
                    playerRuntime,
                    "FRIENDLY-PLAYER-INCOMING",
                    out BattleRuntimeEnemyDirectAttackResult direct,
                    out _))
            {
                return false;
            }

            return resolution.MonsterVulnerableBonus == 2 &&
                   resolution.DamageBeforeDefense == 5 &&
                   resolution.MonsterDamage == 5 &&
                   monster.Status.Vulnerable == 0 &&
                   direct.PlayerVulnerableBonus == 2 &&
                   direct.FinalIncomingDamage == 5 &&
                   direct.PlayerDamage == 5 &&
                   playerRuntime.Player.Status.Vulnerable == 0 &&
                   playerRuntime.Player.CurrentHealth ==
                   BattlePlayerState.DefaultMaximumHealth - 5;
        }

        private static bool ValidateFriendlyTurnEnd(CardData card)
        {
            BattleRuntimeState runtime = StartWithMonster(
                card,
                "FRIENDLY-TURN-END",
                "FRIENDLY-TURN-END-ENEMY",
                20,
                out BattleMonsterState monster,
                out int eventStartIndex);
            if (runtime == null || monster == null ||
                monster.Status.ApplyInjury(1) != 1 ||
                monster.Status.ApplyBind(2) != 2 ||
                monster.Status.ApplyStun(1) != 1 ||
                monster.Status.ApplyWeaken(2) != 2 ||
                monster.Status.ApplyVulnerable(1) != 1 ||
                runtime.Player.Status.ApplyInjury(1) != 1 ||
                runtime.Player.Status.ApplyBind(2) != 2 ||
                runtime.Player.Status.ApplyStun(1) != 1 ||
                runtime.Player.Status.ApplyWeaken(2) != 2 ||
                runtime.Player.Status.ApplyVulnerable(1) != 1)
            {
                return false;
            }

            int monsterHealthBefore = monster.CurrentHealth;
            int playerHealthBefore = runtime.Player.CurrentHealth;
            bool resolved = BattleRuntimeTurnEffectService.TryEndPlayerTurn(
                runtime,
                eventStartIndex,
                out BattleRuntimeTurnEffectResult result,
                out BattleTurnFailure failure);
            BattleRuntimeFriendlyStatusTurnEntryResult monsterEntry =
                result?.FriendlyStatusTurnEnd?.Entries.FirstOrDefault(
                    item => item.TargetId == monster.BattleCardId);
            BattleRuntimeFriendlyStatusTurnEntryResult playerEntry =
                result?.FriendlyStatusTurnEnd?.Entries.FirstOrDefault(
                    item => item.TargetsPlayer);

            return resolved &&
                   failure == BattleTurnFailure.None &&
                   result != null &&
                   result.ResolvedC03Count == 0 &&
                   result.TotalDefenseGained == 0 &&
                   result.Outcome == BattleOutcome.Ongoing &&
                   result.FriendlyStatusTurnEnd != null &&
                   result.FriendlyStatusTurnEnd.TotalInjuryDamage == 4 &&
                   !result.FriendlyStatusTurnEnd.PlayerDefeated &&
                   monsterEntry != null &&
                   monsterEntry.BaseInjuryDamage == 1 &&
                   monsterEntry.VulnerableBonus == 1 &&
                   monsterEntry.DamageApplied == 2 &&
                   playerEntry != null &&
                   playerEntry.BaseInjuryDamage == 1 &&
                   playerEntry.VulnerableBonus == 1 &&
                   playerEntry.DamageApplied == 2 &&
                   monster.CurrentHealth == monsterHealthBefore - 2 &&
                   runtime.Player.CurrentHealth == playerHealthBefore - 2 &&
                   monster.Status.Injury == 0 &&
                   monster.Status.Bind == 1 &&
                   monster.Status.Stun == 0 &&
                   monster.Status.Weaken == 1 &&
                   monster.Status.Vulnerable == 0 &&
                   runtime.Player.Status.Injury == 0 &&
                   runtime.Player.Status.Bind == 1 &&
                   runtime.Player.Status.Stun == 0 &&
                   runtime.Player.Status.Weaken == 1 &&
                   runtime.Player.Status.Vulnerable == 0 &&
                   runtime.Turn.Phase == BattleTurnPhase.EnemyTurn &&
                   runtime.EventLog.Events.Any(item =>
                       item.EventType ==
                       BattleEventType.PlayerMonsterActionBlocked);
        }

        private static bool ValidateInjuryDefeatStopsEnemyTurn()
        {
            BattleRuntimeState runtime = new(
                Array.Empty<BattleCardInstance>(),
                413);
            if (!runtime.TryAddEnemy(
                    "FRIENDLY-INJURY-DEFEAT",
                    10,
                    20,
                    EnemyFieldPosition.Center,
                    out _) ||
                !BeginPlayerTurn(runtime) ||
                runtime.Player.ApplyDamage(
                    BattlePlayerState.DefaultMaximumHealth - 1) !=
                BattlePlayerState.DefaultMaximumHealth - 1 ||
                runtime.Player.Status.ApplyInjury(1) != 1)
            {
                return false;
            }

            bool resolved = BattleRuntimeRoundService.TryResolve(
                runtime,
                runtime.EventLog.Events.Count,
                new[]
                {
                    BattleRuntimeEnemyTurnCommand.CreateAutomaticAttack(
                        "FRIENDLY-INJURY-DEFEAT",
                        1,
                        new[] { 0 })
                },
                out BattleRuntimeRoundResult result,
                out BattleRuntimeRoundFailure failure,
                out BattleTurnFailure turnFailure,
                out BattleRuntimeEnemyTurnPipelineFailure pipelineFailure,
                out BattleRuntimeEnemyTurnPlanFailure planFailure,
                out BattleRuntimeEnemyTurnFailure enemyTurnFailure,
                out int failedActionIndex);

            return resolved &&
                   failure == BattleRuntimeRoundFailure.None &&
                   turnFailure == BattleTurnFailure.None &&
                   pipelineFailure ==
                   BattleRuntimeEnemyTurnPipelineFailure.None &&
                   planFailure == BattleRuntimeEnemyTurnPlanFailure.None &&
                   enemyTurnFailure == BattleRuntimeEnemyTurnFailure.None &&
                   failedActionIndex == -1 &&
                   result != null &&
                   result.Outcome == BattleOutcome.Defeat &&
                   result.EnemyTurnPipeline == null &&
                   result.ProcessedEnemyActionCount == 0 &&
                   !result.PlayerTurnStarted &&
                   result.PlayerTurnEndEffects.FriendlyStatusTurnEnd
                       .PlayerDefeated &&
                   runtime.Player.IsDefeated &&
                   runtime.Turn.Phase == BattleTurnPhase.PlayerAction;
        }

        private static BattleRuntimeState StartWithMonster(
            CardData card,
            string suffix,
            string enemyId,
            int enemyHealth,
            out BattleMonsterState monster,
            out int eventStartIndex)
        {
            monster = null;
            eventStartIndex = 0;
            BattleCardInstance instance = new(
                card,
                new CardInstanceIds(
                    card.CatalogCardId,
                    $"OWNED-{suffix}",
                    $"BATTLE-{suffix}"),
                1,
                CardZone.DrawPile);
            BattleRuntimeState runtime = new(
                new[] { instance },
                411);
            if (!runtime.TryAddEnemy(
                    enemyId,
                    3,
                    enemyHealth,
                    EnemyFieldPosition.Center,
                    out _) ||
                !BeginPlayerTurn(runtime))
            {
                return null;
            }

            eventStartIndex = runtime.EventLog.Events.Count;
            if (!BattleRuntimeCardPlayService.TryPlay(
                    runtime,
                    instance.Ids.BattleCardId,
                    out BattleRuntimeCardPlayResult playResult,
                    out _,
                    out _))
            {
                return null;
            }

            monster = playResult.SummonedMonster;
            return runtime;
        }

        private static bool BeginPlayerTurn(BattleRuntimeState runtime)
        {
            return runtime.Turn.TryBeginBattle(out _) &&
                   runtime.Turn.TryConfirmStartingHand(
                       Array.Empty<string>(),
                       out _,
                       out _,
                       out _);
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
