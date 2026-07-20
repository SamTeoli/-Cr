# Step 35H - C04/C12 이동 반응 런타임 통합

기존 확정 카드 데이터와 수치를 변경하지 않고 다음 연결을 추가합니다.

- 모든 `EnemyMoved` 사건을 C04 몬스터와 C12 결계에 함께 전달
- C05처럼 플레이어 턴에 발생한 적 이동에도 반응
- 적 턴의 자연 이동에도 같은 통합 진입점 사용
- C04 공격 강화와 C12 취약·5레벨 직접 피해 처리
- 기존 `BattleRuntimeTurnEffectService.TryResolveEnemyMoved` 호출도 새 통합 서비스로 연결
- 동일 이동 사건의 중복 해결 방지

## 적용

ZIP 안의 `Assets` 폴더를 Unity 프로젝트 루트의 `Assets` 폴더에 병합합니다.
`BattleRuntimeTurnEffectService.cs`는 기존 파일을 덮어씁니다.
그 외 기존 파일 삭제는 필요하지 않습니다.

## 검증

Unity 컴파일 완료 후 Console 오류가 0개인지 확인하고 다음 메뉴를 실행합니다.

`Have a Break > Validate Runtime C04 C12 Movement Reactions`

성공 문구:

`Battle runtime C04 and C12 movement reactions passed.`

## GitHub Desktop Summary

`C04 C12 이동 반응 런타임 통합`
