from pathlib import Path


def replace_exact(text: str, old: str, new: str, label: str) -> str:
    count = text.count(old)
    if count != 1:
        raise RuntimeError(f"{label}: {count} matches")
    return text.replace(old, new, 1)


def replace_method(text: str, name: str, replacement: str) -> str:
    start_token = f"        private void {name}("
    start = text.index(start_token)
    end = text.find("\n        private ", start + len(start_token))
    if end < 0:
        raise RuntimeError(f"next method not found after {name}")
    return text[:start] + replacement.rstrip() + "\n" + text[end:]


runtime_path = Path("Assets/Scripts/Prototype/RuntimePrototypeScreen.cs")
runtime = runtime_path.read_text(encoding="utf-8")
runtime = replace_exact(
    runtime,
    """        private readonly List<string> deckEditingSelection = new();
        private bool deckEditing;
        private RunOwnedCardState runPreparationCards;
        private readonly List<string> runPreparationSelection = new();""",
    """        private readonly RunDeckSelectionViewModel deckSelection = new();
        private RunOwnedCardState runPreparationCards;""",
    "runtime fields")

runtime = replace_method(runtime, "DrawRunPreparation", '''        private void DrawRunPreparation()
        {
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("새 런 덱 준비", titleStyle);
            GUILayout.Label(
                "보유카드 중 이번 런에서 사용할 카드를 선택하세요. " +
                "카드를 선택한 순서가 덱 순서가 됩니다.", wrappedStyle);
            GUILayout.Space(12f);
            GUILayout.Label($"선택 {deckSelection.SelectedCount}장", headingStyle);
            foreach (RunDeckSelectionOption option in
                     deckSelection.CreateOptions(runPreparationCards))
            {
                if (!GUILayout.Button(option.DisplayLabel)) continue;
                deckSelection.Toggle(option.OwnedCardId);
            }

            GUILayout.Space(12f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("취소", GUILayout.Height(44f)))
            {
                CancelRunPreparation();
            }
            if (GUILayout.Button("이 덱으로 런 시작", GUILayout.Height(44f)))
            {
                ConfirmRunPreparation();
            }
            GUILayout.EndHorizontal();
            DrawMessage();
            GUILayout.EndScrollView();
        }''')

runtime = replace_method(runtime, "DrawDeckEditor", '''        private void DrawDeckEditor()
        {
            if (progress.RunState.RunEnded) return;
            GUILayout.Label("런 덱 편집", headingStyle);
            if (!deckSelection.IsOpen)
            {
                if (GUILayout.Button("덱 편집 열기", GUILayout.Width(140f)))
                {
                    deckSelection.OpenFromDeck(progress.RunDeck);
                }
                return;
            }

            GUILayout.Label($"선택 {deckSelection.SelectedCount}장");
            foreach (RunDeckSelectionOption option in
                     deckSelection.CreateOptions(progress.OwnedCards))
            {
                if (!GUILayout.Button(option.DisplayLabel)) continue;
                deckSelection.Toggle(option.OwnedCardId);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("취소")) deckSelection.Close();
            if (GUILayout.Button("선택한 덱 적용")) ApplyDeckEditing();
            GUILayout.EndHorizontal();
        }''')

runtime = replace_method(runtime, "ApplyDeckEditing", '''        private void ApplyDeckEditing()
        {
            if (deckSelection.TryApply(
                    progress, out RunDeckFailure failure))
            {
                selectedUpgradeCardId =
                    progress.RunDeck.Cards.FirstOrDefault()?.OwnedCardId;
                message = $"런 덱을 {progress.RunDeck.Count}장으로 변경했습니다.";
                SaveRun(null);
                return;
            }
            message = $"런 덱 변경 실패: {failure}";
        }''')

runtime = replace_method(runtime, "BeginRunPreparation", '''        private void BeginRunPreparation()
        {
            runPreparationCards = new RunOwnedCardState();
            int index = 0;
            foreach (CardData card in config.CardDatabase.Cards.Where(card => card != null))
            {
                RunCardInstance ownedCard = new(
                    card, $"OWNED-RUN-{++index:00}-{card.CatalogCardId}", 1);
                if (!runPreparationCards.TryAdd(ownedCard, out _)) continue;
            }
            deckSelection.OpenWithAllOwnedCards(runPreparationCards);
            scroll = Vector2.zero;
            message = "런에 사용할 덱을 선택한 뒤 확정하세요.";
        }''')

