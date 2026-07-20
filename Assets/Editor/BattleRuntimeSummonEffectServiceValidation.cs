using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleRuntimeSummonEffectServiceValidation
    {
        [MenuItem("Have a Break/Validate Runtime C01 C02 Summon Effects")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            EditorUtility.DisplayDialog(
                "Runtime C01 C02 Summon Effects Validation",
                valid
                    ? "Runtime C01 and C02 summon effects passed."
                    : "Runtime C01 and C02 summon effects failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            CardData c01Data = FindCard("C01");
            CardData c02Data = FindCard("C02");
            if (c01Data == null || c02Data == null)
            {
                Debug.LogError("Runtime summon validation requires C01 and C02.");
                return false;
            }

            BattleCardInstance c01 = CreateCard(c01Data, "C01");
            BattleCardInstance c02 = CreateCard(c02Data, "C02");
            BattleRuntimeState runtime = new(
                new List<BattleCardInstance> { c01, c02 }, 352);
            bool valid = runtime.TryAddEnemy(
                "ENEMY-A", 3, 10, EnemyFieldPosition.Center, out _);
            valid &= runtime.Turn.TryBeginBattle(out _);
            valid &= runtime.Turn.TryConfirmStartingHand(
                Array.Empty<string>(), out _, out _, out _);

            valid &= EnchantFixedTargetResolver.TryDeclare(
                c01.Ids.BattleCardId,
                "ENEMY-A",
                runtime.EnemyPositions,
                runtime.Enchants,
                out EnchantFixedTargetDeclaration c01Target);
            valid &= BattleRuntimeCardPlayService.TryPlay(
                runtime,
                c01.Ids.BattleCardId,
                out BattleRuntimeCardPlayResult c01Play,
                out _,
                out _);
            valid &= BattleRuntimeSummonEffectService.TryResolve(
                runtime,
                c01Play,
                c01Target,
                out BattleRuntimeSummonEffectResult c01Effect,
                out BattleRuntimeSummonEffectFailure c01Failure);
            valid &= c01Failure == BattleRuntimeSummonEffectFailure.None &&
                     c01Effect.CatalogCardId == "C01" &&
                     c01Effect.C01Result.MovementSucceeded &&
                     c01Effect.C01Result.ResolvedTargetEnemyId == "ENEMY-A" &&
                     runtime.EnemyPositions.FindPosition("ENEMY-A") ==
                     EnemyFieldPosition.Left;
            valid &= !BattleRuntimeSummonEffectService.TryResolve(
                runtime, c01Play, c01Target, out _, out _);

            valid &= BattleRuntimeCardPlayService.TryPlay(
                runtime,
                c02.Ids.BattleCardId,
                out BattleRuntimeCardPlayResult c02Play,
                out _,
                out _);
            valid &= BattleRuntimeSummonEffectService.TryResolve(
                runtime,
                c02Play,
                null,
                out BattleRuntimeSummonEffectResult c02Effect,
                out BattleRuntimeSummonEffectFailure c02Failure);
            valid &= c02Failure == BattleRuntimeSummonEffectFailure.None &&
                     c02Effect.CatalogCardId == "C02" &&
                     c02Effect.C02Result.CostReduction == 1 &&
                     runtime.NextSkillModifiers.PendingCount == 1 &&
                     runtime.CardPlay.Mana.CurrentMana == 0 &&
                     runtime.Turn.Phase == BattleTurnPhase.PlayerAction;

            if (valid)
            {
                Debug.Log("Runtime C01 and C02 summon effects passed.");
            }
            else
            {
                Debug.LogError("Runtime C01 and C02 summon effects failed.");
            }

            return valid;
        }

        private static BattleCardInstance CreateCard(CardData card, string suffix)
        {
            return new BattleCardInstance(
                card,
                new CardInstanceIds(
                    card.CatalogCardId,
                    $"OWNED-RUNTIME-{suffix}",
                    $"BATTLE-RUNTIME-{suffix}"),
                1,
                CardZone.DrawPile);
        }

        private static CardData FindCard(string id)
        {
            return AssetDatabase.FindAssets("t:CardData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<CardData>(path))
                .FirstOrDefault(card => card != null &&
                    string.Equals(card.CatalogCardId, id, StringComparison.OrdinalIgnoreCase));
        }
    }
}
