# 36F — 전체 런타임 회귀검증 갱신

## 변경 내용

기존 C01~C12 전체 런타임 검증에 다음 적 턴 통합 흐름을 추가했습니다.

- 적 이동과 C08 이동 대체
- 적 이동 뒤 C04·C12 반응
- 적 공격 선언과 C09 자동 발동
- 방어 소모, 몬스터 피해, 초과 피해의 플레이어 전가
- 약화가 적용된 적 공격과 방어 우선 소모
- C10 레벨 5 단일 대상 능력 취소와 패 복귀
- C10 레벨 3 광역 능력 제한
- C10 레벨 4 광역 능력 취소와 묘지 이동

## Unity 검증

`Have a Break > Validate Full Battle Runtime C01-C12`

성공 메시지:

`Full battle runtime regression C01-C12 and enemy flow passed.`

Console에는 각 하위 검증의 성공 로그가 개별적으로 출력됩니다.
