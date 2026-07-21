using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class PrototypeEnemyAbilityPatternValidation
    {
        private const string PrototypeEncounterId =
            "TEST-ENCOUNTER-PROTOTYPE-01";

        [MenuItem("Have a Break/Validate Prototype Enemy Ability Patterns")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            if (valid)
            {
                Debug.Log("Prototype enemy ability pattern flow passed.");
            }
            else
            {
                Debug.LogError(
                    "Prototype enemy ability pattern flow failed.");
            }

            EditorUtility.DisplayDialog(
                "Prototype Enemy Ability Pattern Validation",
                valid
                    ? "Prototype enemy ability pattern flow passed."
                    : "Prototype enemy ability pattern flow failed. Check the Console.",
                "OK");
        }

        [MenuItem("Have a Break/Validate Prototype Enemy Movement Pattern")]
        private static void ValidateMovementFromMenu()
        {
            bool valid = Validate();
            if (valid)
            {
                Debug.Log(
                    "Prototype enemy movement pattern flow passed.");
            }
            else
            {
                Debug.LogError(
                    "Prototype enemy movement pattern flow failed.");
            }

            EditorUtility.DisplayDialog(
                "Prototype Enemy Movement Pattern Validation",
                valid
                    ? "Prototype enemy movement pattern flow passed."
                    : "Prototype enemy movement pattern flow failed. Check the Console.",
                "OK");
        }

        [MenuItem("Have a Break/Validate Prototype Enemy Move Then Attack")]
        private static void ValidateMoveThenAttackFromMenu()
        {
            bool valid = Validate();
            if (valid)
            {
                Debug.Log(
                    "Prototype enemy move-then-attack flow passed.");
            }
            else
            {
                Debug.LogError(
                    "Prototype enemy move-then-attack flow failed.");
            }

            EditorUtility.DisplayDialog(
                "Prototype Enemy Move Then Attack Validation",
                valid
                    ? "Prototype enemy move-then-attack flow passed."
                    : "Prototype enemy move-then-attack flow failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            EncounterData encounter = FindEncounter();
            CardData c01 = FindCard(TestContentIds.C01);
            CardData c10 = FindCard(TestContentIds.C10);
            return encounter != null && c01 != null && c10 != null &&
                   ValidateAuthoredPatterns(encounter) &&
                   ValidateStatusApplications(encounter, c01) &&
                   ValidateC10Cancellation(encounter, c10);
        }

        private static bool ValidateAuthoredPatterns(EncounterData encounter)
        {
            if (EncounterDataValidationService
                    .ValidateEncounter(encounter).Count != 0 ||
                encounter.EnemySlots.Count != 3)
            {
                return false;
            }

            return ValidateSlot(
                       encounter.EnemySlots[0],
                       EnemyFieldPosition.Left,
                       "TEST-ABILITY-BIND",
                       StatusKeyword.Bind,
                       false,
                       true) &&
                   ValidateSlot(
                       encounter.EnemySlots[1],
                       EnemyFieldPosition.Center,
                       "TEST-ABILITY-INJURY",
                       StatusKeyword.Injury,
                       false,
                       false) &&
                   ValidateSlot(
                       encounter.EnemySlots[2],
                       EnemyFieldPosition.Right,
                       "TEST-ABILITY-WEAKEN-AREA",
                       StatusKeyword.Weaken,
                       true,
                       false);
        }

        private static bool ValidateSlot(
            EncounterEnemySlot slot,
            EnemyFieldPosition expectedPosition,
            string expectedAbilityId,
            StatusKeyword expectedStatus,
            bool expectedArea,
            bool expectedMovement)
        {
            if (slot?.Enemy?.ActionPattern?.Turns == null ||
                slot.Position != expectedPosition ||
                slot.Enemy.ActionPattern.Turns.Count != 3)
            {
                return false;
            }

            EnemyTurnPatternStep attackTurn =
                slot.Enemy.ActionPattern.Turns[0];
            EnemyTurnPatternStep abilityTurn =
                slot.Enemy.ActionPattern.Turns[1];
            EnemyTurnPatternStep movementTurn =
                slot.Enemy.ActionPattern.Turns[2];
            if (attackTurn == null || abilityTurn == null ||
                movementTurn == null ||
                attackTurn.Moves || attackTurn.AttackCount != 1 ||
                attackTurn.Abilities.Count != 0 ||
                abilityTurn.Moves || abilityTurn.AttackCount != 0 ||
                abilityTurn.Abilities.Count != 1 ||
                movementTurn.Moves != expectedMovement ||
                movementTurn.MoveSteps != 1 ||
                movementTurn.Abilities.Count != 0 ||
                movementTurn.AttackCount != 1 ||
                (expectedMovement &&
                 movementTurn.MoveDirection != EnemyMoveDirection.Right))
            {
                return false;
            }

            EnemyPatternAbilityData ability = abilityTurn.Abilities[0];
            return ability != null &&
                   string.Equals(
                       ability.AbilityId,
                       expectedAbilityId,
                       StringComparison.Ordinal) &&
                   ability.AffectsFriendlySide &&
                   ability.IsAreaAbility == expectedArea &&
                   ability.StatusKeyword == expectedStatus &&
                   ability.StatusAmount == 1;
        }

        private static bool ValidateStatusApplications(
            EncounterData encounter,
            CardData card)
        {
            if (!TryCreateSession(
                    encounter,
                    card,
                    1,
                    "STATUS",
                    out BattleRuntimeSessionState session,
                    out _))
            {
                return false;
            }

            if (!TryResolvePatternRound(session, encounter, 4100, out _) ||
                session.Runtime.Player.CurrentHealth != 27 ||
                session.Runtime.Turn.PlayerTurnNumber != 2 ||
                session.Runtime.Player.Status.Injury != 0 ||
                session.Runtime.Player.Status.Bind != 0 ||
                session.Runtime.Player.Status.Weaken != 0)
            {
                return false;
            }

            if (!TryResolvePatternRound(
                    session,
                    encounter,
                    4200,
                    out BattleRuntimeSessionRoundResult result))
            {
                return false;
            }

            IReadOnlyList<BattleRuntimeEnemyTurnActionResult> actions =
                result.Round.EnemyTurnPipeline.TurnResult.ActionResults;
            if (actions.Count != 3 ||
                !actions.All(action =>
                    action.Command.ActionType ==
                    BattleRuntimeEnemyTurnActionType.Ability &&
                    action.AbilityResult != null &&
                    !action.AbilityResult.Cancelled) ||
                session.Runtime.Player.CurrentHealth != 27 ||
                session.Runtime.Player.Status.Bind != 1 ||
                session.Runtime.Player.Status.Injury != 1 ||
                session.Runtime.Player.Status.Weaken != 1 ||
                session.Runtime.Turn.PlayerTurnNumber != 3)
            {
                return false;
            }

            if (!TryResolvePatternRound(
                    session,
                    encounter,
                    4250,
                    out BattleRuntimeSessionRoundResult movementResult))
            {
                return false;
            }

            IReadOnlyList<BattleRuntimeEnemyTurnActionResult> movementActions =
                movementResult.Round.EnemyTurnPipeline.TurnResult.ActionResults;
            string leftEnemyId = encounter.EnemySlots[0].EnemyInstanceId;
            string centerEnemyId = encounter.EnemySlots[1].EnemyInstanceId;
            string rightEnemyId = encounter.EnemySlots[2].EnemyInstanceId;
            return movementActions.Count == 4 &&
                   movementActions[0].Command.ActionType ==
                   BattleRuntimeEnemyTurnActionType.Move &&
                   movementActions[0].MoveResult != null &&
                   !movementActions[0].MoveResult.ReplacedByTrap &&
                   movementActions[0].MoveResult.ResolvedSteps == 1 &&
                   movementActions[0].MoveResult.Moves.Count == 3 &&
                   movementActions.Skip(1).All(action =>
                       action.Command.ActionType ==
                       BattleRuntimeEnemyTurnActionType.Attack) &&
                   session.Runtime.EnemyPositions.FindPosition(leftEnemyId) ==
                   EnemyFieldPosition.Center &&
                   session.Runtime.EnemyPositions.FindPosition(centerEnemyId) ==
                   EnemyFieldPosition.Right &&
                   session.Runtime.EnemyPositions.FindPosition(rightEnemyId) ==
                   EnemyFieldPosition.Left &&
                   session.Runtime.Player.CurrentHealth == 23 &&
                   session.Runtime.Player.Status.Bind == 0 &&
                   session.Runtime.Player.Status.Injury == 0 &&
                   session.Runtime.Player.Status.Weaken == 0 &&
                   session.Runtime.Turn.PlayerTurnNumber == 4;
        }

        private static bool ValidateC10Cancellation(
            EncounterData encounter,
            CardData c10)
        {
            if (!TryCreateSession(
                    encounter,
                    c10,
                    5,
                    "C10",
                    out BattleRuntimeSessionState session,
                    out BattleCardInstance trap) ||
                !BattleRuntimeCardPlayService.TryPlay(
                    session.Runtime,
                    trap.Ids.BattleCardId,
                    out BattleRuntimeCardPlayResult play,
                    out _,
                    out _) ||
                !BattleRuntimeTrapEffectService.TryRegisterInstallation(
                    session.Runtime,
                    play,
                    out _))
            {
                return false;
            }

            if (!TryResolvePatternRound(session, encounter, 4300, out _) ||
                trap.Zone != CardZone.SkillField ||
                !TryResolvePatternRound(
                    session,
                    encounter,
                    4400,
                    out BattleRuntimeSessionRoundResult result))
            {
                return false;
            }

            IReadOnlyList<BattleRuntimeEnemyTurnActionResult> actions =
                result.Round.EnemyTurnPipeline.TurnResult.ActionResults;
            BattleRuntimeEnemyTurnActionResult cancelled =
                actions.FirstOrDefault(action =>
                    action.AbilityResult?.Cancelled == true);

            return actions.Count == 3 &&
                   actions.Count(action =>
                       action.AbilityResult?.Cancelled == true) == 1 &&
                   cancelled?.Command.Ability.AbilityId ==
                   "TEST-ABILITY-BIND" &&
                   cancelled.AbilityResult.TotalStatusApplied == 0 &&
                   session.Runtime.Player.Status.Bind == 0 &&
                   session.Runtime.Player.Status.Injury == 1 &&
                   session.Runtime.Player.Status.Weaken == 1 &&
                   trap.Zone == CardZone.Hand &&
                   session.Runtime.TrapInstallations.Count == 0;
        }

        private static bool TryCreateSession(
            EncounterData encounter,
            CardData card,
            int cardLevel,
            string suffix,
            out BattleRuntimeSessionState session,
            out BattleCardInstance instance)
        {
            instance = new BattleCardInstance(
                card,
                new CardInstanceIds(
                    card.CatalogCardId,
                    $"OWNED-PROTOTYPE-ABILITY-{suffix}",
                    $"BATTLE-PROTOTYPE-ABILITY-{suffix}"),
                cardLevel,
                CardZone.DrawPile);
            if (!BattleRuntimeBootstrapService.TryCreate(
                    new[] { instance },
                    new RunBattleState(30, 30, 0),
                    encounter,
                    4500 + cardLevel,
                    10,
                    out BattleRuntimeBootstrapResult bootstrap,
                    out _,
                    out _) ||
                !BattleRuntimeSessionService.TryBegin(
                    bootstrap.Session,
                    Array.Empty<string>(),
                    out _,
                    out _,
                    out _,
                    out _))
            {
                session = null;
                return false;
            }

            session = bootstrap.Session;
            return true;
        }

        private static bool TryResolvePatternRound(
            BattleRuntimeSessionState session,
            EncounterData encounter,
            int tieBreakerSeed,
            out BattleRuntimeSessionRoundResult result)
        {
            return BattleRuntimeEnemyPatternService.TryEndPlayerTurn(
                session,
                encounter,
                tieBreakerSeed,
                out result,
                out _,
                out _,
                out _,
                out _,
                out _,
                out _,
                out _,
                out _);
        }

        private static EncounterData FindEncounter()
        {
            return AssetDatabase.FindAssets("t:EncounterData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<EncounterData>)
                .FirstOrDefault(encounter => encounter != null &&
                    string.Equals(
                        encounter.EncounterId,
                        PrototypeEncounterId,
                        StringComparison.OrdinalIgnoreCase));
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
