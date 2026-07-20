using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleRuntimeEnemyAbilityC10Validation
    {
        [MenuItem("Have a Break/Validate Runtime Enemy Ability And C10")]
        private static void ValidateFromMenu()
        {
            CardData c10 = FindCard("C10");
            bool valid = c10 != null &&
                         ValidateLevelFiveSingleTarget(c10) &&
                         ValidateLevelThreeAreaRestriction(c10) &&
                         ValidateLevelFourAreaCancellation(c10);

            if (valid)
            {
                Debug.Log("Battle runtime enemy ability and C10 passed.");
            }
            else
            {
                Debug.LogError("Battle runtime enemy ability and C10 failed.");
            }

            EditorUtility.DisplayDialog(
                "Battle Runtime Enemy Ability And C10 Validation",
                valid
                    ? "Battle runtime enemy ability and C10 passed."
                    : "Battle runtime enemy ability and C10 failed. Check the Console.",
                "OK");
        }

        private static bool ValidateLevelFiveSingleTarget(CardData c10)
        {
            if (!TryPrepare(
                    c10,
                    5,
                    "LEVEL5",
                    out BattleRuntimeState runtime,
                    out BattleCardInstance trap))
            {
                return false;
            }

            EnemyAbilityResolutionContext ability = new(
                "ABILITY-SINGLE",
                "ENEMY-C10-LEVEL5",
                false,
                true,
                false);
            bool resolved = BattleRuntimeEnemyAbilityService.TryResolve(
                runtime,
                ability,
                out BattleRuntimeEnemyAbilityResult result,
                out BattleRuntimeEnemyAbilityFailure failure);
            BattleEnemyStatusState status =
                runtime.EnemyStatuses.Find("ENEMY-C10-LEVEL5");

            return resolved &&
                   failure == BattleRuntimeEnemyAbilityFailure.None &&
                   result != null &&
                   result.Cancelled &&
                   result.ReturnedTrapToHand &&
                   result.TriggeredTrapBattleCardId ==
                   trap.Ids.BattleCardId &&
                   result.DeclaredEvent.EventType ==
                   BattleEventType.EnemyAbilityDeclared &&
                   result.ResolutionEvent.EventType ==
                   BattleEventType.EnemyAbilityCancelled &&
                   result.ResolutionEvent.ParentEventId ==
                   result.DeclaredEvent.EventId &&
                   trap.Zone == CardZone.Hand &&
                   status != null && status.Weaken == 1 &&
                   runtime.TrapInstallations.Count == 0;
        }

        private static bool ValidateLevelThreeAreaRestriction(CardData c10)
        {
            if (!TryPrepare(
                    c10,
                    3,
                    "LEVEL3-AREA",
                    out BattleRuntimeState runtime,
                    out BattleCardInstance trap))
            {
                return false;
            }

            EnemyAbilityResolutionContext ability = new(
                "ABILITY-AREA-LEVEL3",
                "ENEMY-C10-LEVEL3-AREA",
                false,
                true,
                true);
            bool resolved = BattleRuntimeEnemyAbilityService.TryResolve(
                runtime,
                ability,
                out BattleRuntimeEnemyAbilityResult result,
                out BattleRuntimeEnemyAbilityFailure failure);
            BattleEnemyStatusState status =
                runtime.EnemyStatuses.Find("ENEMY-C10-LEVEL3-AREA");

            return resolved &&
                   failure == BattleRuntimeEnemyAbilityFailure.None &&
                   result != null &&
                   !result.Cancelled &&
                   !result.ReturnedTrapToHand &&
                   string.IsNullOrWhiteSpace(
                       result.TriggeredTrapBattleCardId) &&
                   result.ResolutionEvent.EventType ==
                   BattleEventType.EnemyAbilityCompleted &&
                   trap.Zone == CardZone.SkillField &&
                   status != null && status.Weaken == 0 &&
                   runtime.TrapInstallations.Count == 1;
        }

        private static bool ValidateLevelFourAreaCancellation(CardData c10)
        {
            if (!TryPrepare(
                    c10,
                    4,
                    "LEVEL4-AREA",
                    out BattleRuntimeState runtime,
                    out BattleCardInstance trap))
            {
                return false;
            }

            EnemyAbilityResolutionContext ability = new(
                "ABILITY-AREA-LEVEL4",
                "ENEMY-C10-LEVEL4-AREA",
                false,
                true,
                true);
            bool resolved = BattleRuntimeEnemyAbilityService.TryResolve(
                runtime,
                ability,
                out BattleRuntimeEnemyAbilityResult result,
                out BattleRuntimeEnemyAbilityFailure failure);
            BattleEnemyStatusState status =
                runtime.EnemyStatuses.Find("ENEMY-C10-LEVEL4-AREA");

            return resolved &&
                   failure == BattleRuntimeEnemyAbilityFailure.None &&
                   result != null &&
                   result.Cancelled &&
                   !result.ReturnedTrapToHand &&
                   result.TriggeredTrapBattleCardId ==
                   trap.Ids.BattleCardId &&
                   result.ResolutionEvent.EventType ==
                   BattleEventType.EnemyAbilityCancelled &&
                   trap.Zone == CardZone.Graveyard &&
                   status != null && status.Weaken == 1 &&
                   runtime.TrapInstallations.Count == 0;
        }

        private static bool TryPrepare(
            CardData c10,
            int level,
            string suffix,
            out BattleRuntimeState runtime,
            out BattleCardInstance trap)
        {
            trap = Instance(c10, level, suffix);
            runtime = new BattleRuntimeState(new[] { trap }, 360 + level, 10);
            string enemyId = $"ENEMY-C10-{suffix}";
            if (!runtime.TryAddEnemy(
                    enemyId,
                    5,
                    10,
                    EnemyFieldPosition.Left,
                    out _) ||
                !runtime.Turn.TryBeginBattle(out _) ||
                !runtime.Turn.TryConfirmStartingHand(
                    Array.Empty<string>(), out _, out _, out _) ||
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
                !EndPlayerTurn(runtime))
            {
                return false;
            }

            return true;
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
                    $"OWNED-RUNTIME-36E-{suffix}",
                    $"BATTLE-RUNTIME-36E-{suffix}"),
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
