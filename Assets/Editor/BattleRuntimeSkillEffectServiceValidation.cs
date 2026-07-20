using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleRuntimeSkillEffectServiceValidation
    {
        [MenuItem("Have a Break/Validate Runtime C05 C06 Skill Effects")]
        private static void ValidateFromMenu()
        {
            bool valid = ValidateC05() && ValidateC06();
            if (valid)
            {
                Debug.Log("Battle runtime C05 and C06 skill effects passed.");
            }
            else
            {
                Debug.LogError("Battle runtime C05 and C06 skill effects failed.");
            }

            EditorUtility.DisplayDialog(
                "Battle Runtime Skill Effects Validation",
                valid
                    ? "Battle runtime C05 and C06 skill effects passed."
                    : "Battle runtime C05 and C06 skill effects failed. Check the Console.",
                "OK");
        }

        private static bool ValidateC05()
        {
            CardData card = FindCard("C05");
            if (!TryCreateStartedRuntime(
                    card, 1, "C05", out BattleRuntimeState runtime,
                    out BattleCardInstance instance))
            {
                return false;
            }

            if (!BattleRuntimeCardPlayService.TryPlay(
                    runtime,
                    instance.Ids.BattleCardId,
                    out BattleRuntimeCardPlayResult playResult,
                    out _,
                    out _))
            {
                return false;
            }

            bool valid = BattleRuntimeSkillEffectService.TryResolve(
                runtime,
                playResult,
                "ENEMY-A",
                out BattleRuntimeSkillEffectResult result,
                out BattleRuntimeSkillEffectFailure failure);
            BattleEnemyStatusState target = runtime.EnemyStatuses.Find("ENEMY-A");
            valid &= failure == BattleRuntimeSkillEffectFailure.None &&
                     result != null &&
                     result.CatalogCardId == "C05" &&
                     result.TargetEnemyId == "ENEMY-A" &&
                     result.MovedSteps == 1 &&
                     result.WeakenGained == 1 &&
                     result.VulnerableGained == 0 &&
                     target != null &&
                     target.Weaken == 1 &&
                     target.Vulnerable == 0 &&
                     runtime.EventLog.Events.Any(item =>
                         item.EventType == BattleEventType.EnemyMoved &&
                         item.ParentEventId == playResult.PlayedEvent.EventId);

            valid &= !BattleRuntimeSkillEffectService.TryResolve(
                         runtime, playResult, "ENEMY-A", out _,
                         out BattleRuntimeSkillEffectFailure duplicateFailure) &&
                     duplicateFailure == BattleRuntimeSkillEffectFailure.ResolutionFailed;
            return valid;
        }

        private static bool ValidateC06()
        {
            CardData card = FindCard("C06");
            if (!TryCreateStartedRuntime(
                    card, 5, "C06", out BattleRuntimeState runtime,
                    out BattleCardInstance instance))
            {
                return false;
            }

            if (!BattleRuntimeCardPlayService.TryPlay(
                    runtime,
                    instance.Ids.BattleCardId,
                    out BattleRuntimeCardPlayResult playResult,
                    out _,
                    out _))
            {
                return false;
            }

            bool valid = BattleRuntimeSkillEffectService.TryResolve(
                runtime,
                playResult,
                "ENEMY-A",
                out BattleRuntimeSkillEffectResult result,
                out BattleRuntimeSkillEffectFailure failure);
            BattleEnemyStatusState target = runtime.EnemyStatuses.Find("ENEMY-A");
            BattleEnemyStatusState secondary = runtime.EnemyStatuses.Find("ENEMY-B");
            valid &= failure == BattleRuntimeSkillEffectFailure.None &&
                     result != null &&
                     result.CatalogCardId == "C06" &&
                     result.TargetEnemyId == "ENEMY-A" &&
                     result.SecondaryEnemyId == "ENEMY-B" &&
                     target != null &&
                     target.Bind == 2 &&
                     target.Weaken == 1 &&
                     secondary != null &&
                     secondary.Weaken == 1;
            return valid;
        }

        private static bool TryCreateStartedRuntime(
            CardData card,
            int level,
            string suffix,
            out BattleRuntimeState runtime,
            out BattleCardInstance instance)
        {
            runtime = null;
            instance = null;
            if (card == null)
            {
                return false;
            }

            instance = new BattleCardInstance(
                card,
                new CardInstanceIds(
                    card.CatalogCardId,
                    $"OWNED-RUNTIME-35E-{suffix}",
                    $"BATTLE-RUNTIME-35E-{suffix}"),
                level,
                CardZone.DrawPile);
            runtime = new BattleRuntimeState(new[] { instance }, 35, 20);
            bool valid = runtime.TryAddEnemy(
                             "ENEMY-A", 3, 10, EnemyFieldPosition.Left, out _) &&
                         runtime.TryAddEnemy(
                             "ENEMY-B", 9, 10, EnemyFieldPosition.Center, out _) &&
                         runtime.TryAddEnemy(
                             "ENEMY-C", 5, 10, EnemyFieldPosition.Right, out _) &&
                         runtime.Turn.TryBeginBattle(out _) &&
                         runtime.Turn.TryConfirmStartingHand(
                             Array.Empty<string>(), out _, out _, out _);
            return valid && instance.Zone == CardZone.Hand;
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
