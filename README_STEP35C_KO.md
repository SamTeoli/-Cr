# 35C단계 — C01·C02 소환 효과 런타임 연결

35B에서 기록한 MonsterSummoned 사건을 카드 ID에 따라 C01 또는 C02 해결기로 전달합니다.

- C01은 플레이 전에 선언한 고정 대상을 사용합니다.
- C02는 같은 런타임의 다음 스킬 비용 수정 상태를 사용합니다.
- 동일 소환 사건의 중복 해결을 차단합니다.
- 다른 몬스터 카드는 지원하지 않는 카드로 명확히 구분합니다.

## Unity 확인

1. ZIP의 `Assets` 폴더를 프로젝트에 덮어씁니다.
2. 컴파일 완료 후 Console 오류가 없는지 확인합니다.
3. `Have a Break > Validate Runtime C01 C02 Summon Effects`를 실행합니다.
4. `Runtime C01 and C02 summon effects passed.`를 확인합니다.

## GitHub Desktop Summary

`C01 C02 소환 효과 런타임 연결`
