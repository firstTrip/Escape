# 개발 로그

## 2026-08-24 — 프롤로그 + 0막(집, 1차 방문) 구현

- `00_Prologue.unity` 신규 씬: 집 허브 배경 + 13개 핫스팟(현관/전화/냉장고/싱크대/식탁/
  편지꽂이 자물쇠/손목시계/화장대 서랍/소파/음반 선반/사진 조합 서랍/턴테이블/성냥갑).
- 프롤로그 연출(`PrologueSequence`): 암전 → 페이드인 → 전화벨 → 모티프 A 허밍 자막 →
  탐험 가능 상태 전환. 전화 오브젝트를 다시 클릭하면 음성메시지를 재생할 수 있다.
- 퍼즐 3종 구현: 313 자물쇠(`LockCodePuzzleController`), 사진 3장 조합
  (`PhotoCombinePuzzleController`), 음반 재생(`RecordShelfHotspot` + `TurntableHotspot`).
- 인벤토리 시스템(`InventoryManager` / `InventoryUI`), 자막(`SubtitleUI`), 확대 보기
  팝업(`ExaminePanelUI`), 화면 페이드(`FadeScreen`) 등 공용 UI 시스템 추가.
- 기존 `CursorController`가 사용하던 `Physics.Raycast` 기반 초안(2D 콜라이더와 맞지 않아
  동작하지 않던 상태)을 제거하고, 씬에 이미 구성돼 있던 uGUI 이벤트 시스템을 그대로
  활용하는 방식으로 재작성.
- 러스티 레이크풍 플랫 일러스트 배경/아이콘, 절차적 효과음(전화벨/빗소리/자물쇠/모티프 A
  4음 허밍)을 자리표시용으로 생성해 채워 넣음. 한글 UI 텍스트를 위해 Noto Sans KR 폰트 추가.
- 다이너(1번 공간) 이후는 이번 범위 밖. 성냥갑 오브젝트는 세 퍼즐을 모두 풀면 "다음
  업데이트에서 계속됩니다" 안내만 표시하는 스텁으로 남겨둠.
