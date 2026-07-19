# 18단계: E01 따뜻한 좌석 전투 효과

## 구현 범위

- 활성 상태로 장착된 E01은 몬스터 전투 상태 생성 시 최대 생명력과 현재 생명력을 2 증가시킨다.
- E01이 비활성화되거나 제거되면 최대 생명력 보너스를 제거하고 현재 생명력을 새 상한으로 정리한다.
- E01이 다시 활성화되면 최대 생명력과 현재 생명력을 다시 2 증가시킨다.
- 같은 상태를 반복 적용해도 보너스가 중복 누적되지 않는다.
- 다른 카드의 인첸트 상태를 잘못 전달하면 전투 상태를 변경하지 않는다.
- C01~C12와 E01~E08 원본 데이터는 변경하지 않는다.

## 프로젝트에 적용

ZIP 안의 `Assets` 폴더를 Unity 프로젝트 최상위 폴더에 복사하고 파일 교체를 허용한다.
기존 `BattleMonsterState.cs`와 `BattleMonsterRegistry.cs`는 최신 18단계 파일로 교체된다.

## Unity에서 확인

1. Unity로 돌아가 스크립트 컴파일이 끝날 때까지 기다린다.
2. Console에 빨간 컴파일 오류가 없는지 확인한다.
3. 상단 메뉴 `Have a Break > Validate E01 Warm Seat Battle Effect`를 누른다.
4. `E01 Warm Seat battle effect passed.`가 표시되는지 확인한다.

## GitHub Desktop Summary

`E01 따뜻한 좌석 전투 효과 연결`
