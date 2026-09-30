# Channel Zero 런타임 감사 및 기반 설계

- 기준일: 2026-09-22
- 기준 문서: `ChannelZero_PlanningDocumentIndex.md` 및 그 우선순위 1~7,
  클로즈업 기준은 `ChannelZero_CloseupResourceSpec.md`
- 제외 문서: `ChannelZero_ResourceAudit_20260922.md`는 과거 스냅샷으로만 취급
- 구현 범위: 프롤로그~제2장 확장 기반, 최초 수직 슬라이스는 `2001 현관 → 거실`

## 1. 읽기 전용 감사 결과

### 확인된 현재 상태

- Unity 버전은 `6000.3.17f1`, Input System과 uGUI가 설치되어 있다.
- 작업 트리는 이미 다수의 사용자 변경을 포함한다. 기존 파일은 삭제하거나 되돌리지 않는다.
- `Assets/Scenes/00_Prologue.unity`는 TheTableWeShared 서사와 퍼즐, 개별 방/가구 레이어를 포함한 과거 구조다.
- `Assets/Scenes/ChannelZero_LivingRoom2001.unity`는 `Canvas + GraphicRaycaster + Image + IPointerClickHandler` 방식의 UI 핫스팟 검증 씬이다.
- `Assets/Resources/ChannelZero/PlayRooms/`에 현관 2001, 거실 4개 연도, 작업실 4개 연도의 완성 배경이 존재한다.
- 기존 `ChannelZeroHotspot`은 Logical ID와 UnityEvent를 제공하지만 방/연도/상태 전환과 저장 모델에는 연결되어 있지 않다.
- 기존 `RoomNavigationController`는 배열 인덱스와 TheTableWeShared PlayerPrefs 키에 결합되어 Channel Zero의 Logical ID·시대 상태에는 적합하지 않다.
- 기존 `InventoryManager`는 ScriptableObject 참조 목록만 런타임에 유지하며 버전 저장, HOLD, 시대 간 보존 규칙이 없다.

### 최신 기획과의 차이

1. 현관에서 거실로 이어지는 Channel Zero 전용 플레이 흐름이 없다.
2. 1961/1981/2001/2021을 하나의 방 상태 모델로 전환하는 런타임이 없다.
3. `REC/PLAY/LOAD/REW/HOLD`의 상태와 연결 지점이 없다.
4. Logical ID 핫스팟이 현재 방·시대·퍼즐 상태에 따라 활성화되는 구조가 없다.
5. 인벤토리, HOLD 아이템, 퍼즐 상태, 조사 기록을 함께 저장/복구하는 버전 DTO가 없다.
6. 작업실 확대 퍼즐을 별도 패널로 여는 공통 진입점이 없다.
7. `사라지는 계단`을 제2장에서는 예고만 하고 제4장 이전 개방을 차단하는 명시적 상태 가드가 없다.
8. 기존 검증 씬은 디버그 박스가 기본 표시되고, 2001 단일 배경만 사용하며 실제 게임 수직 슬라이스가 아니다.

## 2. 책임 분리

```text
ChannelZeroSessionState (순수 C# 런타임 상태)
  ├─ 현재 장/방/연도/방 배경 상태
  ├─ REC/PLAY/LOAD/REW/HOLD 선택 상태
  ├─ 방문 방, 조사 Logical ID, 인벤토리, HOLD 아이템
  ├─ 퍼즐별 문자열 상태
  └─ 사라지는 계단 상태와 장 진행 가드

ChannelZeroSaveService
  └─ 버전 DTO JSON 저장/복구. 화면 오브젝트 참조를 저장하지 않음

ChannelZeroRoomCatalog (ScriptableObject, 설정 데이터)
  ├─ Room ID + 연도 + 상태 → 완성 배경 Sprite
  └─ 런타임에서 변경하지 않는 제작 설정

ChannelZeroRoomPresenter (화면 표현)
  ├─ RoomBackground.sprite 전체 교체
  ├─ 현재 방/연도에 맞는 UI 핫스팟 활성화
  └─ 물리 Raycast/Collider를 사용하지 않음

ChannelZeroHotspot (입력 어댑터)
  └─ 투명 Image raycast + IPointerClickHandler + Logical ID 이벤트

ChannelZeroVerticalSliceController (유스케이스 조정)
  ├─ 2001 현관 → 거실 이동
  ├─ 거실 연도 전환과 배경 교체
  ├─ 핫스팟 조사 및 작업실 잠금 피드백
  ├─ 저장/복구 호출
  └─ 오디오·연출·확대 패널이 구독할 이벤트 발행

ChannelZeroCloseupCanvasController (정밀 퍼즐 표현)
  ├─ Artwork / Interaction / Feedback / ExactText 책임 분리
  ├─ InventoryStrip / CloseButton 공통 제공
  └─ 진입 당시 방·연도·배경 상태를 저장하고 정확히 복귀
```

