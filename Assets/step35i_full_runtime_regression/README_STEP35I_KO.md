# Step 35I - C01~C12 전체 전투 런타임 회귀 검증

이 단계는 지금까지 개별 연결한 C01~C12 전투 런타임 효과를 한 메뉴에서 다시 검사합니다.

## 적용 방법

1. Unity를 종료합니다.
2. ZIP을 프로젝트 루트에 압축 해제합니다.
3. `Assets` 폴더를 기존 프로젝트의 `Assets` 폴더와 병합합니다.
4. Unity를 열고 컴파일이 끝날 때까지 기다립니다.
5. `Have a Break > Validate Full Battle Runtime C01-C12`를 실행합니다.

성공 메시지:

`Full battle runtime regression C01-C12 passed.`

Console에는 런타임 구성, 카드 사용 이벤트, 소환, 턴 종료, 스킬, 트랩, 이동 반응의 개별 통과 결과도 출력됩니다.

## 변경 범위

- 전체 런타임 회귀 검증 Editor 메뉴 1개 추가
- 기존 C01~C12 카드 데이터, 코스트, 수치, 효과 내용 변경 없음
- 기존 런타임 구현 변경 없음

## GitHub Desktop Summary

`C01-C12 전체 런타임 회귀 검증 추가`
