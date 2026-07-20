# 37C — 적 턴 계획 전체 회귀검증 편입

## 변경 내용

기존 C01~C12 전체 런타임 회귀검증에 다음 두 검증을 추가했습니다.

- 37A 적 턴 지휘 시스템
- 37B 적 행동 계획 스냅샷과 표시용 의도 데이터

전체 회귀검증 한 번으로 카드 효과, 적 이동·공격·능력 흐름, 적 턴의 순차 실행,
행동 계획 검증과 기존 실행 계층 연결을 함께 확인합니다.

카드 데이터, 카드 효과, 적 수치와 행동 선택 규칙은 변경하지 않습니다.

## Unity 검증

`Have a Break > Validate Full Battle Runtime C01-C12`

성공 메시지:

`Full battle runtime regression C01-C12, enemy flow, and planning passed.`

Console에는 기존 하위 검증과 다음 항목이 각각 출력됩니다.

- `Enemy turn orchestration`
- `Enemy turn planning and intents`
