# STEP 43A~43C: 적·인카운터 데이터와 전투 부트스트랩

## 구현 범위

- 적 ID, 표시 이름, 기본 공격력과 최대 체력을 보존하는 적 정의를 추가합니다.
- 적 인스턴스 ID와 왼쪽·중앙·오른쪽 위치를 보존하는 인카운터를 추가합니다.
- 중복 적 인스턴스 ID, 중복 위치, 빈 ID와 잘못된 수치를 검증합니다.
- 기존 `BattleCardInstance` 덱과 `RunBattleState`를 사용해 전투 런타임과 세션을 생성합니다.
- 런의 최대 체력과 현재 체력을 전투 플레이어 상태에 그대로 반영합니다.
- 실제 적 에셋과 수치는 만들지 않았습니다.
- 카드 C01~C12와 인챈트 E01~E08 데이터는 변경하지 않았습니다.

## Unity 검증

1. `agent/step43abc-encounter-bootstrap` 브랜치로 전환합니다.
2. Unity 컴파일과 Console 오류 0개를 확인합니다.
3. `Have a Break > Validate Full Battle Runtime C01-C12`를 실행합니다.
4. 아래 문구가 나오면 성공입니다.

`Full battle runtime regression C01-C12, encounter bootstrap, and multi-round sessions passed.`