## 3. 폴더와 네임스페이스

```text
Assets/ChannelZero/
  Runtime/
    Core/           ChannelZero.Runtime.Core
    Presentation/   ChannelZero.Runtime.Presentation
  Tests/EditMode/   ChannelZero.Tests.EditMode
Assets/Editor/
  ChannelZeroVerticalSliceSceneCreator.cs
Assets/Scenes/
  ChannelZero_VerticalSlice.unity
```

기존 전역 네임스페이스 코드와 충돌하지 않도록 신규 코드는 `ChannelZero.Runtime.*`에 둔다. 과거 시스템을 즉시 마이그레이션하지 않고 새 수직 슬라이스에서만 새 모듈을 사용한다.

## 4. 상태 전환 규칙

- 시작: `Prologue / Entry / 2001 / default`.
- `Entry_LivingDoor`: `Chapter1 / LivingRoom / 2001 / default`로 이동한다.
- 거실에서는 네 연도 전환이 가능하며 `RoomBackground` 한 장만 교체한다.
- 핫스팟은 현재 Room ID와 허용 연도 조건을 모두 만족할 때만 raycast를 받는다.
- 작업실 문은 제1장 완료 플래그 전까지 열리지 않는다. 기반 코드에는 전환 지점만 둔다.
- 작업실 확대 퍼즐은 CloseupCanvas로 열며 방 배경을 개별 가구 스프라이트로 조립하지 않는다.
- 배선도는 `WKS-Z04`, 수리 기록장은 `WKS-Z05`, 열쇠 절삭기는 `WKS-Z06`을 사용한다.
- 접이식 크랭크는 확대 퍼즐이 아닌 인벤토리 조사 리소스 `WKS-I01`로 관리한다.
- `사라지는 계단`은 `Hidden → Foreshadowed → LocksKnown → ReadyForLaterChapter → Open`으로 모델링한다.
- `Open` 전환은 제4장 이상이며 도면, 순서, 크랭크 HOLD 조건이 모두 충족될 때만 허용한다. 제2장에서는 최대 `Foreshadowed` 또는 `LocksKnown`이다.
- `REW`는 향후 방의 물리 퍼즐 상태를 초기화하되 조사 기록은 유지하는 별도 유스케이스로 확장한다.

## 5. 최소 구현 순서

1. 순수 상태 모델과 저장 DTO, `사라지는 계단` 장 가드.
2. 완성 배경 카탈로그와 단일 `RoomBackground` 표현기.
3. UI Logical ID 핫스팟과 현재 방/연도 필터.
4. 2001 현관에서 거실 이동.
5. 거실 1961/1981/2001/2021 배경 교체.
6. 클릭 상태 표시와 오디오·연출 이벤트 연결 지점.
7. 공통 CloseupCanvas와 `PRO-Z01` 임시 기능 검증.
8. 다음 단계에서 작업실 4시대와 도면/크랭크를 연결하되 지하 계단은 열지 않는다.

## 6. 검증 구분

| 항목 | 완료 판정 조건 |
|---|---|
| 정적 감사 | 파일·씬 YAML·리소스·Git 상태를 읽어 근거 기록 |
| 컴파일 | Unity batchmode 또는 Editor 콘솔에서 신규 컴파일 오류 0 확인 |
| EditMode 테스트 | 상태 전환, 저장 왕복, 제2장 계단 개방 차단 테스트 통과 |
| 씬 정적 검증 | 단일 배경, GraphicRaycaster, UI 핫스팟, 물리 Collider 0 검사 |
| Play Mode 육안 검증 | 실제 실행 화면에서 현관→거실, 연도 버튼, 클릭 피드백을 사람이 확인 |
| Windows 빌드 | 별도 빌드 실행 및 산출물 확인 |

정적 생성이나 batchmode 검증은 Play Mode 육안 검증 또는 Windows 빌드 완료를 의미하지 않는다.

## 7. 2026-09-22 구현 및 검증 기록

### 구현됨

