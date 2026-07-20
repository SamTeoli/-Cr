# Step 35D - C03/C04 턴 반응 효과 런타임 연결

이번 단계는 기존 확정 카드 데이터와 수치를 변경하지 않고 다음 연결만 추가합니다.

- 플레이어 턴 종료 직전에 필드의 C03 효과를 처리한 뒤 적 턴으로 전환
- 적 턴의 `EnemyMoved` 사건을 필드의 C04 효과에 전달
- 하나의 이동 명령으로 여러 적이 이동해도 C04가 중복 발동하지 않는지 검증

## 적용

ZIP 안의 `Assets` 폴더를 Unity 프로젝트 루트의 `Assets` 폴더에 병합합니다.
기존 파일 삭제는 필요하지 않습니다.

## 검증

Unity 컴파일이 끝나고 Console 오류가 0개인지 확인한 뒤 다음 메뉴를 실행합니다.

`Have a Break > Validate Runtime C03 C04 Turn Effects`

성공 문구:

`Battle runtime C03 and C04 turn effects passed.`

## GitHub Desktop Summary

`C03 C04 턴 반응 효과 런타임 연결`
