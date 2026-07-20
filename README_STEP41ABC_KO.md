# STEP 41A~41C: 완전한 전투 라운드 실행

## 구현 범위

- 플레이어 행동 단계에서 턴 종료 효과를 처리합니다.
- 외부에서 전달된 적 명령을 계획·정렬하고 적 턴 파이프라인으로 실행합니다.
- 전투가 진행 중이면 다음 플레이어 턴을 시작합니다.
- 플레이어가 패배하면 다음 턴을 시작하지 않습니다.
- 잘못된 턴 단계와 이미 끝난 전투의 라운드 실행을 거부합니다.
- 카드 C01~C12와 인챈트 E01~E08 데이터는 변경하지 않았습니다.

## Unity 검증

1. `agent/step41abc-complete-battle-round` 브랜치로 전환합니다.
2. Unity 컴파일과 Console 오류 0개를 확인합니다.
3. `Have a Break > Validate Full Battle Runtime C01-C12`를 실행합니다.
4. 아래 문구가 나오면 성공입니다.

`Full battle runtime regression C01-C12, complete rounds, and defeat stopping passed.`