- 기존 씬과 과거 코드에 의존하지 않는 `ChannelZero.Runtime` 어셈블리.
- 버전 1 세션 상태와 JSON 저장/복구.
- Logical ID 기반 투명 UI 핫스팟.
- 단일 `RoomBackground`의 전체 Sprite 교체.
- `2001 현관 → 2001 거실` 이동.
- 거실 1961/1981/2001/2021 배경 전환.
- `REC/PLAY/LOAD/REW/HOLD` 선택 상태 및 오디오·연출용 이벤트 연결 지점.
- `Artwork / InteractionLayer / FeedbackLayer / ExactTextLayer / InventoryStrip / CloseButton` 공통 CloseupCanvas.
- `PRO-Z01` 요청서의 정확한 한글 문구를 TMP로 합성하고 읽음 기록 및 원래 현관 상태 복귀.
- 제2장에서는 `사라지는 계단`을 열 수 없는 상태 가드.
- 생성 씬: `Assets/Scenes/ChannelZero_VerticalSlice.unity`.

### 확인 결과

| 구분 | 결과 | 근거 |
|---|---|---|
| Unity 컴파일 | 통과 | `Logs/ChannelZeroVerticalSliceBuild.log`, batchmode return code 0 |
| 씬 정적 검증 | 통과 | RoomBackground 1, UI hotspot 17, Collider/Collider2D 0 |
| EditMode 테스트 | 4/4 통과 | `Logs/ChannelZeroEditModeResults.xml` |
| 자동 PlayMode 테스트 | 1/1 통과 | 현관문 pointer click → 거실, 2001 → 1961 배경 Sprite 교체, Collider 0 |
| Play Mode 육안 검증 | 미수행 | 자동 테스트는 화면의 미적 배치·가독성을 사람이 확인한 증거가 아님 |
| Windows 빌드 | 미수행 | 현재 최소 기반 범위에서 빌드 산출물 생성 안 함 |

배치 실행 초기에 Unity 라이선스 IPC 재접속 경고가 있었으나 이후 entitlement가 정상 확인되었고, 컴파일·씬 생성·두 테스트 러너는 모두 정상 종료했다.

### 클로즈업 명세 반영 후 추가 상태

- `ChannelZero_CloseupResourceSpec.md`의 전체 ID를 `ChannelZeroIds`에 고정했다.
- `WKS-Z04=배선도`, `WKS-Z05=수리 기록장`, `WKS-Z06=열쇠 절삭기`, `WKS-I01=접이식 크랭크 조사`로 충돌을 해소했다.
- 클로즈업 진입 원본 방·연도·배경 상태, 활성 클로즈업/상태, 입력 잠금, 읽은 기록을 저장 DTO에 추가했다.
- `REW`용 물리 퍼즐 체크포인트 복구가 읽은 기록을 지우지 않도록 분리했다.
- `CloseupCanvas` 생성기에 DimmedBackdrop, Artwork, InteractionLayer, FeedbackLayer, ExactTextLayer(TMP), InventoryStrip, CloseButton 구조를 추가했다.
- 방 이동 기록에 이전 방·장·연도·배경 상태를 저장하고, 현관을 떠난 뒤 표시되는 공통 `뒤로` 버튼으로 정확히 복귀하도록 추가했다. 최초 현관에서는 버튼을 숨긴다.
- 최종 클로즈업 아트가 아직 없으므로 `PRO-Z01`은 임시 컬러 패널과 TMP 정확 문구로만 기능 검증하도록 구성했다. 최종 아트 완료로 간주하지 않는다.
- C# 프로젝트 빌드는 Runtime, Editor, EditMode Tests, PlayMode Tests 모두 경고 0/오류 0이다.
- Unity Editor가 프로젝트를 열고 있어 별도 batchmode 씬 재생성 및 갱신된 테스트 실행은 충돌 방지를 위해 수행하지 않았다. 현재 열린 Editor에서 `Tools > Channel Zero > Create Vertical Slice Scene`을 실행한 뒤 갱신된 Unity 테스트를 다시 확인해야 한다.

### 다음 구현 순서

1. Unity Editor에서 수직 슬라이스를 육안 재생해 16:9 크롭, UI 겹침, 실제 클릭 가독성을 확인한다.
2. 제1장 완료 조건과 작업실 잠금 해제를 상태 모델에 연결한다.
3. 작업실 4시대 배경과 공통 핫스팟을 같은 카탈로그에 추가한다.
4. `WKS-Z03` 도면, `WKS-Z04` 배선도, `WKS-Z05` 기록장, `WKS-Z06` 절삭기 클로즈업을 연결한다.
5. `WKS-I01` 크랭크 인벤토리 조사와 획득/HOLD를 연결한다.
6. 도면 확인 시 계단 상태를 `LocksKnown`까지만 올리고 제2장에서는 개방하지 않는 통합 테스트를 추가한다.
