using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.EditorTools;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class RunEndToEndManualValidationToolValidation
    {
        [MenuItem("Have a Break/Validate Manual Run E2E Tool")]
        private static void ValidateFromMenu()
        {
            Debug.Log(Validate()
                ? "Manual run E2E validation tool passed."
                : "Manual run E2E validation tool failed.");
        }

        internal static bool Validate()
        {
            RunEndToEndManualStep[] steps =
                RunEndToEndManualValidationCatalog.Steps;
            if (steps == null || steps.Length < 20 ||
                steps.Any(step => step == null ||
                    string.IsNullOrWhiteSpace(step.Id) ||
                    string.IsNullOrWhiteSpace(step.Section) ||
                    string.IsNullOrWhiteSpace(step.Title) ||
                    string.IsNullOrWhiteSpace(step.Action) ||
                    string.IsNullOrWhiteSpace(step.Expected) ||
                    string.IsNullOrWhiteSpace(step.Evidence)) ||
                steps.Select(step => step.Id)
                    .Distinct(StringComparer.Ordinal).Count() != steps.Length)
            {
                return false;
            }

            string[] requiredIds =
            {
                "preflight-harness",
                "deck-preparation",
                "shop-flow",
                "situation-event",
                "rest-or-upgrade",
                "battle-start",
                "c07-banish-selection",
                "checkpoint-continue",
                "victory-settlement",
                "battle-rewards",
                "defeat-flow",
                "final-boss-permanent-reward",
                "restart-and-persistence",
                "full-run-summary"
            };
            HashSet<string> ids = steps
                .Select(step => step.Id)
                .ToHashSet(StringComparer.Ordinal);
            if (requiredIds.Any(id => !ids.Contains(id)))
            {
                return false;
            }

            RunEndToEndManualSession session = new()
            {
                tester = "Validator",
                startedAtUtc = "2026-07-26T00:00:00.0000000Z",
                updatedAtUtc = "2026-07-26T01:00:00.0000000Z",
                unityVersion = Application.unityVersion,
                projectPath = Application.dataPath,
                branchOrBuild = "main",
                generalNotes = "전체 메모",
                steps = new List<RunEndToEndManualStepResult>()
            };
            foreach (RunEndToEndManualStep step in steps)
            {
                RunEndToEndManualStepResult result =
                    session.FindOrCreate(step.Id);
                result.status = step.Id == "preflight-harness"
                    ? RunEndToEndManualStatus.Passed
                    : step.Id == "defeat-flow"
                        ? RunEndToEndManualStatus.Blocked
                        : RunEndToEndManualStatus.NotRun;
                result.note = $"note:{step.Id}";
                result.updatedAtUtc = "2026-07-26T01:00:00.0000000Z";
            }

            string json = JsonUtility.ToJson(session);
            RunEndToEndManualSession restored =
                JsonUtility.FromJson<RunEndToEndManualSession>(json);
            if (restored == null || restored.steps == null ||
                restored.steps.Count != steps.Length ||
                restored.FindOrCreate("preflight-harness").status !=
                    RunEndToEndManualStatus.Passed ||
                restored.FindOrCreate("defeat-flow").status !=
                    RunEndToEndManualStatus.Blocked ||
                restored.generalNotes != "전체 메모")
            {
                return false;
            }

            string report = RunEndToEndManualReportBuilder.Build(restored);
            return !string.IsNullOrWhiteSpace(report) &&
                   report.Contains(
                       "# Have a Break 한 런 자동 E2E 검증 보고서",
                       StringComparison.Ordinal) &&
                   report.Contains(
                       "판정 방식: 전체 자동 회귀 하네스",
                       StringComparison.Ordinal) &&
                   report.Contains(
                       "통과 1 / 실패 0 / 차단 1",
                       StringComparison.Ordinal) &&
                   steps.All(step =>
                       report.Contains($"`{step.Id}`", StringComparison.Ordinal) &&
                       report.Contains(step.Title, StringComparison.Ordinal));
        }
    }
}

