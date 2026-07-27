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
            "Have a Break/Tests/Open Manual Run E2E Validation";
        private readonly Dictionary<string, bool> sectionFoldouts = new();
        private RunEndToEndManualSession session;
        private Vector2 scroll;
        private GUIStyle wrappedLabel;

        private static string PreferenceKey =>
            "HaveABreak.RunEndToEndManualValidation." + Application.dataPath;

        [MenuItem(MenuPath)]
        public static void ShowWindow()
        {
            RunEndToEndManualValidationWindow window =
                GetWindow<RunEndToEndManualValidationWindow>(
                    "Manual Run E2E");
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
                "한 런 수동 엔드투엔드 검증",
                EditorStyles.largeLabel);
            EditorGUILayout.HelpBox(
                "병합된 main에서 새 런 시작부터 최종 보스·영구 보상까지 " +
                "실제 화면 조작을 기록합니다. 자동 하네스는 사전 조건이며 " +
                "이 창은 수동 플레이 결과와 증거를 남기는 용도입니다.",
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
                    "자동 사전 검사",
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
            int total = RunEndToEndManualValidationCatalog.Steps.Length;
            int completed = RunEndToEndManualValidationCatalog.Steps.Count(step =>
            {
                RunEndToEndManualStatus status =
                    session.FindOrCreate(step.Id).status;
                return status != RunEndToEndManualStatus.NotRun;
            });
            int passed = RunEndToEndManualValidationCatalog.Steps.Count(step =>
                session.FindOrCreate(step.Id).status ==
                RunEndToEndManualStatus.Passed);
            int failed = RunEndToEndManualValidationCatalog.Steps.Count(step =>
                session.FindOrCreate(step.Id).status ==
                RunEndToEndManualStatus.Failed);
            int blocked = RunEndToEndManualValidationCatalog.Steps.Count(step =>
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
                $"진행 {completed}/{total} · 통과 {passed} · 실패 {failed} · 차단 {blocked}");
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
                    "자동 사전 검사",
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
                         "자동 사전 검사 실행",
                         "전체 회귀 하네스를 다시 실행합니다. Unity가 검사 중 " +
                         "응답하지 않는 것처럼 보일 수 있습니다.",
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
                    "자동 사전 검사",
                    "전체 회귀 하네스를 실행하고 있습니다.",
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
            RunEndToEndManualStepResult result =
                session.FindOrCreate("preflight-harness");
            result.status = passed
                ? RunEndToEndManualStatus.Passed
                : RunEndToEndManualStatus.Failed;
            result.note = note;
            result.updatedAtUtc = DateTime.UtcNow.ToString("O");
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
