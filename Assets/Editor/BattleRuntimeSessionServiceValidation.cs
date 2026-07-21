using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class BattleRuntimeSessionServiceValidation
    {
        [MenuItem("Have a Break/Validate Unified Player Turn End")]
        private static void ValidateUnifiedPlayerTurnEndFromMenu()
        {
            bool valid = ValidateUnifiedPlayerTurnEnd();
            if (valid)
            {
                Debug.Log("Unified player turn end passed.");
            }
            else
            {
                Debug.LogError("Unified player turn end failed.");
            }

            EditorUtility.DisplayDialog(
                "Unified Player Turn End Validation",
                valid
                    ? "Unified player turn end passed."
                    : "Unified player turn end failed. Check the Console.",
                "OK");
        }

        [MenuItem("Have a Break/Validate Multi-Round Battle Runtime Session")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            if (valid)
            {
                Debug.Log("Multi-round battle runtime session passed.");
            }
            else
            {
                Debug.LogError("Multi-round battle runtime session failed.");
            }

            EditorUtility.DisplayDialog(
                "Multi-Round Battle Runtime Session Validation",
                valid
                    ? "Multi-round battle runtime session passed."
                    : "Multi-round battle runtime session failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            CardData c01 = FindCard(TestContentIds.C01);
            bool valid = c01 != null;
            valid &= Run(
                "two consecutive rounds",
                () => c01 != null && ValidateTwoRounds(c01));
            valid &= Run(
                "unified player turn end",
                ValidateUnifiedPlayerTurnEnd);
            valid &= Run(
                "prototype battle play window bootstrap",
                PrototypeBattleHarnessFactory.Validate);
            valid &= Run(
                "terminal outcome locking",
                ValidateTerminalLocking);
            valid &= Run(
                "session lifecycle rejection",
                ValidateLifecycleRejection);
            return valid;
        }

        private static bool ValidateUnifiedPlayerTurnEnd()
        {
            BattleRuntimeState runtime = new(
                Array.Empty<BattleCardInstance>(),
                424,
                5,
                30);
            if (!runtime.TryAddEnemy(
                    "ENEMY-TEST-TURN-RIGHT",
                    3,
                    10,
                    EnemyFieldPosition.Right,
                    out _) ||
                !runtime.TryAddEnemy(
                    "ENEMY-TEST-TURN-LEFT",
                    1,
                    10,
                    EnemyFieldPosition.Left,
                    out _) ||
                !runtime.TryAddEnemy(
                    "ENEMY-TEST-TURN-CENTER",
                    2,
                    10,
                    EnemyFieldPosition.Center,
                    out _))
            {
                return false;
            }

            BattleRuntimeSessionState session = new(runtime);
            if (!BattleRuntimeSessionService.TryBegin(
                    session,
                    Array.Empty<string>(),
                    out _, out _, out _, out _) ||
                !BattleRuntimeTestTurnService.TryEndPlayerTurn(
                    session,
                    4240,
                    out BattleRuntimeSessionRoundResult result,
                    out BattleRuntimeSessionFailure sessionFailure,
                    out BattleRuntimeRoundFailure roundFailure,
                    out BattleTurnFailure playerTurnEndFailure,
                    out BattleRuntimeEnemyTurnPipelineFailure pipelineFailure,
                    out BattleRuntimeEnemyTurnPlanFailure planFailure,
                    out BattleRuntimeEnemyTurnFailure enemyTurnFailure,
                    out int failedActionIndex))
            {
                return false;
            }

            IReadOnlyList<BattleRuntimeEnemyTurnCommand> commands =
                result.Round.EnemyTurnPipeline.Plan.Commands;
            return sessionFailure == BattleRuntimeSessionFailure.None &&
                   roundFailure == BattleRuntimeRoundFailure.None &&
                   playerTurnEndFailure == BattleTurnFailure.None &&
                   pipelineFailure ==
                   BattleRuntimeEnemyTurnPipelineFailure.None &&
                   planFailure == BattleRuntimeEnemyTurnPlanFailure.None &&
                   enemyTurnFailure == BattleRuntimeEnemyTurnFailure.None &&
                   failedActionIndex == -1 &&
                   result.CompletedRoundCount == 1 &&
                   result.Outcome == BattleOutcome.Ongoing &&
                   result.PlayerTurnStarted &&
                   result.Round.ProcessedEnemyActionCount == 3 &&
                   commands.Count == 3 &&
                   IsSingleAttack(commands[0], "ENEMY-TEST-TURN-LEFT") &&
                   IsSingleAttack(commands[1], "ENEMY-TEST-TURN-CENTER") &&
                   IsSingleAttack(commands[2], "ENEMY-TEST-TURN-RIGHT") &&
                   runtime.Player.CurrentHealth == 24 &&
                   runtime.Turn.Phase == BattleTurnPhase.PlayerAction &&
                   runtime.Turn.PlayerTurnNumber == 2;
        }

        private static bool IsSingleAttack(
            BattleRuntimeEnemyTurnCommand command,
            string expectedEnemyId)
        {
            return command != null &&
                   command.ActionType ==
                   BattleRuntimeEnemyTurnActionType.Attack &&
                   command.UsesAutomaticTargeting &&
                   command.AutomaticAttackCount == 1 &&
                   command.AttackTieBreakerValues.Count == 1 &&
                   string.Equals(
                       command.EnemyId,
                       expectedEnemyId,
                       StringComparison.Ordinal);
        }

        private static bool ValidateTwoRounds(CardData card)
        {
            BattleCardInstance ally = Instance(card, "TWO-ROUNDS");
            BattleRuntimeState runtime = new(new[] { ally }, 421, 10);
            if (!runtime.TryAddEnemy(
                    "ENEMY-SESSION-ONGOING",
                    1,
                    10,
                    EnemyFieldPosition.Center,
                    out _))
            {
                return false;
            }

            BattleRuntimeSessionState session = new(runtime);
            if (!BattleRuntimeSessionService.TryBegin(
                    session,
                    Array.Empty<string>(),
                    out List<BattleCardInstance> replacements,
                    out BattleRuntimeSessionFailure beginFailure,
                    out StartingHandRedrawFailure redrawFailure,
                    out BattleTurnFailure beginTurnFailure) ||
                beginFailure != BattleRuntimeSessionFailure.None ||
                redrawFailure != StartingHandRedrawFailure.None ||
                beginTurnFailure != BattleTurnFailure.None ||
                replacements.Count != 0 ||
                !runtime.Deck.Zones.TryMove(
                    ally.Ids.BattleCardId,
                    CardZone.MonsterField,
                    out _) ||
                !runtime.TryRegisterFieldMonster(
                    ally.Ids.BattleCardId,
                    PlayerMonsterFieldPosition.Center,
                    out BattleMonsterState monster))
            {
                return false;
            }

            int initialHealth = monster.CurrentHealth;
            int firstBaseline = session.PlayerTurnEventStartIndex;
            if (!ResolveOngoingRound(
                    session,
                    out BattleRuntimeSessionRoundResult first) ||
                first.CompletedRoundCount != 1 ||
                first.Outcome != BattleOutcome.Ongoing ||
                !first.PlayerTurnStarted ||
                monster.CurrentHealth != initialHealth - 1 ||
                session.PlayerTurnEventStartIndex <= firstBaseline)
            {
                return false;
            }

            int secondBaseline = session.PlayerTurnEventStartIndex;
            return ResolveOngoingRound(
                       session,
                       out BattleRuntimeSessionRoundResult second) &&
                   second.CompletedRoundCount == 2 &&
                   second.Outcome == BattleOutcome.Ongoing &&
                   second.PlayerTurnStarted &&
                   monster.CurrentHealth == initialHealth - 2 &&
                   session.CompletedRoundCount == 2 &&
                   session.PlayerTurnEventStartIndex > secondBaseline &&
                   runtime.Turn.Phase == BattleTurnPhase.PlayerAction &&
                   runtime.Turn.PlayerTurnNumber == 3;
        }

        private static bool ResolveOngoingRound(
            BattleRuntimeSessionState session,
            out BattleRuntimeSessionRoundResult result)
        {
            bool resolved = BattleRuntimeSessionService.TryResolveRound(
                session,
                new[]
                {
                    BattleRuntimeEnemyTurnCommand.CreateAutomaticAttack(
                        "ENEMY-SESSION-ONGOING", 1, new[] { 0 })
                },
                out result,
                out BattleRuntimeSessionFailure sessionFailure,
                out BattleRuntimeRoundFailure roundFailure,
                out BattleTurnFailure playerTurnEndFailure,
                out BattleRuntimeEnemyTurnPipelineFailure pipelineFailure,
                out BattleRuntimeEnemyTurnPlanFailure planFailure,
                out BattleRuntimeEnemyTurnFailure enemyTurnFailure,
                out int failedActionIndex);

            return resolved &&
                   sessionFailure == BattleRuntimeSessionFailure.None &&
                   roundFailure == BattleRuntimeRoundFailure.None &&
                   playerTurnEndFailure == BattleTurnFailure.None &&
                   pipelineFailure ==
                   BattleRuntimeEnemyTurnPipelineFailure.None &&
                   planFailure == BattleRuntimeEnemyTurnPlanFailure.None &&
                   enemyTurnFailure == BattleRuntimeEnemyTurnFailure.None &&
                   failedActionIndex == -1 &&
                   result != null;
        }

        private static bool ValidateTerminalLocking()
        {
            BattleRuntimeState runtime = new(
                Array.Empty<BattleCardInstance>(),
                422,
                10,
                2);
            if (!runtime.TryAddEnemy(
                    "ENEMY-SESSION-DEFEAT",
                    2,
                    10,
                    EnemyFieldPosition.Center,
                    out _))
            {
                return false;
            }

            BattleRuntimeSessionState session = new(runtime);
            if (!BattleRuntimeSessionService.TryBegin(
                    session,
                    Array.Empty<string>(),
                    out _, out _, out _, out _) ||
                !BattleRuntimeSessionService.TryResolveRound(
                    session,
                    new[]
                    {
                        BattleRuntimeEnemyTurnCommand.CreateAutomaticAttack(
                            "ENEMY-SESSION-DEFEAT",
                            3,
                            new[] { 0, 0, 0 })
                    },
                    out BattleRuntimeSessionRoundResult result,
                    out BattleRuntimeSessionFailure sessionFailure,
                    out BattleRuntimeRoundFailure roundFailure,
                    out _, out _, out _, out _, out _))
            {
                return false;
            }

            bool locked = !BattleRuntimeSessionService.TryResolveRound(
                    session,
                    Array.Empty<BattleRuntimeEnemyTurnCommand>(),
                    out _,
                    out BattleRuntimeSessionFailure lockedFailure,
                    out _, out _, out _, out _, out _, out _);
            return sessionFailure == BattleRuntimeSessionFailure.None &&
                   roundFailure == BattleRuntimeRoundFailure.None &&
                   result != null &&
                   result.CompletedRoundCount == 1 &&
                   result.Outcome == BattleOutcome.Defeat &&
                   !result.PlayerTurnStarted &&
                   session.IsFinished &&
                   runtime.Player.IsDefeated &&
                   locked &&
                   lockedFailure == BattleRuntimeSessionFailure.BattleFinished;
        }

        private static bool ValidateLifecycleRejection()
        {
            BattleRuntimeSessionState session = new(
                new BattleRuntimeState(
                    Array.Empty<BattleCardInstance>(), 423));
            bool roundBeforeStartRejected =
                !BattleRuntimeSessionService.TryResolveRound(
                    session,
                    Array.Empty<BattleRuntimeEnemyTurnCommand>(),
                    out _,
                    out BattleRuntimeSessionFailure beforeStartFailure,
                    out _, out _, out _, out _, out _, out _) &&
                beforeStartFailure == BattleRuntimeSessionFailure.NotStarted;

            bool began = BattleRuntimeSessionService.TryBegin(
                session,
                Array.Empty<string>(),
                out _, out _, out _, out _);
            bool duplicateRejected =
                !BattleRuntimeSessionService.TryBegin(
                    session,
                    Array.Empty<string>(),
                    out _,
                    out BattleRuntimeSessionFailure duplicateFailure,
                    out _, out _) &&
                duplicateFailure ==
                BattleRuntimeSessionFailure.AlreadyStarted;

            return roundBeforeStartRejected &&
                   began &&
                   session.Outcome == BattleOutcome.Victory &&
                   session.IsFinished &&
                   duplicateRejected;
        }

        private static bool Run(string label, Func<bool> validation)
        {
            try
            {
                bool passed = validation();
                if (passed)
                {
                    Debug.Log($"Battle session validation passed: {label}.");
                }
                else
                {
                    Debug.LogError($"Battle session validation failed: {label}.");
                }

                return passed;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Battle session validation threw: {label}.\n{exception}");
                return false;
            }
        }

        private static BattleCardInstance Instance(
            CardData card,
            string suffix)
        {
            return new BattleCardInstance(
                card,
                new CardInstanceIds(
                    card.CatalogCardId,
                    $"OWNED-RUNTIME-42-{suffix}",
                    $"BATTLE-RUNTIME-42-{suffix}"),
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

    internal static class PrototypeBattleHarnessFactory
    {
        private const string CardDatabasePath =
            "Assets/GameData/CardDatabase.asset";
        private const string EncounterDatabasePath =
            "Assets/GameData/EncounterDatabase.asset";
        private const string PrototypeEncounterId =
            "TEST-ENCOUNTER-PROTOTYPE-01";

        private static readonly string[] TestCardIds =
        {
            TestContentIds.C01,
            TestContentIds.C02,
            TestContentIds.C03,
            TestContentIds.C04,
            TestContentIds.C05,
            TestContentIds.C06,
            TestContentIds.C07,
            TestContentIds.C08,
            TestContentIds.C09,
            TestContentIds.C10,
            TestContentIds.C11,
            TestContentIds.C12
        };

        internal static bool TryCreate(
            out BattleRuntimeEncounterContext context,
            out string error)
        {
            context = null;
            CardDatabase cardDatabase =
                AssetDatabase.LoadAssetAtPath<CardDatabase>(CardDatabasePath);
            if (cardDatabase == null)
            {
                error = $"Card database not found: {CardDatabasePath}";
                return false;
            }

            EncounterDatabase encounterDatabase =
                AssetDatabase.LoadAssetAtPath<EncounterDatabase>(
                    EncounterDatabasePath);
            if (encounterDatabase == null)
            {
                error =
                    $"Encounter database not found: {EncounterDatabasePath}";
                return false;
            }

            List<string> databaseErrors =
                encounterDatabase.GetValidationErrors();
            if (databaseErrors.Count > 0)
            {
                error =
                    "Encounter database is invalid:\n" +
                    string.Join("\n", databaseErrors);
                return false;
            }

            if (!encounterDatabase.TryGetEncounter(
                    PrototypeEncounterId,
                    out EncounterData encounter))
            {
                error =
                    $"Prototype encounter not found: {PrototypeEncounterId}";
                return false;
            }

            List<BattleCardInstance> deck = new();
            for (int i = 0; i < TestCardIds.Length; i++)
            {
                string catalogCardId = TestCardIds[i];
                if (!cardDatabase.TryGetCard(
                        catalogCardId,
                        out CardData card))
                {
                    error = $"Test card not found: {catalogCardId}";
                    return false;
                }

                deck.Add(new BattleCardInstance(
                    card,
                    new CardInstanceIds(
                        catalogCardId,
                        $"OWNED-PROTOTYPE-{catalogCardId}",
                        $"BATTLE-PROTOTYPE-{catalogCardId}"),
                    1,
                    CardZone.DrawPile));
            }

            if (!BattleRuntimeEncounterFlowService.TryCreateAndBegin(
                    deck,
                    new RunBattleState(30, 30, 0),
                    encounter,
                    20260721,
                    5,
                    Array.Empty<string>(),
                    20260721u,
                    out context,
                    out BattleRuntimeEncounterFlowFailure flowFailure,
                    out BattleRuntimeBootstrapFailure bootstrapFailure,
                    out BattleRuntimeSessionFailure sessionFailure,
                    out StartingHandRedrawFailure redrawFailure,
                    out BattleTurnFailure turnFailure,
                    out List<string> validationErrors))
            {
                error =
                    $"Could not create encounter battle: {flowFailure} / " +
                    $"{bootstrapFailure} / {sessionFailure} / " +
                    $"{redrawFailure} / {turnFailure}" +
                    (validationErrors.Count == 0
                        ? string.Empty
                        : $"\n{string.Join("\n", validationErrors)}");
                context = null;
                return false;
            }

            error = null;
            return true;
        }

        internal static bool Validate()
        {
            if (!TryCreate(
                    out BattleRuntimeEncounterContext context,
                    out string error))
            {
                Debug.LogError(error);
                return false;
            }

            BattleRuntimeSessionState session = context.Session;
            BattleRuntimeState runtime = session.Runtime;
            bool patternCreated =
                BattleRuntimeEnemyPatternService.TryCreateCommands(
                    session,
                    context.Encounter,
                    20260721,
                    out List<BattleRuntimeEnemyTurnCommand> commands,
                    out BattleRuntimeEnemyPatternFailure patternFailure);
            return session.Started &&
                   !session.IsFinished &&
                   session.Outcome == BattleOutcome.Ongoing &&
                   runtime.Turn.Phase == BattleTurnPhase.PlayerAction &&
                   runtime.Turn.PlayerTurnNumber == 1 &&
                   runtime.CardPlay.Mana.CurrentMana == 5 &&
                   runtime.Player.CurrentHealth == 30 &&
                   runtime.Deck.Zones.Cards.Count == TestCardIds.Length &&
                   runtime.Deck.Zones.Count(CardZone.Hand) ==
                   BattleDeckState.StartingHandSize &&
                   context.Encounter != null &&
                   string.Equals(
                       context.Encounter.EncounterId,
                       PrototypeEncounterId,
                       StringComparison.Ordinal) &&
                   MatchesEncounter(runtime, context.Encounter) &&
                   patternCreated &&
                   patternFailure == BattleRuntimeEnemyPatternFailure.None &&
                   MatchesFirstTurnCommands(commands, context.Encounter);
        }

        private static bool MatchesEncounter(
            BattleRuntimeState runtime,
            EncounterData encounter)
        {
            if (runtime == null || encounter?.EnemySlots == null ||
                runtime.Enemies.Count != encounter.EnemySlots.Count ||
                runtime.LivingEnemies.Count != encounter.EnemySlots.Count)
            {
                return false;
            }

            foreach (EncounterEnemySlot slot in encounter.EnemySlots)
            {
                BattleEnemyRuntimeState enemy =
                    runtime.FindEnemy(slot.EnemyInstanceId);
                if (enemy == null || !enemy.IsAlive ||
                    enemy.Attack != slot.Enemy.Attack ||
                    enemy.Vital.CurrentHealth != slot.Enemy.MaximumHealth ||
                    runtime.EnemyPositions.FindPosition(enemy.EnemyId) !=
                    slot.Position)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool MatchesFirstTurnCommands(
            IReadOnlyList<BattleRuntimeEnemyTurnCommand> commands,
            EncounterData encounter)
        {
            if (commands == null || encounter?.EnemySlots == null ||
                commands.Count != encounter.EnemySlots.Count)
            {
                return false;
            }

            for (int i = 0; i < commands.Count; i++)
            {
                BattleRuntimeEnemyTurnCommand command = commands[i];
                EncounterEnemySlot slot = encounter.EnemySlots[i];
                if (command == null || slot == null ||
                    command.ActionType !=
                    BattleRuntimeEnemyTurnActionType.Attack ||
                    command.AutomaticAttackCount != 1 ||
                    !string.Equals(
                        command.EnemyId,
                        slot.EnemyInstanceId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public sealed class PrototypeBattleTestWindow : EditorWindow
    {
        private BattleRuntimeEncounterContext context;
        private BattleRuntimeSessionState session;
        private string selectedTargetEnemyId;
        private string selectedBanishBattleCardId;
        private string lastMessage;
        private Vector2 scroll;

        [MenuItem("Have a Break/Tests/Open Prototype Battle")]
        public static void ShowWindow()
        {
            PrototypeBattleTestWindow window =
                GetWindow<PrototypeBattleTestWindow>("Prototype Battle");
            window.minSize = new Vector2(720f, 620f);
            window.Show();
        }

        [MenuItem("Have a Break/Tests/Validate Prototype Battle Screen")]
        private static void ValidateFromMenu()
        {
            bool valid = PrototypeBattleHarnessFactory.Validate();
            EditorUtility.DisplayDialog(
                "Prototype Battle Screen Validation",
                valid
                    ? "Prototype battle screen bootstrap passed."
                    : "Prototype battle screen bootstrap failed. Check the Console.",
                "OK");
        }

        private void OnEnable()
        {
            if (session == null)
            {
                StartNewBattle();
            }
        }

        private void OnGUI()
        {
            DrawToolbar();
            if (session?.Runtime == null)
            {
                EditorGUILayout.HelpBox(
                    string.IsNullOrWhiteSpace(lastMessage)
                        ? "테스트 전투를 시작할 수 없습니다."
                        : lastMessage,
                    MessageType.Error);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawSummary();
            DrawEnemies();
            DrawPlayerMonsters();
            DrawHand();
            DrawZones();
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button(
                    "새 테스트 전투",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(120f)))
            {
                StartNewBattle();
            }

            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(
                       session == null || session.IsFinished))
            {
                if (GUILayout.Button(
                        "턴 종료",
                        EditorStyles.toolbarButton,
                        GUILayout.Width(90f)))
                {
                    EndPlayerTurn();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSummary()
        {
            BattleRuntimeState runtime = session.Runtime;
            EditorGUILayout.LabelField(
                context?.Encounter == null
                    ? "C01~C12 직접 조작 전투"
                    : $"{context.Encounter.DisplayName} " +
                      $"({context.Encounter.EncounterId})",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "적을 먼저 선택한 뒤 카드를 사용하거나 아군 몬스터로 공격하세요. " +
                "C07은 패에서 함께 소멸시킬 카드를 선택해야 합니다.",
                MessageType.Info);
            EditorGUILayout.LabelField(
                $"결과: {OutcomeLabel(session.Outcome)}    " +
                $"턴: {runtime.Turn.PlayerTurnNumber}    " +
                $"단계: {runtime.Turn.Phase}    " +
                $"플레이어 HP: {runtime.Player.CurrentHealth}/" +
                $"{runtime.Player.MaximumHealth}    " +
                $"마력: {runtime.CardPlay.Mana.CurrentMana}/" +
                $"{runtime.CardPlay.Mana.MaximumMana}");
            BattleCommonStatusState playerStatus = runtime.Player.Status;
            EditorGUILayout.LabelField(
                $"플레이어 상태: 부상 {playerStatus.Injury}  " +
                $"약화 {playerStatus.Weaken}  " +
                $"취약 {playerStatus.Vulnerable}  " +
                $"속박 {playerStatus.Bind}  " +
                $"기절 {playerStatus.Stun}");

            if (!string.IsNullOrWhiteSpace(lastMessage))
            {
                EditorGUILayout.HelpBox(lastMessage, MessageType.None);
            }

            EditorGUILayout.Space(8f);
        }

        private void DrawEnemies()
        {
            BattleRuntimeState runtime = session.Runtime;
            EditorGUILayout.LabelField("적 필드", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            foreach (EnemyFieldPosition position in
                     Enum.GetValues(typeof(EnemyFieldPosition)))
            {
                string enemyId = runtime.EnemyPositions.GetOccupant(position);
                EditorGUILayout.BeginVertical(
                    EditorStyles.helpBox,
                    GUILayout.MinHeight(92f));
                EditorGUILayout.LabelField(
                    PositionLabel(position),
                    EditorStyles.miniBoldLabel);
                if (string.IsNullOrWhiteSpace(enemyId))
                {
                    EditorGUILayout.LabelField("빈 칸");
                }
                else
                {
                    BattleEnemyRuntimeState enemy = runtime.FindEnemy(enemyId);
                    BattleEnemyStatusState status =
                        runtime.EnemyStatuses.Find(enemyId);
                    EncounterEnemySlot encounterSlot =
                        FindEncounterSlot(enemyId);
                    EnemyDefinitionData definition = encounterSlot?.Enemy;
                    bool selected = string.Equals(
                        selectedTargetEnemyId,
                        enemyId,
                        StringComparison.OrdinalIgnoreCase);
                    EditorGUILayout.LabelField(
                        $"{definition?.DisplayName ?? enemyId}\n" +
                        $"{enemyId}\nHP {enemy.Vital.CurrentHealth}  " +
                        $"공격 {enemy.Attack}\n" +
                        $"현재 패턴 {PatternLabel(definition)}\n" +
                        $"부상 {status?.Injury ?? 0}  " +
                        $"약화 {status?.Weaken ?? 0}  " +
                        $"취약 {status?.Vulnerable ?? 0}  " +
                        $"속박 {status?.Bind ?? 0}  " +
                        $"기절 {status?.Stun ?? 0}",
                        EditorStyles.wordWrappedLabel);
                    if (GUILayout.Button(selected ? "선택됨" : "대상 선택"))
                    {
                        selectedTargetEnemyId = enemyId;
                        Repaint();
                    }
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(8f);
        }

        private void DrawPlayerMonsters()
        {
            BattleRuntimeState runtime = session.Runtime;
            EditorGUILayout.LabelField("아군 몬스터 필드", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            foreach (PlayerMonsterFieldPosition position in
                     Enum.GetValues(typeof(PlayerMonsterFieldPosition)))
            {
                string battleCardId =
                    runtime.PlayerMonsterPositions.GetOccupant(position);
                EditorGUILayout.BeginVertical(
                    EditorStyles.helpBox,
                    GUILayout.MinHeight(108f));
                EditorGUILayout.LabelField(
                    PlayerPositionLabel(position),
                    EditorStyles.miniBoldLabel);
                BattleMonsterState monster = string.IsNullOrWhiteSpace(
                        battleCardId)
                    ? null
                    : runtime.Monsters.Find(battleCardId);
                if (monster == null)
                {
                    EditorGUILayout.LabelField("빈 칸");
                }
                else
                {
                    EditorGUILayout.LabelField(
                        $"{monster.Card.SourceCard.DisplayName}\n" +
                        $"공격 {monster.Attack}  HP " +
                        $"{monster.CurrentHealth}/{monster.MaximumHealth}\n" +
                        $"방어 {monster.Defense}\n" +
                        $"부상 {monster.Status.Injury}  " +
                        $"약화 {monster.Status.Weaken}  " +
                        $"취약 {monster.Status.Vulnerable}  " +
                        $"속박 {monster.Status.Bind}  " +
                        $"기절 {monster.Status.Stun}",
                        EditorStyles.wordWrappedLabel);
                    using (new EditorGUI.DisabledScope(
                               session.IsFinished ||
                               string.IsNullOrWhiteSpace(
                                   selectedTargetEnemyId)))
                    {
                        if (GUILayout.Button("선택한 적 공격"))
                        {
                            Attack(monster.BattleCardId);
                        }
                    }
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(8f);
        }

        private void DrawHand()
        {
            BattleRuntimeState runtime = session.Runtime;
            List<BattleCardInstance> hand =
                runtime.Deck.Zones.GetCards(CardZone.Hand);
            EditorGUILayout.LabelField(
                $"패 ({hand.Count})",
                EditorStyles.boldLabel);
            if (hand.Count == 0)
            {
                EditorGUILayout.HelpBox("패에 카드가 없습니다.", MessageType.Info);
                return;
            }

            foreach (BattleCardInstance card in hand)
            {
                DrawHandCard(card, hand);
            }

            EditorGUILayout.Space(8f);
        }

        private void DrawHandCard(
            BattleCardInstance card,
            IReadOnlyList<BattleCardInstance> hand)
        {
            ResolvedCardData resolved = card.Resolved;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                $"{card.SourceCard.CatalogCardId}  " +
                $"{card.SourceCard.DisplayName}  " +
                $"[{card.SourceCard.CardType}]  비용 {resolved.ManaCost}",
                EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            bool hasBanishCandidate = true;
            if (string.Equals(
                    card.SourceCard.CatalogCardId,
                    TestContentIds.C07,
                    StringComparison.OrdinalIgnoreCase))
            {
                hasBanishCandidate = DrawBanishSelection(card, hand);
            }

            bool requiresTarget = RequiresEnemyTarget(
                card.SourceCard.CatalogCardId);
            bool hasRequiredTarget = !requiresTarget ||
                                     !string.IsNullOrWhiteSpace(
                                         selectedTargetEnemyId);
            bool canPreview = session.Runtime.CardPlay.TryPreviewPlay(
                card.Ids.BattleCardId,
                out _,
                out _);
            using (new EditorGUI.DisabledScope(
                       session.IsFinished || !canPreview ||
                       !hasRequiredTarget || !hasBanishCandidate))
            {
                if (GUILayout.Button("사용", GUILayout.Width(70f)))
                {
                    PlayCard(card.Ids.BattleCardId);
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(
                resolved.RulesText,
                EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();
        }

        private bool DrawBanishSelection(
            BattleCardInstance c07,
            IReadOnlyList<BattleCardInstance> hand)
        {
            List<BattleCardInstance> candidates = hand
                .Where(candidate => candidate != null && candidate != c07)
                .ToList();
            if (candidates.Count == 0)
            {
                selectedBanishBattleCardId = null;
                GUILayout.Label("소멸 대상 없음", EditorStyles.miniLabel);
                return false;
            }

            int selectedIndex = candidates.FindIndex(candidate =>
                string.Equals(
                    candidate.Ids.BattleCardId,
                    selectedBanishBattleCardId,
                    StringComparison.OrdinalIgnoreCase));
            selectedIndex = Mathf.Max(0, selectedIndex);
            string[] labels = candidates
                .Select(candidate =>
                    $"{candidate.SourceCard.CatalogCardId} " +
                    candidate.SourceCard.DisplayName)
                .ToArray();
            selectedIndex = EditorGUILayout.Popup(
                selectedIndex,
                labels,
                GUILayout.Width(170f));
            selectedBanishBattleCardId =
                candidates[selectedIndex].Ids.BattleCardId;
            return true;
        }

        private void DrawZones()
        {
            BattleCardZoneState zones = session.Runtime.Deck.Zones;
            EditorGUILayout.LabelField("영역 요약", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                $"드로우 {zones.Count(CardZone.DrawPile)}  |  " +
                $"묘지 {zones.Count(CardZone.Graveyard)}  |  " +
                $"소멸 {zones.Count(CardZone.Banished)}  |  " +
                $"스킬/트랩/결계 필드 {zones.Count(CardZone.SkillField)}  |  " +
                $"이벤트 {session.Runtime.EventLog.Events.Count}");
        }

        private void StartNewBattle()
        {
            if (!PrototypeBattleHarnessFactory.TryCreate(
                    out context,
                    out string error))
            {
                session = null;
                selectedTargetEnemyId = null;
                selectedBanishBattleCardId = null;
                lastMessage = error;
                return;
            }

            session = context.Session;
            selectedBanishBattleCardId = null;
            SelectFirstLivingEnemy();
            lastMessage =
                $"{context.Encounter.DisplayName} 전투를 실제 조우 데이터로 " +
                "시작했습니다. 카드와 수치는 검증용 임시 콘텐츠입니다.";
            Repaint();
        }

        private void PlayCard(string battleCardId)
        {
            if (!BattleRuntimePlayerCardActionService.TryResolve(
                    session.Runtime,
                    battleCardId,
                    selectedTargetEnemyId,
                    selectedBanishBattleCardId,
                    out BattleRuntimePlayerCardActionResult result,
                    out BattleRuntimePlayerCardActionFailure failure,
                    out BattleRuntimeCardPlayFailure playFailure,
                    out CardPlayFailure cardPlayFailure))
            {
                lastMessage =
                    $"카드 사용 실패: {failure} / {playFailure} / " +
                    $"{cardPlayFailure}";
                return;
            }

            lastMessage =
                $"{result.Play.Card.SourceCard.DisplayName} 사용 완료.";
            selectedBanishBattleCardId = null;
            RefreshTerminalOutcome();
            SelectFirstLivingEnemy();
            Repaint();
        }

        private void Attack(string attackerBattleCardId)
        {
            if (!BattleRuntimePlayerAttackService.TryResolve(
                    session.Runtime,
                    attackerBattleCardId,
                    selectedTargetEnemyId,
                    out BattleRuntimePlayerAttackResult result,
                    out BattleRuntimePlayerAttackFailure failure))
            {
                lastMessage = $"공격 실패: {failure}";
                return;
            }

            lastMessage =
                $"공격 완료: 피해 {result.DamageApplied}" +
                (result.TargetDefeated ? " / 적 처치" : string.Empty);
            RefreshTerminalOutcome();
            SelectFirstLivingEnemy();
            Repaint();
        }

        private void EndPlayerTurn()
        {
            int tieBreakerSeed =
                20260721 + session.CompletedRoundCount * 10;
            if (!BattleRuntimeEnemyPatternService.TryEndPlayerTurn(
                    session,
                    context?.Encounter,
                    tieBreakerSeed,
                    out BattleRuntimeSessionRoundResult result,
                    out BattleRuntimeEnemyPatternFailure patternFailure,
                    out BattleRuntimeSessionFailure sessionFailure,
                    out BattleRuntimeRoundFailure roundFailure,
                    out BattleTurnFailure turnFailure,
                    out BattleRuntimeEnemyTurnPipelineFailure pipelineFailure,
                    out BattleRuntimeEnemyTurnPlanFailure planFailure,
                    out BattleRuntimeEnemyTurnFailure enemyTurnFailure,
                    out int failedActionIndex))
            {
                lastMessage =
                    $"턴 종료 실패: {patternFailure} / {sessionFailure} / " +
                    $"{roundFailure} / " +
                    $"{turnFailure} / {pipelineFailure} / {planFailure} / " +
                    $"{enemyTurnFailure} / action {failedActionIndex}";
                return;
            }

            lastMessage = result.Outcome == BattleOutcome.Ongoing
                ? $"적 턴 완료. 플레이어 턴 " +
                  $"{session.Runtime.Turn.PlayerTurnNumber} 시작."
                : $"전투 종료: {OutcomeLabel(result.Outcome)}";
            selectedBanishBattleCardId = null;
            SelectFirstLivingEnemy();
            Repaint();
        }

        private EncounterEnemySlot FindEncounterSlot(string enemyId)
        {
            return context?.Encounter?.EnemySlots?.FirstOrDefault(
                slot => slot != null && string.Equals(
                    slot.EnemyInstanceId,
                    enemyId,
                    StringComparison.OrdinalIgnoreCase));
        }

        private string PatternLabel(EnemyDefinitionData definition)
        {
            if (definition?.ActionPattern == null || session?.Runtime == null ||
                !definition.ActionPattern.TryGetTurn(
                    session.Runtime.Turn.PlayerTurnNumber,
                    out EnemyTurnPatternStep turn))
            {
                return "없음";
            }

            List<string> actions = new();
            if (turn.Moves)
            {
                actions.Add($"{turn.MoveDirection} {turn.MoveSteps}칸 이동");
            }

            if (turn.AttackCount > 0)
            {
                actions.Add($"공격 {turn.AttackCount}회");
            }

            if (turn.Abilities.Count > 0)
            {
                actions.Add($"능력 {turn.Abilities.Count}개");
            }

            return actions.Count == 0
                ? "행동 없음"
                : string.Join(" → ", actions);
        }

        private void RefreshTerminalOutcome()
        {
            if (session.IsFinished)
            {
                return;
            }

            if (BattleRuntimeSessionService.TryFinalizeTerminalOutcome(
                    session,
                    out BattleOutcome outcome,
                    out BattleRuntimeSessionFailure failure))
            {
                lastMessage += $" 전투 종료: {OutcomeLabel(outcome)}";
            }
            else if (failure != BattleRuntimeSessionFailure.BattleOngoing)
            {
                lastMessage += $" 승패 확인 실패: {failure}";
            }
        }

        private void SelectFirstLivingEnemy()
        {
            if (session?.Runtime == null)
            {
                selectedTargetEnemyId = null;
                return;
            }

            BattleRuntimeState runtime = session.Runtime;
            if (!string.IsNullOrWhiteSpace(selectedTargetEnemyId) &&
                runtime.LivingEnemies.Contains(selectedTargetEnemyId))
            {
                return;
            }

            selectedTargetEnemyId = runtime.Enemies
                .FirstOrDefault(enemy => enemy != null && enemy.IsAlive &&
                    runtime.LivingEnemies.Contains(enemy.EnemyId))
                ?.EnemyId;
        }

        private static bool RequiresEnemyTarget(string catalogCardId)
        {
            return string.Equals(
                       catalogCardId,
                       TestContentIds.C01,
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       catalogCardId,
                       TestContentIds.C05,
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       catalogCardId,
                       TestContentIds.C06,
                       StringComparison.OrdinalIgnoreCase);
        }

        private static string OutcomeLabel(BattleOutcome outcome)
        {
            return outcome switch
            {
                BattleOutcome.Victory => "승리",
                BattleOutcome.Defeat => "패배",
                _ => "진행 중"
            };
        }

        private static string PositionLabel(EnemyFieldPosition position)
        {
            return position switch
            {
                EnemyFieldPosition.Left => "왼쪽 적",
                EnemyFieldPosition.Center => "가운데 적",
                _ => "오른쪽 적"
            };
        }

        private static string PlayerPositionLabel(
            PlayerMonsterFieldPosition position)
        {
            return position switch
            {
                PlayerMonsterFieldPosition.Left => "왼쪽 아군",
                PlayerMonsterFieldPosition.Center => "가운데 아군",
                _ => "오른쪽 아군"
            };
        }
    }
}
