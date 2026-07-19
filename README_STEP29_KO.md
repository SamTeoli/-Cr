# 29단계 — C02 등불 짐꾼 소환 효과

## 적용 방법

1. Unity를 종료합니다.
2. ZIP의 `Assets` 폴더를 프로젝트 루트에 덮어씁니다.
3. Unity를 열고 컴파일 완료를 기다립니다.
4. `Have a Break > Validate C02 Lantern Bearer Summon Effect`를 실행합니다.
5. `C02 Lantern Bearer summon effect passed.`를 확인합니다.

## 구현 범위

- 소환 시 이번 턴 다음 스킬 비용 1 감소(최소 0)
- 미리보기에는 반영하지만 소비하지 않음
- 스킬 행동이 확정된 뒤 소비하며, 이후 효과 무효화 시에도 반환하지 않음
- 사용하지 않은 감소 효과는 플레이어 턴 종료 시 제거
- 레벨 4 이상: 소환 후 자신 방어 1
- 레벨 5: 비용 감소를 받은 스킬의 첫 수치형 효과 +1을 1회 조회 가능
- 기존 인챈트 비용 계산 후 C02 감소를 적용하여 0까지 감소 가능

## GitHub Desktop Summary

`Implement C02 Lantern Bearer skill discount effect`
