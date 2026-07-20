using System;
using System.Linq;
using System.Reflection;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleRuntimeFullRegressionValidation
    {
        private const BindingFlags StaticMethods =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        [MenuItem("Have a Break/Validate Full Battle Runtime C01-C12")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            EditorUtility.DisplayDialog(
                "Full Battle Runtime Regression Validation",
                valid
                    ? "Full battle runtime regression C01-C12, enemy flow, planning, and ordering pipeline passed."
                    : "Full battle runtime regression failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            CardData c01 = FindCard("C01");
            CardData c03 = FindCard("C03");
            CardData c04 = FindCard("C04");
            CardData c05 = FindCard("C05");
            CardData c07 = FindCard("C07");
            CardData c08 = FindCard("C08");
            CardData c09 = FindCard("C09");
            CardData c10 = FindCard("C10");
            CardData c11 = FindCard("C11");
            CardData c12 = FindCard("C12");

            bool valid = true;
            valid &= Run(
                "Runtime state composition",
                BattleRuntimeStateValidation.Validate);
            valid &= Run(
                "Runtime card play events",
                BattleRuntimeCardPlayServiceValidation.Validate);
            valid &= Run(
                "C01 C02 summon effects",
                BattleRuntimeSummonEffectServiceValidation.Validate);
            valid &= Run("C03 turn end effect", () => ValidateC03(c03));
            valid &= Run(
                "C05 skill effect",
                () => Invoke(
                    typeof(BattleRuntimeSkillEffectServiceValidation),
                    "ValidateC05"));
            valid &= Run(
                "C06 skill effect",
                () => Invoke(
                    typeof(BattleRuntimeSkillEffectServiceValidation),
                    "ValidateC06"));
            valid &= Run(
                "C07 skill effect",
                () => c01 != null && c07 != null &&
                      Invoke(
                          typeof(BattleRuntimeC07C11EffectValidation),
                          "ValidateC07",
                          c01,
                          c07));
            valid &= Run(
                "C11 barrier effect",
                () => c01 != null && c11 != null &&
                      Invoke(
                          typeof(BattleRuntimeC07C11EffectValidation),
                          "ValidateC11",
                          c01,
                          c11));
            valid &= Run(
                "C08 trap effect",
                () => c08 != null &&
                      Invoke(
                          typeof(BattleRuntimeTrapEffectServiceValidation),
                          "ValidateC08",
                          c08));
            valid &= Run(
                "C09 trap effect",
                () => c01 != null && c09 != null &&
                      Invoke(
                          typeof(BattleRuntimeTrapEffectServiceValidation),
                          "ValidateC09",
                          c01,
                          c09));
            valid &= Run(
                "C10 trap effect",
                () => c10 != null &&
                      Invoke(
                          typeof(BattleRuntimeTrapEffectServiceValidation),
                          "ValidateC10",
                          c10));
            valid &= Run(
                "C04 C12 movement reactions",
                () => c04 != null && c05 != null && c12 != null &&
                      Invoke(
                          typeof(BattleRuntimeMovementReactionServiceValidation),
                          "Validate",
                          c04,
                          c05,
                          c12));

            valid &= Run(
                "Enemy move and C08 replacement",
                () => c08 != null &&
                      Invoke(
                          typeof(BattleRuntimeEnemyMoveServiceValidation),
                          "ValidateC08Replacement",
                          c08));
            valid &= Run(
                "Enemy move C04 C12 reactions",
                () => c04 != null && c12 != null &&
                      Invoke(
                          typeof(BattleRuntimeEnemyMoveServiceValidation),
                          "ValidateMovementReactions",
                          c04,
                          c12));
            valid &= Run(
                "Enemy attack declaration and C09",
                () => c01 != null && c09 != null &&
                      Invoke(
                          typeof(BattleRuntimeEnemyAttackServiceValidation),
                          "Validate",
                          c01,
                          c09));
            valid &= Run(
                "Enemy attack C09 overflow damage",
                () => c01 != null && c09 != null &&
                      Invoke(
                          typeof(BattleRuntimeEnemyAttackDamageValidation),
                          "ValidateC09Overflow",
                          c01,
                          c09));
            valid &= Run(
                "Enemy attack weaken and defense",
                () => c01 != null &&
                      Invoke(
                          typeof(BattleRuntimeEnemyAttackDamageValidation),
                          "ValidateWeakenAndDefense",
                          c01));
            valid &= Run(
                "Enemy ability C10 level 5 single target",
                () => c10 != null &&
                      Invoke(
                          typeof(BattleRuntimeEnemyAbilityC10Validation),
                          "ValidateLevelFiveSingleTarget",
                          c10));
            valid &= Run(
                "Enemy ability C10 level 3 area restriction",
                () => c10 != null &&
                      Invoke(
                          typeof(BattleRuntimeEnemyAbilityC10Validation),
                          "ValidateLevelThreeAreaRestriction",
                          c10));
            valid &= Run(
                "Enemy ability C10 level 4 area cancellation",
                () => c10 != null &&
                      Invoke(
                          typeof(BattleRuntimeEnemyAbilityC10Validation),
                          "ValidateLevelFourAreaCancellation",
                          c10));
            valid &= Run(
                "Enemy turn orchestration",
                BattleRuntimeEnemyTurnServiceValidation.Validate);
            valid &= Run(
                "Enemy turn planning and intents",
                BattleRuntimeEnemyTurnPlanValidation.Validate);
            valid &= Run(
                "Enemy turn ordering",
                BattleRuntimeEnemyTurnOrderValidation.Validate);
            valid &= Run(
                "Ordered enemy turn pipeline",
                BattleRuntimeEnemyTurnPipelineValidation.Validate);

            if (valid)
            {
                Debug.Log(
                    "Full battle runtime regression C01-C12, enemy flow, planning, and ordering pipeline passed.");
            }
            else
            {
                Debug.LogError("Full battle runtime regression failed.");
            }

            return valid;
        }

        private static bool ValidateC03(CardData card)
        {
            if (card == null)
            {
                return false;
            }

            BattleCardInstance instance = new(
                card,
                new CardInstanceIds(
                    card.CatalogCardId,
                    "OWNED-RUNTIME-35I-C03",
                    "BATTLE-RUNTIME-35I-C03"),
                1,
                CardZone.DrawPile);
            BattleRuntimeState runtime = new(new[] { instance }, 35);

            if (!runtime.Turn.TryBeginBattle(out _) ||
                !runtime.Turn.TryConfirmStartingHand(
                    Array.Empty<string>(), out _, out _, out _) ||
                !runtime.Deck.Zones.TryMove(
                    instance.Ids.BattleCardId,
                    CardZone.MonsterField,
                    out _) ||
                !runtime.TryRegisterFieldMonster(
                    instance.Ids.BattleCardId,
                    out BattleMonsterState monster))
            {
                return false;
            }

            int firstPlayerTurnEventIndex = runtime.EventLog.Events.Count;
            bool resolved = BattleRuntimeTurnEffectService.TryEndPlayerTurn(
                runtime,
                firstPlayerTurnEventIndex,
                out BattleRuntimeTurnEffectResult result,
                out BattleTurnFailure failure);

            return resolved &&
                   failure == BattleTurnFailure.None &&
                   result != null &&
                   result.ResolvedC03Count == 1 &&
                   result.TotalDefenseGained == 3 &&
                   monster.Defense == 3 &&
                   runtime.Turn.Phase == BattleTurnPhase.EnemyTurn;
        }

        private static bool Invoke(
            Type validationType,
            string methodName,
            params object[] arguments)
        {
            MethodInfo method = validationType.GetMethod(
                methodName,
                StaticMethods);
            if (method == null)
            {
                throw new MissingMethodException(
                    validationType.FullName,
                    methodName);
            }

            try
            {
                object result = method.Invoke(null, arguments);
                return result is bool passed && passed;
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        }

        private static bool Run(string label, Func<bool> validation)
        {
            try
            {
                bool passed = validation();
                if (passed)
                {
                    Debug.Log($"Runtime regression passed: {label}.");
                }
                else
                {
                    Debug.LogError($"Runtime regression failed: {label}.");
                }

                return passed;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Runtime regression threw an exception: {label}.\n{exception}");
                return false;
            }
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
