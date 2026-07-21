# 런타임 통합 프로토타입

Unity에서 `Assets/Scenes/SampleScene`을 연 뒤 Play를 누르면 런타임 통합 화면이
자동으로 열립니다. 씬에 별도 오브젝트를 배치할 필요는 없습니다.

연결된 범위:

- 새 런과 이어하기, 수동 저장
- 12노드 선택과 막 진행
- 일반·엘리트·중간보스·보스 전투
- 카드 사용, 대상 선택, 몬스터 공격, 턴 종료
- 전투 중 소모아이템
- 상점, 리롤, 상황 이벤트, 회복과 카드 강화
- 전투 정산, 골드·인첸트·소모아이템 보상
- 런 완료와 패배

런타임 데이터 연결은
`Assets/Resources/GameData/RuntimePrototypeConfig.asset`에서 관리합니다.
검증은 `Have a Break > Validate Integrated Run Prototype` 또는 전체 테스트
하네스에서 실행할 수 있습니다.
