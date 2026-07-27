using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HaveABreak.Editor;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.EditorTools
{
    public sealed class RunEndToEndManualValidationWindow : EditorWindow
    {
        private const string MenuPath =
            "Have a Break/Tests/Open Automated Run E2E Validation";
        private readonly Dictionary<string, bool> sectionFoldouts = new();
        private RunEndToEndManualSession session;
        private Vector2 scroll;
        private GUIStyle wrappedLabel;
        private string focusedStepId;

        private static string PreferenceKey =>
            "HaveABreak.RunEndToEndManualValidation." + Application.dataPath;

        [MenuItem(MenuPath)]
        public static void ShowWindow()
        {
            RunEndToEndManualValidationWindow window =
                GetWindow<RunEndToEndManualValidationWindow>(
                    "Automated Run E2E");
            window.minSize = new Vector2(760f, 620f);
            window.Show();
        }

        private void OnEnable()
        {
            LoadSession();
            InitializeFoldouts();
        }

        private void OnDisable()
        {
            SaveSession();
        }

        private void OnLostFocus()
        {
            SaveSession();
        }

        private void OnGUI()
        {
            EnsureStyles();
            EnsureSession();
            DrawHeader();
            DrawToolbar();
            DrawProgress();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawSessionMetadata();
            DrawSteps();
            DrawGeneralNotes();
            EditorGUILayout.EndScrollView();
        }

        private void EnsureStyles()
        {
            wrappedLabel ??= new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                richText = true
            };
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField(
                "한 런 자동 엔드투엔드 검증",
                EditorStyles.largeLabel);
            EditorGUILayout.HelpBox(
                "전체 자동 하네스로 새 런 준비부터 최종 보스·영구 보상까지 " +
                "21개 검사항목을 한 번에 판정합니다. 필요할 때만 각 항목의 " +
                "상태와 메모를 수동으로 보완할 수 있습니다.",
                MessageType.Info);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button(
                    "통합 프로토타입 열기",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(135f)))
            {
                IntegratedRunPrototypeWindow.ShowWindow();
            }

            if (GUILayout.Button(
                    "전체 자동 검사",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(110f)))
            {
                RunAutomatedPreflight();
            }

            if (GUILayout.Button(
                    "보고서 내보내기",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(110f)))
            {
                ExportReport();
            }

            if (GUILayout.Button(
                    "다음 미완료 검사",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(110f)))
            {
                FocusNextPendingManualStep();
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button(
                    "새 검증 세션",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(105f)))
            {
                StartNewSessionWithConfirmation();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawProgress()
        {
            RunEndToEndManualStep[] steps =
                RunEndToEndManualValidationCatalog.Steps;
            int total = steps.Length;
            int completed = steps.Count(step =>
                session.FindOrCreate(step.Id).status !=
                RunEndToEndManualStatus.NotRun);
            int passed = steps.Count(step =>
                session.FindOrCreate(step.Id).status ==
                RunEndToEndManualStatus.Passed);
            int failed = steps.Count(step =>
                session.FindOrCreate(step.Id).status ==
                RunEndToEndManualStatus.Failed);
            int blocked = steps.Count(step =>
                session.FindOrCreate(step.Id).status ==
                RunEndToEndManualStatus.Blocked);

            Rect rect = GUILayoutUtility.GetRect(
                20f,
                22f,
                GUILayout.ExpandWidth(true));
            float ratio = total == 0 ? 0f : (float)completed / total;
            EditorGUI.ProgressBar(
                rect,
                ratio,
                $"전체 자동 검사 {completed}/{total} · 통과 {passed} · " +
                $"실패 {failed} · 차단 {blocked}");

            RunEndToEndManualStep next = FindNextPendingManualStep();
            EditorGUILayout.HelpBox(
                next == null
                    ? "모든 자동 검사항목의 결과가 기록되었습니다."
                    : $"다음 미완료 검사: [{next.Section}] {next.Title}",
                next == null ? MessageType.Info : MessageType.None);
            EditorGUILayout.Space(4f);
        }

        private void DrawSessionMetadata()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("검증 세션", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            session.tester = EditorGUILayout.TextField(
                "검사자",
                session.tester ?? string.Empty);
            session.branchOrBuild = EditorGUILayout.TextField(
                "브랜치/빌드",
                session.branchOrBuild ?? string.Empty);
            EditorGUILayout.LabelField(
                "시작 UTC",
                session.startedAtUtc ?? string.Empty);
            EditorGUILayout.LabelField(
                "최근 갱신 UTC",
                session.updatedAtUtc ?? string.Empty);
            EditorGUILayout.LabelField(
                "Unity",
                session.unityVersion ?? string.Empty);
            EditorGUILayout.LabelField(
                "프로젝트",
                session.projectPath ?? string.Empty,
                wrappedLabel);
            if (EditorGUI.EndChangeCheck())
            {
                TouchAndSave();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawSteps()
        {
            if (!string.IsNullOrWhiteSpace(focusedStepId))
            {
                RunEndToEndManualStep focused =
                    RunEndToEndManualValidationCatalog.Steps.FirstOrDefault(
                        step => step.Id == focusedStepId);
                if (focused != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(
                        $"현재 검사 · {focused.Section}",
                        EditorStyles.boldLabel);
                    if (GUILayout.Button("전체 목록 보기", GUILayout.Width(110f)))
                    {
                        focusedStepId = null;
                    }
                    EditorGUILayout.EndHorizontal();
                    DrawStep(focused);
                    EditorGUILayout.Space(6f);
                    return;
                }

                focusedStepId = null;
            }

            foreach (IGrouping<string, RunEndToEndManualStep> section in
                     RunEndToEndManualValidationCatalog.Steps.GroupBy(step =>
                         step.Section))
            {
                bool expanded = sectionFoldouts.TryGetValue(
                    section.Key,
                    out bool current) ? current : true;
                expanded = EditorGUILayout.Foldout(
                    expanded,
                    SectionLabel(section.Key, section),
                    true,
                    EditorStyles.foldoutHeader);
                sectionFoldouts[section.Key] = expanded;
                if (!expanded)
                {
                    continue;
                }

                foreach (RunEndToEndManualStep step in section)
                {
                    DrawStep(step);
                }
                EditorGUILayout.Space(6f);
            }
        }

        private void DrawStep(RunEndToEndManualStep step)
        {
            RunEndToEndManualStepResult result =
                session.FindOrCreate(step.Id);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                step.Title,
                EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUI.BeginChangeCheck();
            RunEndToEndManualStatus status =
                (RunEndToEndManualStatus)EditorGUILayout.EnumPopup(
                    result.status,
                    GUILayout.Width(100f));
            if (EditorGUI.EndChangeCheck())
            {
                result.status = status;
                result.updatedAtUtc = DateTime.UtcNow.ToString("O");
                TouchAndSave();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            DrawStatusButton(
                result,
                RunEndToEndManualStatus.Passed,
                "통과");
            DrawStatusButton(
                result,
                RunEndToEndManualStatus.Failed,
                "실패");
            DrawStatusButton(
                result,
                RunEndToEndManualStatus.Blocked,
                "차단");
            DrawStatusButton(
                result,
                RunEndToEndManualStatus.NotRun,
                "미실행");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(
                $"<b>실행</b>  {step.Action}",
                wrappedLabel);
            EditorGUILayout.LabelField(
                $"<b>기대 결과</b>  {step.Expected}",
                wrappedLabel);
            EditorGUILayout.LabelField(
                $"<b>증거</b>  {step.Evidence}",
                wrappedLabel);

            EditorGUI.BeginChangeCheck();
            string note = EditorGUILayout.TextArea(
                result.note ?? string.Empty,
                GUILayout.MinHeight(42f));
            if (EditorGUI.EndChangeCheck())
            {
                result.note = note;
                result.updatedAtUtc = DateTime.UtcNow.ToString("O");
                TouchAndSave();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawStatusButton(
            RunEndToEndManualStepResult result,
            RunEndToEndManualStatus status,
            string label)
        {
            EditorGUI.BeginDisabledGroup(result.status == status);
            if (GUILayout.Button(label))
            {
                result.status = status;
                result.updatedAtUtc = DateTime.UtcNow.ToString("O");
                TouchAndSave();
                Repaint();
            }
            EditorGUI.EndDisabledGroup();
        }

        private RunEndToEndManualStep FindNextPendingManualStep()
        {
            return RunEndToEndManualValidationCatalog.Steps.FirstOrDefault(
                step => session.FindOrCreate(step.Id).status ==
                        RunEndToEndManualStatus.NotRun);
        }

        private void FocusNextPendingManualStep()
        {
            RunEndToEndManualStep next = FindNextPendingManualStep();
            if (next == null)
            {
                EditorUtility.DisplayDialog(
                    "검사 결과",
                    "모든 검사항목의 상태가 기록되었습니다.",
                    "확인");
                focusedStepId = null;
                return;
            }

            focusedStepId = next.Id;
            scroll = Vector2.zero;
            Repaint();
        }

        private void DrawGeneralNotes()
        {
            EditorGUILayout.LabelField("전체 메모", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            string notes = EditorGUILayout.TextArea(
                session.generalNotes ?? string.Empty,
                GUILayout.MinHeight(100f));
            if (EditorGUI.EndChangeCheck())
            {
                session.generalNotes = notes;
                TouchAndSave();
            }
        }

        private void RunAutomatedPreflight()
        {
            if (BattleScreenCompleteHarnessValidation.TryGetRecentPass(
                    TimeSpan.FromMinutes(30),
                    out string recentSummary))
            {
                int choice = EditorUtility.DisplayDialogComplex(
                    "전체 자동 검사",
                    recentSummary + "\n\n이미 통과한 결과를 사용할 수 있습니다.",
                    "최근 결과 사용",
                    "취소",
                    "다시 실행");
                if (choice == 1)
                {
                    return;
                }

                if (choice == 0)
                {
                    ApplyPreflightResult(true, recentSummary);
                    return;
                }
            }
            else if (!EditorUtility.DisplayDialog(
                         "전체 자동 검사 실행",
                         "21개 항목을 전체 회귀 하네스로 자동 판정합니다. " +
                         "Unity가 검사 중 응답하지 않는 것처럼 보일 수 있습니다.",
                         "실행",
                         "취소"))
            {
                return;
            }

            System.Diagnostics.Stopwatch stopwatch =
                System.Diagnostics.Stopwatch.StartNew();
            bool passed = false;
            string note;
            try
            {
                EditorUtility.DisplayProgressBar(
                    "전체 자동 검사",
                    "21개 엔드투엔드 항목을 자동 판정하고 있습니다.",
                    0.5f);
                passed = BattleScreenCompleteHarnessValidation.Run();
                note = passed
                    ? $"자동 통합 하네스 통과 · {stopwatch.Elapsed.TotalSeconds:F1}초"
                    : $"자동 통합 하네스 실패 · {stopwatch.Elapsed.TotalSeconds:F1}초";
            }
            catch (Exception exception)
            {
                note = "자동 통합 하네스 예외 · " +
                       exception.GetType().Name + ": " + exception.Message;
                Debug.LogException(exception);
            }
            finally
            {
                stopwatch.Stop();
                EditorUtility.ClearProgressBar();
            }

            ApplyPreflightResult(passed, note);
        }

        private void ApplyPreflightResult(bool passed, string note)
        {
            string updatedAt = DateTime.UtcNow.ToString("O");
            foreach (RunEndToEndManualStep step in
                     RunEndToEndManualValidationCatalog.Steps)
            {
                RunEndToEndManualStepResult result =
                    session.FindOrCreate(step.Id);
                result.status = passed
                    ? RunEndToEndManualStatus.Passed
                    : step.Id == "preflight-harness"
                        ? RunEndToEndManualStatus.Failed
                        : RunEndToEndManualStatus.Blocked;
                result.note = step.Id == "preflight-harness"
                    ? note
                    : passed
                        ? "전체 자동 하네스에서 해당 흐름 검증 통과"
                        : "전체 자동 하네스 실패로 개별 자동 판정 차단";
                result.updatedAtUtc = updatedAt;
            }
            TouchAndSave();
            Repaint();
        }

        private void StartNewSessionWithConfirmation()
        {
            if (!EditorUtility.DisplayDialog(
                    "새 검증 세션",
                    "현재 체크 상태와 메모를 초기화합니다. 내보내지 않은 기록은 " +
                    "복구할 수 없습니다.",
                    "초기화",
                    "취소"))
            {
                return;
            }

            session = CreateSession();
            SaveSession();
            Repaint();
        }

        private void ExportReport()
        {
            TouchAndSave();
            string suggestedName =
                $"HaveABreak-Manual-E2E-{DateTime.Now:yyyyMMdd-HHmmss}.md";
            string path = EditorUtility.SaveFilePanel(
                "한 런 수동 E2E 검증 보고서 저장",
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                suggestedName,
                "md");
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            File.WriteAllText(
                path,
                RunEndToEndManualReportBuilder.Build(session));
            EditorUtility.RevealInFinder(path);
            Debug.Log($"Manual run E2E report exported: {path}");
        }

        private void LoadSession()
        {
            string json = EditorPrefs.GetString(PreferenceKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    session = JsonUtility.FromJson<RunEndToEndManualSession>(json);
                }
                catch (ArgumentException)
                {
                    session = null;
                }
            }
            EnsureSession();
        }

        private void EnsureSession()
        {
            session ??= CreateSession();
            session.steps ??= new List<RunEndToEndManualStepResult>();
            foreach (RunEndToEndManualStep step in
                     RunEndToEndManualValidationCatalog.Steps)
            {
                session.FindOrCreate(step.Id);
            }
        }

        private static RunEndToEndManualSession CreateSession()
        {
            string now = DateTime.UtcNow.ToString("O");
            RunEndToEndManualSession value = new()
            {
                tester = Environment.UserName,
                startedAtUtc = now,
                updatedAtUtc = now,
                unityVersion = Application.unityVersion,
                projectPath = Application.dataPath,
                branchOrBuild = ResolveCurrentBranch(),
                generalNotes = string.Empty,
                steps = new List<RunEndToEndManualStepResult>()
            };
            foreach (RunEndToEndManualStep step in
                     RunEndToEndManualValidationCatalog.Steps)
            {
                value.FindOrCreate(step.Id);
            }
            return value;
        }

        private static string ResolveCurrentBranch()
        {
            string projectRoot =
                Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                return "unknown";
            }

            try
            {
                System.Diagnostics.ProcessStartInfo startInfo = new()
                {
                    FileName = "git",
                    Arguments = "rev-parse --abbrev-ref HEAD",
                    WorkingDirectory = projectRoot,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using System.Diagnostics.Process process =
                    System.Diagnostics.Process.Start(startInfo);
                if (process == null || !process.WaitForExit(3000) ||
                    process.ExitCode != 0)
                {
                    return "unknown";
                }

                string branch = process.StandardOutput.ReadToEnd().Trim();
                return string.IsNullOrWhiteSpace(branch)
                    ? "unknown"
                    : branch;
            }
            catch (Exception)
            {
                return "unknown";
            }
        }

        private void SaveSession()
        {
            if (session == null)
            {
                return;
            }
            EditorPrefs.SetString(
                PreferenceKey,
                JsonUtility.ToJson(session));
        }

        private void TouchAndSave()
        {
            session.Touch();
            SaveSession();
        }

        private void InitializeFoldouts()
        {
            foreach (string section in RunEndToEndManualValidationCatalog.Steps
                         .Select(step => step.Section)
                         .Distinct())
            {
                sectionFoldouts[section] = true;
            }
        }

        private string SectionLabel(
            string section,
            IEnumerable<RunEndToEndManualStep> steps)
        {
            RunEndToEndManualStep[] values = steps.ToArray();
            int passed = values.Count(step => session.FindOrCreate(step.Id).status ==
                                              RunEndToEndManualStatus.Passed);
            int completed = values.Count(step => session.FindOrCreate(step.Id).status !=
                                                 RunEndToEndManualStatus.NotRun);
            return $"{section} · 진행 {completed}/{values.Length} · 통과 {passed}";
        }
    }
}

