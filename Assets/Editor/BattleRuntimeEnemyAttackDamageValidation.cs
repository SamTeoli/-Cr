using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleRuntimeEnemyAttackDamageValidation
    {
        [MenuItem("Have a Break/Validate Runtime Enemy Attack Damage")]
        private static void ValidateFromMenu()
        {
            CardData c01 = FindCard("C01");
            CardData c09 = FindCard("C09");
            bool valid = c01 != null && c09 != null &&
                         ValidateC09Overflow(c01, c09) &&
                         ValidateWeakenAndDefense(c01);

            if (valid)
            {
                Debug.Log("Battle runtime enemy attack damage passed.");
            }
            else
            {
                Debug.LogError("Battle runtime enemy attack damage failed.");
            }

            EditorUtility.DisplayDialog(
                "Battle Runtime Enemy Attack Damage Validation",
                valid
                    ? "Battle runtime enemy attack damage passed."
                    : "Battle runtime enemy attack damage failed. Check the Console.",
                "OK");
        }

        private static bool ValidateC09Overflow(CardData c01, CardData c09)
        {
            int monsterHealth = Mathf.Max(1, c01.ResolveLevel(1).Health);
            BattleCardInstance ally = Instance(c01, 1, "OVERFLOW-ALLY");
            BattleCardInstance trap = Instance(c09, 5, "OVERFLOW-C09");
            BattleRuntimeState runtime = new(
                new[] { ally, trap }, 361, 10);
            if (!runtime.TryAddEnemy(
                    "ENEMY-OVERFLOW",
                    monsterHealth + 8,
                    10,
                    EnemyFieldPosition.Left,
                    out _) ||
                !PrepareMonster(runtime, ally, out BattleMonsterState allyState) ||
                !BattleRuntimeCardPlayService.TryPlay(
                    runtime,
                    trap.Ids.BattleCardId,
                    out BattleRuntimeCardPlayResult playResult,
                    out _,
                    out _) ||
                !BattleRuntimeTrapEffectService.TryRegisterInstallation(
                    runtime,
                    playResult,
                    out _) ||
                !EndPlayerTurn(runtime) ||
                !BattleRuntimeEnemyAttackService.TryDeclare(
                    runtime,
                    "ENEMY-OVERFLOW",
                    ally.Ids.BattleCardId,
                    out BattleRuntimeEnemyAttackDeclarationResult declaration,
                    out _) ||
                !BattleRuntimeEnemyAttackService.TryResolveDamage(
                    runtime,
                    declaration,
                    out BattleRuntimeEnemyAttackResolutionResult resolution,
                    out BattleRuntimeEnemyAttackFailure resolutionFailure))
            {
                return false;
            }

            int playerHealthAfterFirstResolution = runtime.Player.CurrentHealth;
            bool duplicateBlocked =
                !BattleRuntimeEnemyAttackService.TryResolveDamage(
                    runtime,
                    declaration,
                    out _,
                    out BattleRuntimeEnemyAttackFailure duplicateFailure) &&
                duplicateFailure == BattleRuntimeEnemyAttackFailure.AlreadyResolved &&
                runtime.Player.CurrentHealth == playerHealthAfterFirstResolution;

            return resolutionFailure == BattleRuntimeEnemyAttackFailure.None &&
                   resolution.RawAttack == monsterHealth + 8 &&
                   resolution.WeakenReduction == 0 &&
                   resolution.AdjustedAttack == monsterHealth + 8 &&
                   declaration.DefenseGained == 5 &&
                   resolution.DefenseConsumed == 5 &&
                   resolution.MonsterDamage == monsterHealth &&
                   resolution.OverflowDamage == 3 &&
                   resolution.PlayerDamage == 3 &&
                   runtime.Player.MaximumHealth ==
                   BattlePlayerState.DefaultMaximumHealth &&
                   runtime.Player.CurrentHealth == 27 &&
                   allyState.Defense == 0 &&
                   allyState.CurrentHealth == 0 &&
                   ally.Zone == CardZone.Graveyard &&
                   runtime.Monsters.Find(ally.Ids.BattleCardId) == null &&
                   resolution.DefenseConsumedEvent != null &&
                   resolution.MonsterDamageEvent != null &&
                   resolution.PlayerDamageEvent != null &&
                   resolution.DestructionEvents.Count == 1 &&
                   resolution.CompletedAttack != null &&
                   resolution.CompletedAttack.EventType ==
                   BattleEventType.AttackCompleted &&
                   duplicateBlocked;
        }

        private static bool ValidateWeakenAndDefense(CardData c01)
        {
            int monsterHealth = Mathf.Max(1, c01.ResolveLevel(1).Health);
            BattleCardInstance ally = Instance(c01, 1, "WEAKEN-ALLY");
            BattleRuntimeState runtime = new(new[] { ally }, 362, 10);
            if (!runtime.TryAddEnemy(
                    "ENEMY-WEAKEN",
                    monsterHealth + 5,
                    10,
                    EnemyFieldPosition.Left,
                    out _) ||
                !PrepareMonster(runtime, ally, out BattleMonsterState allyState))
            {
                return false;
            }

            BattleEnemyStatusState status =
                runtime.EnemyStatuses.Find("ENEMY-WEAKEN");
            if (status == null || status.ApplyWeaken(2) != 2 ||
                allyState.ApplyDefense(3) != 3 ||
                !EndPlayerTurn(runtime) ||
                !BattleRuntimeEnemyAttackService.TryDeclare(
                    runtime,
                    "ENEMY-WEAKEN",
                    ally.Ids.BattleCardId,
                    out BattleRuntimeEnemyAttackDeclarationResult declaration,
                    out _) ||
                !BattleRuntimeEnemyAttackService.TryResolveDamage(
                    runtime,
                    declaration,
                    out BattleRuntimeEnemyAttackResolutionResult resolution,
                    out BattleRuntimeEnemyAttackFailure resolutionFailure))
            {
                return false;
            }

            return resolutionFailure == BattleRuntimeEnemyAttackFailure.None &&
                   resolution.RawAttack == monsterHealth + 5 &&
                   resolution.WeakenReduction == 2 &&
                   resolution.AdjustedAttack == monsterHealth + 3 &&
                   resolution.DefenseConsumed == 3 &&
                   resolution.MonsterDamage == monsterHealth &&
                   resolution.OverflowDamage == 0 &&
                   resolution.PlayerDamage == 0 &&
                   resolution.PlayerDamageEvent == null &&
                   runtime.Player.CurrentHealth ==
                   BattlePlayerState.DefaultMaximumHealth &&
                   ally.Zone == CardZone.Graveyard &&
                   resolution.DestructionEvents.Count == 1 &&
                   resolution.CompletedAttack != null;
        }

        private static bool PrepareMonster(
            BattleRuntimeState runtime,
            BattleCardInstance ally,
            out BattleMonsterState allyState)
        {
            allyState = null;
            return runtime.Turn.TryBeginBattle(out _) &&
                   runtime.Turn.TryConfirmStartingHand(
                       Array.Empty<string>(), out _, out _, out _) &&
                   runtime.Deck.Zones.TryMove(
                       ally.Ids.BattleCardId,
                       CardZone.MonsterField,
                       out _) &&
                   runtime.TryRegisterFieldMonster(
                       ally.Ids.BattleCardId,
                       out allyState);
        }

        private static bool EndPlayerTurn(BattleRuntimeState runtime)
        {
            int firstPlayerTurnEventIndex = runtime.EventLog.Events.Count;
            return BattleRuntimeTurnEffectService.TryEndPlayerTurn(
                       runtime,
                       firstPlayerTurnEventIndex,
                       out _,
                       out _) &&
                   runtime.Turn.Phase == BattleTurnPhase.EnemyTurn;
        }

        private static BattleCardInstance Instance(
            CardData card,
            int level,
            string suffix)
        {
            return new BattleCardInstance(
                card,
                new CardInstanceIds(
                    card.CatalogCardId,
                    $"OWNED-RUNTIME-36D-{suffix}",
                    $"BATTLE-RUNTIME-36D-{suffix}"),
                level,
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
