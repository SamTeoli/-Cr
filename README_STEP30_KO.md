# 30단계 — C03 좌석 수리공 턴 종료 효과

## 적용 방법

1. Unity를 종료합니다.
2. ZIP의 `Assets` 폴더를 프로젝트 루트에 덮어씁니다.
3. Unity를 열고 컴파일 완료를 기다립니다.
4. `Have a Break > Validate C03 Seat Repairer Turn End Effect`를 실행합니다.
5. `C03 Seat Repairer turn end effect passed.`를 확인합니다.

## 구현 범위

- 플레이어 턴 종료 시 현재 턴의 `AttackCompleted` 기록을 확인
- 공격 선언만 있고 완료되지 않은 공격은 공격하지 않은 것으로 판정
- 이전 턴의 공격 완료 기록은 현재 턴 판정에서 제외
- 레벨 1~2: 공격하지 않았다면 방어 3
- 레벨 3~4: 공격하지 않았다면 방어 4
- 레벨 5: 방어 4와 반격 1
- 같은 카드·같은 플레이어 턴의 중복 해결 방지
- 원본 카드 데이터는 변경하지 않음

## GitHub Desktop Summary

`C03 좌석 수리공 턴 종료 방어 효과 구현`
