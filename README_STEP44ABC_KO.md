# STEP 44A~44C: 인카운터 생명주기·정산·보상 연결

## 구현 범위

- 인카운터 데이터에 기존 `BattleEncounterGrade`를 연결합니다.
- 부트스트랩 뒤 시작 손패 확정까지 한 번에 전투를 시작합니다.
- 플레이어 행동 중 마지막 적이 쓰러진 경우 세션 승리를 즉시 확정합니다.
- 기존 `BattleSettlementService`를 통해 전투 체력을 런 상태에 반영합니다.
- 패배 시 런 종료 상태를 반영하고 승리 보상을 차단합니다.
- 승리 시 기존 `BattleVictoryRewardService`를 연결하고 중복 골드 지급을 차단합니다.
- 실제 적·인카운터 에셋과 새로운 보상 수치는 만들지 않았습니다.
- 카드 C01~C12와 인챈트 E01~E08 데이터는 변경하지 않았습니다.

## Unity 검증

1. `agent/step44abc-encounter-lifecycle` 브랜치로 전환합니다.
2. Unity 컴파일과 Console 오류 0개를 확인합니다.
3. `Have a Break > Validate Full Battle Runtime C01-C12`를 실행합니다.
4. 아래 문구가 나오면 성공입니다.

`Full battle runtime regression C01-C12, encounter lifecycle, settlement, and rewards passed.`
