# 31A단계 — C04·C12 적 이동 연계 통합

## 구현 범위

- C04 종점 길고양이: 적 턴의 실제 이동 명령 기준 공격력 강화
- 한 이동 명령의 연쇄 밀림은 C04 발동 1회로 계산
- C04 레벨 3 이상 강화 2, 레벨 5는 적 턴당 두 이동 명령까지 발동
- C12 노선도 위의 별빛: 실제 이동 완료 적에게 취약 부여
- C12 레벨 2 이상 취약 2, 레벨 4 이상 적 턴당 두 이동 완료까지 발동
- C12 레벨 5는 취약 부여 후 피해 1
- C04와 C12가 같은 EnemyMoved 사건을 독립적으로 처리하는 통합 검증
- 기존 약화 상태 파일과 몬스터 전투 상태를 호환 방식으로 확장

## Unity 확인

1. ZIP의 `Assets` 폴더를 프로젝트에 덮어씁니다.
2. 컴파일 완료 후 Console 오류가 없는지 확인합니다.
3. `Have a Break > Validate C04 And C12 Movement Integration`을 실행합니다.
4. `C04 and C12 movement integration passed.`를 확인합니다.

## GitHub Desktop Summary

`C04와 C12 적 이동 연계 효과 통합 구현`
