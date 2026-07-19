# 28단계: C01 막차의 침낭지기 소환 효과

## 확정 규칙

- 소환될 때 선언된 적 1개를 왼쪽으로 1칸 이동한다.
- 이동은 27단계의 연쇄 밀기와 끝 위치 순환 규칙을 사용한다.
- 고정 적 때문에 이동이 실패하면 레벨 1~3은 방어 2를 얻는다.
- 레벨 4~5는 고정 실패 시 방어 3을 얻는다.
- 레벨 5는 이동 성공 시에도 방어 1을 얻는다.
- 대상 상실과 E08 위치가 빈 경우는 고정 실패가 아니므로 방어를 얻지 않는다.
- E08 장착 시 선언 당시 적 ID 대신 선언 위치의 현재 적에게 해결한다.

## 사건 기록

- 실제 이동한 최초 대상과 밀린 적 각각을 `EnemyMoved` 사건으로 기록한다.
- 이동 전후 위치는 사건의 `BeforeValue`와 `AfterValue`에 위치 enum 값으로 저장한다.
- 방어 획득은 적용 전후 수치를 `StatusApplied` 사건으로 기록한다.
- 같은 소환 사건의 C01 효과는 한 번만 처리한다.

C01의 이름, 비용, 공격력, 생명력, 레벨과 효과 문구는 변경하지 않는다.

## 프로젝트에 적용

ZIP 안의 `Assets` 폴더를 Unity 프로젝트 최상위 폴더에 복사하고 파일 교체를 허용한다.
`CardEnums.cs`와 `BattleMonsterState.cs`의 기존 Unity GUID는 유지된다.

## Unity에서 확인

1. Unity 컴파일이 끝날 때까지 기다린다.
2. Console에 빨간 컴파일 오류가 없는지 확인한다.
3. `Have a Break > Validate C01 Sleeper Keeper Summon Effect`를 누른다.
4. `C01 Sleeper Keeper summon effect passed.`가 표시되는지 확인한다.

## GitHub Desktop Summary

`C01 막차의 침낭지기 소환 효과 연결`
