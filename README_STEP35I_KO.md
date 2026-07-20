# Step 35I 경로 정리 - C01~C12 전체 전투 런타임 회귀 검증

이 정리 파일은 잘못 중첩된 35I 폴더를 제거하고 검증 파일을 정상적인 `Assets/Editor` 경로에 배치합니다.

## 적용 방법

1. Unity를 종료합니다.
2. 프로젝트의 `Assets/step35i_full_runtime_regression` 폴더를 폴더 전체로 삭제합니다.
3. 이 ZIP을 Unity 프로젝트 루트(`Assets`, `Packages`, `ProjectSettings`가 보이는 위치)에 압축 해제합니다.
4. ZIP의 `Assets` 폴더를 기존 프로젝트의 `Assets` 폴더와 병합합니다.
5. 최종 파일 경로가 `Assets/Editor/BattleRuntimeFullRegressionValidation.cs`인지 확인합니다.
6. Unity를 열고 컴파일이 끝날 때까지 기다립니다.
7. `Have a Break > Validate Full Battle Runtime C01-C12`를 실행합니다.

성공 메시지:

`Full battle runtime regression C01-C12 passed.`

Console에는 런타임 구성, 카드 사용 이벤트, 소환, 턴 종료, 스킬, 트랩, 이동 반응의 개별 통과 결과도 출력됩니다.

## 변경 범위

- 35I 전체 런타임 회귀 검증 파일의 위치만 정리
- 기존 C01~C12 카드 데이터, 코스트, 수치, 효과 내용 변경 없음
- 기존 런타임 구현 변경 없음
- 검증 코드 내용 변경 없음

## GitHub Desktop Summary

`35I 전체 런타임 검증 파일 경로 정리`
