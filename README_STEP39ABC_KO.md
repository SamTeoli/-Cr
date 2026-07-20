# STEP 39A~39C: 적 자동·연속 공격

## 구현 범위

- 39A: 플레이어 필드에 몬스터가 없을 때 적이 플레이어를 직접 공격합니다.
- 39B: 기존 위치 기반 대상 선택 결과에 따라 몬스터 공격 또는 플레이어 직접 공격을 자동 실행합니다.
- 39C: 외부에서 전달받은 공격 횟수와 동률 판정값으로 연속공격을 실행하며, 매 공격 직전에 대상을 다시 찾습니다.

카드 C01~C12와 인챈트 E01~E08 데이터는 변경하지 않았습니다. 적의 공격 횟수와 동률 판정값도 서비스 외부에서 전달받습니다.

## Unity 검증

1. GitHub Desktop에서 `agent/step39abc-enemy-automatic-attacks` 브랜치를 가져옵니다.
2. Unity가 스크립트 컴파일을 마칠 때까지 기다립니다.
3. `Have a Break > Validate Full Battle Runtime C01-C12`를 실행합니다.
4. 다음 문구가 나오면 성공입니다.

`Full battle runtime regression C01-C12, enemy targeting, and repeated attacks passed.`

오류가 나오면 커밋하지 말고 Console 전체 화면을 보내 주세요.
