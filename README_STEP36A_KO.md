# Step 36A - 전투 런타임 트랩 설치 등록부

이 단계는 C08~C10 트랩의 설치 정보를 `BattleRuntimeState` 안에 보관합니다. 이후 적 턴 이동·공격·능력 처리기가 현재 설치된 트랩을 직접 찾아 발동시키기 위한 기반입니다.

## 적용 방법

1. Unity를 종료합니다.
2. ZIP을 Unity 프로젝트 루트에 압축 해제합니다.
3. `Assets` 폴더 병합과 기존 C# 파일 덮어쓰기를 허용합니다.
4. Unity를 열고 컴파일이 끝날 때까지 기다립니다.
5. `Have a Break > Validate Runtime Trap Installation Registry`를 실행합니다.

성공 메시지:

`Battle runtime trap installation registry passed.`

## 검증 범위

- 설치된 트랩이 런타임 등록부에 저장되는지 검사
- 같은 전투카드ID의 중복 설치 등록 차단
- 스킬필드를 벗어난 트랩의 비활성 등록 정리
- C10이 패로 돌아왔을 때 설치 등록도 제거
- 기존 C01~C12 카드 데이터와 확정 효과 내용 변경 없음

## GitHub Desktop Summary

`전투 런타임 트랩 설치 등록부 추가`
