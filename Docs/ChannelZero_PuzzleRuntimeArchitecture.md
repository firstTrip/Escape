# Channel Zero Puzzle Runtime Architecture

## 목표

프롤로그부터 제2장까지의 퍼즐을 실제 저장 가능한 플레이 상태로 실행하면서,
제3장 이후 퍼즐이 방 컨트롤러의 `switch` 문을 비대하게 만들지 않도록 분리한다.

## 책임 분리

```text
ChannelZeroVerticalSliceController
  -> ChannelZeroGameContext (composition root)
      -> ChannelZeroInteractionRouter
          -> ChannelZeroInteractionDispatcher
              -> IChannelZeroInteractionHandler
      -> ChannelZeroOperationService
      -> ChannelZeroPuzzleService
      -> IChannelZeroPuzzleRegistry
          -> IChannelZeroPuzzle
              -> PuzzleView / PuzzleActionResult
  -> IChannelZeroPuzzleView
      -> ChannelZeroCloseupCanvasController
          -> IPuzzleActionView
              -> ChannelZeroPuzzleActionButton
  -> IChannelZeroInteractionSource
      -> ChannelZeroHotspot
```

- `IChannelZeroPuzzle`: 퍼즐 상태를 화면 모델로 만들고 액션을 처리하는 계약이다.
- `IChannelZeroPuzzleRegistry`: 클로즈업 ID와 퍼즐 구현을 연결한다.
- `ChannelZeroPuzzleService`: 씬과 무관한 퍼즐 실행 진입점이다.
- `PuzzleView`: 버튼, 설명, 아트 상태를 UI에 전달하는 읽기 모델이다.
- `PuzzleActionResult`: 상태 변화 후 화면과 내러티브 트리거를 반환한다.
- `ChannelZeroSessionState`: 퍼즐 문자열 상태, 플래그, 인벤토리, 읽은 기록을 저장한다.
- `ChannelZeroCloseupCanvasController`: 퍼즐 규칙을 모르며 `PuzzleView`만 렌더링한다.
- `IChannelZeroPuzzleView`: 클로즈업 열기, 상태 렌더링, 입력 잠금, 피드백 표시 계약이다.
- `IPuzzleActionView`: 버튼 구현과 액션 ID 전달을 분리한다.
- `IChannelZeroInteractionSource`: 방 오브젝트의 활성 조건과 클릭 이벤트를 통일한다.
- `ChannelZeroInteractionRouter`: JSON의 오브젝트 ID와 현재 모드에서 실행할 명령을 찾는다.
- `IChannelZeroInteractionHandler`: 클로즈업, 이동, 내러티브, 뒤로 가기 실행 계약이다.
- `ChannelZeroGameContext`: 저장·퍼즐·인벤토리·진행·시간·힌트 서비스를 조립하는 유일한 위치다.
- `ChannelZeroOperationService`: REC/PLAY/LOAD/REW/HOLD 명령 실행과 결과 문구를 담당한다.
- `PuzzleDefinitionCatalog`: `puzzles_ko-KR.v1.json`의 퍼즐·아이템 정의를 검증하고 인덱싱한다.
- `ChannelZeroInventoryService`: 아이템 선택, 표시명·설명, HOLD 가능 여부를 관리한다.
- `ChannelZeroRoomNavigationService`: 장 완료 조건에 따른 방 진입을 관리한다.
- `ChannelZeroChapterProgressionService`: 장 완료 조건을 한곳에서 계산한다.
- `ChannelZeroTimelineService`: REC/PLAY/LOAD/REW/HOLD와 물리 체크포인트를 관리한다.
- `ChannelZeroHintService`: 퍼즐 정의에 저장된 3단계 힌트 진행도를 저장한다.

## 현재 재사용 규칙

- `SequencePuzzle`: 2749, 8888처럼 순서가 있는 숫자·기호 입력.
- `LootPuzzle`: 의료함, 수리함, 부품 서랍처럼 열기와 복수 아이템 획득.
- `EraEvidencePuzzle`: 가족사진, 수리 기록장처럼 네 시대의 증거 수집.
- `InspectPuzzle`: 도면·배선도·후속 장치처럼 읽음 플래그를 남기는 조사.
- 전용 퍼즐: CRT 후면 수리, 손 치료, REC 합성, 진공관 시험기, 크랭크 HOLD.

## 새 퍼즐 추가 절차

