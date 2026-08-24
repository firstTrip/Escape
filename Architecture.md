# 아키텍처

## 개요

`00_Prologue.unity` 하나의 씬 안에서 프롤로그와 0막(집, 1차 방문)이 이어서 진행된다.
집 전체가 하나의 배경 이미지(`Assets/Resources/Hub/bg_hub_room.png`) 위에 투명한
UI 클릭 영역(핫스팟)을 겹쳐 놓는 방식으로 구현되어 있다. 카메라/3D 콜라이더 기반이
아니라 uGUI(Canvas + GraphicRaycaster + EventSystem)의 포인터 이벤트를 그대로 사용한다.

## 폴더 구조

```
Assets/
  Scenes/00_Prologue.unity      프롤로그 + 0막(집 1차 방문) 씬
  Script/
    Core/                       GameManager, CursorController
    Gameplay/
      Cursor/IHoverable.cs
      Interaction/              InteractableHotspot(공용 베이스), Door, ExamineHotspot, PhoneHotspot
      Inventory/                InventoryManager, InventorySlotUI, InventoryUI
      Item/                     PickupHotspot
      Puzzle/                   LockCodePuzzleController, PhotoCombinePuzzleController,
                                 RecordShelfHotspot, TurntableHotspot
    Chapter/                    PrologueSequence, Act0HouseController, ChapterTransitionStub
    SO/                         InventoryItemSO
    UI/                         SubtitleUI, ExaminePanelUI, FadeScreen
  Data/Items/*.asset            인벤토리 아이템(ScriptableObject) 인스턴스
  Resources/
    Hub/                        집 배경, 확대(examine) 아트, 아이콘
    Prologue/                   프롤로그 전용 클로즈업 배경
    UIStory/                    패널/버튼/커서 등 UI 크롬
  Audio/Sfx, Audio/Music        절차적으로 생성한 효과음 · 모티프 A
  Fonts/                        Noto Sans KR (한글 UI 텍스트용)
```

## 상호작용 시스템

- `InteractableHotspot`는 `IPointerClickHandler / IPointerEnterHandler / IPointerExitHandler`를
  구현하는 공용 베이스 클래스다. 모든 핫스팟(문, 사진, 자물쇠, 턴테이블 등)은 이 클래스를
  상속해 `OnInteract()`만 오버라이드한다.
- `CursorController`는 호버 상태에 따라 커서 스프라이트만 교체하는 싱글턴이다. 과거
  `Physics.Raycast` 기반 초안은 Screen Space Overlay 캔버스와 맞지 않아 제거하고,
  이미 씬에 존재하던 `GraphicRaycaster + EventSystem` 조합을 그대로 활용하도록 재작성했다.

## 퍼즐 3종 (0막 1차 방문)

| 퍼즐 | 스크립트 | 트리거 오브젝트 |
|---|---|---|
| 313 자물쇠 | `LockCodePuzzleController` | 식탁 위 편지꽂이 |
| 사진 3장 조합 | `PhotoCombinePuzzleController` | 거실 선반 서랍 |
| 음반 재생 | `RecordShelfHotspot` + `TurntableHotspot` | 거실 선반 음반 / 턴테이블 |

`Act0HouseController`가 세 퍼즐의 `IsSolved`를 취합해 `AllSolved`를 노출하고,
냉장고 위 성냥갑(`ChapterTransitionStub`)이 이를 확인해 다음 챕터(다이너)로 넘어갈 수
있는지 판단한다. 이번 빌드 범위(프롤로그 + 0막 1차 방문)에는 다이너 씬이 없으므로,
조건을 만족하면 "다음 업데이트에서 계속됩니다" 안내만 표시한다.

## 아트 · 오디오

이번 범위의 배경/아이콘/효과음은 전부 절차적으로 생성한 자리표시용(placeholder) 리소스다
(PIL로 그린 러스티 레이크풍 플랫 일러스트, 파이썬으로 합성한 사인파 기반 효과음).
추후 실제 원화·레코딩으로 교체하기 쉽도록 파일명과 폴더 구조만 최종 형태에 맞춰 두었다.