runtime = replace_method(runtime, "CancelRunPreparation", '''        private void CancelRunPreparation()
        {
            runPreparationCards = null;
            deckSelection.Close();
            scroll = Vector2.zero;
            message = "새 런 준비를 취소했습니다.";
        }''')

runtime = replace_method(runtime, "ConfirmRunPreparation", '''        private void ConfirmRunPreparation()
        {
            if (!deckSelection.TryCreateDeck(
                    runPreparationCards,
                    out RunDeckState deck, out RunDeckFailure failure))
            {
                message = $"새 런 덱 확정 실패: {failure}";
                return;
            }

            RunBattleState run =
                config.RunStartProgressionConfig.CreateInitialRunState();
            LoadPermanentRewards();
            progress = new RunEncounterProgressState(
                run, runPreparationCards, deck, permanentRewards,
                Array.Empty<string>(), 0);
            campaign = new RunCampaignState(Environment.TickCount & int.MaxValue);
            selectedUpgradeCardId = deck.Cards.FirstOrDefault()?.OwnedCardId;
            selectedEnemyId = null;
            selectedBanishCardIds.Clear();
            deckSelection.Close();
            runPreparationCards = null;
            scroll = Vector2.zero;
            message = "새 런을 시작했습니다.";
            SaveRun(null);
        }''')

runtime = replace_method(runtime, "ContinueRun", '''        private void ContinueRun()
        {
            runPreparationCards = null;
            deckSelection.Close();
            LoadPermanentRewards();
            if (!IntegratedRunSaveService.TryLoad(
                    config.CardDatabase, config.EnchantDatabase,
                    config.EncounterDatabase, permanentRewards,
                    out campaign, out progress, out _, out RunResumeSource source,
                    out RunCampaignFailure failure))
            {
                campaign = null;
                progress = null;
                message = $"이어하기 실패: {failure}";
                return;
            }
            selectedUpgradeCardId =
                progress.OwnedCards.Cards.FirstOrDefault()?.OwnedCardId;
            selectedBanishCardIds.Clear();
            deckSelection.Close();
            scroll = Vector2.zero;
            SelectFirstEnemy();
            message = $"이어하기 완료: {source}";
        }''')
runtime_path.write_text(runtime, encoding="utf-8")

editor_path = Path("Assets/Editor/IntegratedRunPrototypeWindow.cs")
editor = editor_path.read_text(encoding="utf-8")
editor = replace_exact(
    editor,
    """        private readonly List<string> deckEditingSelection = new();
        private bool deckEditing;
        private RunOwnedCardState runPreparationCards;
        private readonly List<string> runPreparationSelection = new();""",
    """        private readonly RunDeckSelectionViewModel deckSelection = new();
        private RunOwnedCardState runPreparationCards;""",
    "editor fields")

editor = replace_method(editor, "DrawRunPreparation", '''        private void DrawRunPreparation()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("새 런 덱 준비", EditorStyles.largeLabel);
            EditorGUILayout.HelpBox(
                "보유카드 중 이번 런에서 사용할 카드를 선택하세요. " +
                "카드를 선택한 순서가 덱 순서가 됩니다.",
                MessageType.Info);
            EditorGUILayout.LabelField(
                $"선택 {deckSelection.SelectedCount}장",
                EditorStyles.boldLabel);
            foreach (RunDeckSelectionOption option in
                     deckSelection.CreateOptions(runPreparationCards))
            {
                if (!GUILayout.Button(option.DisplayLabel)) continue;
                deckSelection.Toggle(option.OwnedCardId);
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("취소", GUILayout.Height(40f)))
            {
                CancelRunPreparation();
            }
            if (GUILayout.Button("이 덱으로 런 시작", GUILayout.Height(40f)))
            {
                ConfirmRunPreparation();
            }
            EditorGUILayout.EndHorizontal();
            DrawMessage();
            EditorGUILayout.EndScrollView();
        }''')