1. 기존 규칙으로 표현 가능하면 해당 규칙 인스턴스를 레지스트리에 등록한다.
2. 새로운 입력 방식이면 `IChannelZeroPuzzle` 구현을 하나 추가한다.
3. 상태는 `ChannelZeroSessionState.SetPuzzleState`, 진행 조건은 `SetFlag`, 획득물은 `AddItem`을 사용한다.
4. UI는 `PuzzleView.actions`만 구성하고 직접 씬 오브젝트를 찾지 않는다.
5. 결과 문구가 필요하면 JSON 내러티브의 `trigger`를 `PuzzleActionResult.narrativeTrigger`로 반환한다.
6. 장 완료 조건은 `ChannelZeroProgression.Refresh`에서 명시적으로 합성한다.
7. EditMode에서 성공·실패·재시도·저장 상태를 검증하고 PlayMode에서 실제 버튼 흐름을 검증한다.

## 레거시 프롤로그 호환 계층

- `IPuzzle`은 `IInteractable`을 확장하고 `IsSolved`만 공통으로 노출한다.
- `NumericCodePuzzleBase`가 숫자 변경, 정답 검사, 패널 열기·닫기를 한 번만 구현한다.
- 라디오와 잠금상자는 성공·실패 결과만 오버라이드한다.
- `INumericPuzzleView`가 숫자 라벨과 버튼 이벤트를 담당한다.
- `IPuzzleFeedbackView`와 `IPuzzleDocumentView`가 자막 및 조사 패널 표시를 담당한다.
- 새 숫자 퍼즐은 베이스 클래스를 상속하고 기본 정답과 결과 처리만 작성한다.

## 데이터 원본

- `Assets/Resources/ChannelZero/Data/puzzles_ko-KR.v1.json`
- 퍼즐 규칙, 제목, 정답, 획득물, 상태 아트, 힌트는 데이터에 둔다.
- C#에는 `sequence`, `loot`, `era_evidence` 같은 재사용 규칙과 특수 규칙 구현만 둔다.
- 아이템 내부 ID와 한국어 표시명·설명을 분리하며 인벤토리는 선택 아이템을 저장한다.
- `Assets/Resources/ChannelZero/Data/interactions_ko-KR.v1.json`
- 오브젝트 라우팅, 해금 조건, 장 완료 조건은 interaction 데이터에 둔다.
- 새 오브젝트는 컨트롤러 `switch`를 수정하지 않고 route를 추가한다.

## 상태 지속성

- `Physical`: 방과 필요 시 시대 범위를 가지며 REC/LOAD/REW 대상이다.
- `Knowledge`: 문서·사진·관찰처럼 플레이어가 이미 안 사실이며 REW 뒤에도 유지한다.
- `Meta`: 힌트 진행도와 장 완료 계산값처럼 물리 역사 바깥의 상태다.
- 런타임은 상태 ID 문자열을 분석하지 않는다. 구버전 문자열 해석은 v2→v3 마이그레이션에서만 수행한다.

## 저장

- 현재 세이브 버전은 v3이며 `ISaveMigration`을 순서대로 적용한다.
- 실제 세이브는 `Application.persistentDataPath/Saves`에 임시 파일 작성 후 교체한다.
- 직전 정상 파일은 `.bak`으로 보존하고 주 파일 파싱 실패 시 복구한다.
- 기존 `PlayerPrefs` v1 세이브는 최초 로드 후 파일 저장으로 이전한다.

## 시간 명령

- `REC`: 현재 방·시대의 물리 퍼즐 상태를 스냅샷으로 저장한다.
- `PLAY`: 현재 방에서 가장 가까운 기록을 확인한다.
- `LOAD`: 기록된 물리 상태를 현재 방에 적용한다.
- `REW`: 물리 상태를 체크포인트로 되돌리되 문서·사진·힌트 지식은 유지한다.
- `HOLD`: 인벤토리에서 선택한 보존 가능 아이템을 시대 전환 대상으로 고정한다.

## 장 경계

- 제1장 완료: CRT 수리, 2749, 손 치료, 8888, MASTER 테이프.
- 제2장 완료: 주택 도면, 정상 진공관 판정, 네 시대 수리 기록, 접이식 크랭크.
- 제2장에서는 `사라지는 계단`을 열 수 없다. 도면과 크랭크는 후반 장의 입력으로만 저장한다.
- `REW`는 물리 퍼즐 상태를 되돌려도 읽은 문서와 `seenTextIds`는 유지한다.

## 씬 경계

- 실제 빌드 진입점은 `ChannelZero_VerticalSlice` 하나다.
- `00_Prologue`, `SampleScene`, `ChannelZero_LivingRoom2001`은 편집·호환 참고용으로 보존하지만 빌드에서는 비활성화한다.
- 새 장과 새 퍼즐은 레거시 `Assets/Script` 퍼즐 체계가 아니라 Channel Zero 런타임에만 추가한다.
