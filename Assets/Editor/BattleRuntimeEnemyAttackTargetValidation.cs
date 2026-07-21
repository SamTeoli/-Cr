using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleRuntimeEnemyAttackTargetValidation
    {
        [MenuItem("Have a Break/Validate Runtime Enemy Attack Targeting")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            if (valid)
            {
                Debug.Log("Battle runtime enemy attack targeting passed.");
            }
            else
            {
                Debug.LogError("Battle runtime enemy attack targeting failed.");
            }

            EditorUtility.DisplayDialog(
                "Battle Runtime Enemy Attack Targeting Validation",
                valid
                    ? "Battle runtime enemy attack targeting passed."
                    : "Battle runtime enemy attack targeting failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            CardData c01 = FindCard(TestContentIds.C01);
            return c01 != null &&
                   ValidateDefaultAndExplicitPlacement(c01) &&
                   ValidateFrontPriority(c01) &&
                   ValidateNearestAndTie(c01) &&
                   ValidateContinuousReselection(c01) &&
                   ValidatePlayerFallback();
        }

        private static bool ValidateDefaultAndExplicitPlacement(CardData card)
        {
            List<BattleCardInstance> cards = Instances(card, 3, "PLACE");
            BattleRuntimeState runtime = new(cards, 381);
            for (int i = 0; i < cards.Count; i++)
            {
                if (!runtime.Deck.Zones.TryMove(
                        cards[i].Ids.BattleCardId,
                        CardZone.MonsterField,
                        out _) ||
                    !runtime.TryRegisterFieldMonster(
                        cards[i].Ids.BattleCardId,
                        out _))
                {
                    return false;
                }
            }

            BattleCardInstance extra = Instance(card, "EXPLICIT");
            BattleRuntimeState explicitRuntime = new(new[] { extra }, 382);
            bool explicitPlaced = explicitRuntime.Deck.Zones.TryMove(
                                      extra.Ids.BattleCardId,
                                      CardZone.MonsterField,
                                      out _) &&
                                  explicitRuntime.TryRegisterFieldMonster(
                                      extra.Ids.BattleCardId,
                                      PlayerMonsterFieldPosition.Right,
                                      out _);

            return runtime.PlayerMonsterPositions.Count == 3 &&
                   runtime.PlayerMonsterPositions.GetOccupant(
                       PlayerMonsterFieldPosition.Left) ==
                   cards[0].Ids.BattleCardId &&
                   runtime.PlayerMonsterPositions.GetOccupant(
                       PlayerMonsterFieldPosition.Center) ==
                   cards[1].Ids.BattleCardId &&
                   runtime.PlayerMonsterPositions.GetOccupant(
                       PlayerMonsterFieldPosition.Right) ==
                   cards[2].Ids.BattleCardId &&
                   explicitPlaced &&
                   explicitRuntime.PlayerMonsterPositions.FindPosition(
                       extra.Ids.BattleCardId) ==
                   PlayerMonsterFieldPosition.Right;
        }

        private static bool ValidateFrontPriority(CardData card)
        {
            BattleRuntimeState runtime = CreateRuntime(
                card,
                "FRONT",
                PlayerMonsterFieldPosition.Left,
                PlayerMonsterFieldPosition.Center,
                PlayerMonsterFieldPosition.Right);
            if (runtime == null ||
                !runtime.TryAddEnemy(
                    "TARGET-LEFT", 1, 10,
                    EnemyFieldPosition.Left, out _) ||
                !runtime.TryAddEnemy(
                    "TARGET-CENTER", 1, 10,
                    EnemyFieldPosition.Center, out _) ||
                !runtime.TryAddEnemy(
                    "TARGET-RIGHT", 1, 10,
                    EnemyFieldPosition.Right, out _))
            {
                return false;
            }

            return Selects(
                       runtime, "TARGET-LEFT", 0,
                       PlayerMonsterFieldPosition.Left, false) &&
                   Selects(
                       runtime, "TARGET-CENTER", 0,
                       PlayerMonsterFieldPosition.Center, false) &&
                   Selects(
                       runtime, "TARGET-RIGHT", 0,
                       PlayerMonsterFieldPosition.Right, false);
        }

        private static bool ValidateNearestAndTie(CardData card)
        {
            BattleRuntimeState tieRuntime = CreateRuntime(
                card,
                "TIE",
                PlayerMonsterFieldPosition.Left,
                PlayerMonsterFieldPosition.Right);
            if (tieRuntime == null ||
                !tieRuntime.TryAddEnemy(
                    "TARGET-TIE", 1, 10,
                    EnemyFieldPosition.Center, out _))
            {
                return false;
            }

            BattleRuntimeState nearestRuntime = CreateRuntime(
                card,
                "NEAREST",
                PlayerMonsterFieldPosition.Center,
                PlayerMonsterFieldPosition.Right);
            if (nearestRuntime == null ||
                !nearestRuntime.TryAddEnemy(
                    "TARGET-NEAREST", 1, 10,
                    EnemyFieldPosition.Left, out _))
            {
                return false;
            }

            return Selects(
                       tieRuntime, "TARGET-TIE", 0,
                       PlayerMonsterFieldPosition.Left, true) &&
                   Selects(
                       tieRuntime, "TARGET-TIE", 1,
                       PlayerMonsterFieldPosition.Right, true) &&
                   Selects(
                       nearestRuntime, "TARGET-NEAREST", 0,
                       PlayerMonsterFieldPosition.Center, false);
        }

        private static bool ValidateContinuousReselection(CardData card)
        {
            BattleRuntimeState runtime = CreateRuntime(
                card,
                "RESELECT",
                PlayerMonsterFieldPosition.Center,
                PlayerMonsterFieldPosition.Right);
            if (runtime == null ||
                !runtime.TryAddEnemy(
                    "TARGET-RESELECT", 1, 10,
                    EnemyFieldPosition.Center, out _) ||
                !Selects(
                    runtime, "TARGET-RESELECT", 0,
                    PlayerMonsterFieldPosition.Center, false))
            {
                return false;
            }

            string centerId = runtime.PlayerMonsterPositions.GetOccupant(
                PlayerMonsterFieldPosition.Center);
            return runtime.PlayerMonsterPositions.TryRemove(centerId) &&
                   runtime.Monsters.TryRemove(centerId, out _) &&
                   runtime.Deck.Zones.TryMove(
                       centerId, CardZone.Graveyard, out _) &&
                   Selects(
                       runtime, "TARGET-RESELECT", 0,
                       PlayerMonsterFieldPosition.Right, false);
        }

        private static bool ValidatePlayerFallback()
        {
            BattleRuntimeState runtime = new(
                Array.Empty<BattleCardInstance>(), 383);
            if (!runtime.TryAddEnemy(
                    "TARGET-PLAYER", 1, 10,
                    EnemyFieldPosition.Center, out _) ||
                !BattleRuntimeEnemyAttackTargetService.TrySelect(
                    runtime,
                    "TARGET-PLAYER",
                    0,
                    out BattleRuntimeEnemyAttackTargetResult result,
                    out BattleRuntimeEnemyAttackTargetFailure failure))
            {
                return false;
            }

            return failure ==
                   BattleRuntimeEnemyAttackTargetFailure.None &&
                   result.TargetType ==
                   BattleRuntimeEnemyAttackTargetType.Player &&
                   result.TargetId == BattlePlayerState.PlayerTargetId &&
                   result.TargetMonster == null &&
                   !result.TargetPosition.HasValue &&
                   !result.UsedTieBreaker;
        }

        private static bool Selects(
            BattleRuntimeState runtime,
            string enemyId,
            int tieBreakerValue,
            PlayerMonsterFieldPosition expectedPosition,
            bool expectedTie)
        {
            return BattleRuntimeEnemyAttackTargetService.TrySelect(
                       runtime,
                       enemyId,
                       tieBreakerValue,
                       out BattleRuntimeEnemyAttackTargetResult result,
                       out BattleRuntimeEnemyAttackTargetFailure failure) &&
                   failure ==
                   BattleRuntimeEnemyAttackTargetFailure.None &&
                   result.TargetType ==
                   BattleRuntimeEnemyAttackTargetType.Monster &&
                   result.TargetPosition == expectedPosition &&
                   result.TargetMonster != null &&
                   result.TargetId == result.TargetMonster.BattleCardId &&
                   result.UsedTieBreaker == expectedTie;
        }

        private static BattleRuntimeState CreateRuntime(
            CardData card,
            string suffix,
            params PlayerMonsterFieldPosition[] positions)
        {
            List<BattleCardInstance> cards =
                Instances(card, positions.Length, suffix);
            BattleRuntimeState runtime = new(cards, 384);
            for (int i = 0; i < positions.Length; i++)
            {
                string battleCardId = cards[i].Ids.BattleCardId;
                if (!runtime.Deck.Zones.TryMove(
                        battleCardId, CardZone.MonsterField, out _) ||
                    !runtime.TryRegisterFieldMonster(
                        battleCardId, positions[i], out _))
                {
                    return null;
                }
            }

            return runtime;
        }

        private static List<BattleCardInstance> Instances(
            CardData card,
            int count,
            string suffix)
        {
            List<BattleCardInstance> cards = new();
            for (int i = 0; i < count; i++)
            {
                cards.Add(Instance(card, $"{suffix}-{i}"));
            }

            return cards;
        }

        private static BattleCardInstance Instance(
            CardData card,
            string suffix)
        {
            return new BattleCardInstance(
                card,
                new CardInstanceIds(
                    card.CatalogCardId,
                    $"OWNED-RUNTIME-38-{suffix}",
                    $"BATTLE-RUNTIME-38-{suffix}"),
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
