# Step 35G - C08/C09/C10 트랩 효과 런타임 연결

기존 확정 카드 데이터와 수치를 변경하지 않고 다음 연결을 추가합니다.

- 트랩 설치 시 설치 카드와 활성화 가능한 적 턴을 기록
- C08: 적 이동 시 이동 수치를 0으로 교체하고 속박·약화 적용
- C09: 적 공격 선언 후 피해 전 방어를 적용하고 방어 유지 상태 기록
- C10: 아군 측에 영향을 주는 적 능력을 취소하고 약화 및 카드 이동 처리
- 모든 트랩 효과가 적 턴에만 반응하는지 검증

## 적용

ZIP 안의 `Assets` 폴더를 Unity 프로젝트 루트의 `Assets` 폴더에 병합합니다.
기존 파일 삭제는 필요하지 않습니다.

## 검증

Unity 컴파일 완료 후 Console 오류가 0개인지 확인하고 다음 메뉴를 실행합니다.

`Have a Break > Validate Runtime C08 C09 C10 Trap Effects`

성공 문구:

`Battle runtime C08, C09, and C10 trap effects passed.`

## GitHub Desktop Summary

`C08 C09 C10 트랩 효과 런타임 연결`
