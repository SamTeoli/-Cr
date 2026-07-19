# 24단계: E03 구겨진 왕복 승차권 본 효과 완료 드로우

## 구현 범위

- 기존 enum 직렬화 순서를 보존하면서 `MainEffectCompleted` 사건을 추가한다.
- 같은 부모 사건과 같은 카드에는 본 효과 완료 사건을 한 번만 생성한다.
- 활성 E03 장착 카드의 본 효과 완료 시 현재 마력이 0이면 카드 1장을 드로우한다.
- 장착 카드별 플레이어 턴당 1회만 발동한다.
- 같은 완료 사건은 다른 턴에 다시 처리할 수 없다.
- 드로우 성공 시 `DrawPile → Hand`의 `CardMoved` 사건을 기록한다.
- 패가 10장이면 드로우는 실패하고 덱 맨 위 카드와 순서를 그대로 유지한다.
- 패가 가득 찬 실패도 해당 완료 사건의 E03 발동을 소비한다.
- 현재 마력이 1 이상이면 발동하지 않는다.
- C01~C12와 E01~E08 원본 데이터는 변경하지 않는다.

## 프로젝트에 적용

ZIP 안의 `Assets` 폴더를 Unity 프로젝트 최상위 폴더에 복사하고 파일 교체를 허용한다.

## Unity에서 확인

1. Unity 컴파일이 끝날 때까지 기다린다.
2. Console에 빨간 컴파일 오류가 없는지 확인한다.
3. `Have a Break > Validate E03 Round Trip Ticket Main Effect Draw`를 누른다.
4. `E03 Round Trip Ticket main effect draw passed.`가 표시되는지 확인한다.

## GitHub Desktop Summary

`E03 구겨진 왕복 승차권 드로우 효과 연결`
