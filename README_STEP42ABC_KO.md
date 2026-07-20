# STEP 42A~42C: 다중 라운드 전투 세션

## 구현 범위

- 전투 시작과 시작 손패 확정을 세션 서비스가 처리합니다.
- 각 플레이어 턴의 이벤트 시작 위치를 자동으로 보관합니다.
- 완전한 전투 라운드를 여러 번 연속 실행합니다.
- 승리·패배가 확정되면 추가 라운드 실행을 차단합니다.
- 시작 전 라운드 실행과 중복 전투 시작을 거부합니다.
- 적 명령과 수치는 외부 입력을 그대로 사용합니다.
- 카드 C01~C12와 인챈트 E01~E08 데이터는 변경하지 않았습니다.

## Unity 검증

1. `agent/step42abc-multi-round-session` 브랜치로 전환합니다.
2. Unity 컴파일과 Console 오류 0개를 확인합니다.
3. `Have a Break > Validate Full Battle Runtime C01-C12`를 실행합니다.
4. 아래 문구가 나오면 성공입니다.

`Full battle runtime regression C01-C12, multi-round sessions, and terminal locking passed.`
