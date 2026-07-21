using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleRuntimeTrapRegistryValidation
    {
        [MenuItem("Have a Break/Validate Runtime Trap Installation Registry")]
        private static void ValidateFromMenu()
        {
            CardData c08 = FindCard(TestContentIds.C08);
            CardData c10 = FindCard(TestContentIds.C10);
            bool valid = c08 != null && c10 != null &&
                         ValidateRegistrationAndPruning(c08) &&
                         ValidateC10Removal(c10);

            if (valid)
            {
                Debug.Log("Battle runtime trap installation registry passed.");
            }
            else
            {
                Debug.LogError("Battle runtime trap installation registry failed.");
            }

            EditorUtility.DisplayDialog(
                "Battle Runtime Trap Registry Validation",
                valid
                    ? "Battle runtime trap installation registry passed."
                    : "Battle runtime trap installation registry failed. Check the Console.",
                "OK");
        }

        private static bool ValidateRegistrationAndPruning(CardData card)
        {
            BattleRuntimeState runtime = Start(card, 1, TestContentIds.C08);
            if (runtime == null ||
                !BattleRuntimeCardPlayService.TryPlay(
                    runtime,
                    "BATTLE-RUNTIME-36A-C08",
                    out BattleRuntimeCardPlayResult playResult,
                    out _,
                    out _) ||
                !BattleRuntimeTrapEffectService.TryRegisterInstallation(
                    runtime,
                    playResult,
                    out BattleRuntimeTrapInstallation installation))
            {
                return false;
            }

            bool valid = runtime.TrapInstallations.Count == 1 &&
                         runtime.TrapInstallations.Find(
                             installation.SourceTrap.Ids.BattleCardId) ==
                         installation;
            valid &= !BattleRuntimeTrapEffectService.TryRegisterInstallation(
                runtime, playResult, out _);
            valid &= runtime.TrapInstallations.Count == 1;
            valid &= runtime.Deck.Zones.TryMove(
                installation.SourceTrap.Ids.BattleCardId,
                CardZone.Graveyard,
                out _);
            valid &= runtime.TrapInstallations.PruneInactive() == 1;
            valid &= runtime.TrapInstallations.Count == 0;
            return valid;
        }

        private static bool ValidateC10Removal(CardData card)
        {
            BattleRuntimeState runtime = Start(card, 5, TestContentIds.C10);
            if (runtime == null ||
                !runtime.TryAddEnemy(
                    "ENEMY-A",
                    5,
                    10,
                    EnemyFieldPosition.Left,
                    out _) ||
                !BattleRuntimeCardPlayService.TryPlay(
                    runtime,
                    "BATTLE-RUNTIME-36A-C10",
                    out BattleRuntimeCardPlayResult playResult,
                    out _,
                    out _) ||
                !BattleRuntimeTrapEffectService.TryRegisterInstallation(
                    runtime,
                    playResult,
                    out BattleRuntimeTrapInstallation installation))
            {
                return false;
            }

            int firstPlayerTurnEventIndex = runtime.EventLog.Events.Count;
            if (!BattleRuntimeTurnEffectService.TryEndPlayerTurn(
                    runtime,
                    firstPlayerTurnEventIndex,
                    out _,
                    out _))
            {
                return false;
            }

            BattleEventRecord abilityEvent = runtime.EventLog.Record(
                BattleEventType.CardPlayed,
                "EnemyAbility",
                "ENEMY-A",
                "ENEMY-A",
                "PLAYER");
            EnemyAbilityResolutionContext ability = new(
                "ABILITY-A", "ENEMY-A", false, true, true);
            bool resolved = BattleRuntimeTrapEffectService.TryCancelEnemyAbility(
                runtime,
                installation,
                abilityEvent,
                ability,
                out bool cancelled,
                out bool returnedToHand);

            return resolved && cancelled && returnedToHand &&
                   installation.SourceTrap.Zone == CardZone.Hand &&
                   runtime.TrapInstallations.Count == 0 &&
                   runtime.TrapInstallations.Find(
                       installation.SourceTrap.Ids.BattleCardId) == null;
        }

        private static BattleRuntimeState Start(
            CardData card,
            int level,
            string suffix)
        {
            BattleCardInstance instance = new(
                card,
                new CardInstanceIds(
                    card.CatalogCardId,
                    $"OWNED-RUNTIME-36A-{suffix}",
                    $"BATTLE-RUNTIME-36A-{suffix}"),
                level,
                CardZone.DrawPile);
            BattleRuntimeState runtime = new(
                new[] { instance }, 36, 10);
            return runtime.Turn.TryBeginBattle(out _) &&
                   runtime.Turn.TryConfirmStartingHand(
                       Array.Empty<string>(), out _, out _, out _)
                ? runtime
                : null;
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
