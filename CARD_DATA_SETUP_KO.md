# 카드 데이터 구조 v1 적용 확인

## Unity에서 확인

1. Unity Hub에서 이 프로젝트를 Unity `6000.3.11f1`로 연다.
2. 첫 임포트가 끝날 때까지 기다린다.
3. Console에 빨간 컴파일 오류가 없는지 확인한다.
4. 상단 메뉴 `Have a Break > Card Data Tools`를 연다.
5. Project 창의 `Assets/GameData/Cards`에서 C01~C12 카드 12종을 확인한다.
6. `Validate Cards`를 실행해 `Validation passed (12 card(s))`를 확인한다.
7. `Validate Level Resolution`을 실행해 `Level resolution passed.`를 확인한다.
8. 필요하면 `Rebuild Card Database`를 실행해 데이터베이스를 다시 갱신한다.

## 생성되는 위치

- 카드 에셋: `Assets/GameData/Cards`
- 카드 데이터베이스: `Assets/GameData/CardDatabase.asset`

Unity가 새 스크립트와 폴더의 `.meta` 파일을 자동 생성한다. Unity를 연 뒤 GitHub Desktop에서
새 코드, 생성된 `.meta` 파일, 필요한 카드 에셋을 함께 커밋한다.
