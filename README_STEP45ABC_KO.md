# STEP 45A~45C: 런 덱과 전투 스냅샷

## 구현 범위

- 런 동안 보유카드ID, 카드 레벨과 인챈트 슬롯 상태를 유지합니다.
- 같은 보유카드ID가 런 덱에 중복 등록되는 것을 차단합니다.
- 전투 인스턴스ID와 보유카드ID로 전투카드ID를 결정적으로 생성합니다.
- 전투가 바뀌면 전투카드ID만 바뀌고 수록카드ID와 보유카드ID는 유지됩니다.
- 런 카드 레벨과 드로우 더미 시작 영역을 전투 카드에 반영합니다.
- 런 카드의 인챈트 상태를 기존 전투 인챈트 레지스트리에 연결합니다.
- 덱 장수 제한 등 확정되지 않은 규칙은 추가하지 않았습니다.
- 카드 C01~C12와 인챈트 E01~E08 데이터는 변경하지 않았습니다.

## Unity 검증

1. `agent/step45abc-run-deck-snapshot` 브랜치로 전환합니다.
2. Unity 컴파일과 Console 오류 0개를 확인합니다.
3. `Have a Break > Validate Full Battle Runtime C01-C12`를 실행합니다.
4. 아래 문구가 나오면 성공입니다.

`Full battle runtime regression C01-C12, run deck snapshots, enchants, and encounter flow passed.`
