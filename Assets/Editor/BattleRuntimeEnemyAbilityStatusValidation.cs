using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleRuntimeEnemyAbilityStatusValidation
    {
        [MenuItem("Have a Break/Validate Enemy Status Ability Effects")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            if (valid)
            {
                Debug.Log("Enemy status ability effect flow passed.");
            }
            else
            {
                Debug.LogError("Enemy status ability effect flow failed.");
            }

            EditorUtility.DisplayDialog(
                "Enemy Status Ability Effect Validation",
                valid
                    ? "Enemy status ability effect flow passed."
                    : "Enemy status ability effect flow failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            CardData c01 = FindCard(TestContentIds.C01);
            return c01 != null &&
                   ValidateFriendlyTargeting(c01) &&
                   ValidatePlayerFallback(c01) &&
                   ValidateEnemySideTargeting(c01) &&
                   ValidateInvalidEffects(c01);
        }

        private static bool ValidateFriendlyTargeting(CardData card)
        {
            BattleRuntimeState runtime = CreateRuntime(
                card,
                "FRIENDLY",
                true,
                out BattleMonsterState center,
                out BattleMonsterState right);
            if (runtime == null || center == null || right == null)
            {
                return false;
            }

            EnemyAbilityResolutionContext single = new(
                "ABILITY-BIND-SINGLE",
                "ABILITY-SOURCE-FRIENDLY",
                false,
                true,
                false,
                StatusKeyword.Bind,
                2,
                0);
            if (!BattleRuntimeEnemyAbilityService.TryResolve(
                    runtime,
                    single,
                    out BattleRuntimeEnemyAbilityResult singleResult,
                    out BattleRuntimeEnemyAbilityFailure singleFailure) ||
                singleFailure != BattleRuntimeEnemyAbilityFailure.None ||
                singleResult == null || singleResult.Cancelled ||
                singleResult.TotalStatusApplied != 2 ||
                singleResult.AffectedTargetCount != 1 ||
                singleResult.StatusApplicationEvents[0].TargetId !=
                center.BattleCardId ||
                center.Status.Bind != 2 || right.Status.Bind != 0 ||
                runtime.Player.Status.Bind != 0)
            {
                return false;
            }

            EnemyAbilityResolutionContext area = new(
                "ABILITY-WEAKEN-AREA",
                "ABILITY-SOURCE-FRIENDLY",
                false,
                true,
                true,
                StatusKeyword.Weaken,
                1);
            bool resolved = BattleRuntimeEnemyAbilityService.TryResolve(
                runtime,
                area,
                out BattleRuntimeEnemyAbilityResult areaResult,
                out BattleRuntimeEnemyAbilityFailure areaFailure);

            return resolved &&
                   areaFailure == BattleRuntimeEnemyAbilityFailure.None &&
                   areaResult != null && !areaResult.Cancelled &&
                   areaResult.TotalStatusApplied == 2 &&
                   areaResult.AffectedTargetCount == 2 &&
                   center.Status.Weaken == 1 &&
                   right.Status.Weaken == 1 &&
                   runtime.Player.Status.Weaken == 0;
        }

        private static bool ValidatePlayerFallback(CardData card)
        {
            BattleRuntimeState runtime = CreateRuntime(
                card,
                "PLAYER",
                false,
                out _,
                out _);
            if (runtime == null)
            {
                return false;
            }

            EnemyAbilityResolutionContext ability = new(
                "ABILITY-INJURY-PLAYER",
                "ABILITY-SOURCE-PLAYER",
                false,
                true,
                false,
                StatusKeyword.Injury,
                2,
                17);
            bool resolved = BattleRuntimeEnemyAbilityService.TryResolve(
                runtime,
                ability,
                out BattleRuntimeEnemyAbilityResult result,
                out BattleRuntimeEnemyAbilityFailure failure);

            return resolved &&
                   failure == BattleRuntimeEnemyAbilityFailure.None &&
                   result != null && !result.Cancelled &&
                   result.TotalStatusApplied == 2 &&
                   result.AffectedTargetCount == 1 &&
                   result.StatusApplicationEvents[0].TargetId ==
                   BattlePlayerState.PlayerTargetId &&
                   runtime.Player.Status.Injury == 2;
        }

        private static bool ValidateEnemySideTargeting(CardData card)
        {
            BattleRuntimeState runtime = CreateRuntime(
                card,
                "ENEMY",
                false,
                out _,
                out _);
            BattleEnemyStatusState source = runtime?.EnemyStatuses.Find(
                "ABILITY-SOURCE-ENEMY");
            BattleEnemyStatusState secondary = runtime?.EnemyStatuses.Find(
                "ABILITY-SECONDARY-ENEMY");
            if (runtime == null || source == null || secondary == null)
            {
                return false;
            }

            EnemyAbilityResolutionContext single = new(
                "ABILITY-VULNERABLE-SELF",
                "ABILITY-SOURCE-ENEMY",
                false,
                false,
                false,
                StatusKeyword.Vulnerable,
                2);
            if (!BattleRuntimeEnemyAbilityService.TryResolve(
                    runtime,
                    single,
                    out BattleRuntimeEnemyAbilityResult singleResult,
                    out _) ||
                singleResult.TotalStatusApplied != 2 ||
                singleResult.AffectedTargetCount != 1 ||
                source.Vulnerable != 2 || secondary.Vulnerable != 0)
            {
                return false;
            }

            EnemyAbilityResolutionContext area = new(
                "ABILITY-STUN-ENEMY-AREA",
                "ABILITY-SOURCE-ENEMY",
                false,
                false,
                true,
                StatusKeyword.Stun,
                1);
            bool resolved = BattleRuntimeEnemyAbilityService.TryResolve(
                runtime,
                area,
                out BattleRuntimeEnemyAbilityResult areaResult,
                out BattleRuntimeEnemyAbilityFailure areaFailure);

            return resolved &&
                   areaFailure == BattleRuntimeEnemyAbilityFailure.None &&
                   areaResult.TotalStatusApplied == 2 &&
                   areaResult.AffectedTargetCount == 2 &&
                   source.Stun == 1 && secondary.Stun == 1;
        }

        private static bool ValidateInvalidEffects(CardData card)
        {
            BattleRuntimeState runtime = CreateRuntime(
                card,
                "INVALID",
                false,
                out _,
                out _);
            if (runtime == null)
            {
                return false;
            }

            int eventCount = runtime.EventLog.Events.Count;
            bool missingAmountRejected =
                !BattleRuntimeEnemyAbilityService.TryResolve(
                    runtime,
                    new EnemyAbilityResolutionContext(
                        "ABILITY-INVALID-AMOUNT",
                        "ABILITY-SOURCE-INVALID",
                        false,
                        true,
                        false,
                        StatusKeyword.Bind,
                        0),
                    out _,
                    out BattleRuntimeEnemyAbilityFailure amountFailure) &&
                amountFailure ==
                BattleRuntimeEnemyAbilityFailure.InvalidAbility;
            bool missingKeywordRejected =
                !BattleRuntimeEnemyAbilityService.TryResolve(
                    runtime,
                    new EnemyAbilityResolutionContext(
                        "ABILITY-INVALID-KEYWORD",
                        "ABILITY-SOURCE-INVALID",
                        false,
                        true,
                        false,
                        StatusKeyword.None,
                        1),
                    out _,
                    out BattleRuntimeEnemyAbilityFailure keywordFailure) &&
                keywordFailure ==
                BattleRuntimeEnemyAbilityFailure.InvalidAbility;

            return missingAmountRejected && missingKeywordRejected &&
                   runtime.EventLog.Events.Count == eventCount;
        }

        private static BattleRuntimeState CreateRuntime(
            CardData card,
            string suffix,
            bool deployMonsters,
            out BattleMonsterState center,
            out BattleMonsterState right)
        {
            center = null;
            right = null;
            BattleCardInstance first = Instance(card, $"{suffix}-A");
            BattleCardInstance second = Instance(card, $"{suffix}-B");
            BattleRuntimeState runtime = new(
                new[] { first, second },
                4100 + suffix.Length,
                5);
            string sourceEnemyId = $"ABILITY-SOURCE-{suffix}";
            string secondaryEnemyId = $"ABILITY-SECONDARY-{suffix}";
            if (!runtime.TryAddEnemy(
                    sourceEnemyId,
                    1,
                    10,
                    EnemyFieldPosition.Left,
                    out _) ||
                !runtime.TryAddEnemy(
                    secondaryEnemyId,
                    1,
                    10,
                    EnemyFieldPosition.Right,
                    out _) ||
                !runtime.Turn.TryBeginBattle(out _) ||
                !runtime.Turn.TryConfirmStartingHand(
                    Array.Empty<string>(), out _, out _, out _))
            {
                return null;
            }

            if (deployMonsters &&
                (!runtime.Deck.Zones.TryMove(
                     first.Ids.BattleCardId,
                     CardZone.MonsterField,
                     out _) ||
                 !runtime.TryRegisterFieldMonster(
                     first.Ids.BattleCardId,
                     PlayerMonsterFieldPosition.Center,
                     out center) ||
                 !runtime.Deck.Zones.TryMove(
                     second.Ids.BattleCardId,
                     CardZone.MonsterField,
                     out _) ||
                 !runtime.TryRegisterFieldMonster(
                     second.Ids.BattleCardId,
                     PlayerMonsterFieldPosition.Right,
                     out right)))
            {
                return null;
            }

            int firstPlayerTurnEventIndex = runtime.EventLog.Events.Count;
            return BattleRuntimeTurnEffectService.TryEndPlayerTurn(
                       runtime,
                       firstPlayerTurnEventIndex,
                       out _,
                       out _) &&
                   runtime.Turn.Phase == BattleTurnPhase.EnemyTurn
                ? runtime
                : null;
        }

        private static BattleCardInstance Instance(
            CardData card,
            string suffix)
        {
            return new BattleCardInstance(
                card,
                new CardInstanceIds(
                    card.CatalogCardId,
                    $"OWNED-ABILITY-STATUS-{suffix}",
                    $"BATTLE-ABILITY-STATUS-{suffix}"),
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
