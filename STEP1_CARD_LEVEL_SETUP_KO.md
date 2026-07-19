# 1단계 · 카드 레벨 적용 시스템

## 적용

이 ZIP의 내용을 Unity 프로젝트 최상위 폴더에 덮어쓴다. `Assets` 폴더가 기존
프로젝트의 `Assets` 폴더와 합쳐져야 한다.

## Unity 확인

1. Unity가 스크립트 임포트를 마칠 때까지 기다린다.
2. Console에 빨간 컴파일 오류가 없는지 확인한다.
3. `Have a Break > Card Data Tools`를 연다.
4. `Validate Cards`를 눌러 `Validation passed (12 card(s)).`를 확인한다.
5. `Validate Level Resolution`을 눌러 `Level resolution passed.`를 확인한다.

## 자동검사 기준

- C01 레벨 5: 비용 3, 공격력 4, 생명력 8
- C05 레벨 3: 비용 0
- C10 레벨 2: 비용 1
- C12 레벨 5: 비용 1, `취약 부여 후 피해 1`
- 요청 레벨 0: 레벨 1로 보정
- 요청 레벨 6: 레벨 5로 보정

이번 단계는 레벨 데이터 조회와 최종 수치 반환까지만 담당한다. 카드 효과 실행,
런스테이지와 배틀스테이지 연결은 포함하지 않는다.
