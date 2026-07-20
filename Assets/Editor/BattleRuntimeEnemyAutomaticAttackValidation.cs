using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleRuntimeEnemyAutomaticAttackValidation
    {
        [MenuItem("Have a Break/Validate Runtime Enemy Automatic Attacks")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            if (valid)
            {
                Debug.Log("Battle runtime enemy automatic and repeated attacks passed.");
            }
            else
            {
                Debug.LogError("Battle runtime enemy automatic and repeated attacks failed.");
            }

            EditorUtility.DisplayDialog(
                "Battle Runtime Enemy Automatic Attack Validation",
                valid
                    ? "Battle runtime enemy automatic and repeated attacks passed."
                    : "Battle runtime enemy automatic and repeated attacks failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            CardData c01 = FindCard("C01");
            return c01 != null &&
                   ValidateDirectPlayerAttack() &&
                   ValidateAutomaticMonsterAttack(c01) &&
                   ValidateRepeatedReselection(c01);
        }

        private static bool ValidateDirectPlayerAttack()
        {
            BattleRuntimeState runtime = new(
                Array.Empty<BattleCardInstance>(), 391);
            if (!runtime.TryAddEnemy(
                    "AUTO-DIRECT", 5, 10,
                    EnemyFieldPosition.Center, out _) ||
                !BeginEnemyTurn(runtime) ||
                runtime.EnemyStatuses.Find("AUTO-DIRECT")?.ApplyWeaken(2) != 2 ||
                !BattleRuntimeEnemyAutoAttackService.TryResolve(
                    runtime,
                    "AUTO-DIRECT",
                    0,
                    out BattleRuntimeEnemyAutoAttackResult result,
                    out BattleRuntimeEnemyAutoAttackFailure failure))
            {
                return false;
            }

            return failure == BattleRuntimeEnemyAutoAttackFailure.None &&
                   result != null &&
                   result.AttackedPlayer &&
                   result.Target.TargetId == BattlePlayerState.PlayerTargetId &&
                   result.MonsterDeclaration == null &&
                   result.MonsterResolution == null &&
                   result.PlayerResolution != null &&
                   result.PlayerResolution.RawAttack == 5 &&
                   result.PlayerResolution.WeakenReduction == 2 &&
                   result.PlayerResolution.AdjustedAttack == 3 &&
                   result.PlayerResolution.PlayerDamage == 3 &&
                   runtime.Player.CurrentHealth ==
                   BattlePlayerState.DefaultMaximumHealth - 3 &&
                   result.CompletedAttack != null;
        }

        private static bool ValidateAutomaticMonsterAttack(CardData card)
        {
            BattleCardInstance instance = Instance(card, "AUTO-MONSTER");
            BattleRuntimeState runtime = new(new[] { instance }, 392);
            if (!PlaceMonster(
                    runtime, instance,
                    PlayerMonsterFieldPosition.Center,
                    out BattleMonsterState monster) ||
                !runtime.TryAddEnemy(
                    "AUTO-MONSTER", 1, 10,
                    EnemyFieldPosition.Center, out _) ||
                !BeginEnemyTurn(runtime) ||
                !BattleRuntimeEnemyAutoAttackService.TryResolve(
                    runtime,
                    "AUTO-MONSTER",
                    0,
                    out BattleRuntimeEnemyAutoAttackResult result,
                    out BattleRuntimeEnemyAutoAttackFailure failure))
            {
                return false;
            }

            return failure == BattleRuntimeEnemyAutoAttackFailure.None &&
                   result != null &&
                   !result.AttackedPlayer &&
                   result.Target.TargetMonster == monster &&
                   result.Target.TargetPosition ==
                   PlayerMonsterFieldPosition.Center &&
                   result.MonsterDeclaration != null &&
                   result.MonsterResolution != null &&
                   result.PlayerResolution == null &&
                   result.MonsterResolution.AdjustedAttack == 1 &&
                   result.CompletedAttack != null;
        }

        private static bool ValidateRepeatedReselection(CardData card)
        {
            BattleCardInstance centerCard = Instance(card, "REPEAT-CENTER");
            BattleCardInstance rightCard = Instance(card, "REPEAT-RIGHT");
            BattleRuntimeState runtime = new(
                new[] { centerCard, rightCard }, 393);
            if (!PlaceMonster(
                    runtime, centerCard,
                    PlayerMonsterFieldPosition.Center,
                    out BattleMonsterState centerMonster) ||
                !PlaceMonster(
                    runtime, rightCard,
                    PlayerMonsterFieldPosition.Right,
                    out BattleMonsterState rightMonster) ||
                centerMonster.MaximumHealth != rightMonster.MaximumHealth ||
                !runtime.TryAddEnemy(
                    "AUTO-REPEAT",
                    centerMonster.MaximumHealth,
                    10,
                    EnemyFieldPosition.Center,
                    out _) ||
                !BeginEnemyTurn(runtime) ||
                !BattleRuntimeEnemyRepeatedAttackService.TryResolve(
                    runtime,
                    "AUTO-REPEAT",
                    3,
                    new[] { 0, 0, 0 },
                    out BattleRuntimeEnemyRepeatedAttackResult result,
                    out BattleRuntimeEnemyRepeatedAttackFailure failure,
                    out int failedAttackIndex))
            {
                return false;
            }

            IReadOnlyList<BattleRuntimeEnemyAutoAttackResult> attacks =
                result.Attacks;
            return failure ==
                   BattleRuntimeEnemyRepeatedAttackFailure.None &&
                   failedAttackIndex == -1 &&
                   result.RequestedAttackCount == 3 &&
                   result.ResolvedAttackCount == 3 &&
                   !result.StoppedByPlayerDefeat &&
                   attacks[0].Target.TargetPosition ==
                   PlayerMonsterFieldPosition.Center &&
                   attacks[1].Target.TargetPosition ==
                   PlayerMonsterFieldPosition.Right &&
                   attacks[2].AttackedPlayer &&
                   attacks[2].Target.TargetId ==
                   BattlePlayerState.PlayerTargetId &&
                   runtime.PlayerMonsterPositions.Count == 0 &&
                   runtime.Monsters.Find(centerCard.Ids.BattleCardId) == null &&
                   runtime.Monsters.Find(rightCard.Ids.BattleCardId) == null &&
                   centerCard.Zone == CardZone.Graveyard &&
                   rightCard.Zone == CardZone.Graveyard &&
                   attacks[2].PlayerResolution.PlayerDamage > 0;
        }

        private static bool BeginEnemyTurn(BattleRuntimeState runtime)
        {
            return runtime.Turn.TryBeginBattle(out _) &&
                   runtime.Turn.TryConfirmStartingHand(
                       Array.Empty<string>(), out _, out _, out _) &&
                   runtime.Turn.TryEndPlayerTurn(out _);
        }

        private static bool PlaceMonster(
            BattleRuntimeState runtime,
            BattleCardInstance card,
            PlayerMonsterFieldPosition position,
            out BattleMonsterState monster)
        {
            monster = null;
            return runtime.Deck.Zones.TryMove(
                       card.Ids.BattleCardId,
                       CardZone.MonsterField,
                       out _) &&
                   runtime.TryRegisterFieldMonster(
                       card.Ids.BattleCardId,
                       position,
                       out monster);
        }

        private static BattleCardInstance Instance(
            CardData card,
            string suffix)
        {
            return new BattleCardInstance(
                card,
                new CardInstanceIds(
                    card.CatalogCardId,
                    $"OWNED-RUNTIME-39-{suffix}",
                    $"BATTLE-RUNTIME-39-{suffix}"),
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
