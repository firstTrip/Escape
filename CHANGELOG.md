# CHANGELOG

## [Unreleased]

### Added
- `00_Prologue.unity` — 프롤로그와 0막(집, 1차 방문)을 담은 신규 씬.
- 상호작용 공용 베이스 `InteractableHotspot`, 핫스팟 스크립트 8종
  (Door, ExamineHotspot, PhoneHotspot, PickupHotspot, LockCodePuzzleController,
  PhotoCombinePuzzleController, RecordShelfHotspot, TurntableHotspot).
- 인벤토리 시스템(`InventoryManager`, `InventoryItemSO`, `InventoryUI`)과 아이템
  에셋 7종(여벌 열쇠, 라이터, 사진 조각 3장, 조합된 사진, 음반).
- 공용 UI: `SubtitleUI`, `ExaminePanelUI`, `FadeScreen`.
- 챕터 진행 로직: `PrologueSequence`, `Act0HouseController`, `ChapterTransitionStub`.
- 러스티 레이크풍 배경/아이콘 아트, 절차적 효과음/모티프 A, Noto Sans KR 폰트.

### Changed
- `CursorController`를 `Physics.Raycast` 기반에서 uGUI 포인터 이벤트 기반으로 재작성.
- `Door`를 `InteractableHotspot` 상속 구조로 변경.

### Known limitations
- 다이너(1번 공간) 이후 챕터는 이번 범위에 포함되지 않음.
- 아트/오디오는 전부 자리표시용(placeholder) 절차적 생성물.
