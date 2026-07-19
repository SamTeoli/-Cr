# -Cr

카드 수집형 덱빌딩 로그라이크 **Have a Break, and then..**

## 카드 데이터 시작하기

1. Unity에서 프로젝트를 연다.
2. 상단 메뉴에서 `Have a Break > Card Data Tools`를 선택한다.
3. 카드 종류, 수록카드ID, 이름, 등급, 마력 코스트를 입력하고 `Create Card Asset`을 누른다.
4. 생성된 카드의 상세 수치와 효과를 Inspector에서 편집한다.
5. `Validate Cards`로 누락·중복 ID를 검사한다.
6. `Rebuild Card Database`로 `Assets/GameData/CardDatabase.asset`을 갱신한다.

카드 에셋은 기본적으로 `Assets/GameData/Cards`에 생성된다. 카드 종류는 몬스터, 스킬,
트랩, 결계이며 등급은 일반, 희귀, 전설이다. 코드의 영문 enum 이름 `Barrier`는 게임 내
표시 용어인 **결계**에 대응한다.

ID 계층은 수록카드ID(`CatalogCardId`), 보유카드ID(`OwnedCardId`),
전투카드ID(`BattleCardId`), 전투 중 임시 생성·복제용 `InstantId` 순으로 분리한다.

## 시험 카드 C01~C12

`Assets/GameData/Cards`에 Google Docs `카드 모음집 · C01~C12` v0.4를 기준으로 한
시험 카드 12종이 등록되어 있다. 각 에셋은 기본 카드 정보와 레벨 1~5의 비용·공격력·
생명력·효과 문구를 보유한다. `CardDatabase.asset`에도 C01부터 C12까지 ID 순으로 연결되어
있다. 효과 문구는 기획 원문을 보존한 데이터이며 실제 전투 효과 실행기는 후속 단계에서
연결한다.

`CardData.ResolveLevel(level)`은 요청 레벨을 1~5로 보정하고 해당 레벨의 비용·공격력·
생명력·효과 문구를 `ResolvedCardData`로 반환한다. Editor 도구의
`Validate Level Resolution`으로 대표 카드와 범위 보정을 검사할 수 있다.
