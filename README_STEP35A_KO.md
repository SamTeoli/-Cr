# 35A단계 — 전투 런타임 상태 통합

개별 카드 해결기를 실제 전투 흐름에 연결하기 전, 덱·턴·마나·카드 인챈트·아군 몬스터·
적 위치·적 상태·이벤트 로그·중복 해결 추적기를 하나의 전투 단위로 구성합니다.

카드 데이터와 기존 C01~C12 및 E01~E08 효과는 변경하지 않습니다.

## Unity 확인

1. ZIP의 `Assets` 폴더를 프로젝트에 덮어씁니다.
2. 컴파일 완료 후 Console 오류가 없는지 확인합니다.
3. `Have a Break > Validate Battle Runtime State Composition`을 실행합니다.
4. `Battle runtime state composition passed.`를 확인합니다.

## GitHub Desktop Summary

`전투 런타임 상태 통합 기반 추가`
