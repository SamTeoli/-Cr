# Step 35F - C07/C11 효과 런타임 연결

기존 확정 카드 데이터와 수치를 변경하지 않고 다음 연결을 추가합니다.

- C07 플레이 사건 이후 드로우, 선택 카드 소멸, 5레벨 방어 효과 처리
- 동일한 C07 플레이 사건의 중복 해결 방지
- C11을 결계로 스킬존에 유지
- 적 턴 완료와 새 플레이어 턴 시작 후 필드의 C11 자동 처리
- 기본 턴 드로우 이후 C11 추가 드로우 및 5레벨 방어 효과 확인

## 적용

ZIP 안의 `Assets` 폴더를 Unity 프로젝트 루트의 `Assets` 폴더에 병합합니다.
기존 파일 삭제는 필요하지 않습니다.

## 검증

Unity 컴파일 완료 후 Console 오류가 0개인지 확인하고 다음 메뉴를 실행합니다.

`Have a Break > Validate Runtime C07 C11 Effects`

성공 문구:

`Battle runtime C07 and C11 effects passed.`

## GitHub Desktop Summary

`C07 C11 효과 런타임 연결`