editor = replace_method(editor, "DrawDeckEditor", '''        private void DrawDeckEditor()
        {
            if (progress.RunState.RunEnded) return;
            EditorGUILayout.LabelField("런 덱 편집", EditorStyles.miniBoldLabel);
            if (!deckSelection.IsOpen)
            {
                if (GUILayout.Button("덱 편집 열기"))
                {
                    deckSelection.OpenFromDeck(progress.RunDeck);
                }
                return;
            }

            EditorGUILayout.LabelField($"선택 {deckSelection.SelectedCount}장");
            foreach (RunDeckSelectionOption option in
                     deckSelection.CreateOptions(progress.OwnedCards))
            {
                if (!GUILayout.Button(option.DisplayLabel)) continue;
                deckSelection.Toggle(option.OwnedCardId);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("취소")) deckSelection.Close();
            if (GUILayout.Button("선택한 덱 적용")) ApplyDeckEditing();
            EditorGUILayout.EndHorizontal();
        }''')

editor = replace_method(editor, "ApplyDeckEditing", '''        private void ApplyDeckEditing()
        {
            if (deckSelection.TryApply(
                    progress, out RunDeckFailure failure))
            {
                selectedUpgradeCardId =
                    progress.RunDeck.Cards.FirstOrDefault()?.OwnedCardId;
                message = $"런 덱을 {progress.RunDeck.Count}장으로 변경했습니다.";
                SaveRun(null);
                return;
            }
            message = $"런 덱 변경 실패: {failure}";
        }''')

editor = replace_method(editor, "BeginRunPreparation", '''        private void BeginRunPreparation()
        {
            if (!DatabasesReady())
            {
                LoadDatabases();
                return;
            }

            runPreparationCards = new RunOwnedCardState();
            int index = 0;
            foreach (CardData card in cardDatabase.Cards.Where(card => card != null))
            {
                RunCardInstance ownedCard = new(
                    card, $"OWNED-RUN-{++index:00}-{card.CatalogCardId}", 1);
                if (!runPreparationCards.TryAdd(ownedCard, out _)) continue;
            }
            deckSelection.OpenWithAllOwnedCards(runPreparationCards);

            scroll = Vector2.zero;
            message = "런에 사용할 덱을 선택한 뒤 확정하세요.";
        }''')

editor = replace_method(editor, "CancelRunPreparation", '''        private void CancelRunPreparation()
        {
            runPreparationCards = null;
            deckSelection.Close();
            scroll = Vector2.zero;
            message = "새 런 준비를 취소했습니다.";
        }''')

editor = replace_method(editor, "ConfirmRunPreparation", '''        private void ConfirmRunPreparation()
        {
            if (!deckSelection.TryCreateDeck(
                    runPreparationCards,
                    out RunDeckState deck, out RunDeckFailure failure))
            {
                message = $"새 런 덱 확정 실패: {failure}";
                return;
            }

            RunBattleState run =
                prototypeConfig.RunStartProgressionConfig.CreateInitialRunState();
            LoadPermanentRewards();
            progress = new RunEncounterProgressState(
                run, runPreparationCards, deck, permanentRewards,
                Array.Empty<string>(), 0);
            campaign = new RunCampaignState(20260722);
            selectedEnemyId = null;
            selectedBanishCardId = null;
            selectedUpgradeCardId = deck.Cards.FirstOrDefault()?.OwnedCardId;
            deckSelection.Close();
            runPreparationCards = null;
            message = "새 통합 런을 시작했습니다.";
            SaveRun(null);
        }''')

editor = replace_method(editor, "ContinueRun", '''        private void ContinueRun()
        {
            runPreparationCards = null;
            deckSelection.Close();
            LoadPermanentRewards();
            if (!IntegratedRunSaveService.TryLoad(
                    cardDatabase, enchantDatabase, encounterDatabase,
                    permanentRewards,
                    out campaign, out progress, out _, out RunResumeSource source,
                    out RunCampaignFailure failure))
            {
                campaign = null;
                progress = null;
                message = $"이어하기 실패: {failure}";
                return;
            }

            selectedUpgradeCardId =
                progress.OwnedCards.Cards.FirstOrDefault()?.OwnedCardId;
            deckSelection.Close();
            SelectFirstEnemy();
            message = $"이어하기 완료: {source}";
        }''')
editor_path.write_text(editor, encoding="utf-8")

for path in (runtime_path, editor_path):
    source = path.read_text(encoding="utf-8")
    for obsolete in ("deckEditingSelection", "runPreparationSelection", "deckEditing"):
        if obsolete in source:
            raise RuntimeError(f"{path}: obsolete state remains: {obsolete}")
    if "RunDeckSelectionViewModel deckSelection" not in source:
        raise RuntimeError(f"{path}: ViewModel field missing")
