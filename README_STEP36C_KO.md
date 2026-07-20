# Step 36C - 적 공격 선언과 C09 자동 발동

이 단계는 적 턴의 공격 선언을 전투 런타임에 연결합니다. 공격 대상을 확인한 뒤 `AttackDeclared` 사건을 만들고, 설치 등록부의 C09를 최근 등록 순서부터 자동 확인해 피격 전 방어를 적용합니다.

실제 피해, 방어 소모, 초과 피해의 플레이어 전가와 `AttackCompleted` 사건은 확정 규칙을 보존하기 위해 다음 36D 단계에서 연결합니다.

## GitHub 직접 작업 방식

1. GitHub Desktop에서 `Fetch origin`을 누릅니다.
2. Current branch에서 `agent/step36c-enemy-attack-c09`를 선택합니다.
3. Unity를 열고 컴파일이 끝날 때까지 기다립니다.
4. `Have a Break > Validate Runtime Enemy Attack And C09`를 실행합니다.

성공 메시지:

`Battle runtime enemy attack and C09 passed.`

## 검증 범위

- 적 턴이 아닌 공격 선언 차단
- 살아 있는 적과 몬스터 필드 대상 확인
- 공격 수치 스냅샷과 `AttackDeclared` 사건 기록
- C09 자동 탐색과 방어 5 적용
- C09 레벨 4 이상 방어 유지 표식 적용
- 같은 적 턴의 C09 중복 발동 차단
- 기존 카드 데이터와 확정 효과 내용 변경 없음
