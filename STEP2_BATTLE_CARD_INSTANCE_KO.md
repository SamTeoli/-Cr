# 2단계 · 전투 카드 인스턴스 구조

## 적용

ZIP의 내용을 Unity 프로젝트 최상위 폴더에 덮어쓴다. 기존 `Assets` 폴더와 합쳐져야 한다.

## 추가된 구조

- `CardInstanceIds`: Unity 직렬화 가능한 수록·보유·전투·인스턴트 ID 묶음
- `CardZone`: 드로우 더미, 패, 몬스터 필드, 스킬 필드, 묘지, 소멸 영역
- `BattleCardInstance`: 전투 중 현재 레벨, 영역, 원본 카드와 ID 관리
- 임시 생성·복제 카드: `InstantId`가 있을 때 `IsTemporary = true`

## Unity 확인

1. 스크립트 임포트가 끝날 때까지 기다린다.
2. Console에 빨간 컴파일 오류가 없는지 확인한다.
3. `Have a Break > Card Data Tools`를 연다.
4. `Validate Cards`와 `Validate Level Resolution`을 차례로 실행한다.
5. `Validate Battle Card Instances`를 실행한다.
6. `Battle card instances passed.`가 표시되는지 확인한다.

자동검사는 C01 일반 보유 카드의 레벨 5 수치와 패→몬스터 필드 이동, C05 임시 카드의
레벨 하한 보정과 소멸 영역 이동을 확인한다.

이번 단계에는 전투 덱 생성, 드로우, 셔플과 효과 실행이 포함되지 않는다.
