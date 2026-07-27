using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace HaveABreak.EditorTools
{
    internal enum RunEndToEndManualStatus
    {
        NotRun,
        Passed,
        Failed,
        Blocked
    }

    [Serializable]
    internal sealed class RunEndToEndManualStepResult
    {
        public string stepId;
        public RunEndToEndManualStatus status;
        public string note;
        public string updatedAtUtc;
    }

    [Serializable]
    internal sealed class RunEndToEndManualSession
    {
        public string tester;
        public string startedAtUtc;
        public string updatedAtUtc;
        public string unityVersion;
        public string projectPath;
        public string branchOrBuild;
        public string generalNotes;
        public List<RunEndToEndManualStepResult> steps = new();

        internal RunEndToEndManualStepResult FindOrCreate(string stepId)
        {
            RunEndToEndManualStepResult result = steps.FirstOrDefault(value =>
                value != null && string.Equals(
                    value.stepId,
                    stepId,
                    StringComparison.Ordinal));
            if (result != null)
            {
                return result;
            }

            result = new RunEndToEndManualStepResult
            {
                stepId = stepId,
                status = RunEndToEndManualStatus.NotRun,
                note = string.Empty,
                updatedAtUtc = string.Empty
            };
            steps.Add(result);
            return result;
        }

        internal void Touch()
        {
            updatedAtUtc = DateTime.UtcNow.ToString("O");
        }
    }

    internal sealed class RunEndToEndManualStep
    {
        internal RunEndToEndManualStep(
            string id,
            string section,
            string title,
            string action,
            string expected,
            string evidence)
        {
            Id = id;
            Section = section;
            Title = title;
            Action = action;
            Expected = expected;
            Evidence = evidence;
        }

        internal string Id { get; }
        internal string Section { get; }
        internal string Title { get; }
        internal string Action { get; }
        internal string Expected { get; }
        internal string Evidence { get; }
    }

    internal static class RunEndToEndManualValidationCatalog
    {
        internal static readonly RunEndToEndManualStep[] Steps =
        {
            new(
                "preflight-harness",
                "사전 검사",
                "전체 자동 하네스",
                "Have a Break > Tests > Run Complete Test Harness With Battle Screen을 실행한다.",
                "Console 오류와 경고가 0이고 최종 통합 성공 로그가 출력된다.",
                "최종 성공 로그 또는 Console 캡처"),
            new(
                "new-run-confirmation",
                "런 시작",
                "새 런 덮어쓰기 확인",
                "기존 저장이 있는 상태에서 새 런을 누르고 취소한 뒤 다시 확인한다.",
                "취소 시 현재 진행이 유지되고, 확인 시에만 새 런 준비 화면으로 이동한다.",
                "확인 팝업과 취소 후 상태"),
            new(
                "deck-preparation",
                "런 시작",
                "덱 준비와 선택 순서",
                "보유카드를 서로 다른 순서로 선택하고 선택한 덱으로 런을 시작한다.",
                "선택 수량과 순서가 표시되며 확정된 런 덱 순서가 선택 순서와 일치한다.",
                "선택 순서와 런 덱 표시"),
            new(
                "node-selection",
                "런 진행",
                "노드 선택과 진행도",
                "연결된 노드를 선택하고 최소 두 노드를 완료한다.",
                "선택하지 않은 노드로 이동하지 않으며 완료 노드 수와 막 진행도가 증가한다.",
                "노드 ID와 완료 수"),
            new(
                "shop-flow",
                "비전투 노드",
                "상점 구매와 리롤",
                "소모아이템 또는 인첸트를 구매하고 전체 리롤 후 상점을 나간다.",
                "골드가 정확히 차감되고 구매 슬롯과 리롤 상품이 갱신되며 노드가 완료된다.",
                "구매 전후 골드와 상품"),
            new(
                "situation-event",
                "비전투 노드",
                "상황 이벤트 선택",
                "상황 이벤트의 선택지 하나를 실행한다.",
                "선택 효과가 한 번만 적용되고 이벤트 노드가 완료된다.",
                "선택 문구와 적용 결과"),
            new(
                "rest-or-upgrade",
                "비전투 노드",
                "회복 또는 카드 강화",
                "회복과 강화 중 하나를 선택한다. 별도 런에서는 반대 선택도 확인한다.",
                "회복량 또는 카드 레벨이 규칙에 맞게 변경되고 노드가 완료된다.",
                "선택 전후 HP 또는 카드 레벨"),
            new(
                "battle-start",
                "전투",
                "조우 시작과 초기 상태",
                "전투 노드에 진입해 패, 마력, 적 배치와 시작 체크포인트를 확인한다.",
                "시작 패 5장, 최대 마력 기준 충전, 설정된 적 배치가 표시되고 저장 오류가 없다.",
                "전투 상단 요약과 적 필드"),
            new(
                "enemy-target-and-attack",
                "전투",
                "적 선택과 아군 공격",
                "적을 선택하고 소환한 아군 몬스터로 공격한다.",
                "선택 표시가 한 적에만 적용되고 피해·공격 횟수·전투 기록이 갱신된다.",
                "공격 전후 적 HP와 최근 기록"),
            new(
                "card-play-boundaries",
                "전투",
                "카드 사용 경계",
                "마력 부족, 필드 포화 또는 대상 누락 상태를 만든 뒤 카드를 사용하고 정상 상태에서도 사용한다.",
                "불가능한 사용은 차단 사유가 표시되고 상태가 보존되며 정상 사용만 자원과 영역을 변경한다.",
                "차단 문구와 영역·마력 변화"),
            new(
                "c07-banish-selection",
                "전투",
                "C07 소멸 대상",
                "C07의 소멸 대상을 변경한 뒤 사용한다.",
                "선택한 카드만 소멸 영역으로 이동하고 C07은 정상 소비되며 선택 상태가 초기화된다.",
                "사용 전 대상과 사용 후 소멸 영역"),
            new(
                "battle-consumable",
                "전투",
                "전투 소모아이템",
                "사용 가능한 전투 소모아이템을 사용하고 같은 아이템을 다시 사용한다.",
                "첫 사용만 적용되고 수량이 감소하며 남은 수량이 없으면 재사용이 차단된다.",
                "사용 전후 수량과 적용량"),
            new(
                "checkpoint-continue",
                "저장·이어하기",
                "전투 체크포인트 재시작",
                "전투 중 몇 가지 행동 후 수동 저장하고 이어하기를 실행한다.",
                "현재 전투 진행은 저장되지 않고 동일 조우의 전투 시작 상태에서 재개된다.",
                "저장 전 진행 상태와 재개 후 시작 상태"),
            new(
                "turn-and-enemy-flow",
                "전투",
                "턴 종료와 적 행동",
                "플레이어 턴을 종료하고 이동·공격·능력이 포함된 적 턴을 확인한다.",
                "적이 좌→우 순서와 이동 우선 규칙으로 행동하고 다음 플레이어 턴이 시작된다.",
                "적 의도와 행동 후 필드·HP·턴 번호"),
            new(
                "victory-settlement",
                "정산·보상",
                "승리 정산과 골드",
                "일반 전투에서 모든 적을 제거하고 전투 정산을 실행한다.",
                "골드가 한 번만 수령되고 캠페인이 보상 단계로 전환되며 중복 정산이 차단된다.",
                "정산 전후 골드와 캠페인 단계"),
            new(
                "battle-rewards",
                "정산·보상",
                "인첸트·소모아이템 보상",
                "필수 인첸트와 소모아이템 보상을 선택하고 다음 노드로 이동한다.",
                "선택한 보상만 적용되고 필수 보상 전에는 완료 버튼이 차단된다.",
                "보상 선택과 카드·인벤토리 변화"),
            new(
                "run-consumables",
                "런 관리",
                "인첸트 망치와 변이 주문서",
                "망치로 카드 슬롯을 늘리고 변이 주문서로 장착 인첸트를 교체한다.",
                "슬롯 상한과 대상 제한이 적용되고 아이템은 성공 시에만 한 개 소비된다.",
                "사용 전후 슬롯·인첸트·아이템 수량"),
            new(
                "defeat-flow",
                "패배",
                "패배 정산과 런 종료",
                "별도 검증 런에서 플레이어 HP를 0으로 만들고 정산한다.",
                "활성 조우가 폐기되고 캠페인이 패배 단계로 전환되며 런이 종료된다.",
                "패배 결과와 활성 조우·캠페인 상태"),
            new(
                "final-boss-permanent-reward",
                "런 완료",
                "최종 보스와 영구 보상",
                "최종 보스를 처치하고 정산·보상을 완료한다.",
                "최종 보스 영구 보상이 한 번만 수령되고 캠페인이 완료 단계로 전환된다.",
                "영구 보상 ID와 완료 단계"),
            new(
                "restart-and-persistence",
                "런 완료",
                "에디터 재시작 후 영속성",
                "Unity를 종료 후 다시 열어 이어하기와 영구 보상 상태를 확인한다.",
                "완료된 런 또는 의도된 저장 슬롯 상태가 복원되고 영구 보상이 유지된다.",
                "재시작 전후 저장 슬롯과 영구 보상"),
            new(
                "full-run-summary",
                "완료",
                "한 런 결과 요약",
                "검증 보고서의 모든 실패·차단 항목에 재현 절차와 증거를 기록한다.",
                "미실행 항목이 없고 실패·차단 항목이 후속 작업으로 식별된다.",
                "내보낸 Markdown 보고서")
        };
    }

    internal static class RunEndToEndManualReportBuilder
    {
        internal static string Build(RunEndToEndManualSession session)
        {
            session ??= new RunEndToEndManualSession();
            StringBuilder builder = new();
            builder.AppendLine("# Have a Break 한 런 자동 E2E 검증 보고서");
            builder.AppendLine();
            builder.AppendLine($"- 검사자: {Value(session.tester)}");
            builder.AppendLine($"- 시작 UTC: {Value(session.startedAtUtc)}");
            builder.AppendLine($"- 갱신 UTC: {Value(session.updatedAtUtc)}");
            builder.AppendLine($"- Unity: {Value(session.unityVersion)}");
            builder.AppendLine($"- 브랜치/빌드: {Value(session.branchOrBuild)}");
            builder.AppendLine($"- 프로젝트: {Value(session.projectPath)}");
            builder.AppendLine("- 판정 방식: 전체 자동 회귀 하네스");
            builder.AppendLine();

            int passed = Count(session, RunEndToEndManualStatus.Passed);
            int failed = Count(session, RunEndToEndManualStatus.Failed);
            int blocked = Count(session, RunEndToEndManualStatus.Blocked);
            int notRun = Count(session, RunEndToEndManualStatus.NotRun);
            builder.AppendLine("## 요약");
            builder.AppendLine();
            builder.AppendLine(
                $"- 통과 {passed} / 실패 {failed} / 차단 {blocked} / 미실행 {notRun}");
            builder.AppendLine();

            foreach (IGrouping<string, RunEndToEndManualStep> section in
                     RunEndToEndManualValidationCatalog.Steps.GroupBy(step => step.Section))
            {
                builder.AppendLine($"## {section.Key}");
                builder.AppendLine();
                foreach (RunEndToEndManualStep step in section)
                {
                    RunEndToEndManualStepResult result = session.FindOrCreate(step.Id);
                    builder.AppendLine($"### [{StatusMark(result.status)}] {step.Title}");
                    builder.AppendLine();
                    builder.AppendLine($"- ID: `{step.Id}`");
                    builder.AppendLine($"- 상태: {StatusLabel(result.status)}");
                    builder.AppendLine($"- 검증 범위: {step.Action}");
                    builder.AppendLine($"- 기대 결과: {step.Expected}");
                    builder.AppendLine($"- 증거: {step.Evidence}");
                    builder.AppendLine($"- 메모: {Value(result.note)}");
                    builder.AppendLine($"- 갱신 UTC: {Value(result.updatedAtUtc)}");
                    builder.AppendLine();
                }
            }

            builder.AppendLine("## 전체 메모");
            builder.AppendLine();
            builder.AppendLine(Value(session.generalNotes));
            return builder.ToString();
        }

        internal static string StatusLabel(RunEndToEndManualStatus status)
        {
            return status switch
            {
                RunEndToEndManualStatus.Passed => "통과",
                RunEndToEndManualStatus.Failed => "실패",
                RunEndToEndManualStatus.Blocked => "차단",
                _ => "미실행"
            };
        }

        private static int Count(
            RunEndToEndManualSession session,
            RunEndToEndManualStatus status)
        {
            return RunEndToEndManualValidationCatalog.Steps.Count(step =>
                session.FindOrCreate(step.Id).status == status);
        }

        private static string StatusMark(RunEndToEndManualStatus status)
        {
            return status == RunEndToEndManualStatus.Passed ? "x" : " ";
        }

        private static string Value(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
        }
    }
}

