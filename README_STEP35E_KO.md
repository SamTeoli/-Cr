# Step 35E - C05/C06 스킬 효과 런타임 연결

기존 확정 카드 데이터와 수치를 변경하지 않고 다음 연결을 추가합니다.

- C05 카드 플레이 사건에서 대상 적 이동, 약화, 조건부 취약 처리
- C06 카드 플레이 사건에서 대상 적 속박·약화와 5레벨 보조 대상 처리
- 살아 있는 적의 현재 공격력 스냅샷을 C06 처리기에 전달
- 같은 카드 플레이 사건의 효과가 두 번 처리되지 않는지 검증

## 적용

ZIP 안의 `Assets` 폴더를 Unity 프로젝트 루트의 `Assets` 폴더에 병합합니다.
기존 파일 삭제는 필요하지 않습니다.

## 검증

Unity 컴파일 완료 후 Console 오류가 0개인지 확인하고 다음 메뉴를 실행합니다.

`Have a Break > Validate Runtime C05 C06 Skill Effects`

성공 문구:

`Battle runtime C05 and C06 skill effects passed.`

## GitHub Desktop Summary

`C05 C06 스킬 효과 런타임 연결`
