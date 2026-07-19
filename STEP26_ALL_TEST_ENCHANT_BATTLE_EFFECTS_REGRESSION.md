# 26단계: E01~E08 전투 효과 통합 회귀 검증

## 목적

시험 인첸트 데이터, 추가 호환 조건과 E01~E08 개별 전투 효과를 한 번에 다시 검사한다.
향후 카드 효과 실행기나 전투 UI를 연결할 때 기존 인첸트 규칙의 회귀를 빠르게 찾기 위한
Editor 전용 검증 진입점이다.

## 실행되는 검증

1. E01~E08 시험 인첸트 데이터와 `EnchantDatabase`
2. E01~E08 × C01~C12 추가 호환 행렬
3. E01 따뜻한 좌석 최대·현재 생명력
4. E02 닳은 손잡이 공격 완료 후 반격
5. E03 구겨진 왕복 승차권 마력 0 드로우
6. E04 예비 전원 비용 감소와 최소 1
7. E05 녹슨 안내방송 영향받은 적 약화
8. E06 별빛 각인 반복 효과 첫 수치 증가
9. E07 환승 도장 최초 묘지 이동 대체
10. E08 노선 고정핀 위치 대상 해결

각 검증은 독립 상태를 생성하며 카드·인첸트 에셋의 확정 내용을 수정하지 않는다. 하나의
검증에서 예외가 발생해도 나머지 검증을 계속 실행하고 Console에 실패 항목을 표시한다.

## 프로젝트에 적용

ZIP 안의 `Assets` 폴더를 Unity 프로젝트 최상위 폴더에 복사한다. 새 Editor 검증 파일만
추가되며 기존 파일은 교체하지 않는다.

## Unity에서 확인

1. Unity 컴파일이 끝날 때까지 기다린다.
2. Console에 빨간 컴파일 오류가 없는지 확인한다.
3. `Have a Break > Validate All Test Enchant Battle Effects E01-E08`을 누른다.
4. `All test enchant battle effects E01-E08 passed.`가 표시되는지 확인한다.
5. Console에 `All test enchant battle effects E01-E08 passed.` 로그가 있는지 확인한다.

## GitHub Desktop Summary

`E01~E08 전투 효과 통합 회귀 검증 추가`
