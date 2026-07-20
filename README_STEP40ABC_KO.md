# STEP 40A~40C: 자동공격 적 턴 파이프라인

## 구현 범위

- 자동 대상 공격 명령은 공격 횟수와 타격별 동률 판정값을 외부에서 받습니다.
- 기존 고정 대상 공격 명령은 그대로 유지됩니다.
- 순서화된 적 턴 파이프라인에서 이동, 자동·연속 공격, 능력을 실행합니다.
- 연속공격 도중 플레이어가 패배하면 남은 타격과 후속 적 행동을 중단합니다.
- 카드 C01~C12와 인챈트 E01~E08 데이터는 변경하지 않았습니다.

## Unity 검증

1. `agent/step40abc-automatic-turn-pipeline` 브랜치로 전환합니다.
2. Unity 컴파일과 Console 오류 0개를 확인합니다.
3. `Have a Break > Validate Full Battle Runtime C01-C12`를 실행합니다.
4. 아래 문구가 나오면 성공입니다.

`Full battle runtime regression C01-C12, automatic enemy turns, and defeat stopping passed.`
