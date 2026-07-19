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

`BattleCardInstance`는 전투 중 카드의 원본 수록카드, 보유카드ID, 전투카드ID,
인스턴트ID, 현재 레벨과 현재 영역을 관리한다. 전투 중 생성·복제된 카드는
`InstantId`로 임시 카드임을 구분한다.

`BattleCardZoneState`는 전투 카드 인스턴스를 전투카드ID로 한 번만 등록하고 카드 영역별
조회와 이동을 담당한다. 패는 10장, 몬스터 필드와 스킬 필드는 각각 3장으로 제한하며,
용량 초과와 중복 전투카드ID는 상태를 변경하지 않고 실패 사유를 반환한다.

`BattleDeckState`는 전투 시작 시 시드 기반으로 드로우 더미를 섞고 시작 패 5장을
뽑는다. 플레이어의 첫 턴 드로우는 건너뛰며 이후 턴부터 1장씩 뽑는다. 드로우 더미가
비면 묘지를 같은 난수 상태로 다시 섞어 드로우 더미로 옮긴다. 패가 10장이면 카드를
소모하지 않고 `HandFull`을 반환한다. Editor 도구의 `Validate Battle Deck Draw`로
초기 패, 첫 턴 예외, 패 상한, 묘지 재구성과 동일 시드 재현성을 검사할 수 있다.

`BattleCardPlayState`는 카드 사용의 미리보기와 최종 확정을 분리한다. 확정 직전에 패,
마력, 필드 공간과 같은 이름 결계 중복을 다시 검사하며 실패하면 마력과 카드 위치를
변경하지 않는다. 몬스터는 몬스터존에 소환되고, 스킬은 스킬존을 임시 점유한 뒤 묘지로
이동하며, 트랩과 결계는 스킬존에 유지된다. 임시 스킬이 묘지로 이동하려 하면 소멸
영역으로 대체된다. `BattleManaState`는 기본 최대 마력 5, 시스템 상한 10, 플레이어 턴
시작 충전과 턴 종료 제거를 관리한다.
