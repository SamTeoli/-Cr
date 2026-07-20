# Step 36B - 적 이동과 C08 자동 발동

이 단계는 적 턴의 이동 명령을 전투 런타임에 연결합니다. 이동 전에 설치 등록부의 C08을 역순으로 확인하고, 발동하면 이동을 0으로 대체합니다. 실제로 이동한 적과 밀려난 적은 `EnemyMoved` 사건을 만들며 C04와 C12 반응을 이어서 처리합니다.

## GitHub 직접 작업 방식

1. GitHub Desktop에서 `Fetch origin`을 누릅니다.
2. Current branch에서 `agent/step36b-enemy-move-c08`을 선택합니다.
3. Unity를 열고 컴파일이 끝날 때까지 기다립니다.
4. `Have a Break > Validate Runtime Enemy Move And C08`을 실행합니다.

성공 메시지:

`Battle runtime enemy move and C08 passed.`

## 검증 범위

- 적 턴이 아닌 이동 요청 차단
- C08 자동 탐색과 이동 수치 0 대체
- C08 속박·약화 적용
- 적 1칸 이동과 위치 사건 기록
- 이동 후 C04 공격 강화와 C12 취약·피해 연결
- 기존 카드 데이터와 확정 효과 내용 변경 없음
