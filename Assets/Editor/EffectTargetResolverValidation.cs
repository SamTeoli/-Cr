using System;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class EffectTargetResolverValidation
    {
        [MenuItem("Have a Break/Validate Effect Target Resolver")]
        private static void ValidateFromMenu()
        {
            Debug.Log(Validate()
                ? "Effect target resolver passed."
                : "Effect target resolver failed.");
        }

        public static void ValidateFromCommandLine()
        {
            if (!Validate())
            {
                throw new InvalidOperationException(
                    "Effect target and text validation failed.");
            }

            Debug.Log("Effect target and text validation passed.");
        }

        internal static bool Validate()
        {
            CardDatabase database =
                AssetDatabase.LoadAssetAtPath<CardDatabase>(
                    "Assets/GameData/CardDatabase.asset");
            CardData c01 = database?.Cards.FirstOrDefault(card =>
                card?.CatalogCardId == TestContentIds.C01);
            CardData c05 = database?.Cards.FirstOrDefault(card =>
                card?.CatalogCardId == TestContentIds.C05);
            if (c01 == null || c05 == null)
            {
                return false;
            }

            BattleCardInstance card = new(
                c01,
                new CardInstanceIds(
                    c01.CatalogCardId,
                    "OWNED-TARGET-RESOLVER",
                    "BATTLE-TARGET-RESOLVER"),
                1,
                CardZone.Hand);
            BattleCardInstance handTarget = new(
                c01,
                new CardInstanceIds(
                    c01.CatalogCardId,
                    "OWNED-TARGET-RESOLVER-HAND",
                    "BATTLE-TARGET-RESOLVER-HAND"),
                1,
                CardZone.Hand);
            BattleCardInstance healthyAlly = new(
                c01,
                new CardInstanceIds(
                    c01.CatalogCardId,
                    "OWNED-TARGET-RESOLVER-ALLY-A",
                    "BATTLE-TARGET-RESOLVER-ALLY-A"),
                1,
                CardZone.MonsterField);
            BattleCardInstance woundedAlly = new(
                c01,
                new CardInstanceIds(
                    c01.CatalogCardId,
                    "OWNED-TARGET-RESOLVER-ALLY-B",
                    "BATTLE-TARGET-RESOLVER-ALLY-B"),
                1,
                CardZone.MonsterField);
            BattleRuntimeState runtime = new(
                new[] { card, handTarget, healthyAlly, woundedAlly },
                915,
                20);
            if (!runtime.Deck.Zones.TryMove(
                    card.Ids.BattleCardId,
                    CardZone.Hand,
                    out _) ||
                !runtime.Deck.Zones.TryMove(
                    handTarget.Ids.BattleCardId,
                    CardZone.Hand,
                    out _) ||
                !runtime.Deck.Zones.TryMove(
                    healthyAlly.Ids.BattleCardId,
                    CardZone.MonsterField,
                    out _) ||
                !runtime.TryRegisterFieldMonster(
                    healthyAlly.Ids.BattleCardId,
                    out _) ||
                !runtime.Deck.Zones.TryMove(
                    woundedAlly.Ids.BattleCardId,
                    CardZone.MonsterField,
                    out _) ||
                !runtime.TryRegisterFieldMonster(
                    woundedAlly.Ids.BattleCardId,
                    out BattleMonsterState woundedMonster) ||
                !runtime.TryAddEnemy(
                    "ENEMY-TARGET-A",
                    1,
                    10,
                    EnemyFieldPosition.Center,
                    out _) ||
                !runtime.TryAddEnemy(
                    "ENEMY-TARGET-B",
                    1,
                    10,
                    EnemyFieldPosition.Right,
                    out _))
            {
                return false;
            }
            woundedMonster.ApplyDamage(1);

            EffectTargetSpec spec =
                BuiltInEffectTargetSpecs
                    .EnemyMonsterSingleAfterPlacement;
            EffectTargetSpec activationSpec =
                BuiltInEffectTargetSpecs
                    .EnemyMonsterSingleOnActivation;
            EffectTargetSpec handSpec =
                BuiltInEffectTargetSpecs.HandCardSingleOnActivation;
            EffectTargetSpec multiEnemySpec =
                BuiltInEffectTargetSpecs
                    .EnemyMonsterOneOrTwoOnActivation;
            EffectTargetSpec enemyResolutionSpec =
                BuiltInEffectTargetSpecs
                    .EnemyMonsterSingleOnResolution;
            EffectTargetSpec allyResolutionSpec =
                BuiltInEffectTargetSpecs
                    .AllyMonsterSingleOnResolution;
            var legal = EffectTargetResolver.GetLegalTargets(
                runtime,
                spec);
            var legalHand = EffectTargetResolver.GetLegalTargets(
                runtime,
                handSpec,
                card.Ids.BattleCardId);
            PendingEffectActivation pending = new(
                card.Ids.BattleCardId,
                c01.CatalogCardId,
                spec);
            PendingEffectActivation multiPending = new(
                card.Ids.BattleCardId,
                "VALIDATION-MULTI-TARGET",
                multiEnemySpec);
            bool registrationsValid =
                HasTargetSpec(
                    TestContentIds.C01,
                    spec) &&
                HasTargetSpec(
                    TestContentIds.C05,
                    activationSpec) &&
                HasTargetSpec(
                    TestContentIds.C06,
                    activationSpec) &&
                HasTargetSpec(
                    TestContentIds.C07,
                    handSpec) &&
                HasTargetSpec(
                    TestContentIds.C08,
                    enemyResolutionSpec) &&
                HasTargetSpec(
                    TestContentIds.C09,
                    allyResolutionSpec) &&
                HasTargetSpec(
                    TestContentIds.C10,
                    enemyResolutionSpec) &&
                HasTargetSpec(
                    TestContentIds.C11,
                    allyResolutionSpec) &&
                HasTargetSpec(
                    TestContentIds.C12,
                    enemyResolutionSpec);
            bool assetsValid =
                IsTargetAsset(
                    spec,
                    "EnemyMonsterSingleAfterPlacement") &&
                IsTargetAsset(
                    activationSpec,
                    "EnemyMonsterSingleOnActivation") &&
                IsTargetAsset(
                    multiEnemySpec,
                    "EnemyMonsterOneOrTwoOnActivation") &&
                IsTargetAsset(
                    handSpec,
                    "HandCardSingleOnActivation") &&
                IsTargetAsset(
                    enemyResolutionSpec,
                    "EnemyMonsterSingleOnResolution") &&
                IsTargetAsset(
                    allyResolutionSpec,
                    "AllyMonsterSingleOnResolution");
            CardEffectData weakenEffect = new(
                "TEXT-WEAKEN",
                EffectTrigger.OnUse,
                EffectOperation.ApplyStatus,
                1,
                activationSpec,
                StatusKeyword.Weaken);
            CardEffectData multiDamageEffect = new(
                "TEXT-MULTI-DAMAGE",
                EffectTrigger.OnUse,
                EffectOperation.Damage,
                3,
                multiEnemySpec);
            CardEffectData drawEffect = new(
                "TEXT-DRAW",
                EffectTrigger.TurnStart,
                EffectOperation.Draw,
                2);
            CardEffectData moveEffect = new(
                "TEXT-MOVE",
                EffectTrigger.OnUse,
                EffectOperation.Move,
                2,
                activationSpec,
                fallbackDescription:
                    "{target:object} 오른쪽으로 {value}칸 이동시킨다.");
            bool effectTextValid =
                CardEffectTextFormatter.Format(
                    weakenEffect) ==
                "사용할 때, 적 1개에게 약화 1을 부여한다." &&
                CardEffectTextFormatter.Format(
                    multiDamageEffect) ==
                "사용할 때, 적 1~2개에게 3 피해를 준다." &&
                CardEffectTextFormatter.Format(drawEffect) ==
                "턴 시작 시, 카드를 2장 뽑는다." &&
                CardEffectTextFormatter.Format(moveEffect) ==
                "사용할 때, 적 1개를 오른쪽으로 2칸 이동시킨다." &&
                CardEffectTextFormatter.BuildCardRulesText(c05) ==
                c05.RulesText;
            return spec.Timing ==
                       EffectTargetTiming.AfterPlacement &&
                   spec.Side == EffectTargetSide.Enemy &&
                   spec.Kind == EffectTargetKind.Monster &&
                   spec.MinimumCount == 1 &&
                   spec.MaximumCount == 1 &&
                   activationSpec.Timing ==
                       EffectTargetTiming.OnActivation &&
                   activationSpec.Side == spec.Side &&
                   activationSpec.Kind == spec.Kind &&
                   handSpec.Timing ==
                       EffectTargetTiming.OnActivation &&
                   handSpec.Side == EffectTargetSide.Self &&
                   handSpec.Kind == EffectTargetKind.HandCard &&
                   multiEnemySpec.MinimumCount == 1 &&
                   multiEnemySpec.MaximumCount == 2 &&
                   EffectTargetResolver.TryResolveTargets(
                       runtime,
                       multiEnemySpec,
                       new[]
                       {
                           "ENEMY-TARGET-A",
                           "ENEMY-TARGET-B"
                       },
                       out var resolvedMulti) &&
                   resolvedMulti.Count == 2 &&
                   !EffectTargetResolver.TryResolveTargets(
                       runtime,
                       multiEnemySpec,
                       new[]
                       {
                           "ENEMY-TARGET-A",
                           "enemy-target-a"
                       },
                       out _) &&
                   multiPending.TryAddTarget(
                       runtime,
                       "ENEMY-TARGET-A",
                       out _) &&
                   !multiPending.TryAddTarget(
                       runtime,
                       "enemy-target-a",
                       out _) &&
                   multiPending.TryAddTarget(
                       runtime,
                       "ENEMY-TARGET-B",
                       out _) &&
                   multiPending.SelectedTargetIds.Count == 2 &&
                   multiPending.TryDeclare(runtime) &&
                   multiPending.Phase ==
                       PendingEffectActivationPhase.Declared &&
                   enemyResolutionSpec.Timing ==
                       EffectTargetTiming.OnResolution &&
                   enemyResolutionSpec.Side ==
                       EffectTargetSide.Enemy &&
                   enemyResolutionSpec.Kind ==
                       EffectTargetKind.Monster &&
                   allyResolutionSpec.Timing ==
                       EffectTargetTiming.OnResolution &&
                   allyResolutionSpec.Side ==
                       EffectTargetSide.Ally &&
                   allyResolutionSpec.Kind ==
                       EffectTargetKind.Monster &&
                   EffectTargetSelectionPolicy
                       .TrySelectLowestHealthAllyMonster(
                           runtime,
                           allyResolutionSpec,
                           out EffectTargetCandidate lowestHealthAlly) &&
                   lowestHealthAlly.TargetId ==
                       woundedAlly.Ids.BattleCardId &&
                   legal.Count == 2 &&
                   legal.All(candidate =>
                       candidate.Side == EffectTargetSide.Enemy &&
                       candidate.Kind == EffectTargetKind.Monster) &&
                   legalHand.Count == 1 &&
                   legalHand[0].TargetId ==
                       handTarget.Ids.BattleCardId &&
                   EffectTargetResolver.TryResolveSingleTarget(
                       runtime,
                       handSpec,
                       handTarget.Ids.BattleCardId,
                       card.Ids.BattleCardId,
                       out EffectTargetCandidate selectedHand) &&
                   selectedHand.Kind == EffectTargetKind.HandCard &&
                   !EffectTargetResolver.TryResolveSingleTarget(
                       runtime,
                       handSpec,
                       card.Ids.BattleCardId,
                       card.Ids.BattleCardId,
                       out _) &&
                   EffectTargetResolver.TryResolveSingleTarget(
                       runtime,
                       spec,
                       "enemy-target-a",
                       out EffectTargetCandidate selected) &&
                   selected.TargetId == "ENEMY-TARGET-A" &&
                   pending.IsAwaitingTarget &&
                   pending.TrySelectSingleTarget(
                       runtime,
                       "ENEMY-TARGET-B",
                       out EffectTargetCandidate pendingTarget) &&
                   pendingTarget.TargetId == "ENEMY-TARGET-B" &&
                   pending.SingleTargetId == "ENEMY-TARGET-B" &&
                   pending.TryDeclare(runtime) &&
                   pending.Phase ==
                       PendingEffectActivationPhase.Declared &&
                   registrationsValid &&
                   assetsValid &&
                   effectTextValid &&
                   !pending.IsAwaitingTarget &&
                   !pending.TrySelectSingleTarget(
                       runtime,
                       "ENEMY-TARGET-A",
                       out _) &&
                   !EffectTargetResolver.TryResolveSingleTarget(
                       runtime,
                       spec,
                       "MISSING-ENEMY",
                       out _);
        }

        private static bool HasTargetSpec(
            string catalogCardId,
            EffectTargetSpec expected)
        {
            CardData card = AssetDatabase.FindAssets("t:CardData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<CardData>)
                .FirstOrDefault(candidate =>
                    candidate?.CatalogCardId == catalogCardId);
            return card != null &&
                   card.EffectTargetSpec == expected &&
                   CardEffectRegistrationCatalog.TryFind(
                       catalogCardId,
                       out CardEffectRegistration registration) &&
                   registration.TargetSpec == expected &&
                   registration.ResolveTargetSpec(card) == expected;
        }

        private static bool IsTargetAsset(
            EffectTargetSpec actual,
            string assetName)
        {
            return actual != null &&
                   AssetDatabase.LoadAssetAtPath<EffectTargetSpec>(
                       $"Assets/Resources/GameData/EffectTargets/" +
                       $"{assetName}.asset") == actual;
        }
    }
}
